using log4net;
using Microsoft.Win32;
using QuasaroDRV.AppInfoWindows;
using QuasaroDRV.Configuration;
using QuasaroDRV.DialogWindows;
using QuasaroDRV.DriveManagement;
using QuasaroDRV.Properties;
using QuasaroDRV.Updating;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;
using System.Windows.Threading;

namespace QuasaroDRV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        ILog log;

        #region UI General
        bool closeWindow = false; // set to true, when the Close-event should not be ignored

        bool hiddenStart = false;
        ApplicationOptions appOptions;
        MicrosoftOfficeConfiguration officeConfiguration;


        public MainWindow()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            try
            {
                InitializeComponent();
                DisableWPFTabletSupport();

                // show only relevant (>0) version number parts (at least two)
                string versionString = App.Current.SoftwareVersion.Major + "." + App.Current.SoftwareVersion.Minor;
                if (App.Current.SoftwareVersion.Build > 0 || App.Current.SoftwareVersion.Revision > 0)
                {
                    versionString += "." + App.Current.SoftwareVersion.Build;
                    if (App.Current.SoftwareVersion.Revision > 0)
                        versionString += "." + App.Current.SoftwareVersion.Revision;
                }
                //this.Title = string.Format(Branding.MainWindowTitle, versionString);

                // do not initialize components when the application is already shutting down
                if (!App.Current.ShuttingDown)
                {
                    // only show background hint at startup in some situations
                    // the following codes decides
                    bool showBackgroundHint = false;

                    this.appOptions = App.Current.ApplicationOptions;
                    this.officeConfiguration = App.Current.OfficeConfiguration;

                    if (this.appOptions.IsFirstStart || this.appOptions.IsNewConfiguration)
                    {
                        this.appOptions.UseAutoStart = true;
                        this.log.Info("Autostart set to " + this.appOptions.UseAutoStart);
                    }

                    string[] args = Environment.GetCommandLineArgs();
                    hiddenStart = (args.Length >= 2 && args[1] == "-hidden");
                    if (this.appOptions.IsFirstStart || !NetworkDriveCollection.DrivesConfigured())
                    {
                        this.log.Info("Install application");

                        if (CreateConfiguration())
                        {
                            // show at first initialization
                            showBackgroundHint = this.appOptions.IsFirstStart;
                        }
                        else
                            UpdateDrivesList();

                        if (App.Current.ConfigSuffix != null)
                        {
                            if (App.Current.DriveCollection.Count == 0)
                            {
                                try
                                {
                                    LoadConfigurationSuffix(App.Current.ConfigSuffix);
                                    App.Current.DriveCollection.SaveDrives();

                                    this.log.Info("-> Load created drives");
                                    UpdateDrivesList();
                                    this.log.Info("-> Connect created drives");
                                    ReConnectAllDrives();

                                }
                                catch (Exception ex)
                                {
                                    this.log.Error("-> Could not load configuration: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                                    UpdateDrivesList();
                                }
                                // show at first initialization
                                showBackgroundHint = this.appOptions.IsFirstStart;
                            }
                            else
                            {
                                this.log.Info("Already configured, ignoring configuration passed by installer");
                                UpdateDrivesList();
                            }
                        }
                        else
                        {
                            this.log.Info("Configuration string not set -> using empty configuration");
                        }
                    }
                    else
                    {
                        UpdateDrivesList();

                        if (!App.Current.ApplicationOptions.PreloadMainWindow && hiddenStart)
                        {
                            this.Visibility = Visibility.Hidden;
                        }
                    }

                    // create notify icon AFTER showing all windows
                    // clicking exit while dialogs are shown in the constructor causes the application to crash
                    InitNotifyIcon();

                    if (showBackgroundHint)
                        ShowBackgroundHint();

                    // start auto-updater asynchronuously
                    if (App.Current.ApplicationOptions.UseAutoUpdate && App.Current.ApplicationOptions.AllowAutoUpdate)
                        App.Current.AutoUpdater.BeginUpdateCheck();

                    UpdateControlVisibilities();
                }
                else
                    log.Info("Do not init main window, app is shutting down");
            }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in MainWindow.ctor: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(string.Format(Strings.MessageFatalError, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);

                App.Current.Shutdown(-1);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // https://stackoverflow.com/questions/14635862/exception-occurs-while-pressing-a-button-on-touchscreen-using-a-stylus-or-a-fing
            if (App.Current.ApplicationOptions.PreloadMainWindow && hiddenStart)
                this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => { HideWindow(); }));
        }

        public void DisableWPFTabletSupport()
        {
            // https://stackoverflow.com/questions/14635862/exception-occurs-while-pressing-a-button-on-touchscreen-using-a-stylus-or-a-fing
            // https://msdn.microsoft.com/en-us/library/dd901337(v=vs.90).aspx

            try
            {
                // Get a collection of the tablet devices for this window.  
                TabletDeviceCollection devices = Tablet.TabletDevices;

                if (devices.Count > 0)
                {
                    // Get the Type of InputManager.
                    Type inputManagerType = typeof(InputManager);

                    // Call the StylusLogic method on the InputManager.Current instance.
                    object stylusLogic = inputManagerType.InvokeMember("StylusLogic", BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, InputManager.Current, null);

                    if (stylusLogic != null)
                    {
                        //  Get the type of the stylusLogic returned from the call to StylusLogic.
                        Type stylusLogicType = stylusLogic.GetType();

                        // Loop until there are no more devices to remove.
                        while (devices.Count > 0)
                        {
                            // Remove the first tablet device in the devices collection.
                            stylusLogicType.InvokeMember("OnTabletRemoved", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic, null, stylusLogic, new object[] { (uint)0 });
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error(ex.GetType().Name + " in DisableWPFTabletSupport(): " + ex.Message, ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closeWindow)
            {
                this.notifyIcon.Visible = false;
                this.notifyIcon.Dispose();
            }
            else
            {
                e.Cancel = true;
                HideWindow();
            }
        }


        public void ShowWindow()
        {
            this.Visibility = Visibility.Visible;
            this.ShowInTaskbar = true;
            // DoEvents is neccessary for the window to update to it's new visibility state
            // otherwise the change of WindowState will be ignored
            System.Windows.Forms.Application.DoEvents();

            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            // bring window to the front, source: http://stackoverflow.com/questions/257587/bring-a-window-to-the-front-in-wpf
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        public void HideWindow()
        {
            // do not close window while child windows are visible
            if (this.OwnedWindows.Count > 0)
            {
                // OwnedWindows is not thread safe. maybe it is cleared in the meantime, no chance to lock it...
                try
                {
                    this.OwnedWindows[0].Activate();
                }
                catch { }
                return;
            }

            this.Visibility = Visibility.Hidden;
            this.ShowInTaskbar = false;
        }

        public void CloseWindow()
        {
            this.closeWindow = true;
            base.Close();
        }


        void ShowBackgroundHint()
        {
            this.notifyIcon.ShowBalloonTip(20000, Branding.ApplicationName, Branding.NotifyIconBackgroundHintText, System.Windows.Forms.ToolTipIcon.Info);
        }


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void MenuOptions_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow dialog = new SettingsWindow(this.appOptions, this.officeConfiguration);
            try
            {
                dialog.Owner = this;
            }
            catch { }
            if (dialog.ShowDialog().Value)
            {
                if (dialog.RequireRestart)
                {
                    if (MessageBox.Show(Branding.MessageSettingsRestartRequired, Strings.MainWindowMenuOptionsTitle, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    {
                        //TODO (SD-97): do not disconnect drives, send "-noconnect" so the current state is kept

                        // -wait tells the other application to wait for this application to exit so it can own the mutex
                        Process.Start(Application.ResourceAssembly.Location, "-wait");
                        CloseWindow();
                    }
                }
            }
            UpdateControlVisibilities();
        }


        private void UpdateControlVisibilities()
        {
            if (itemShowLogFile != null)
                itemShowLogFile.Visible = this.appOptions.EnableShowLogMenu;
            SeparatorRestartWebClient.Visibility = (this.appOptions.ShowRestartWebClientButton ? Visibility.Visible : Visibility.Collapsed);
            MenuRestartWebClient.Visibility = (this.appOptions.ShowRestartWebClientButton ? Visibility.Visible : Visibility.Collapsed);
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                UpdateDrivesList(false);
        }
        #endregion

        #region Configuration Setup
        private bool CreateConfiguration()
        {
            string filePath = NetworkDriveCollection.GetDefaultDrivesStoragePath();
            if (!File.Exists(filePath))
            {
                App.Current.DriveCollection.Clear();
                return true;
            }
            return false;
        }

        private void LoadConfigurationSuffix(ConfigurationSuffix14 suffix)
        {
            if (suffix.HasDriveConfiguration)
            {
                App.Current.DriveCollection.Clear();

                // check configuration conflicts
                bool showConfigWindow = suffix.ShowConfigWindow;
                if (!showConfigWindow)
                {
                    foreach (DriveConfiguration14 drive in suffix.Drives)
                    {
                        if (!App.Current.DriveCollection.IsDriveLetterAvailable(drive.DriveLetter))
                        {
                            log.Info("-> Conflicts with system configuration found (Drive letter " + drive.DriveLetter + ":)");
                            showConfigWindow = true;
                            break;
                        }
                    }
                }

                List<NetworkDrive> drives = new List<NetworkDrive>();
                if (showConfigWindow)
                {
                    InitConfigurationWindow dialog = new InitConfigurationWindow(suffix, drives);
                    dialog.ShowDialog();
                }
                else
                {
                    foreach (DriveConfiguration14 driveConf in suffix.Drives)
                    {
                        NetworkDrive drive = new NetworkDrive(driveConf.DriveLetter.ToString(), driveConf.DriveLabel, driveConf.RemoteAddress, null, (SecureString)null);
                        drives.Add(drive);
                    }
                }

                if (drives.Count > 0)
                {
                    LoadingWindow.TryShowLoadingWindow(Strings.ProgressMessageConnectingDrives, new Action(() =>
                    {
                        SetupDrives(drives);
                    }));
                }

                App.Current.DriveCollection.SaveDrives();
            }

            try
            {
                // set office configuration
                if (suffix.HasOfficeConfiguration)
                {
                    if (suffix.DisableProtectedViewExcel) this.officeConfiguration.ExcelDisableProtectedViewFromInternetSource = true;
                    if (suffix.DisableProtectedViewWord) this.officeConfiguration.WordDisableProtectedViewFromInternetSource = true;
                    if (suffix.DisableProtectedViewPowerPoint) this.officeConfiguration.PowerPointDisableProtectedViewFromInternetSource = true;
                }
            }
            catch (Exception ex)
            {
                log.Error("Could not update office configuration: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        private void SetupDrives(IEnumerable<NetworkDrive> drives)
        {
            Dictionary<string, Credentials> domainCredentials = new Dictionary<string, Credentials>();
            foreach (NetworkDrive drive in drives)
            {
                // find credentials from previous input
                string domain = Helper.GetDomain(drive.RemoteAddress);
                Credentials credentials;
                if (!domainCredentials.TryGetValue(domain.ToUpper(), out credentials))
                {
                    credentials = new Credentials("", null);
                    domainCredentials.Add(domain.ToUpper(), credentials);
                }

                SetupDrive(drive, credentials);
            }
        }

        private bool SetupDrive(NetworkDrive drive, Credentials credentials)
        {
            try
            {
                if (drive.HasCredentials)
                {
                    // overwrite credentials with drive specific credentials
                    credentials.Username = drive.Username;
                    credentials.Password = drive.GetSecurePassword();
                }
                else if (!string.IsNullOrEmpty(drive.Username))
                {
                    if (credentials.Username != drive.Username)
                    {
                        // drive has different credentials attached? -> overwrite
                        credentials.Username = drive.Username;
                        credentials.Password = null;
                    }
                }

                bool added = false;
                bool keepAsking = true;
                do
                {
                    if (credentials.IsValid)
                    {
                        drive.SetCredentials(credentials.Username, credentials.Password);
                        if (CheckNetworkDrive(drive))
                        {
                            added = true;
                            App.Current.DriveCollection.AddDrive(drive);
                            keepAsking = false;
                        }
                    }

                    if (keepAsking)
                    {
                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            CredentialsWindow dialog = new CredentialsWindow();
                            dialog.Owner = LoadingWindow.GetStaticLoadingWindow();
                            dialog.Description = string.Format(Strings.CredentialsWindowEnterCredentialsForDomainText, Helper.GetDomain(drive.RemoteAddress));
                            dialog.Username = drive.Username;
                            if (dialog.ShowDialog() == true)
                            {
                                credentials.Username = dialog.Username;
                                credentials.Password = dialog.Password;
                            }
                            else
                            {
                                drive.ConnectOnStartup = false;
                                keepAsking = false;
                            }
                        }));
                    }
                } while (keepAsking);

                return added;
            }
            catch (Exception ex)
            {
                this.log.Info("Could not setup drive \"" + drive.LocalDriveLetter + "\": " + ex.Message, ex);
                return false;
            }
        }

        private bool CheckNetworkDrive(NetworkDrive drive)
        {
            // try connect
            Exception ex;
            if (!drive.CheckConnection(out ex))
            {
                log.Error("Connection check of \"" + drive.LocalDriveLetter + "\" to \"" + drive.ExpandedRemoteAddress + "\" with user \"" + drive.Username + "\" failed: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Strings.MessageCouldNotConnect, drive.LocalDriveLetter, Helper.GetUserMessage(ex)), Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }));
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region Notify Icon
        System.Windows.Forms.NotifyIcon notifyIcon = null;
        System.Windows.Forms.ToolStripMenuItem itemConnectAll = null;
        System.Windows.Forms.ToolStripMenuItem itemDisconnectAll = null;
        System.Windows.Forms.ToolStripMenuItem itemShowLogFile = null;


        private void InitNotifyIcon()
        {
            this.notifyIcon = new System.Windows.Forms.NotifyIcon();
            this.notifyIcon.DoubleClick += notifyIcon_DoubleClick;
            this.notifyIcon.BalloonTipClicked += notifyIcon_BalloonTipClicked;

            this.notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            System.Windows.Forms.ToolStripMenuItem itemShowMainWindow = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconShowMainWindowTitle);
            itemShowMainWindow.Font = new System.Drawing.Font(itemShowMainWindow.Font, System.Drawing.FontStyle.Bold);
            itemShowMainWindow.Click += itemShowMainWindow_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(itemShowMainWindow);

            this.notifyIcon.ContextMenuStrip.Items.Add("-");

            this.itemConnectAll = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconConnectAllDrivesTitle);
            this.itemConnectAll.Enabled = MenuConnectAll.IsEnabled;
            this.itemConnectAll.Image = Properties.Resources.connect;
            this.itemConnectAll.Click += itemConnectAll_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(itemConnectAll);

            this.itemDisconnectAll = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconDisconnectAllDrivesTitle);
            this.itemDisconnectAll.Enabled = MenuDisconnectAll.IsEnabled;
            this.itemDisconnectAll.Click += itemDisconnectAll_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(itemDisconnectAll);

            this.notifyIcon.ContextMenuStrip.Items.Add("-");

            this.itemShowLogFile = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconShowLogFile);
            this.itemShowLogFile.Click += ItemShowLogFile_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(this.itemShowLogFile);

            System.Windows.Forms.ToolStripMenuItem itemShowAboutWindow = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconAboutTitle);
            itemShowAboutWindow.Click += itemShowAboutWindow_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(itemShowAboutWindow);

            System.Windows.Forms.ToolStripMenuItem itemExit = new System.Windows.Forms.ToolStripMenuItem(Strings.NotifyIconExitTitle);
            itemExit.Click += itemExit_Click;
            this.notifyIcon.ContextMenuStrip.Items.Add(itemExit);

            this.notifyIcon.Icon = new Icon(Path.Combine(App.Current.ApplicationPath, "Resources\\notify_icon.ico"));
            this.notifyIcon.Visible = true;
            this.notifyIcon.Text = (this.Title.Length > 63 ? this.Title.Substring(0, 63) : this.Title);

            UpdateControlVisibilities();
        }

        void notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            ShowWindow();
        }

        void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Hidden)
                ShowWindow();
            else
                HideWindow();
        }

        void itemShowMainWindow_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }

        void itemShowAboutWindow_Click(object sender, EventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            if (this.IsLoaded)
            {
                try
                {
                    aboutWindow.Owner = this;
                }
                catch { }
            }
            aboutWindow.Show();
        }

        void itemConnectAll_Click(object sender, EventArgs e)
        {
            ReConnectAllDrives();
        }

        void itemDisconnectAll_Click(object sender, EventArgs e)
        {
            DisconnectAllDrives();
        }


        private void ItemShowLogFile_Click(object sender, EventArgs e)
        {
            try
            {
                log4net.Repository.Hierarchy.Hierarchy hierarchy = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
                log4net.Appender.FileAppender fileAppender = (hierarchy.Root.GetAppender("LogFileAppender") as log4net.Appender.FileAppender);
                Process.Start(fileAppender.File);
            }
            catch (Exception ex)
            {
                log.Error("Unhandled Exception in MainWindow.ItemShowLogFile_Click: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(this, string.Format(Strings.MessageGenericError, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        void itemExit_Click(object sender, EventArgs e)
        {
            if (LoadingWindow.IsLoadingWindowVisible() || this.OwnedWindows.Count > 0)
            {
                MessageBox.Show(this, Strings.MessageWaitForOperationAndCloseWindows, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (App.Current.DriveCollection.HasConnectedDrives())
            {
                MessageBoxResult result = MessageBox.Show(this, Strings.MessageDisconnectAllNetworkDrives, Branding.ApplicationName, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    PerformDriveAction(App.Current.DriveCollection.Drives, NetworkDriveAction.Disconnect);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // do not close the window
                    return;
                }
            }

            CloseWindow();
            App.Current.Shutdown();
        }
        #endregion

        #region Menu Bar
        private void Menu_AddDrive_Click(object sender, RoutedEventArgs e)
        {
            AddNewDrive();
        }

        private void Menu_EditDrive_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedDrive();
        }

        private void Menu_RemoveDrive_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedDrives();
        }

        private void Menu_ExportSelectedDrives_Click(object sender, RoutedEventArgs e)
        {
            ExportDrives(GetSelectedDrives());
        }

        private void Menu_ExportAllDrives_Click(object sender, RoutedEventArgs e)
        {
            ExportDrives(App.Current.DriveCollection.Drives);
        }

        private void Menu_ImportDrive_Click(object sender, RoutedEventArgs e)
        {
            ImportDrives();
        }

        private void Menu_CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.HideWindow();
        }

        private void Menu_ConnectSelected_Click(object sender, RoutedEventArgs e)
        {
            ConnectSelectedDrive();
        }

        private void Menu_DisconnectSelected_Click(object sender, RoutedEventArgs e)
        {
            DisconnectSelectedDrive();
        }

        private void Menu_ConnectAll_Click(object sender, RoutedEventArgs e)
        {
            ReConnectAllDrives();
        }

        private void Menu_DisconnectAll_Click(object sender, RoutedEventArgs e)
        {
            DisconnectAllDrives();
        }

        private void MenuRestartWebClient_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, Strings.MessageRestartWebClient, Strings.MainWindowMenuRestartWebClientTitle, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.OK)
            {
                LoadingWindow.TryShowLoadingWindow(Strings.MainWindowMenuRestartWebClientTitle, new Action(() =>
                {
                    // save connected drives in list and disconnect them. this should cause less trouble when deactivating the WebClient service
                    List<NetworkDrive> restoreDrives = new List<NetworkDrive>();
                    foreach (NetworkDrive drive in App.Current.DriveCollection.Drives)
                    {
                        if (drive.TargetConnectivity || drive.IsConnectedCached)
                        {
                            restoreDrives.Add(drive);
                            try
                            {
                                drive.Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected | DisconnectDriveFlags.DoNotWaitForDisconnection | DisconnectDriveFlags.DoNotUpdateTargetState);
                            }
                            catch { }
                        }
                    }

                    bool runElevatedResult = Helper.RunElevated("RestartWebClient");

                    bool allDrivesRestored = true;
                    foreach (NetworkDrive drive in restoreDrives)
                    {
                        try
                        {
                            drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Could not restore drive " + drive.LocalDriveLetter + " after restarting WebClient service: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                            allDrivesRestored = false;
                        }
                    }

                    // only show one message to the user -> the most urgent one
                    if (!runElevatedResult)
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), Strings.MessageRunElevatedFailed, Strings.MainWindowMenuRestartWebClientTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }
                    else if (!allDrivesRestored)
                    {
                        // service restarted but drives could not be connected?
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), Strings.MessageNotAllDrivesRestored, Strings.MainWindowMenuRestartWebClientTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }));
                    }
                }));
            }
        }

        private void Menu_CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            // do not allow updates while there are drives beeing processed
            if (LoadingWindow.IsLoadingWindowVisible())
            {
                MessageBox.Show(this, Strings.MessageWaitForOperation, Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            Version latestVersion;
            UpdateStates state = App.Current.AutoUpdater.GetUpdateState(out latestVersion);
            if (state == UpdateStates.UpToDate)
            {
                MessageBox.Show(this, Branding.AutoUpdateUpToDateMessage, Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (state == UpdateStates.ServerUnreachable)
            {
                MessageBox.Show(this, Strings.AutoUpdateCouldNotReachServerMessage, Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else if (state == UpdateStates.InvalidResponse)
            {
                MessageBox.Show(this, Strings.AutoUpdateInvalidResponseMessage, Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            else
            {
                if (MessageBox.Show(this, string.Format(Branding.AutoUpdateUpdateToVersionMessage, latestVersion.ToString()), Branding.AutoUpdateTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    log.Info("User requested update");
                    try
                    {
                        App.Current.AutoUpdater.PerformUpdate();
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error while updating: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        MessageBox.Show(this, string.Format(Strings.AutoUpdateErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow window = new AboutWindow();
            try
            {
                window.Owner = this;
            }
            catch { }
            window.ShowDialog();
        }
        #endregion

        #region UI
        bool ignoreConnectCheckedChange = false;


        private void drivesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditDrive.IsEnabled = (drivesListView.SelectedItems.Count == 1);
            MenuEditDrive.IsEnabled = EditDrive.IsEnabled;
            MenuEditDriveIcon.Source = Helper.ConvertToBitmapSource(MenuEditDrive.IsEnabled ? Properties.Resources.edit : Properties.Resources.edit_grey);
            RemoveDrive.IsEnabled = (drivesListView.SelectedItems.Count > 0);
            MenuRemoveDrive.IsEnabled = RemoveDrive.IsEnabled;
            MenuRemoveDriveIcon.Source = Helper.ConvertToBitmapSource(MenuRemoveDrive.IsEnabled ? Properties.Resources.remove : Properties.Resources.remove_grey);
            MenuConnectSelected.IsEnabled = (drivesListView.SelectedItems.Count > 0);
            MenuDisconnectSelected.IsEnabled = (drivesListView.SelectedItems.Count > 0);
            MenuExportSelectedDrives.IsEnabled = (drivesListView.SelectedItems.Count > 0);
        }

        private void drivesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EditSelectedDrive();
        }

        private void CheckBoxConnectOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            App.Current.DriveCollection.SaveDrives();
        }
        private void CheckBoxConnectOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            App.Current.DriveCollection.SaveDrives();
        }

        private void AddDrive_Click(object sender, RoutedEventArgs e)
        {
            AddNewDrive();
        }

        private void EditDrive_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedDrive();
        }

        private void RemoveDrive_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedDrives();
        }

        private void ConnectAll_Click(object sender, RoutedEventArgs e)
        {
            ReConnectAllDrives();
        }
        #endregion

        #region Drive Management
        void UpdateDrivesList(bool keepSelection = true)
        {
            // do not trigger events while updating the list
            ignoreConnectCheckedChange = true;

            object[] selectedItems = null;
            if (keepSelection)
            {
                // save selected items
                selectedItems = new object[this.drivesListView.SelectedItems.Count];
                this.drivesListView.SelectedItems.CopyTo(selectedItems, 0);
            }

            // force list to update
            this.drivesListView.ItemsSource = null;
            NetworkDrive[] drives = App.Current.DriveCollection.Drives;
            foreach (NetworkDrive drive in drives)
                drive.UpdateConnectivity();
            this.drivesListView.ItemsSource = drives;

            if (keepSelection)
            {
                // restore selected items
                this.drivesListView.SelectedItems.Clear();
                foreach (object obj in selectedItems)
                {
                    try
                    {
                        this.drivesListView.SelectedItems.Add(obj);
                    }
                    catch { }
                }
            }

            // disable buttons which require drives when no drives are available
            ConnectAll.IsEnabled = (drives.Length > 0);
            MenuExportAllDrives.IsEnabled = (drives.Length > 0);
            MenuExportAllDrivesIcon.Source = Helper.ConvertToBitmapSource(MenuExportAllDrives.IsEnabled ? Properties.Resources.export : Properties.Resources.export_grey);
            MenuConnectAll.IsEnabled = (drives.Length > 0);
            MenuConnectAllIcon.Source = Helper.ConvertToBitmapSource(MenuConnectAll.IsEnabled ? Properties.Resources.connect : Properties.Resources.connect_grey);
            MenuDisconnectAll.IsEnabled = (drives.Length > 0);
            // notify icon might not be initialized yet:
            if (this.itemConnectAll != null)
                this.itemConnectAll.Enabled = MenuConnectAll.IsEnabled;
            if (this.itemDisconnectAll != null)
                this.itemDisconnectAll.Enabled = MenuDisconnectAll.IsEnabled;

            ignoreConnectCheckedChange = false;
        }

        NetworkDrive[] GetSelectedDrives()
        {
            NetworkDrive[] selectedDrives = new NetworkDrive[this.drivesListView.SelectedItems.Count];
            this.drivesListView.SelectedItems.CopyTo(selectedDrives, 0);
            return selectedDrives;
        }


        void AddNewDrive()
        {
            // get free drive letters to give the user a selection list
            char[] driveLetters = App.Current.DriveCollection.GetUnusedDriveLetters();

            if (driveLetters.Length == 0)
            {
                MessageBox.Show(this, Strings.MessageCannotAddNoDriveLetters, Strings.MainWindowAddDriveDialogTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            NetworkDriveWindow dialog = new NetworkDriveWindow(driveLetters);
            try
            {
                dialog.Owner = this;
            }
            catch { }
            dialog.NetworkDrive = new NetworkDrive(driveLetters[driveLetters.Length - 1].ToString());
            dialog.Title = Strings.MainWindowAddDriveDialogTitle;
            if (dialog.ShowDialog() == true)
            {
                if (App.Current.DriveCollection.RemoteAddressExists(dialog.NetworkDrive.RemoteAddress))
                {
                    MessageBox.Show(this, Strings.MessageRemoteAddressAlreadyInUse, Strings.MainWindowAddDriveDialogTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                App.Current.DriveCollection.AddDrive(dialog.NetworkDrive);

                UpdateDrivesList();
                App.Current.DriveCollection.SaveDrives();
            }
        }

        private void EditSelectedDrive()
        {
            NetworkDrive drive = (this.drivesListView.SelectedItem as NetworkDrive);
            if (drive != null)
            {
                try
                {
                    // connect the drive when the dialog is closed?
                    bool restore = drive.IsConnectedCached;

                    // get free drive letters to give the user a selection list
                    // ignore the editing drive itself, so it's letter is still available in the list
                    char[] driveLetters = App.Current.DriveCollection.GetUnusedDriveLetters(drive);
                    NetworkDriveWindow dialog = new NetworkDriveWindow(driveLetters);
                    try
                    {
                        dialog.Owner = this;
                    }
                    catch { }
                    dialog.NetworkDrive = drive.Copy();
                    dialog.Title = Strings.MainWindowEditDriveDialogTitle;
                    if (dialog.ShowDialog() == true)
                    {
                        log.Info("Edit network drive \"" + drive.LocalDriveLetter + "\" (restore = " + restore + ")");

                        if (restore)
                        {
                            try
                            {
                                // disconnect old drive. maybe the drive letter has been changed
                                drive.Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);
                            }
                            catch { }

                            LoadingWindow.TryShowLoadingWindow(string.Format(Strings.ProgressMessageConnectingDrive, dialog.NetworkDrive.LocalDriveLetter), new Action(() =>
                            {
                                // connect drive with new connection data
                                try
                                {
                                    dialog.NetworkDrive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                                }
                                catch (Exception ex)
                                {
                                    log.Error("Unable to connect drive \"" + dialog.NetworkDrive.LocalDriveLetter + "\" after editing: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                                    this.Dispatcher.Invoke(new Action(() =>
                                    {
                                        MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Strings.MessageCouldNotConnect, dialog.NetworkDrive.LocalDriveLetter, Helper.GetUserMessage(ex)), Strings.MainWindowEditDriveDialogTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    }));
                                }
                            }));
                        }

                        // copy edited data to original object in drive collection
                        // it is important to preserve the original instance as there may exist
                        // pointers in the application
                        drive.Update(dialog.NetworkDrive);

                        //UpdateDrivesList();
                        App.Current.DriveCollection.SaveDrives();
                    }
                    else
                    {
                        // when the user clicks the check-button in the edit-dialog the drive is disconnected
                        if (restore && !drive.IsConnected)
                        {
                            LoadingWindow.TryShowLoadingWindow(string.Format(Strings.ProgressMessageConnectingDrive, drive.LocalDriveLetter), new Action(() =>
                            {
                                // reconnect, if the connection was lost during editing
                                try
                                {
                                    drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected);
                                }
                                catch (Exception ex)
                                {
                                    log.Error("Unable to re-connect drive \"" + dialog.NetworkDrive.LocalDriveLetter + "\" after editing: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                                }
                            }));
                        }
                    }

                    UpdateDrivesList();
                }
                catch { }
            }
        }

        void RemoveSelectedDrives()
        {
            NetworkDrive[] selectedDrives = GetSelectedDrives();

            if (selectedDrives.Length > 0)
            {
                try
                {
                    // maybe the user has mistakenly clicked on the delete button?
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Strings.MainWindowRemoveDrivesMessage).Append("\n");
                    foreach (NetworkDrive drive in selectedDrives)
                        sb.Append('\n').Append(drive.LocalDriveLetter).Append(" (").Append(drive.RemoteAddress).Append(")");
                    if (MessageBox.Show(sb.ToString(), Strings.MainWindowRemoveDriveTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return;

                    // when set to true, will run disconnect for every drive before deleting
                    bool disconnect = false;

                    if (NetworkDriveCollection.HasConnectedDrives(selectedDrives))
                    {
                        MessageBoxResult msgResult = MessageBox.Show(Strings.MainWindowDisconnectBeforeRemoveMessage, Strings.MainWindowRemoveDriveTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        if (msgResult == MessageBoxResult.Cancel)
                            return;
                        disconnect = (msgResult == MessageBoxResult.Yes);
                    }

                    bool hasErrors = false;
                    LoadingWindow.TryShowLoadingWindow(Strings.MainWindowRemoveDrivesProgressMessage, new Action(() =>
                    {
                        foreach (NetworkDrive drive in selectedDrives)
                        {
                            try
                            {
                                if (disconnect)
                                {
                                    try
                                    {
                                        drive.Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);
                                    }
                                    catch { }
                                }

                                App.Current.DriveCollection.RemoveAndUnlock(drive);
                            }
                            catch (Exception ex)
                            {
                                log.Error("Unable to remove drive \"" + drive.LocalDriveLetter + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                                hasErrors = true;
                            }
                        }
                    }));

                    if (hasErrors)
                    {
                        // show collective error message
                        MessageBox.Show(this, Strings.MainWindowNotAllDrivesRemovedMessage, Strings.MainWindowRemoveDriveTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Unable to remove drives: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                }

                UpdateDrivesList();
                App.Current.DriveCollection.SaveDrives();
            }
        }


        void ExportDrives(NetworkDrive[] drives)
        {
            ExportDrivesWindow dialog = new ExportDrivesWindow(drives);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        void ImportDrives()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = Strings.MainWindowImportTitle;
            dialog.Filter = "*.drives|*.drives|*.*|*.*";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    LoadingWindow.TryShowLoadingWindow(Strings.MainWindowImportProgressMessage, new Action(() =>
                    {
                        try
                        {
                            JArray drivesArray = null;
                            bool isEncrypted = false;
                            string password = null;

                            try
                            {
                                JObject importObj = JObject.Parse(File.ReadAllText(dialog.FileName, Encoding.UTF8));
                                isEncrypted = (importObj["IsEncrypted"] != null && importObj["IsEncrypted"].Value<bool>());
                                if (isEncrypted)
                                {
                                    while (password == null)
                                    {
                                        this.Dispatcher.Invoke(new Action(() =>
                                        {
                                            EnterPasswordWindow pwdDialog = new EnterPasswordWindow(Strings.MainWindowImportPasswordMessage);
                                            pwdDialog.Owner = LoadingWindow.GetStaticLoadingWindow();
                                            if (pwdDialog.ShowDialog() == true)
                                            {
                                                password = pwdDialog.Password;
                                            }
                                            else
                                            {
                                                password = null;
                                            }
                                        }));

                                        if (password == null)
                                            break;

                                        if (Helper.CheckPasswordStore(importObj["Check"].Value<string>(), password))
                                            break;
                                        password = null;
                                    }
                                }
                                drivesArray = (JArray)importObj["Drives"];
                            }
                            catch (Newtonsoft.Json.JsonReaderException)
                            {
                                log.Warn("Unable to import file \"" + Path.GetFileName(dialog.FileName) + "\". Try importing as old version.");
                                drivesArray = JArray.Parse(File.ReadAllText(dialog.FileName, Encoding.UTF8));
                            }

                            if (!isEncrypted || password != null)
                            {
                                bool noDriveLettersAvailable = false;

                                Dictionary<string, Credentials> domainCredentials = new Dictionary<string, Credentials>();
                                foreach (JObject obj in drivesArray)
                                {
                                    if (isEncrypted)
                                    {
                                        foreach (KeyValuePair<string, JToken> kvp in obj)
                                        {
                                            if (kvp.Value.Type == JTokenType.String)
                                            {
                                                obj[kvp.Key] = Helper.DecryptValue(kvp.Value.Value<string>(), password);
                                            }
                                        }
                                    }

                                    NetworkDrive drive = new NetworkDrive(obj["LocalDriveLetter"].Value<string>(), obj["DriveLabel"].Value<string>(), obj["RemoteAddress"].Value<string>(), obj["Username"].Value<string>(), obj["Password"].Value<string>());
                                    drive.ConnectOnStartup = obj["ConnectOnStartup"].Value<bool>();

                                    // skip drive import if the remote address is already added
                                    if (App.Current.DriveCollection.RemoteAddressExists(drive))
                                    {
                                        log.Info("Skip drive \"" + drive.LocalDriveLetter + "\" in import: Remote address \"" + drive.RemoteAddress + "\" already exists.");
                                        continue;
                                    }

                                    // drive letter alread in use?
                                    if (!App.Current.DriveCollection.IsDriveLetterAvailable(drive.LocalDriveLetter[0]))
                                    {
                                        // use an arbitrary free drive letter instead
                                        char[] letters = App.Current.DriveCollection.GetUnusedDriveLetters();
                                        if (letters.Length == 0)
                                        {
                                            log.Warn("Cannot import drive \"" + drive.LocalDriveLetter + "\": No drive letters available in the system.");
                                            noDriveLettersAvailable = true;
                                            continue;
                                        }
                                        drive.LocalDriveLetter = letters[letters.Length - 1].ToString();
                                    }

                                    log.Info("Import drive \"" + drive.LocalDriveLetter + "\" with remote address \"" + drive.RemoteAddress + "\".");

                                    App.Current.DriveCollection.AddDrive(drive);
                                }

                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    UpdateDrivesList();
                                    if (noDriveLettersAvailable)
                                    {
                                        MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), Strings.MessageImportFailedNoDriveLetters, Strings.MainWindowImportTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                    }
                                }));
                                App.Current.DriveCollection.SaveDrives();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error importing drives: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Strings.MainWindowImportErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Strings.MainWindowImportTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }));
                        }
                    }));
                }
                catch (Exception ex)
                {
                    log.Error("Error importing drives: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                    MessageBox.Show(this, string.Format(Strings.MainWindowImportErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Strings.MainWindowImportTitle, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }


        void ConnectSelectedDrive()
        {
            if (this.drivesListView.SelectedItems.Count == 1)
                PerformDriveAction((NetworkDrive)this.drivesListView.SelectedItem, NetworkDriveAction.Connect);
            else
            {
                PerformDriveAction(GetSelectedDrives(), NetworkDriveAction.Connect);
            }
        }

        void DisconnectSelectedDrive()
        {
            if (this.drivesListView.SelectedItems.Count == 1)
                PerformDriveAction((NetworkDrive)this.drivesListView.SelectedItem, NetworkDriveAction.Disconnect);
            else
            {
                PerformDriveAction(GetSelectedDrives(), NetworkDriveAction.Disconnect);
            }
        }

        bool PerformDriveAction(NetworkDrive drive, NetworkDriveAction action)
        {
            // only one async process at once allowed
            if (LoadingWindow.IsLoadingWindowVisible())
            {
                MessageBox.Show(Strings.MessageWaitForOperation, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            try
            {
                LoadingWindow.TryShowLoadingWindow(string.Format((action == NetworkDriveAction.Connect ? Strings.ProgressMessageConnectingDrive : Strings.ProgressMessageDisconnectingDrive), drive.LocalDriveLetter), new Action(() =>
                {
                    this.ignoreConnectCheckedChange = true;

                    try
                    {
                        bool actionPerformed = true;

                        int start = Environment.TickCount;
                        if (action == NetworkDriveAction.Connect) drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                        else if (action == NetworkDriveAction.ReConnect)
                        {
                            if (!App.Current.ApplicationOptions.OnlyReconnectActiveDrives || drive.ConnectOnStartup || drive.IsConnectedCached)
                            {
                                drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                            }
                            else
                            {
                                actionPerformed = false;
                                log.Debug("Action " + action + " not performed on drive \"" + drive.LocalDriveLetter + "\": ConnectOnStartup=" + drive.ConnectOnStartup + "; TargetConnectivity=" + drive.TargetConnectivity);
                            }
                        }
                        else if (action == NetworkDriveAction.Disconnect) drive.Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);

                        drive.UpdateConnectivity();

                        if (actionPerformed)
                            this.log.Debug("Performed action " + action + " on drive " + drive.LocalDriveLetter + " in " + (Environment.TickCount - start) + " ms");
                    }
                    catch (Exception ex)
                    {
                        this.log.Error("Unable to perform action " + action + " on drive \"" + drive.LocalDriveLetter + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            // message dialog is only displayed on top when it is displayed in the main UI-thread
                            string verb = action.ToString();
                            if (action == NetworkDriveAction.Connect) verb = Strings.ActionVerbConnect;
                            if (action == NetworkDriveAction.Disconnect) verb = Strings.ActionVerbDisconnect;
                            if (action == NetworkDriveAction.ReConnect) verb = Strings.ActionVerbReConnect;
                            MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Strings.MainWindowActionFailedMessage, verb, drive.LocalDriveLetter, Helper.GetUserMessage(ex)), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }
                    this.ignoreConnectCheckedChange = false;
                }));
            }
            catch { }

            return true;
        }


        void ReConnectAllDrives()
        {
            PerformDriveAction(App.Current.DriveCollection.Drives, NetworkDriveAction.ReConnect);
        }

        void DisconnectAllDrives()
        {
            PerformDriveAction(App.Current.DriveCollection.Drives, NetworkDriveAction.Disconnect);
        }

        void PerformDriveAction(NetworkDrive[] drives, NetworkDriveAction action)
        {
            // only one async process at once allowed
            if (LoadingWindow.IsLoadingWindowVisible())
            {
                MessageBox.Show(Strings.MessageWaitForOperation, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // the checkboxes are often confused when connectivity is changed in the background -> ignore all events while performing the action
            this.ignoreConnectCheckedChange = true;
            try
            {
                LoadingWindow.TryShowLoadingWindow((action == NetworkDriveAction.Disconnect ? Strings.ProgressMessageDisconnectingDrives : Strings.ProgressMessageConnectingDrives), new Action(() =>
                {
                    bool hasErrors = false;

                    log.Debug("Issued action " + action + " for " + drives.Length + " (of " + App.Current.DriveCollection.Count + ") drive(s)");
                    int start = Environment.TickCount;

                    foreach (NetworkDrive drive in drives)
                    {
                        try
                        {
                            // connect and reconnect only differ in their semantics when to perform

                            if (action == NetworkDriveAction.Connect)
                            {
                                drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                            }
                            else if (action == NetworkDriveAction.ReConnect)
                            {
                                if (!App.Current.ApplicationOptions.OnlyReconnectActiveDrives || drive.ConnectOnStartup || drive.IsConnectedCached)
                                {
                                    drive.Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force);
                                }
                                else
                                {
                                    log.Debug("Action " + action + " not performed on drive \"" + drive.LocalDriveLetter + "\": ConnectOnStartup=" + drive.ConnectOnStartup + "; TargetConnectivity=" + drive.TargetConnectivity);
                                }
                            }
                            else if (action == NetworkDriveAction.Disconnect)
                                drive.Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);

                            drive.UpdateConnectivity();
                        }
                        catch (Exception ex)
                        {
                            log.Error("Unable to perform action " + action + " on drive \"" + drive.LocalDriveLetter + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                            hasErrors = true;
                        }
                    }

                    log.Debug("-> Actions finished after " + (Environment.TickCount - start) + " ms");

                    if (hasErrors)
                    {
                        // show collective error message
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            // message dialog is only displayed on top when it is displayed in the main UI-thread
                            MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), Strings.MainWindowActionsPartiallyFailedMessage, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }));
                    }
                }));
            }
            catch { }
            this.ignoreConnectCheckedChange = false;
        }
        #endregion

    }
}
