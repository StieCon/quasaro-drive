using log4net;
using Microsoft.Win32;
using QuasaroDRV.DriveManagement;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace QuasaroDRV.Configuration
{
    public class ApplicationOptions
    {
        const string ConfigFileName = "config.dat";
        const string ConfigBackupFileName = "config_backup.dat";
        
        const string ConfNameUseAutoUpdate = "UseAutoUpdate";
        const bool ConfDefaultUseAutoUpdate = true;
        const string ConfNameEnableBetaVersions = "EnableBetaVersions";
        const bool ConfDefaultEnableBetaVersions = false;
        const string ConfNameSelectedLanguage = "SelectedLanguage";
        const string ConfDefaultSelectedLanguage = "";
        const string ConfNameOnlyReconnectActiveDrives = "OnlyReconnectActiveDrives";
        const bool ConfDefaultOnlyReconnectActiveDrives = true;
        const string ConfNameForceReconnectAtStartup = "ForceReconnectAtStartup";
        const bool ConfDefaultForceReconnectAtStartup = false;
        const string ConfNamePreloadMainWindow = "PreloadMainWindow";
        const bool ConfDefaultPreloadMainWindow = false;
        const string ConfNameSendSystemUpdateNotifications = "SendSystemUpdateNotifications";
        const bool ConfDefaultSendSystemUpdateNotifications = false;
        const string ConfNameShowHumandReadableErrors = "ShowHumanReadableErrors";
        const bool ConfDefaultShowHumandReadableErrors = true;
        const string ConfNameUseWindowsApi = "UseWindowsApi";
        const bool ConfDefaultUseWindowsApi = true;
        const string ConfNameUseSpecializedWebDavCheck = "UseSpecializedWebDavCheck";
        const bool ConfDefaultUseSpecializedWebDavCheck = false;
        const string ConfNameShowRestartWebClientButton = "ShowRestartWebClientButton";
        const bool ConfDefaultShowRestartWebClientButton = false;
        const string ConfNameEnableShowLogMenu = "EnableShowLogMenu";
        const bool ConfDefaultEnableShowLogMenu = true;

        ILog log;

        JObject config;
        RegistryKey appKey;
        RegistryKey runKey;


        private bool isFirstStart;
        public bool IsFirstStart { get { return this.isFirstStart; } }
        private bool isNewConfiguration;
        public bool IsNewConfiguration { get { return this.isNewConfiguration; } }


        public bool UseAutoStart
        {
            get
            {
                return Helper.RegistryValueExists(this.runKey, Branding.ReplaceInvalidRegistryChars(Branding.ApplicationName));
            }
            set
            {
                if (value)
                    Helper.RegistrySetValue(this.runKey, Branding.ReplaceInvalidRegistryChars(Branding.ApplicationName), "\"" + Application.ExecutablePath + "\" -hidden");
                else
                    Helper.RegistryDeleteValue(this.runKey, Branding.ReplaceInvalidRegistryChars(Branding.ApplicationName));
            }
        }


        public bool AllowAdvancedSettings
        {
            get
            {
                return !Helper.RegistryReadBool(this.appKey, "DisableAdvancedSettings", false);
            }
        }
        
        public bool AllowAutoUpdate
        {
            get { return !Helper.RegistryReadBool(this.appKey, "DisableAutoUpdate", false); }
        }
        public bool UseAutoUpdate
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameUseAutoUpdate, ConfDefaultUseAutoUpdate); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameUseAutoUpdate, value);
                SaveConfiguration();
            }
        }
        public bool EnableBetaVersions
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameEnableBetaVersions, ConfDefaultEnableBetaVersions); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameEnableBetaVersions, value);
                SaveConfiguration();
            }
        }

        public string SelectedLanguage
        {
            get { return Helper.GetJsonValue<string>(this.config, ConfNameSelectedLanguage, ConfDefaultSelectedLanguage); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameSelectedLanguage, value);
                SaveConfiguration();
            }
        }

        public bool OnlyReconnectActiveDrives
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameOnlyReconnectActiveDrives, ConfDefaultOnlyReconnectActiveDrives); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameOnlyReconnectActiveDrives, value);
                SaveConfiguration();
            }
        }

        public bool ForceReconnectAtStartup
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameForceReconnectAtStartup, ConfDefaultForceReconnectAtStartup); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameForceReconnectAtStartup, value);
                SaveConfiguration();
            }
        }

        public bool PreloadMainWindow
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNamePreloadMainWindow, ConfDefaultPreloadMainWindow); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNamePreloadMainWindow, value);
                SaveConfiguration();
            }
        }

        public bool SendSystemUpdateNotifications
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameSendSystemUpdateNotifications, ConfDefaultSendSystemUpdateNotifications); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameSendSystemUpdateNotifications, value);
                SaveConfiguration();
            }
        }

        public bool ShowHumandReadableErrors
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameShowHumandReadableErrors, ConfDefaultShowHumandReadableErrors); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameShowHumandReadableErrors, value);
                SaveConfiguration();
            }
        }

        public bool UseWindowsApi
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameUseWindowsApi, ConfDefaultUseWindowsApi); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameUseWindowsApi, value);
                SaveConfiguration();
            }
        }

        public bool UseSpecializedWebDavCheck
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameUseSpecializedWebDavCheck, ConfDefaultUseSpecializedWebDavCheck); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameUseSpecializedWebDavCheck, value);
                SaveConfiguration();
            }
        }

        public bool ShowRestartWebClientButton
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameShowRestartWebClientButton, ConfDefaultShowRestartWebClientButton); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameShowRestartWebClientButton, value);
                SaveConfiguration();
            }
        }

        public bool EnableShowLogMenu
        {
            get { return Helper.GetJsonValue<bool>(this.config, ConfNameEnableShowLogMenu, ConfDefaultEnableShowLogMenu); }
            set
            {
                Helper.SetJsonValue(this.config, ConfNameEnableShowLogMenu, value);
                SaveConfiguration();
            }
        }


        public ApplicationOptions()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            this.appKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\" + Branding.RegistryKeyName);
            this.runKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            this.isFirstStart = (!File.Exists(NetworkDriveCollection.GetDefaultDrivesStoragePath()) && !File.Exists(Path.Combine(Branding.AppDataPath, ConfigFileName)));
            this.log.Info("Is first start = " + this.isFirstStart);
            this.log.Info("Drives configured = " + NetworkDriveCollection.DrivesConfigured());
            
            ReadConfiguration();

            //TODO (SD-112) re-enable system notifications (temporary fix, takes long time but has almost no effect)
            this.SendSystemUpdateNotifications = false;
        }


        private void SetDefaultConfiguration()
        {
            this.config = new JObject();
            this.config.Add(ConfNameUseAutoUpdate, this.AllowAutoUpdate && ConfDefaultUseAutoUpdate);
            this.config.Add(ConfNameEnableBetaVersions, ConfDefaultEnableBetaVersions);
            this.config.Add(ConfNameSelectedLanguage, ConfDefaultSelectedLanguage);
            this.config.Add(ConfNameOnlyReconnectActiveDrives, ConfDefaultOnlyReconnectActiveDrives);
            this.config.Add(ConfNameForceReconnectAtStartup, ConfDefaultForceReconnectAtStartup);
            this.config.Add(ConfNamePreloadMainWindow, ConfDefaultPreloadMainWindow);
            this.config.Add(ConfNameSendSystemUpdateNotifications, ConfDefaultSendSystemUpdateNotifications);
            this.config.Add(ConfNameShowHumandReadableErrors, ConfDefaultShowHumandReadableErrors);
            this.config.Add(ConfNameUseWindowsApi, ConfDefaultUseWindowsApi);
            this.config.Add(ConfNameUseSpecializedWebDavCheck, ConfDefaultUseSpecializedWebDavCheck);
            this.config.Add(ConfNameShowRestartWebClientButton, ConfDefaultShowRestartWebClientButton);
            this.config.Add(ConfNameEnableShowLogMenu, ConfDefaultEnableShowLogMenu);
        }
        private void MigrateConfiguration()
        {
            try
            {
                string userConfigFile = FindMostRecentConfigFile();

                if (string.IsNullOrEmpty(userConfigFile))
                {
                    this.log.Warn("Old configuration not found. Using default configuration.");
                    return;
                }

                this.log.Info("Migrate configuration from \"" + userConfigFile + "\"");
                XDocument doc = XDocument.Parse(File.ReadAllText(userConfigFile, Encoding.UTF8));
                XElement settings = doc.Element("configuration").Element("userSettings").Element("Share2NetDrive.Properties.Settings");

                foreach(XElement setting in settings.Descendants("setting"))
                {
                    XAttribute name = setting.Attribute("name");
                    if (name != null)
                    {
                        switch (name.Value)
                        {
                            case ConfNameSelectedLanguage:
                                this.config[name.Value] = setting.Element("value").Value;
                                break;

                            case ConfNameUseAutoUpdate:
                            case ConfNameEnableBetaVersions:
                            case ConfNameOnlyReconnectActiveDrives:
                            case ConfNameForceReconnectAtStartup:
                            case ConfNameSendSystemUpdateNotifications:
                                this.config[name.Value] = bool.Parse(setting.Element("value").Value);
                                break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                this.log.Error(ex.GetType().Name + " during configuration migration: " + ex.Message, ex);
            }
        }
        private string FindMostRecentConfigFile()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Concat_AG");
            if (!Directory.Exists(dir))
                return null;

            string appDir = GetMostRecentDir(dir, "Share2NetDrive.exe*");
            if (string.IsNullOrEmpty(appDir))
                return null;

            string versionDir = GetMostRecentDir(appDir, "1.*");
            if (string.IsNullOrEmpty(versionDir))
                return null;

            string confDir = Path.Combine(versionDir, "user.config");
            if (File.Exists(confDir))
                return confDir;
            else
                return null;
        }
        private string GetMostRecentDir(string path, string searchPattern)
        {
            string[] dirs = Directory.GetDirectories(path, searchPattern);

            DateTime lastWrite = DateTime.MinValue;
            string mostRecentDir = null;

            foreach (string dir in dirs)
            {
                DirectoryInfo info = new DirectoryInfo(dir);
                if (info.LastWriteTime > lastWrite)
                {
                    lastWrite = info.LastWriteTime;
                    mostRecentDir = dir;
                }
            }

            return mostRecentDir;
        }


        private void ReadConfiguration()
        {
            this.isNewConfiguration = true;

            try
            {
                this.config = Helper.SafeReadConfig<JObject>(Path.Combine(Branding.AppDataPath, ConfigFileName), Path.Combine(Branding.AppDataPath, ConfigBackupFileName));
                if (this.config != null)
                {
                    this.isNewConfiguration = false;
                }
                else
                {
                    this.log.Info("Configuration file not found. Looking for old configuration");
                    SetDefaultConfiguration();
                    MigrateConfiguration();
                }
            }
            catch (Exception ex)
            {
                this.log.Error("Unable to load configuration. Using default configuration.", ex);
                SetDefaultConfiguration();
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                Helper.SafeWriteConfig(Path.Combine(Branding.AppDataPath, ConfigFileName), Path.Combine(Branding.AppDataPath, ConfigBackupFileName), this.config);
            }
            catch (Exception ex)
            {
                this.log.Error(ex.GetType().Name + " while writing configuration: " + ex.Message, ex);
            }
        }


        public string GetInstallerFileName()
        {
            try
            {
                if (this.appKey == null)
                    return null;

                string result = (this.appKey.GetValue("InstalledFrom") as string);
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Could not get installer file name : " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return null;
            }
        }

        public string GetPreconfiguredLicenseKey()
        {
            try
            {
                if (this.appKey == null)
                    return null;

                string result = (this.appKey.GetValue("PreconfiguredLicenseKey") as string);
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Could not get preconfigured license key : " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return null;
            }
        }
    }
}
