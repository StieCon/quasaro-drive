using log4net;
using QuasaroDRV.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Windows;

namespace QuasaroDRV.Updating
{
    public enum UpdateStates
    {
        UpToDate = 0,
        UpdateAvailable = 1,
        ServerUnreachable = 2,
        InvalidResponse = 3
    }


    public class AutoUpdater
    {
        ILog log;

        ApplicationOptions appOptions;
        Thread updateThread;

        UpdateWindow updateWindow = null;


        public AutoUpdater(ApplicationOptions appOptions)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            this.appOptions = appOptions;
        }

        public void Dispose()
        {
            if (this.updateWindow != null)
            {
                this.updateWindow.Close();
                this.updateWindow = null;
            }
        }


        public void BeginUpdateCheck()
        {
            this.updateThread = new Thread(new ThreadStart(DoUpdateCheck));
            this.updateThread.IsBackground = true;
            this.updateThread.Start();
        }

        string GetUpdateRequestUrl()
        {
            if (this.appOptions.EnableBetaVersions)
                return Branding.UpdateBetaUrl;
            else
                return Branding.UpdateUrl;
        }

        void DoUpdateCheck()
        {
            string str;
            try
            {
                str = Helper.DownloadString(GetUpdateRequestUrl());
            }
            catch (Exception ex)
            {
                this.log.Error("Could not receive \"" + GetUpdateRequestUrl() + "\": " + ex.Message + " (" + ex.GetType() + ")", ex);
                return;
            }

            try
            {
                JObject obj = JObject.Parse(str);
                if (obj["success"].Value<bool>())
                {
                    JObject data = (JObject)obj["data"];
                    Version newestVersion = new Version(data["current_version"].Value<string>());
                    Version localVersion = App.Current.SoftwareVersion;
                    if (newestVersion > localVersion)
                    {
                        log.Info("Update needed. Local Version is " + localVersion.ToString() + ", newest version is " + newestVersion.ToString());

                        MessageBoxResult result = MessageBoxResult.None;
                        App.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            // open message box in mainwindow-context so it is in front of it
                            result = MessageBox.Show(App.Current.BaseMainWindow, string.Format(Branding.AutoUpdateUpdateToVersionMessage, newestVersion.ToString()), Branding.AutoUpdateTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        }));
                        if (result == MessageBoxResult.Yes)
                        {
                            PerformUpdate(data["download_link"].Value<string>(), data["file_name"].Value<string>());
                        }
                    }
                }
                else
                {
                    this.log.Error("API-Error while receiving authorization data: " + obj["message"].Value<string>());
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Could not parse response from \"" + GetUpdateRequestUrl() + "\": " + ex.Message + " (" + ex.GetType() + ")", ex);
            }
        }


        void PerformUpdate(string downloadUrl, string fileName)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                this.updateWindow = new UpdateWindow(downloadUrl, fileName);
                try
                {
                    this.updateWindow.Owner = App.Current.BaseMainWindow;
                }
                catch { }
                this.updateWindow.ShowDialog();
            }));
        }

        public void PerformUpdate()
        {
            string str = Helper.DownloadString(GetUpdateRequestUrl());
            JObject obj = JObject.Parse(str);
            if (obj["success"].Value<bool>())
            {
                JObject data = (JObject)obj["data"];
                PerformUpdate(data["download_link"].Value<string>(), data["file_name"].Value<string>());
            }
            else
            {
                throw new Exception(string.Format(Properties.Strings.ExceptionReceiveAuthApiErrorMessage, obj["message"].Value<string>()));
            }
        }


        public UpdateStates GetUpdateState(out Version latestVersion)
        {
            latestVersion = null;

            string str;
            try
            {
                str = Helper.DownloadString(GetUpdateRequestUrl());
            }
            catch (Exception ex)
            {
                this.log.Error("Could not receive \"" + GetUpdateRequestUrl() + "\": " + ex.Message + " (" + ex.GetType() + ")", ex);
                return UpdateStates.ServerUnreachable;
            }

            try
            {
                JObject obj = JObject.Parse(str);
                if (obj["success"].Value<bool>())
                {
                    JObject data = (JObject)obj["data"];
                    latestVersion = new Version(data["current_version"].Value<string>());
                    Version localVersion = App.Current.SoftwareVersion;
                    if (latestVersion > localVersion)
                    {
                        return UpdateStates.UpdateAvailable;
                    }
                    else
                    {
                        return UpdateStates.UpToDate;
                    }
                }
                else
                {
                    this.log.Error("API-Error while receiving authorization data: " + obj["message"].Value<string>());
                    return UpdateStates.InvalidResponse;
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Could not parse response from \"" + GetUpdateRequestUrl() + "\": " + ex.Message + " (" + ex.GetType() + ")", ex);
                return UpdateStates.InvalidResponse;
            }
        }
    }
}
