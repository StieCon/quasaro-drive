using log4net;
using System;
using System.Threading;
using System.Windows;

namespace QuasaroDRV.DialogWindows
{
    /// <summary>
    /// Interaction logic for LoadingWindow.xaml
    /// </summary>
    public partial class LoadingWindow : Window
    {
        ILog log;

        bool allowClose = false;

        Thread workThread;
        Delegate workMethod = null;

        public LoadingWindow()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !allowClose;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (workMethod != null)
            {
                this.workThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        workMethod.DynamicInvoke(null);
                    }
                    catch (Exception ex)
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            // find innermost exception
                            while (ex.InnerException != null)
                                ex = ex.InnerException;
                            this.log.Error("Unhandled Exception in " + workMethod.Target.GetType().Name + "." + workMethod.Method.Name + ": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                            MessageBox.Show(string.Format(Properties.Strings.MessageGenericErrorSourceAndStackTrace, workMethod.Target.GetType().Name + "." + workMethod.Method.Name, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }
                    HideLoadingWindow();
                }));
                this.workThread.IsBackground = true;
                this.workThread.Start();
            }
        }


        #region Static Control
        static LoadingWindow staticWindow = null;

        public static bool IsLoadingWindowVisible()
        {
            return (staticWindow != null);
        }

        public static LoadingWindow GetStaticLoadingWindow()
        {
            return staticWindow;
        }

        public static bool TryShowLoadingWindow(string message = null, Delegate workMethod = null, Window owner = null)
        {
            while (IsLoadingWindowVisible())
            {
                if (MessageBox.Show(Properties.Strings.MessageWaitForOperation, Branding.ApplicationName, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Cancel)
                    return false;
            }

            ShowLoadingWindow((message == null ? Properties.Strings.LoadingWindowTitle : message), workMethod, owner);
            return true;
        }

        public static void ShowLoadingWindow(string message = null, Delegate workMethod = null, Window owner = null)
        {
            if (IsLoadingWindowVisible())
                throw new InvalidOperationException(Properties.Strings.ExceptionStaticLoadingWindowExists);

            // execute in main thread for thread safety and ui accessibility
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    if (staticWindow == null)
                    {
                        staticWindow = new LoadingWindow();
                        try
                        {
                            try
                            {
                                staticWindow.Owner = (owner == null ? App.Current.BaseMainWindow : owner);
                            }
                            catch { }
                            if (staticWindow.Owner != null && staticWindow.Owner.IsVisible)
                                staticWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            else
                                staticWindow.ShowInTaskbar = true;
                            staticWindow.workMethod = workMethod;
                            staticWindow.lblDescription.Content = (message == null ? Properties.Strings.LoadingWindowTitle : message);
                            staticWindow.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            staticWindow.log.Error("Unhandled " + ex.GetType().Name + " in LoadingWindow.ShowLoadingWindow: " + ex.Message);
                            staticWindow.log.Debug(ex.StackTrace);
                        }
                        staticWindow = null;
                    }
                }
                catch(Exception ex)
                {
                    staticWindow.log.Error("Unhandled " + ex.GetType().Name + " in LoadingWindow.ShowLoadingWindow: " + ex.Message);
                    staticWindow.log.Debug(ex.StackTrace);
                }
            }));
        }

        public static void HideLoadingWindow()
        {
            // execute in main thread for thread safety and ui accessibility
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (staticWindow != null)
                {
                    staticWindow.allowClose = true;
                    staticWindow.Close();
                    staticWindow = null;
                }
            }));
        }
        #endregion
    }
}
