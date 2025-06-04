using QuasaroDRV.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;

namespace QuasaroDRV
{
    public static class Branding
    {
        static JObject brandedValues;

        static bool isInitialized = false;
        public static bool IsInitialized { get { return Branding.isInitialized; } }

        static string cachedAppId = "{APPID}";
        public static string AppId { get { return Branding.cachedAppId; } }
        static string cachedApplicationName = "{APPNAME}";
        public static string ApplicationName { get { return Branding.cachedApplicationName; } }
        static string cachedCompanyName = "{COMPANYNAME}";
        public static string CompanyName { get { return Branding.cachedCompanyName; } }
        static int cachedCopyrightYear = 0;
        public static int CopyrightYear { get { return Branding.cachedCopyrightYear; } }

        static string cachedSupportUrl = "{SUPPORTURL}";
        public static string SupportUrl { get { return Branding.cachedSupportUrl; } }
        static string cachedTelephone = "{TELEPHONE}";
        public static string Telephone { get { return Branding.cachedTelephone; } }
        static string cachedEMail = "{EMAIL}";
        public static string EMail { get { return Branding.cachedEMail; } }

        static string cachedAppDataPath;
        public static string AppDataPath { get { return Branding.cachedAppDataPath; } }

        static string cachedRegistryKeyName;
        public static string RegistryKeyName { get { return Branding.cachedRegistryKeyName; } }

        public static string MainWindowTitle { get { return BrandString(Strings.MainWindowTitleBrandable); } }
        public static string NotifyIconBackgroundHintText { get { return BrandString(Strings.NotifyIconBackgroundHintTextBrandable); } }

        public static string SettingsWindowTitle { get { return BrandString(Strings.SettingsWindowTitleBrandable); } }
        public static string SettingsWindowAutoStartTitle { get { return BrandString(Strings.SettingsWindowAutoStartTitleBrandable); } }

        public static string AboutWindowCopyrightTitle { get { return BrandString(Strings.AboutWindowCopyrightTitleBrandable); } }

        public static string AutoUpdateTitle { get { return BrandString(Strings.AutoUpdateTitleBrandable); } }
        public static string AutoUpdateUpdateToVersionMessage { get { return BrandString(Strings.AutoUpdateUpdateToVersionMessageBrandable); } }
        public static string AutoUpdateUpToDateMessage { get { return BrandString(Strings.AutoUpdateUpToDateMessageBrandable); } }

        public static string MessageSettingsRestartRequired { get { return BrandString(Strings.MessageSettingsRestartRequiredBrandable); } }
        public static string MessageApplicationExpired { get { return BrandString(Strings.MessageApplicationExpiredBrandable); } }
        public static string MessageUnhandledException { get { return BrandString(Strings.MessageUnhandledExceptionBrandable); } }

        public static string InitialConfigWindowTitle { get { return BrandString(Strings.InitialConfigWindowTitleBrandable); } }

        static string cachedUpdateUrl = null;
        public static string UpdateUrl { get { return Branding.cachedUpdateUrl; } }
        static string cachedUpdateBetaUrl = null;
        public static string UpdateBetaUrl { get { return Branding.cachedUpdateBetaUrl; } }
        public static bool IsUpdateConfigured { get { return (!string.IsNullOrEmpty(Branding.cachedUpdateUrl) || !string.IsNullOrEmpty(Branding.cachedUpdateBetaUrl)); } }


        static Branding()
        {
            // cache informations
            try
            {
                Branding.isInitialized = false;
                // checking can speed up the designer as no exception needs to be thrown
                if (!File.Exists(Path.Combine(App.Current.ApplicationPath, "Strings.dat")))
                {
                    Branding.cachedAppId = "Quasaro-Drive";
                    Branding.cachedApplicationName = "Quasaro Drive";
                    Branding.cachedCompanyName = "StieCon IT-Consulting GmbH";
                    Branding.cachedCopyrightYear = 2025;

                    Branding.cachedSupportUrl = "quasaro.de";
                    Branding.cachedTelephone = "+49 6158 747700";
                    Branding.cachedEMail = "support@quasaro.de";

                    Branding.cachedUpdateUrl = "https://license.onstash.de/api/v1/currentSoftwareVersion/Quasaro-Drive";
                    Branding.cachedUpdateBetaUrl = "https://license.onstash.de/api/v1/currentSoftwareVersion/Quasaro-Drive-Beta";

                    Branding.cachedAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ReplaceInvalidPathChars(Branding.ApplicationName));
                    Branding.cachedRegistryKeyName = "StieCon IT-Consulting GmbH\\Quasaro-Drive";
                    Branding.isInitialized = true;

                    return;
                }

                Branding.brandedValues = JObject.Parse(File.ReadAllText(Path.Combine(App.Current.ApplicationPath, "Strings.dat")));

                Branding.cachedAppId = ReadBrandedString("AppId");
                Branding.cachedApplicationName = ReadBrandedString("ApplicationName");
                Branding.cachedCompanyName = ReadBrandedString("CompanyName");
                Branding.cachedCopyrightYear = ReadBrandedInt("CopyrightYear");

                Branding.cachedSupportUrl = ReadBrandedString("SupportUrl");
                Branding.cachedTelephone = ReadBrandedString("Telephone");
                Branding.cachedEMail = ReadBrandedString("EMail");

                Branding.cachedUpdateUrl = ReadBrandedString("UpdateUrl");
                Branding.cachedUpdateBetaUrl = ReadBrandedString("UpdateBetaUrl");

                Branding.cachedAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ReplaceInvalidPathChars(Branding.ApplicationName));
                Branding.cachedRegistryKeyName = ReadBrandedString("RegistryKey");

                Branding.isInitialized = true;
            }
            catch { }
        }
        static string ReadBrandedString(string name)
        {
            string raw = Branding.brandedValues[name].Value<string>();
            byte[] nameBuffer = Encoding.UTF8.GetBytes(name.ToUpper());
            byte[] dataBuffer = Convert.FromBase64String(raw);
            for (int i = 0; i < dataBuffer.Length; i++)
                dataBuffer[i] = (byte)(dataBuffer[i] ^ nameBuffer[i % nameBuffer.Length]);
            return Encoding.UTF8.GetString(dataBuffer);
        }
        static int ReadBrandedInt(string name)
        {
            int raw = Branding.brandedValues[name].Value<int>();
            byte[] nameBuffer = Encoding.UTF8.GetBytes(name.ToUpper());
            byte[] dataBuffer = BitConverter.GetBytes(raw);
            for (int i = 0; i < nameBuffer.Length; i++)
                dataBuffer[i % dataBuffer.Length] = (byte)(nameBuffer[i] ^ dataBuffer[i % dataBuffer.Length]);
            return BitConverter.ToInt32(dataBuffer, 0);
        }

        public static string ExportBrandedString(string name, string value)
        {
            byte[] nameBuffer = Encoding.UTF8.GetBytes(name.ToUpper());
            byte[] dataBuffer = Encoding.UTF8.GetBytes(value);
            for (int i = 0; i < dataBuffer.Length; i++)
                dataBuffer[i] = (byte)(dataBuffer[i] ^ nameBuffer[i % nameBuffer.Length]);
            return Convert.ToBase64String(dataBuffer);
        }
        public static int ExportBrandedInt(string name, int value)
        {
            byte[] nameBuffer = Encoding.UTF8.GetBytes(name.ToUpper());
            byte[] dataBuffer = BitConverter.GetBytes(value);
            for (int i = 0; i < nameBuffer.Length; i++)
                dataBuffer[i % dataBuffer.Length] = (byte)(nameBuffer[i] ^ dataBuffer[i % dataBuffer.Length]);
            return BitConverter.ToInt32(dataBuffer, 0);
        }


        public static string BrandString(string str)
        {
            return str.Replace("{APPNAME}", Branding.ApplicationName).Replace("{VENDORNAME}", Branding.CompanyName).Replace("{COPYRIGHTYEAR}", Branding.cachedCopyrightYear.ToString());
        }


        public static string ReplaceInvalidPathChars(string str)
        {
            foreach (char c in Path.GetInvalidPathChars())
                str = str.Replace(c, '_');
            foreach (char c in Path.GetInvalidFileNameChars())
                str = str.Replace(c, '_');
            return str;
        }
        public static string ReplaceInvalidRegistryChars(string str)
        {
            return ReplaceInvalidPathChars(str);
        }
    }
}
