using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace QuasaroDRV.Configuration
{
    public class ConfigurationSuffix14
    {
        public const char SuffixInitiator = '@'; // no regex special chars allowed
        public const char ShowConfigSymbol = '!';
        public const char LicenseKeySeparator = '$';
        public const char SuffixSeparator = '#'; // add to EscapeString
        public const char NameShorthand = '=';
        public const char DriveWithPrefix = '+';
        public const char DriveWithoutPrefix = '-';
        public const char DriveLabelSeparator = '~'; // add to EscapeString

        string licenseKey;
        public string LicenseKey
        {
            get
            {
                if (this.licenseKey == null) return "";
                else return this.licenseKey;
            }
            set { this.licenseKey = value; }
        }
        public bool HasLicenseKey { get { return !string.IsNullOrEmpty(this.licenseKey); } }

        List<DriveConfiguration14> drives;
        public DriveConfiguration14[] Drives { get { return this.drives.ToArray(); } }
        public bool HasDriveConfiguration { get { return (this.drives.Count > 0); } }

        bool showConfigWindow;
        public bool ShowConfigWindow
        {
            get { return this.showConfigWindow; }
            set { this.showConfigWindow = value; }
        }

        bool disableProtectedViewExcel;
        public bool DisableProtectedViewExcel
        {
            get { return this.disableProtectedViewExcel; }
            set { this.disableProtectedViewExcel = value; }
        }
        bool disableProtectedViewWord;
        public bool DisableProtectedViewWord
        {
            get { return this.disableProtectedViewWord; }
            set { this.disableProtectedViewWord = value; }
        }
        bool disableProtectedViewPowerPoint;
        public bool DisableProtectedViewPowerPoint
        {
            get { return this.disableProtectedViewPowerPoint; }
            set { this.disableProtectedViewPowerPoint = value; }
        }
        public bool HasOfficeConfiguration
        {
            get { return (this.disableProtectedViewExcel || this.disableProtectedViewWord || this.disableProtectedViewPowerPoint); }
        }


        public ConfigurationSuffix14()
        {
            // set default values
            this.licenseKey = null;
            this.drives = new List<DriveConfiguration14>();
            this.disableProtectedViewExcel = false;
            this.disableProtectedViewWord = false;
            this.disableProtectedViewPowerPoint = false;
        }

        public ConfigurationSuffix14(string str)
            : this()
        {
            // normalize suffix string
            if (str.StartsWith(SuffixInitiator.ToString()))
                str = str.Substring(1);
            if (str.StartsWith(ShowConfigSymbol.ToString()))
            {
                this.showConfigWindow = true;
                str = str.Substring(1);
            }
            else
            {
                this.showConfigWindow = false;
            }

            if (str.StartsWith(LicenseKeySeparator.ToString()))
            {
                int licenseKeyEnd = str.IndexOf(LicenseKeySeparator, 1);
                if (licenseKeyEnd == -1)
                {
                    this.licenseKey = UnescapeString(str.Substring(1));
                    str = "";
                }
                else
                {
                    this.licenseKey = UnescapeString(str.Substring(1, licenseKeyEnd - 1));
                    str = str.Substring(licenseKeyEnd + 1);
                }
            }

            string[] parts = str.Split(SuffixSeparator);
            // no configuration set?
            if (parts.Length == 0)
                return;

            // save server address
            string prefix = UnescapeString(parts[0]);
            if (prefix.EndsWith(NameShorthand.ToString()))
                prefix = prefix.Substring(0, prefix.Length - 1) + "/webdav/";
            if (parts.Length == 1)
                return;

            // parse drive configuration
            for (int i = 1; i < parts.Length; i++)
            {
                DriveConfiguration14 config = DriveConfiguration14.Parse(prefix, parts[i]);
                if (config != null)
                    this.drives.Add(config);
            }

            // parse office configuration
            if (parts.Length > (1 + this.drives.Count))
            {
                foreach (char c in parts[1 + this.drives.Count].ToUpper())
                {
                    if (c == 'E') this.disableProtectedViewExcel = true;
                    else if (c == 'W') this.disableProtectedViewWord = true;
                    else if (c == 'P') this.disableProtectedViewPowerPoint = true;
                }
            }
        }


        public void AddDrive(DriveConfiguration14 drive)
        {
            this.drives.Add(drive);
        }

        public void RemoveDrive(DriveConfiguration14 drive)
        {
            this.drives.Remove(drive);
        }

        protected string GetPrefix()
        {
            if (this.drives.Count == 0)
                return "";
            else if (this.drives.Count == 1)
            {
                return this.drives[0].ShortRemoteAddress.Substring(0, GetPathEndPos(this.drives[0].ShortRemoteAddress));
            }
            else
            {
                string[] prefixes = GeneratePrefixes();
                return GetBestPrefix(prefixes);
            }
        }

        protected int GetPathEndPos(string url)
        {
            if (url.StartsWith(@"\\"))
            {
                // remove double backslash indicator
                int domainEnd = url.LastIndexOf('\\');
                if (domainEnd == -1)
                    return url.Length;
                else
                    return domainEnd + 1;
            }
            else
            {
                // extract path
                int pathStart = url.LastIndexOf("/");
                if (pathStart == -1)
                    return url.Length;
                else
                    return pathStart + 1;
            }
        }

        protected string GetBestPrefix(string[] prefixes)
        {
            if (prefixes.Length == 0)
                return "";

            string bestPrefix = prefixes[0];
            int maxLoss = -1;

            foreach (string prefix in prefixes)
            {
                int escapedLength = EscapeString(prefix).Length;
                // the prefix needs to be saved itself and therefore reduces the loss
                int loss = -escapedLength;
                foreach (DriveConfiguration14 drive in this.drives)
                {
                    // wherever the prefix matches, the length would not be saved and can therefore be omitted
                    if (drive.ShortRemoteAddress.StartsWith(prefix))
                    {
                        loss += escapedLength;
                    }
                }

                // is this prefix better?
                if (maxLoss < 0 || loss > maxLoss)
                {
                    bestPrefix = prefix;
                    maxLoss = loss;
                }
            }

            return bestPrefix;
        }

        protected string[] GeneratePrefixes()
        {
            // find all subsets of the drives list to list all possible prefixes
            int combinationCount = (int)(Math.Pow(2, this.drives.Count) - 1);
            List<string> prefixes = new List<string>(combinationCount);
            List<DriveConfiguration14> drives = new List<DriveConfiguration14>();
            for (int i = 1; i <= combinationCount; i++)
            {
                // the binary representation of i describes the in/out pattern of the drives
                // every digit corresponds to a drive (0 = out, 1 = in)
                drives.Clear();
                int num = i;
                for (int j = 0; j < this.drives.Count; j++)
                {
                    if ((num & 1) == 1)
                        drives.Add(this.drives[j]);
                    num >>= 1;
                }
                string prefix = GeneratePrefix(drives.ToArray());
                prefixes.Add(prefix);
            }
            return prefixes.ToArray();
        }

        protected string GeneratePrefix(DriveConfiguration14[] drives)
        {
            // find longest string which is a prefix for all drives in the array
            // start with full first string
            string prefix = drives[0].ShortRemoteAddress;
            for (int i = 1; i < drives.Length; i++)
            {
                // now cut away all chars which differ from other drives
                for (int j = 0; j < Math.Min(prefix.Length, drives[i].ShortRemoteAddress.Length); j++)
                {
                    if (prefix[j] != drives[i].ShortRemoteAddress[j])
                    {
                        prefix = prefix.Substring(0, j);
                        break;
                    }
                }

                if (prefix.Length == 0)
                    break;
            }
            return prefix;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SuffixInitiator);
            if (this.showConfigWindow)
                sb.Append(ShowConfigSymbol);
            if (this.HasLicenseKey)
                sb.Append(LicenseKeySeparator).Append(EscapeString(this.licenseKey)).Append(LicenseKeySeparator);

            string prefix = GetPrefix();
            if (prefix.EndsWith("/webdav/"))
            {
                sb.Append(EscapeString(prefix.Substring(0, prefix.Length - 8))).Append(NameShorthand);
            }
            else
            {
                sb.Append(EscapeString(prefix));
            }

            foreach (DriveConfiguration14 drive in this.drives)
            {
                sb.Append(SuffixSeparator);
                sb.Append(drive.ToString(prefix));
            }

            if (this.HasOfficeConfiguration)
            {
                sb.Append(SuffixSeparator);
                if (this.disableProtectedViewExcel) sb.Append("E");
                if (this.disableProtectedViewWord) sb.Append("W");
                if (this.disableProtectedViewPowerPoint) sb.Append("P");
            }

            return sb.ToString();
        }


        public static string EscapeString(string str)
        {
            return str.Replace("%", "%%").Replace(":", "%D").Replace("/", "%S").Replace("\\", "%B").Replace("~", "%T").Replace("#", "%H").Replace("@", "%A").Replace(" ", "%W").Replace("$", "%M").Replace("\"", "%Q");
        }

        protected static string UnescapeChar(char escaped)
        {
            switch (char.ToUpper(escaped))
            {
                case '%': return "%";
                case 'D': return ":";
                case 'S': return "/";
                case 'B': return "\\";
                case 'T': return "~";
                case 'H': return "#";
                case 'A': return "@";
                case 'W': return " ";
                case 'M': return "$";
                case 'Q': return "\"";
                default: return "%" + escaped;
            }
        }

        public static string UnescapeString(string str)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '%' && (str.Length > i + 1))
                {
                    sb.Append(UnescapeChar(str[i + 1]));
                    i++;
                }
                else
                    sb.Append(str[i]);
            }
            return sb.ToString();
        }


        public static ConfigurationSuffix14 Parse(string str)
        {
            try
            {
                return new ConfigurationSuffix14(str);
            }
            catch
            {
                return null;
            }
        }

        public static ConfigurationSuffix14 FromFileName(string filename)
        {
            Regex pattern = new Regex("^.*?" + SuffixInitiator + "(.*)$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (filename.EndsWith(".msi"))
                filename = filename.Substring(0, filename.Length - 4);
            Match m = pattern.Match(filename);
            if (m.Success)
            {
                return Parse(m.Groups[1].Value);
            }
            else
            {
                return null;
            }
        }
    }

    public enum DriveConfigurationType14
    {
        Invalid = -1,
        Custom = 0,
        Share2NetShared = 1,
        Share2NetMy = 2,
        Share2NetGlobal = 3
    }

    public class DriveConfiguration14
    {
        string remoteAddress;
        public string RemoteAddress
        {
            get { return this.remoteAddress; }
            set { this.remoteAddress = value; }
        }
        public string ShortRemoteAddress
        {
            get
            {
                if (this.remoteAddress.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                    return this.remoteAddress.Substring(8);
                else
                    return this.remoteAddress;
            }
            set
            {
                if (!value.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) && !value.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) && !value.StartsWith("\\\\", StringComparison.InvariantCultureIgnoreCase))
                    this.remoteAddress = "https://" + value;
                else
                    this.remoteAddress = value;
            }
        }

        char driveLetter;
        public char DriveLetter
        {
            get { return this.driveLetter; }
            set { this.driveLetter = value; }
        }

        string driveLabel;
        public string DriveLabel
        {
            get { return this.driveLabel; }
            set { this.driveLabel = value; }
        }

        DriveConfigurationType14 type;
        public DriveConfigurationType14 Type
        {
            get { return this.type; }
            set { this.type = value; }
        }


        public DriveConfiguration14()
        {
            this.remoteAddress = "";
            this.driveLetter = 'Z';
            this.driveLabel = "";
            this.type = DriveConfigurationType14.Custom;
        }

        public DriveConfiguration14(string remoteAddress, char driveLetter, string driveLabel)
        {
            this.ShortRemoteAddress = remoteAddress;
            this.driveLetter = char.ToUpper(driveLetter);

            string remoteName = GetRemoteName(this.remoteAddress);
            string dirName = (remoteName.Length > 0 ? remoteName.Substring(0, 1).ToUpper() + remoteName.Substring(1) : "");
            this.driveLabel = (driveLabel == null ? null : driveLabel.Replace(ConfigurationSuffix14.NameShorthand.ToString(), dirName));
        }

        private string GetRemoteName(string remoteAddress)
        {
            int lastSlash = remoteAddress.Replace('\\', '/').LastIndexOf('/');
            if (lastSlash == -1)
                return remoteAddress;
            else
                return remoteAddress.Substring(lastSlash + 1);
        }

        public string ToString(string prefix)
        {
            string labelString = "";
            if (!string.IsNullOrEmpty(this.driveLabel))
            {
                string remoteName = GetRemoteName(this.remoteAddress);
                string dirName = (remoteName.Length > 0 ? remoteName.Substring(0, 1).ToUpper() + remoteName.Substring(1) : "");
                labelString = ConfigurationSuffix14.DriveLabelSeparator.ToString() + ConfigurationSuffix14.EscapeString(this.driveLabel.Replace(dirName, ConfigurationSuffix14.NameShorthand.ToString()));
            }

            if (this.ShortRemoteAddress.StartsWith(prefix))
            {
                return driveLetter.ToString() + ConfigurationSuffix14.DriveWithPrefix.ToString() + ConfigurationSuffix14.EscapeString(this.ShortRemoteAddress.Substring(prefix.Length)) + labelString;
            }
            else
            {
                return driveLetter.ToString() + ConfigurationSuffix14.DriveWithoutPrefix.ToString() + ConfigurationSuffix14.EscapeString(this.ShortRemoteAddress) + labelString;
            }
        }

        public static DriveConfiguration14 Parse(string prefix, string str)
        {
            if (str.Length < 2)
                return null;

            DriveConfiguration14 config = null;
            string[] parts = str.Substring(2).Split(new char[] { ConfigurationSuffix14.DriveLabelSeparator }, 2);
            if (str[1] == ConfigurationSuffix14.DriveWithoutPrefix)
                config = new DriveConfiguration14(ConfigurationSuffix14.UnescapeString(parts[0]), char.ToUpper(str[0]), (parts.Length == 2 ? ConfigurationSuffix14.UnescapeString(parts[1]) : ""));
            else if (str[1] == ConfigurationSuffix14.DriveWithPrefix)
                config = new DriveConfiguration14(prefix + ConfigurationSuffix14.UnescapeString(parts[0]), char.ToUpper(str[0]), (parts.Length == 2 ? ConfigurationSuffix14.UnescapeString(parts[1]) : ""));
            else
                return null;

            if (config.driveLetter < 'A' || config.driveLetter > 'Z')
                return null;
            return config;
        }
    }
}
