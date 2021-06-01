using log4net;
using Microsoft.Win32;
using System;

namespace QuasaroDRV.Configuration
{
    public enum SupportedOfficeVersions
    {
        None = 0,
        Version12 = 12,
        Version13 = 13,
        Version14 = 14,
        Version15 = 15,
        Version16 = 16
    }


    public class MicrosoftOfficeConfiguration
    {
        ILog log;


        public bool OfficeAvailable { get { return versionRoot != null; } }

        SupportedOfficeVersions version = SupportedOfficeVersions.None;
        public SupportedOfficeVersions OfficeVersion { get { return this.version; } }
        RegistryKey versionRoot = null;


        public MicrosoftOfficeConfiguration()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            // find installed version (use max version first)
            RegistryKey officeKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Office");
            if (officeKey != null)
            {
                if ((this.versionRoot = officeKey.OpenSubKey("16.0", true)) != null) LoadVersion16();
                else if ((this.versionRoot = officeKey.OpenSubKey("15.0", true)) != null) LoadVersion15();
                else if ((this.versionRoot = officeKey.OpenSubKey("14.0", true)) != null) LoadVersion14();
                else if ((this.versionRoot = officeKey.OpenSubKey("13.0", true)) != null) LoadVersion13();
                else if ((this.versionRoot = officeKey.OpenSubKey("12.0", true)) != null) LoadVersion12();
                else log.Warn("No supported Office version detected");
                officeKey.Close();
            }
        }


        public void LoadVersion12()
        {
            this.version = SupportedOfficeVersions.Version12;
            log.Info("MicrosoftOfficeConfiguration: " + this.version);
        }

        public void LoadVersion13()
        {
            this.version = SupportedOfficeVersions.Version13;
            log.Info("MicrosoftOfficeConfiguration: " + this.version);
        }

        public void LoadVersion14()
        {
            this.version = SupportedOfficeVersions.Version14;
            log.Info("MicrosoftOfficeConfiguration: " + this.version);
        }

        public void LoadVersion15()
        {
            this.version = SupportedOfficeVersions.Version15;
            log.Info("MicrosoftOfficeConfiguration: " + this.version);
        }

        public void LoadVersion16()
        {
            this.version = SupportedOfficeVersions.Version16;
            log.Info("MicrosoftOfficeConfiguration: " + this.version);
        }


        public bool ExcelDisableProtectedViewFromInternetSource
        {
            get
            {
                return ReadBoolValue(this.versionRoot, "Excel\\Security\\ProtectedView", "DisableInternetFilesInPV", false);
            }
            set
            {
                SetValue(this.versionRoot, "Excel\\Security\\ProtectedView", "DisableInternetFilesInPV", value);
            }
        }

        public bool WordDisableProtectedViewFromInternetSource
        {
            get
            {
                return ReadBoolValue(this.versionRoot, "Word\\Security\\ProtectedView", "DisableInternetFilesInPV", false);
            }
            set
            {
                SetValue(this.versionRoot, "Word\\Security\\ProtectedView", "DisableInternetFilesInPV", value);
            }
        }

        public bool PowerPointDisableProtectedViewFromInternetSource
        {
            get
            {
                return ReadBoolValue(this.versionRoot, "PowerPoint\\Security\\ProtectedView", "DisableInternetFilesInPV", false);
            }
            set
            {
                SetValue(this.versionRoot, "PowerPoint\\Security\\ProtectedView", "DisableInternetFilesInPV", value);
            }
        }

        private bool ReadBoolValue(RegistryKey root, string subPath, string valueName, bool defaultValue)
        {
            RegistryKey key = root.OpenSubKey(subPath);
            if (key == null)
                return defaultValue;
            return ((int)key.GetValue(valueName, defaultValue)) != 0;
        }

        private void SetValue(RegistryKey root, string subPath, string valueName, bool value)
        {
            try
            {
                RegistryKey key = root.CreateSubKey(subPath);
                if (key != null)
                    key.SetValue(valueName, (value ? 1 : 0), RegistryValueKind.DWord);
                else
                    log.Error("Could open RegistryKey \"" + root.Name + "\\" + subPath + "\"");
            }
            catch (Exception ex)
            {
                log.Error("Could not set RegistryKey \"" + root.Name + "\\" + subPath + "\\" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }
    }
}
