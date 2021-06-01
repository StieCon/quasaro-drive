using log4net;
using log4net.Config;
using QuasaroDRV.Configuration;
using QuasaroDRV.DialogWindows;
using QuasaroDRV.DriveManagement;
using QuasaroDRV.Properties;
using QuasaroDRV.Updating;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace QuasaroDRV
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        enum IpcCommands
        {
            None = 0,
            ShowMainWindow = 1,
            Exit = 2
        }


        const int MaxDaysWithoutSync = 21;
        const int MaxLogFileRetentionDays = 7;

        public static new App Current { get { return (App)Application.Current; } }


        ILog log;

        Mutex applicationMutex;
        const string IpcChannelPrefix = "NetworkDriveBackgroundManager";
        string IpcChannelName;
        NamedPipeServerStream ipcServer;

        string applicationPath;
        public string ApplicationPath { get { return this.applicationPath; } }

        ApplicationOptions appOptions;
        public ApplicationOptions ApplicationOptions { get { return this.appOptions; } }
        MicrosoftOfficeConfiguration officeConfiguration;
        public MicrosoftOfficeConfiguration OfficeConfiguration { get { return this.officeConfiguration; } }

        NetworkDriveCollection driveCollection;
        public NetworkDriveCollection DriveCollection { get { return this.driveCollection; } }

        BackgroundDriveManager backgroundManager;
        public BackgroundDriveManager BackgroundManager { get { return this.backgroundManager; } }

        ConfigurationSuffix14 configSuffix;
        public ConfigurationSuffix14 ConfigSuffix { get { return this.configSuffix; } }

        AutoUpdater updater;
        public AutoUpdater AutoUpdater { get { return this.updater; } }

        public Window BaseMainWindow { get { return base.MainWindow; } }
        public new MainWindow MainWindow
        {
            get
            {
                MainWindow mainWindow = (base.MainWindow as MainWindow);
                if (mainWindow == null)
                    this.log.Warn("Unable to access main window. Currently a window of type " + base.MainWindow.GetType().Name + " is active");
                return mainWindow;
            }
        }
        public Window UppermostWindow
        {
            get
            {
                foreach (Window window in Application.Current.Windows)
                    if (window.IsActive)
                        return window;
                if (LoadingWindow.GetStaticLoadingWindow() != null)
                    return GetUppermostWindow(LoadingWindow.GetStaticLoadingWindow());
                else
                    return BaseMainWindow;
            }
        }
        private Window GetUppermostWindow(Window parent)
        {
            if (parent.OwnedWindows.Count == 1)
                return GetUppermostWindow(parent.OwnedWindows[0]);
            else
                return parent;
        }

        public Version SoftwareVersion
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                return new Version(fileVersion.ProductMajorPart, fileVersion.ProductMinorPart, fileVersion.ProductBuildPart, fileVersion.ProductPrivatePart);
            }
        }

        bool shuttingDown = false;
        public bool ShuttingDown { get { return shuttingDown; } }
        bool shutdownDone = false;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            int startTime = Environment.TickCount;

            try
            {
                this.applicationPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);

                if (!Branding.IsInitialized)
                {
                    MessageBox.Show("Fatal error during initialization. Please reinstall the application.", "Network Drive Service", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(-1);
                    return;
                }

                GlobalContext.Properties["AppDataPath"] = Branding.AppDataPath;
                XmlConfigurator.Configure();
                this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                this.log.Info("Startup");

#if !DEBUG
                //TODO workaround for SD-163 => find root cause...
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                Application.Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
#endif
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().Name + " caught during initialization:\r\n\r\n" + ex.Message, "Network Drive Service", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }

            try
            {
                this.log.Info("-> " + Branding.ApplicationName + ", Version " + SoftwareVersion);
            }
            catch (Exception ex)
            {
                this.log.Warn("Could not retrieve version number (" + ex.GetType().Name + "): " + ex.Message, ex);
            }

            this.appOptions = new ApplicationOptions();
            
            // override system language
            if (!string.IsNullOrEmpty(this.appOptions.SelectedLanguage))
            {
                try
                {
                    /* #### IMPORTANT: needs to be done in STA thread! #### */
                    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(this.appOptions.SelectedLanguage);
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(this.appOptions.SelectedLanguage);
                }
                catch (Exception ex)
                {
                    this.log.Error("Could not set language to \"" + this.appOptions.SelectedLanguage + " (" + ex.GetType().Name + "): " + ex.Message, ex);
                }
            }

            string systemLangName = "";
            try
            {
                systemLangName = Thread.CurrentThread.CurrentUICulture.Name;
                // load translated error messages to the cache
                WNetApiException.UpdateErrorCodes();
            }
            catch (Exception ex)
            {
                this.log.Warn("Could not retrieve language (" + ex.GetType().Name + "): " + ex.Message, ex);
            }
            
            try
            {
                // is this the first instance?
                IpcChannelName = IpcChannelPrefix + Environment.UserDomainName + "." + Environment.UserName;
                bool isMutexOwner;
                this.applicationMutex = new Mutex(true, IpcChannelName, out isMutexOwner);

                if (e.Args.Length > 0 && e.Args[0] == "-wait")
                {
                    if (!isMutexOwner)
                    {
                        log.Info("-> Wait for other instances to close...");

                        do
                        {
                            this.applicationMutex.Close();
                            Thread.Sleep(200);
                            this.applicationMutex = new Mutex(true, IpcChannelName, out isMutexOwner);
                        } while (!isMutexOwner);
                    }
                }

                if (!isMutexOwner)
                {
                    log.Info("-> Another instance is already running");

                    if (e.Args.Length >= 1 && e.Args[0] == "-exit")
                    {
                        // send close command to other applications
                        try
                        {
                            NamedPipeClientStream ipcClient = new NamedPipeClientStream(".", IpcChannelName, PipeDirection.Out, PipeOptions.Asynchronous);
                            ipcClient.Connect();
                            ipcClient.WriteByte((byte)IpcCommands.Exit);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Could not send 'Exit' on Ipc Channel: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        }
                    }
                    else
                    {
                        // send show-window command to other application
                        try
                        {
                            NamedPipeClientStream ipcClient = new NamedPipeClientStream(".", IpcChannelName, PipeDirection.Out, PipeOptions.Asynchronous);
                            ipcClient.Connect();
                            ipcClient.WriteByte((byte)IpcCommands.ShowMainWindow);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Could not send 'ShowMainWindow' on Ipc Channel: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        }
                    }

                    // only one instance allowed -> close this instance
                    Shutdown();
                    return;
                }

                bool initSuccess = false;
                if (e.Args.Length > 0 && e.Args[0] == "-hidden")
                {
                    initSuccess = InitApplication(systemLangName);
                }
                else
                {
                    // disable shutdown on last window closed. otherwise closing the loading window would shutdown the application
                    App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    LoadingWindow.TryShowLoadingWindow(Strings.MessageInitialization, new Action(() =>
                    {
                        initSuccess = InitApplication(systemLangName);
                    }));
                    // re-enable to return to normal behaviour
                    App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                }

                if (!initSuccess)
                {
                    Shutdown(-1);
                    return;
                }

                this.log.Info("Startup took " + (Environment.TickCount - startTime) + " ms");
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled Exception in App.Application_Startup: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(string.Format(Strings.MessageFatalError, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        private bool InitApplication(string systemLangName)
        {
            try
            {
                // init pipe stream for ipc receiver
                try
                {
                    //TODO (SD-184) rework single instance application IPC
                    // https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-application
                    this.ipcServer = new NamedPipeServerStream(IpcChannelName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    this.ipcServer.BeginWaitForConnection(new AsyncCallback(IpcProcessConnection), null);
                }
                catch (Exception ex)
                {
                    this.ipcServer = null;
                    log.Error("Could not listen on Ipc Channel: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                }

                // only the first instance should delete log files
                DeleteOldLogFiles(MaxLogFileRetentionDays);

                if (!NetworkDriveCollection.DrivesConfigured())
                    LoadConfigurationSuffix();

                // initialize licenser silent
                //this.serverAuthorization = new ServerAuthorization(systemLangName);

                // load drives
                try
                {
                    this.driveCollection = new NetworkDriveCollection();
                    this.driveCollection.LoadDrives();
                }
                catch (Exception ex)
                {
                    this.log.Error(ex.GetType().Name + " while loading network drive storage: " + ex.Message, ex);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show(Strings.MessageNetworkDriveStorageCorrupted, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }));
                }

                // init background manager and start activity for automatic mounting
                this.backgroundManager = new BackgroundDriveManager(this.driveCollection);
                this.backgroundManager.BeginBackgroundActivity();

                // check for updates
                this.updater = new AutoUpdater(this.appOptions);

                this.officeConfiguration = new MicrosoftOfficeConfiguration();

                return true;
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled Exception in App.InitApplication: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(string.Format(Strings.MessageFatalError, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                }));
                return false;
            }
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ProcessUnhandledException(e.Exception, false);
            e.Handled = true;
        }
        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ProcessUnhandledException(e.Exception, false);
            e.Handled = true;
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProcessUnhandledException(e.ExceptionObject, e.IsTerminating);
        }
        private void ProcessUnhandledException(object obj, bool isTerminating)
        {
            Exception ex = (obj as Exception);
            if (ex == null)
            {
                if (obj == null)
                {
                    this.log.Error("Unhandled Exception in global context. No furhter information available.");
                }
                else
                {
                    this.log.Error("Unhandled Exception in global context of type " + obj.GetType().Name + ". No further information available.");
                }
            }
            else
            {
                this.log.Error("Unhandled " + ex.GetType().Name + " in global context: " + ex.Message);
                this.log.Debug(ex.StackTrace);
            }

            // TargetInvocationExceptions are often errornuously caught by the global exception handler
            if (!(ex is TargetInvocationException))
            {
                // workaround for SD-163:
                if (!IgnoreException(ex))
                {
                    MessageBox.Show(Branding.MessageUnhandledException, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                    // do not terminate. maybe the application can recover
                    /*if (!isTerminating)
                        Shutdown(-1);*/
                }
            }
        }

        private bool IgnoreException(Exception ex)
        {
            if (ex == null || string.IsNullOrEmpty(ex.StackTrace))
                return false;

            if (ex.StackTrace.Contains("System.Windows.Input.TabletDeviceCollection.HandleTabletAdded")) return true;
            if (ex.StackTrace.Contains("System.Windows.Input.StylusLogic.HandleMessage")) return true;
            if (ex.StackTrace.Contains("System.Windows.Input.StylusLogic.ProcessDisplayChanged")) return true;

            return false;
        }


        void IpcProcessConnection(IAsyncResult ar)
        {
            try
            {
                // connect and read command
                ipcServer.EndWaitForConnection(ar);
                int raw = ipcServer.ReadByte();
                if (raw >= 0)
                {
                    IpcCommands command = (IpcCommands)raw;
                    // commands interact with the main form and must therefore be processed in the UI thread
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        try
                        {
                            if (command == IpcCommands.ShowMainWindow)
                            {
                                this.MainWindow.ShowWindow();
                            }
                            else if (command == IpcCommands.Exit)
                            {
                                this.MainWindow.CloseWindow();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error processing Ipc Message: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                log.Error("Error processing Ipc Message: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }

            try
            {
                ipcServer.Disconnect();
                ipcServer.BeginWaitForConnection(new AsyncCallback(IpcProcessConnection), null);
            }
            catch (Exception ex)
            {
                log.Error("Could not listen on Ipc Channel: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }


        void DeleteOldLogFiles(int days)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(Path.Combine(Branding.AppDataPath, "Logs"), "log-*.txt");
            }
            catch (Exception ex)
            {
                log.Error("Could not list old log files: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return;
            }

            DateTime deleteDate = DateTime.Now - new TimeSpan(days, 0, 0, 0);
            foreach (string file in files)
            {
                try
                {
                    string[] parts = Path.GetFileNameWithoutExtension(file).Split('-');
                    DateTime fileTime = new DateTime(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
                    if (fileTime < deleteDate)
                    {
                        File.Delete(file);
                        log.Info("Log file \"" + Path.GetFileName(file) + "\" deleted");
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Could not delete log file \"" + Path.GetFileName(file) + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                }
            }
        }


        private void LoadConfigurationSuffix()
        {
            string installerFileName = this.appOptions.GetInstallerFileName();
            if (!string.IsNullOrEmpty(installerFileName))
            {
                log.Info("Parse configuration string \"" + installerFileName + "\"");
                this.configSuffix = ConfigurationSuffix14.FromFileName(installerFileName);
                if (this.configSuffix == null)
                {
                    this.log.Info("-> No configuration data detected");
                }
                else
                {
                    this.log.Info("-> OK");
                }
            }
            else
            {
                this.log.Info("-> Initial configuration not possible - No installer file name registered");
            }
        }


        public new void Shutdown()
        {
            this.shuttingDown = true;
            try
            {
                if (this.BaseMainWindow != null)
                    this.BaseMainWindow.Close();
            }
            catch { }
            base.Shutdown();
        }
        public new void Shutdown(int exitCode)
        {
            this.shuttingDown = true;
            try
            {
                if (this.BaseMainWindow != null)
                    this.BaseMainWindow.Close();
            }
            catch { }
            base.Shutdown(exitCode);
        }


        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            this.log.Info("SessionEnding");

            // triggered on windows shutdown or user logoff
            ShutdownApplication();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                this.log.Info("Exit");

                // triggered on window closed
                ShutdownApplication();
            }
            catch { }
        }

        private void ShutdownApplication()
        {
            if (shutdownDone)
                return;
            shutdownDone = true;

            if (this.backgroundManager != null)
                this.backgroundManager.Dispose();

            if (this.updater != null)
                this.updater.Dispose();
        }
    }
}
