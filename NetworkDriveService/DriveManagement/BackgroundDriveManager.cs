using log4net;
using Microsoft.Win32;
using QuasaroDRV.DialogWindows;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;

namespace QuasaroDRV.DriveManagement
{
    public class BackgroundDriveManager
    {
        #region Constants
        const int CheckNetworkInterval = 10000; // interval to check for network connections after startup
        const int CheckNetworkIntervalWakeup = 5000; // interval to check for network connections after wakeup
        const int MinimumNetworkSpeed = 100000; // network adapters with a speed lower than this value will be ignored
        const int MaxNetworkUnavailablePrintCount = 5;

        // use timespan for retries. otherwise the retries will occur at the same time and migth lead to additional errors
        const int ConnectionRetryIntervalMin = 3000; // min wait time for retry after an errorneous connection
        const int ConnectionRetryIntervalMax = 10000; // max wait time for retry after an errorneous connection
        const int PrintStatusInterval = 50; // print status every x retries to see the progress in the log

        const int AskForCredentialsAfterFails = 5; // ask user for credentials after x fails caused by credentials

        const int ContinuousCheckingInterval = 60000; // time between continuous connectivity checks
        #endregion

        #region Initialization and Disposal
        ILog log;

        NetworkDriveCollection drives;

        bool allowConcurrentWork = true;
        public bool AllowConcurrentWork { get { return this.allowConcurrentWork; } }


        public BackgroundDriveManager(NetworkDriveCollection drives)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            this.drives = drives;
            this.activeDrives = new HashSet<NetworkDrive>();

            // add handler for system wake-up after standby-mode
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        public void Dispose()
        {
            try
            {
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.Dispose: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        public void Shutdown()
        {
            try
            {
                // do cleanup stuff here...
                // disconnecting drives not necessary as the system will do it (drives are not persistent)
            }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.Shutdown: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }
        #endregion

        #region Background Activity Management
        HashSet<NetworkDrive> activeDrives;


        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            log.Info("PowerModeChanged: " + e.Mode);
            log.Info("-> IsNetworkAvailable = " + IsNetworkAvailable());

            try
            {
                if (e.Mode == PowerModes.Resume)
                {
                    // progress of earlier background activity might be compromised -> stop and restart completely
                    AbortBackgroundActivity();
                    BeginBackgroundActivity();
                }
            }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.SystemEvents_PowerModeChanged: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }


        public void BeginBackgroundActivity()
        {
            lock (this.activeDrives)
                this.activeDrives.Clear();

            BeginInitialConnect();
            // continuous checking will be activated after initial connect
        }

        private void AbortBackgroundActivity()
        {
            AbortInitialConnect();
            AbortContinuousChecking();

            lock (this.activeDrives)
                this.activeDrives.Clear();
        }
        #endregion

        #region Initial Connection
        Thread initialWorkerThread;
        Random rnd = new Random();

        private void BeginInitialConnect()
        {
            // only one thread necessary
            if (this.initialWorkerThread != null && this.initialWorkerThread.IsAlive)
                return;

            this.rnd = new Random();

            // spawn worker thread for automatic mounting
            this.initialWorkerThread = new Thread(new ThreadStart(DoInitialConnect));
            this.initialWorkerThread.IsBackground = true;
            this.initialWorkerThread.Start();
        }

        private void AbortInitialConnect()
        {
            // no worker thread created? nothing to abort
            if (this.initialWorkerThread == null)
                return;

            // worker thread is not alive? nothing to abort
            if (this.initialWorkerThread.IsAlive)
                return;

            this.initialWorkerThread.Abort(); // send abort request
            this.initialWorkerThread.Join(); // wait for thread exit
            this.initialWorkerThread = null; // release
        }


        void DoInitialConnect()
        {
            try
            {
                // avoid unneccesary errors by waiting for the network to be initialized
                WaitForNetwork();

                // save value to ensure consistency
                bool concurrent = this.allowConcurrentWork;

                int start = Environment.TickCount;

                lock (this.activeDrives)
                    this.activeDrives.Clear();

                // save current drives
                NetworkDrive[] drivesSnapshot = this.drives.Drives;
                // connect every drive in an own thread
                List<Thread> threads = new List<Thread>(drivesSnapshot.Length);
                for (int i = 0; i < drivesSnapshot.Length; i++)
                {
                    if (drivesSnapshot[i].ConnectOnStartup)
                    {
                        // add to active drives so continuous checking doesn't touch the drive until it is initially connected
                        lock (this.activeDrives)
                            if (!this.activeDrives.Contains(drivesSnapshot[i]))
                                this.activeDrives.Add(drivesSnapshot[i]);

                        // drive should be connected in the future
                        drivesSnapshot[i].TargetConnectivity = true;

                        Thread thread = new Thread(new ParameterizedThreadStart(AsyncConnectDrive));
                        thread.IsBackground = true;
                        thread.Start(drivesSnapshot[i]);

                        if (concurrent)
                        {
                            // store thread for later joining
                            threads.Add(thread);
                        }
                        else
                            // join now to prevent concurrency
                            thread.Join();
                    }
                }

                // start continuous background checking before waiting for init connection threads, as they may never terminate
                BeginContinuousChecking();

                // wait for connection threads to finish
                if (concurrent)
                    for (int i = 0; i < threads.Count; i++)
                        threads[i].Join();

                log.Info("Initial connection completed (concurrent = " + concurrent + ", time = " + (Environment.TickCount - start) + "ms)");
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.DoInitialConnect: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        void AsyncConnectDrive(object obj)
        {
            NetworkDrive drive = (NetworkDrive)obj;

            try
            {
                try
                {
                    ConnectDrive(drive);
                }
                catch (Exception ex)
                {
                    log.Error("Unhandled Exception in BackgroundDriveManager.ConnectDrive: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                }

                lock (this.activeDrives)
                    if (this.activeDrives.Contains(drive))
                        this.activeDrives.Remove(drive);
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.AsyncConnectDrive: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        void ConnectDrive(NetworkDrive drive)
        {
            int startTime = Environment.TickCount;
            log.Info("Background connection task for drive \"" + drive.LocalDriveLetter + "\" to \"" + drive.ExpandedRemoteAddress + "\" started");

            int tries = 0;
            int credentialErrors = 0;
            int lastErrorCode = -1;
            do
            {
                // wait some time before connecting again
                if (tries > 0)
                {
                    int sleepTime;
                    lock (this.rnd)
                        sleepTime = this.rnd.Next(ConnectionRetryIntervalMin, ConnectionRetryIntervalMax);
                    Thread.Sleep(sleepTime);
                }

                if (!this.drives.ContainsNetworkDrive(drive))
                {
                    this.log.Info("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: Drive was remove from the list");
                    return;
                }

                if (drive.DoNotChangeConnectionState)
                {
                    // would have been tried, so count it
                    tries++;
                    continue;
                }
                
                try
                {
                    if (drive.ConnectOnStartup && drive.TargetConnectivity)
                    {
                        if (!App.Current.ApplicationOptions.ForceReconnectAtStartup && drive.IsConnected)
                        {
                            this.log.Info("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: Drive is already connected.");
                            return;
                        }

                        try
                        {
                            tries++;

                            if ((tries % PrintStatusInterval) == 0)
                            {
                                // regular status messages in the log file
                                this.log.Info("Try " + tries + " for drive \"" + drive.LocalDriveLetter + "\": IsConnected=" + drive.IsConnectedCached + "; LastErrorCode=" + lastErrorCode);
                            }

                            drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force | ConnectDriveFlags.DoNotUpdateTargetState | ConnectDriveFlags.NoTimeout);
                        }
                        catch (WNetApiException ex)
                        {
                            if (ex.IsCredentialError)
                            {
                                credentialErrors++;
                                if (credentialErrors >= AskForCredentialsAfterFails)
                                {
                                    this.log.Warn("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: " + credentialErrors + " credential errors have occured. Asking the user for a password again");
                                    RenewDrivePassword(drive);
                                    // either RenewDrivePassword resolved the issue or the user aborted the activity
                                    drive.TargetConnectivity = drive.IsConnectedCached;
                                    return;
                                }
                            }
                            else
                            {
                                credentialErrors = 0;

                                if (!ex.IsTemporaryError)
                                {
                                    this.log.Warn("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: " + ex.Message);
                                    drive.TargetConnectivity = drive.IsConnectedCached;
                                    return;
                                }
                            }

                            // do not flood the log with the same error again and again and again and again and again and again and again and again and again
                            if (ex.ErrorCode != lastErrorCode)
                            {
                                lastErrorCode = ex.ErrorCode;
                                this.log.Error("Windows-API returned " + ex.ErrorCode + " for drive \"" + drive.LocalDriveLetter + "\": " + ex.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.log.Error("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                            // an error occured which will not be solved by just retrying
                            drive.TargetConnectivity = false;
                            return;
                        }
                    }
                    else
                    {
                        // user changed it's mind and the drive has no longer to be connected
                        this.log.Info("Background task for drive \"" + drive.LocalDriveLetter + "\" stopped: Drive was changed (ConnectOnStartup=" + drive.ConnectOnStartup + "; TargetConnectivity=" + drive.TargetConnectivity + ")");
                        // update target connectivity to prevent the continuous checking logic from connecting this drive again (and fail...)
                        drive.TargetConnectivity = drive.IsConnectedCached;
                        return;
                    }
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch { } // empty outter error handler -> the drive MUST be unlocked to prevent deadlocks
            } while (!drive.IsConnected);

            this.log.Info("Drive \"" + drive.LocalDriveLetter + "\" connected after " + (Environment.TickCount - startTime) + " ms");
        }


        Mutex renewDriveMutex = new Mutex();
        private void RenewDrivePassword(NetworkDrive drive)
        {
            renewDriveMutex.WaitOne();

            try
            {
                bool keepAsking = true;
                do
                {
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        CredentialsWindow dialog = new CredentialsWindow();
                        dialog.Description = string.Format(Properties.Strings.CredentialsWindowReEnterAfterFailDescription, Helper.GetDomain(drive.RemoteAddress), AskForCredentialsAfterFails);
                        dialog.Username = drive.Username;
                        if (dialog.ShowDialog() == true)
                        {
                            drive.SetCredentials(dialog.Username, dialog.Password);
                        }
                        else
                        {
                            keepAsking = false;
                        }
                    }));

                    if(keepAsking)
                    {
                        if (CheckNetworkDrive(drive))
                        {
                            try
                            {
                                drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                            }
                            catch(Exception ex)
                            {
                                this.log.Warn("Could not connect drive \"" + drive.LocalDriveLetter + "\" after password renewal: " + ex.Message, ex);
                                App.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    MessageBox.Show(null, string.Format(Properties.Strings.MessageConnectFailedAfterCheck), Properties.Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }));
                            }
                            keepAsking = false;
                        }
                    }
                } while (keepAsking);

                App.Current.DriveCollection.SaveDrives();
            }
            catch (Exception ex)
            {
                this.log.Info("Could not renew password for drive \"" + drive.LocalDriveLetter + "\": " + ex.Message, ex);
            }

            renewDriveMutex.ReleaseMutex();
        }

        private bool CheckNetworkDrive(NetworkDrive drive)
        {
            bool result = false;
            LoadingWindow.TryShowLoadingWindow(Properties.Strings.ProgressMessageCheckingConnection, new Action(() =>
            {
                Exception ex;
                result = drive.CheckConnection(out ex);
                if (!result)
                {
                    log.Error("Connection check of \"" + drive.LocalDriveLetter + "\" to \"" + drive.ExpandedRemoteAddress + "\" with user \"" + drive.Username + "\" failed: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                    App.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Properties.Strings.MessageCouldNotConnect, drive.LocalDriveLetter, Helper.GetUserMessage(ex)), Properties.Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }));
                }
            }));

            return result;
        }
        #endregion

        #region Continuous Connectivity Checking
        Thread continuousWorkerThread;
        bool continuousCheckingActive;

        public void BeginContinuousChecking()
        {
            // only one thread necessary
            if (this.continuousWorkerThread != null && this.continuousWorkerThread.IsAlive)
                return;

            this.continuousCheckingActive = true;

            // spawn worker thread for automatic mounting
            this.continuousWorkerThread = new Thread(new ThreadStart(ContinuousCheckingLoop));
            this.continuousWorkerThread.IsBackground = true;
            this.continuousWorkerThread.Start();
        }

        private void AbortContinuousChecking()
        {
            // no worker thread created? nothing to abort
            if (this.continuousWorkerThread == null)
                return;

            // worker thread is not alive? nothing to abort
            if (this.continuousWorkerThread.IsAlive)
                return;

            this.continuousCheckingActive = false;

            this.continuousWorkerThread.Abort(); // send abort request
            this.continuousWorkerThread.Join(); // wait for thread exit
            this.continuousWorkerThread = null; // release
        }


        private void ContinuousCheckingLoop()
        {
            try
            {
                this.log.Info("Continuous checking is now active.");

                while (this.continuousCheckingActive)
                {
                    Thread.Sleep(ContinuousCheckingInterval);

                    // save current drives
                    NetworkDrive[] drivesSnapshot = this.drives.Drives;
                    // check and reconnect all drives
                    foreach (NetworkDrive drive in drivesSnapshot)
                    {
                        // something currently active on this drive? skip for now
                        if (drive.DoNotChangeConnectionState)
                            continue;

                        lock (this.activeDrives)
                        {
                            // skip drives, which have not been initially connected yet
                            if (this.activeDrives.Contains(drive))
                                continue;

                            this.activeDrives.Add(drive);
                        }

                        try
                        {
                            CheckDriveConnectivity(drive);
                        }
                        catch (ThreadAbortException) { }
                        catch (Exception ex)
                        {
                            log.Error("Unhandled Exception in BackgroundDriveManager.ContinuousCheckingLoop: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        }

                        lock (this.activeDrives)
                            if (this.activeDrives.Contains(drive))
                                this.activeDrives.Remove(drive);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in BackgroundDriveManager.ContinuousCheckingLoop: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        private void CheckDriveConnectivity(NetworkDrive drive)
        {
            if (drive.TargetConnectivity)
            {
                if (!drive.IsConnected)
                {
                    this.log.Info("Drive \"" + drive.LocalDriveLetter + "\" lost it's connection to \"" + drive.RemoteAddress + "\" -> Restoring...");
                    try
                    {
                        drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force | ConnectDriveFlags.DoNotUpdateTargetState | ConnectDriveFlags.NoTimeout);
                    }
                    catch (WNetApiException ex)
                    {
                        this.log.Error("Windows-API returned " + ex.ErrorCode + " for drive \"" + drive.LocalDriveLetter + "\": " + ex.Message);
                    }
                }
            }
        }
        #endregion

        #region Network Checking
        void WaitForNetwork()
        {
            int waitTicks = 0;

            int start = Environment.TickCount;
            while (!IsNetworkAvailable())
            {
                if (waitTicks == MaxNetworkUnavailablePrintCount)
                {
                    log.Warn("Network still unavailable. Stop printing this message from now on...");
                }
                else if (waitTicks < MaxNetworkUnavailablePrintCount)
                {
                    log.Warn("Network unavailable. Wait " + CheckNetworkIntervalWakeup + " Milliseconds...");
                }
                Thread.Sleep(CheckNetworkIntervalWakeup);
                waitTicks++;
            }

            if (waitTicks > 0)
            {
                this.log.Info("Network was available after " + (Environment.TickCount - start) + " ms");
            }
        }

        /// <summary>
        /// Indicates whether any network connection is available.
        /// Filter connections below a specified speed, as well as virtual network cards.
        /// </summary>
        /// <param name="minimumSpeed">The minimum speed required. Passing 0 will not filter connection using speed.</param>
        /// <returns>
        ///     <c>true</c> if a network connection is available; otherwise, <c>false</c>.
        /// </returns>
        bool IsNetworkAvailable(long minimumSpeed = MinimumNetworkSpeed)
        {
            // this method is often fooled by virtual network adapters
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            // problem: virtual adapters can be the connection to the network...
            return true;

            // source: http://stackoverflow.com/questions/520347/how-do-i-check-for-a-network-connection
            /*foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // disconnected network adapters can be skipped
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" was ignored because of its operational status: " + ni.OperationalStatus);
                    continue;
                }

                // some network adapter types can be skipped
                if ((ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) || (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                {
                    this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" was ignored because of its type");
                    continue;
                }

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                {
                    this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" was ignored because of low speed: " + ni.Speed + " < " + minimumSpeed);
                    continue;
                }

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                {
                    this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" was ignored");
                    continue;
                }

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" was ignored because of its name or description");
                    continue;
                }

                this.log.Debug("Adapter \"" + ni.Name + "\" (" + ni.Description + ") of type \"" + ni.NetworkInterfaceType + "\" and speed " + ni.Speed + " matched all criterias");
                return true;
            }
            return false;*/
        }
        #endregion
    }
}
