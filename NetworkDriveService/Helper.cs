using log4net;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuasaroDRV
{
    abstract class HumanReadableException : Exception
    {
        public abstract string HumanReadableMessage { get; }


        public HumanReadableException()
            : base()
        { }

        public HumanReadableException(string message)
            : base(message)
        { }
    }

    delegate void WebRequestAction(WebRequest request);
    delegate void WebClientAction(WebClient client);


    static class Helper
    {
        static ILog log;
        static Helper()
        {
            Helper.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public static string GetDomain(string url)
        {
            if (url.StartsWith(@"\\"))
            {
                // remove double backslash indicator
                url = url.Substring(2);
                int domainEnd = url.IndexOf('\\');
                if (domainEnd == -1)
                    return url;
                else
                    return url.Substring(0, domainEnd);
            }
            else
            {
                // remove protocol
                int protEnd = url.IndexOf("://");
                if (protEnd != -1)
                    url = url.Substring(protEnd + 3);
            }

            // remove path
            int pathStart = url.IndexOf("/");
            if (pathStart != -1)
                url = url.Substring(0, pathStart);

            return url;
        }

        public static Regex PrepareWildcardPattern(string pattern)
        {
            // build regex-pattern from wildcard-string
            // the only regex-character which is allowed in urls is '.' -> replace with "\."
            // '*' matches any passage -> ".*"
            // '?' matches any single character -> "."
            // match must be the full string or nothing -> surround with ^ ... $
            return new Regex("^" + pattern.Replace("\\", "\\\\").Replace(".", "\\.").Replace("*", ".*").Replace("?", ".") + "$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }


        public static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            // taken from http://stackoverflow.com/questions/26260654/wpf-converting-bitmap-to-imagesource
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }


        public static bool SafeExec(string Tag, ILog log, bool showMessageBox, Action action)
        {
            try
            {
                action.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    Exception innerException = (ex.InnerException == null ? ex : ex.InnerException);

                    string message;
                    if (Tag == null)
                        message = string.Format(Properties.Strings.MessageGenericError, innerException.GetType().Name, innerException.Message);
                    else
                        message = string.Format(Properties.Strings.MessageGenericErrorSource, Tag, innerException.GetType().Name, innerException.Message);
                    if (log != null)
                        log.Error(message, innerException);
                    if (showMessageBox)
                        MessageBox.Show(message + "\n\n" + innerException.StackTrace, Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { } // the error handler must not fail

                return false;
            }
        }
        public static bool SafeExec(ILog log, bool showMessageBox, Action action)
        {
            return SafeExec(null, log, showMessageBox, action);
        }
        public static bool SafeExec(ILog log, Action action)
        {
            return SafeExec(null, log, false, action);
        }
        public static bool SafeExec(Action action)
        {
            return SafeExec(null, null, false, action);
        }


        public static string GetUserMessage(Exception ex)
        {
            HumanReadableException hrException = (ex as HumanReadableException);
            if (hrException != null) return hrException.HumanReadableMessage;
            else return ex.Message;
        }


        public static string UnprotectData(string str)
        {
            if (str.StartsWith("###-"))
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(str.Substring(4)), null, DataProtectionScope.LocalMachine));
            }
            else
            {
                return str;
            }
        }
        public static string ProtectData(string raw)
        {
            return "###-" + Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(raw), null, DataProtectionScope.LocalMachine));
        }


        public static string EncryptValue(string val, string password)
        {
            // https://stackoverflow.com/questions/202011/encrypt-and-decrypt-a-string

            if (val == null)
                return null;

            RijndaelManaged alg = new RijndaelManaged();
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, Convert.FromBase64String("zAZgdKjfD7Crer+syV1jCUCHgO0So4+0iGT0NU9v3fa/FZUPFDpleRAAo7JjqobFGeYvCDmaTih3q+xE6tphPA=="));
            alg.Key = key.GetBytes(alg.KeySize / 8);
            ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV);
            string result = null;
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
                memStream.Write(alg.IV, 0, alg.IV.Length);
                using (CryptoStream cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                {
                    byte[] data = Encoding.UTF8.GetBytes(val);
                    cryptoStream.Write(BitConverter.GetBytes(data.Length), 0, sizeof(int));
                    cryptoStream.Write(data, 0, data.Length);
                }
                result = Convert.ToBase64String(memStream.ToArray());
            }
            if (alg != null)
                alg.Clear();
            return result;
        }
        public static string DecryptValue(string val, string password)
        {
            // https://stackoverflow.com/questions/202011/encrypt-and-decrypt-a-string

            if (val == null)
                return null;

            RijndaelManaged alg = new RijndaelManaged();
            Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, Convert.FromBase64String("zAZgdKjfD7Crer+syV1jCUCHgO0So4+0iGT0NU9v3fa/FZUPFDpleRAAo7JjqobFGeYvCDmaTih3q+xE6tphPA=="));
            alg.Key = key.GetBytes(alg.KeySize / 8);
            string result = null;
            using (MemoryStream memStream = new MemoryStream(Convert.FromBase64String(val)))
            {
                byte[] buffer = new byte[4];
                memStream.Read(buffer, 0, 4);
                byte[] iv = new byte[BitConverter.ToInt32(buffer, 0)];
                memStream.Read(iv, 0, iv.Length);
                alg.IV = iv;
                ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV);
                using (CryptoStream cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                {
                    cryptoStream.Read(buffer, 0, 4);
                    byte[] data = new byte[BitConverter.ToInt32(buffer, 0)];
                    cryptoStream.Read(data, 0, data.Length);
                    result = Encoding.UTF8.GetString(data);
                }
            }
            if (alg != null)
                alg.Clear();
            return result;
        }


        public static string GeneratePasswordStore(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                Guid guid = Guid.NewGuid();
                string str = guid.ToString() + ":" + password;
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
                return guid.ToString() + ":" + Convert.ToBase64String(hash);
            }
        }
        public static bool CheckPasswordStore(string store, string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string[] parts = store.Split(new char[] { ':' }, 2);
                string str = parts[0] + ":" + password;
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
                return store == (parts[0] + ":" + Convert.ToBase64String(hash));
            }
        }


        public static T SafeReadConfig<T>(string file, string backupFile) where T : JToken
        {
            if (File.Exists(file))
            {
                try
                {
                    // load storage file if it exists and move to backup file after success
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    T config = (T)JToken.Parse(content);
                    File.Copy(file, backupFile, true);
                    return config;
                }
                catch (Exception ex)
                {
                    if (File.Exists(backupFile))
                    {
                        log.Warn(ex.GetType().Name + " while loading config data from \"" + Path.GetFileName(file) + "\". Loading backup instead");

                        // loading storage file failed? try loading backup...
                        string content = File.ReadAllText(backupFile, Encoding.UTF8);
                        T config = (T)JToken.Parse(content);
                        File.Copy(backupFile, file, true);
                        return config;
                    }
                    else
                    {
                        // no backup file existent... just pass exception to caller
                        throw;
                    }
                }
            }
            else
            {
                if (File.Exists(backupFile))
                {
                    log.Warn("Config data file \"" + file + "\" does not exist. Loading backup instead");

                    // storage file does not exist? load backup
                    string content = File.ReadAllText(backupFile, Encoding.UTF8);
                    T config = (T)JToken.Parse(content);
                    File.Copy(backupFile, file, true);
                    return config;
                }
                else
                {
                    // neither storage nor backup exists: no configuration yet available
                    return null;
                }
            }
        }

        public static void SafeWriteConfig<T>(string file, string backupFile, T token) where T : JToken
        {
            // create backup of current file before writing
            if (File.Exists(file))
                File.Copy(file, backupFile, true);
            // now write new config data
            File.WriteAllText(file, token.ToString(), Encoding.UTF8);
        }


        class ExtendedWebClient : WebClient
        {
            WebRequestAction action;

            public ExtendedWebClient(WebRequestAction action)
            {
                this.action = action;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (action != null)
                    action(request);
                return request;
            }
        }
        public static void PerformWebClientAction(WebClientAction webClientAction, WebRequestAction webRequestAction = null, bool forceSsl3 = false)
        {
            if (!forceSsl3)
            {
                try
                {
                    using (ExtendedWebClient client = new ExtendedWebClient(webRequestAction))
                    {
                        webClientAction(client);
                    }
                }
                catch (WebException ex)
                {
                    log.Error(ex.GetType().Name + ": " + ex.Message + " (" + ex.Status + ") -> Retry with SSL3");
                }
            }

            // now try using ssl3:
            // http://poweredbydotnet.blogspot.de/2012/03/solving-received-unexpected-eof-or-0.html
            SecurityProtocolType oldType = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            try
            {
                using (ExtendedWebClient client = new ExtendedWebClient(webRequestAction))
                {
                    webClientAction(client);
                    ServicePointManager.SecurityProtocol = oldType;
                }
            }
            catch
            {
                ServicePointManager.SecurityProtocol = oldType;
                throw;
            }
        }

        public static byte[] DownloadData(string url, WebRequestAction webRequestAction = null, bool forceSsl3 = false)
        {
            byte[] result = null;
            PerformWebClientAction(new WebClientAction((client) => { result = client.DownloadData(url); }), webRequestAction, forceSsl3);
            return result;
        }

        public static string DownloadString(string url, WebRequestAction webRequestAction = null, bool forceSsl3 = false)
        {
            string result = null;
            PerformWebClientAction(new WebClientAction((client) => { result = client.DownloadString(url); }), webRequestAction, forceSsl3);
            return result;
        }

        public static void DownloadFile(string url, string file, WebRequestAction webRequestAction = null, bool forceSsl3 = false)
        {
            PerformWebClientAction(new WebClientAction((client) => { client.DownloadFile(url, file); }), webRequestAction, forceSsl3);
        }


        public static T GetJsonValue<T>(JObject obj, string valueName)
        {
            return obj[valueName].Value<T>();
        }
        public static T GetJsonValue<T>(JObject obj, string valueName, JToken defaultValue)
        {
            if (obj[valueName] == null)
                obj.Add(valueName, defaultValue);
            return obj[valueName].Value<T>();
        }
        public static void SetJsonValue(JObject obj, string valueName, JToken value)
        {
            if (obj[valueName] == null)
                obj.Add(valueName, value);
            else
                obj[valueName] = value;
        }


        public static bool RegistryValueExists(RegistryKey key, string valueName)
        {
            try
            {
                if (key == null)
                    return false;
                object value = key.GetValue(valueName, null);
                return (value != null);
            }
            catch (Exception ex)
            {
                if (key == null)
                    Helper.log.Warn("Could not access RegistryKey \"" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                else
                    Helper.log.Warn("Could not access RegistryKey \"" + key.Name + "\\" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return false;
            }
        }
        public static bool RegistryReadBool(RegistryKey key, string valueName, bool defaultValue)
        {
            try
            {
                if (key == null)
                    return defaultValue;

                object val = key.GetValue(valueName, defaultValue);
                if (val != null && (val is int))
                    return ((int)val != 0);
                else
                    return defaultValue;
            }
            catch (Exception ex)
            {
                if (key == null)
                    Helper.log.Warn("Could not read RegistryKey \"" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                else
                    Helper.log.Warn("Could not read RegistryKey \"" + key.Name + "\\" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return defaultValue;
            }
        }
        public static void RegistryDeleteValue(RegistryKey key, string valueName)
        {
            try
            {
                if (key != null)
                    key.DeleteValue(valueName, false);
            }
            catch (Exception ex)
            {
                if (key == null)
                    Helper.log.Error("Could not delete RegistryKey \"" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                else
                    Helper.log.Error("Could not delete RegistryKey \"" + key.Name + "\\" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }
        public static void RegistrySetValue(RegistryKey key, string valueName, string value)
        {
            try
            {
                if (key != null)
                    key.SetValue(valueName, value, RegistryValueKind.String);
            }
            catch (Exception ex)
            {
                if (key == null)
                    Helper.log.Error("Could not write RegistryKey \"" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                else
                    Helper.log.Error("Could not write RegistryKey \"" + key.Name + "\\" + valueName + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
            }
        }

        
        public static bool RunElevated(string cmd)
        {
            try
            {
                Helper.log.Info("Execute elevated command \"" + cmd + "\"");

                ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(App.Current.ApplicationPath, "NetworkDriveServiceElevation.exe"), cmd);
                Process p = Process.Start(psi);
                p.WaitForExit();
                int code = p.ExitCode;
                if (code == 0)
                {
                    Helper.log.Info("-> Elevated command returned exit code " + code + ".");
                    return true;
                }
                else
                {
                    Helper.log.Warn("-> Elevated command returned exit code " + code + ".");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Helper.log.Error("Could not execute elevated command: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                return false;
            }
        }
    }
}
