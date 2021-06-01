using log4net;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace QuasaroDRV.Updating
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        ILog log;

        string downloadUrl;
        string fileName;
        string tmpFile = null;

        Thread workerThread;


        public UpdateWindow(string downloadUrl, string fileName)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            InitializeComponent();
            this.downloadUrl = downloadUrl;
            this.fileName = fileName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.workerThread = new Thread(new ThreadStart(DoDownload));
            this.workerThread.IsBackground = true;
            this.workerThread.Start();
        }

        void DoDownload()
        {
            try
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    pbrProgress.IsIndeterminate = true;
                }));

                // create temporary target file and delete it if necessary
                this.tmpFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), this.fileName);
                if (File.Exists(this.tmpFile))
                    File.Delete(this.tmpFile);

                Helper.DownloadFile(this.downloadUrl, this.tmpFile);

                this.Dispatcher.Invoke(new Action(() =>
                {
                    pbrProgress.IsIndeterminate = false;
                    pbrProgress.Value = pbrProgress.Maximum;
                    ButtonUpdate.IsEnabled = true;

                    if (MessageBox.Show(Properties.Strings.AutoUpdateDownloadCompleteMessage, Branding.AutoUpdateTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        PerformUpdate();
                    }
                }));
            }
            catch (Exception ex)
            {
                this.log.Error("Could not receive from \"" + this.downloadUrl + "\": " + ex.Message + " (" + ex.GetType() + ")", ex);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(string.Format(Properties.Strings.AutoUpdateReceiveErrorMessage, this.downloadUrl, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.AutoUpdateTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }));
            }
        }


        private void ButtonAbort_Click(object sender, RoutedEventArgs e)
        {
            if (this.workerThread != null && this.workerThread.IsAlive)
                this.workerThread.Abort();
            if (this.tmpFile != null && File.Exists(this.tmpFile))
            {
                try
                {
                    File.Delete(this.tmpFile);
                }
                catch { }
            }
            this.Close();
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            PerformUpdate();
        }

        void PerformUpdate()
        {
            this.log.Info("Install update from \"" + this.tmpFile + "\"");
            if (this.tmpFile != null && File.Exists(this.tmpFile))
            {
                // start installer
                Process p = Process.Start(this.tmpFile);
                // exit application
                App.Current.MainWindow.CloseWindow();
                this.Close();
            }
        }
    }
}
