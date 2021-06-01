using log4net;
using Microsoft.Win32;
using QuasaroDRV.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace QuasaroDRV.DriveManagement
{
    #region Types
    public enum NetworkDriveAction
    {
        None = 0,
        Connect = 1,
        ReConnect = 2,
        Disconnect = 3
    }

    public enum ConnectDriveFlags : int
    {
        None = 0,
        Force = 1,
        IgnoreAlreadyConnected = 2,
        DoNotWaitForConnection = 4,
        DoNotUpdateTargetState = 8,
        NoTimeout = 16,
        DoNotSetDriveLabel = 32
    }

    public enum DisconnectDriveFlags : int
    {
        None = 0,
        IgnoreAlreadyDisconnected = 1,
        DoNotWaitForDisconnection = 2,
        DoNotUpdateTargetState = 4
    }

    public enum NetworkDriveStates : int
    {
        Disconnected = 0,
        Connected = 1,
        Processing = 2,
        Error = 3
    }


    abstract class NetworkDriveException : HumanReadableException
    {
        /// <summary>
        /// Gets a value indicating whether retrying might be helpfull.
        /// </summary>
        public abstract bool IsTemporaryError { get; }

        /// <summary>
        /// Gets whether this error might be caused by wrong user credentials.
        /// </summary>
        public abstract bool IsCredentialError { get; }


        public NetworkDriveException()
            : base()
        { }

        public NetworkDriveException(string message)
            : base(message)
        { }
    }
    
    class WNetApiException : NetworkDriveException
    {
        static Dictionary<int, string> errorCodes;
        static Dictionary<int, string> humanReadableErrorCodes;
        static HashSet<int> temporaryErrorCodes;
        static HashSet<int> credentialErrorCodes;
        static WNetApiException()
        {
            UpdateErrorCodes();

            // list all error codes for which a retry is reasonable
            temporaryErrorCodes = new HashSet<int>();
            temporaryErrorCodes.Add(67);
            temporaryErrorCodes.Add(170);
            temporaryErrorCodes.Add(1202);
            temporaryErrorCodes.Add(1203);
            temporaryErrorCodes.Add(1219);
            temporaryErrorCodes.Add(1222);
            temporaryErrorCodes.Add(1244);
            temporaryErrorCodes.Add(2250);
            temporaryErrorCodes.Add(2401);
            temporaryErrorCodes.Add(2404);

            // list all error codes which may be caused by wrong credentials
            credentialErrorCodes = new HashSet<int>();
            credentialErrorCodes.Add(5);
            credentialErrorCodes.Add(86);
            credentialErrorCodes.Add(1219);
            credentialErrorCodes.Add(1244);
            credentialErrorCodes.Add(1326);
            credentialErrorCodes.Add(2202);
        }

        public static void UpdateErrorCodes()
        {
            errorCodes = new Dictionary<int, string>();
            humanReadableErrorCodes = new Dictionary<int, string>();
            // source: https://msdn.microsoft.com/en-us/library/windows/desktop/ms681381(v=vs.85).aspx
            errorCodes.Add(5, "Access is denied."); // ERROR_ACCESS_DENIED
            humanReadableErrorCodes.Add(5, Strings.WNetErrorAccessDenied);
            errorCodes.Add(53, "The network path was not found."); // ERRROR_BAD_NETPATH
            humanReadableErrorCodes.Add(53, Strings.WNetErrorPathNotFound);
            errorCodes.Add(66, "The network resource type is not correct."); // ERROR_BAD_DEV_TYPE
            humanReadableErrorCodes.Add(66, Strings.WNetErrorNotSupported);
            errorCodes.Add(67, "The network name cannot be found."); // ERROR_BAD_NET_NAME
            humanReadableErrorCodes.Add(67, Strings.WNetErrorPathNotFound);
            errorCodes.Add(85, "The local device name is already in use."); // ERROR_ALREADY_ASSIGNED
            humanReadableErrorCodes.Add(85, Strings.WNetErrorDriveLetterInUse);
            errorCodes.Add(86, "The specified network password is not correct."); // ERROR_INVALID_PASSWORD
            humanReadableErrorCodes.Add(86, Strings.WNetErrorAuthorizationFailed);
            errorCodes.Add(87, "The parameter is incorrect."); // ERROR_INVALID_PARAMETER
            humanReadableErrorCodes.Add(87, Strings.WNetErrorUnknownError);
            errorCodes.Add(170, "The requested resource is in use."); // ERROR_BUSY
            humanReadableErrorCodes.Add(170, Strings.WNetErrorResourceBusy);
            errorCodes.Add(487, "Attempt to access invalid address."); // ERROR_INVALID_ADDRESS
            humanReadableErrorCodes.Add(487, Strings.WNetErrorPathNotFound);
            errorCodes.Add(1200, "The specified device name is invalid."); // ERROR_BAD_DEVICE
            humanReadableErrorCodes.Add(1200, Strings.WNetErrorPathNotFound);
            errorCodes.Add(1202, "The local device name has a remembered connection to another network resource."); // ERROR_DEVICE_ALREADY_REMEMBERED
            humanReadableErrorCodes.Add(1202, Strings.WNetErrorLocalConflict);
            errorCodes.Add(1203, "The network path was either typed incorrectly, does not exist, or the network provider is not currently available."); // ERROR_NO_NET_OR_BAD_PATH
            humanReadableErrorCodes.Add(1203, Strings.WNetErrorPathNotFound);
            errorCodes.Add(1204, "The specified network provider name is invalid."); // ERROR_BAD_PROVIDER
            humanReadableErrorCodes.Add(1204, Strings.WNetErrorPathNotFound);
            errorCodes.Add(1205, "Unable to open the network connection profile."); // ERROR_CANNOT_OPEN_PROFILE
            humanReadableErrorCodes.Add(1205, Strings.WNetErrorUnknownError);
            errorCodes.Add(1206, "The network connection profile is corrupted."); // ERROR_BAD_PROFILE
            humanReadableErrorCodes.Add(1206, Strings.WNetErrorUnknownError);
            errorCodes.Add(1208, "An extended error code has occurred."); // ERROR_EXTENDED_ERROR
            humanReadableErrorCodes.Add(1208, Strings.WNetErrorUnknownError);
            errorCodes.Add(1219, "The credentials supplied conflict with an existing set of credentials."); // ERROR_SESSION_CREDENTIAL_CONFLICT
            humanReadableErrorCodes.Add(1219, Strings.WNetErrorLocalConflict);
            errorCodes.Add(1222, "The network is not present or not started."); // ERROR_NO_NETWORK
            humanReadableErrorCodes.Add(1222, Strings.WNetErrorNoNetwork);
            errorCodes.Add(1223, "The operation was canceled by the user."); // ERROR_CANCELLED
            humanReadableErrorCodes.Add(1223, Strings.WNetErrorCanceledByUser);
            errorCodes.Add(1244, "The operation being requested was not performed because the user has not been authenticated."); // ERROR_NOT_AUTHENTICATED
            humanReadableErrorCodes.Add(1244, Strings.WNetErrorAuthorizationFailed);
            errorCodes.Add(1326, "The user name or password is incorrect."); // ERROR_LOGON_FAILURE
            humanReadableErrorCodes.Add(1326, Strings.WNetErrorAuthorizationFailed);
            errorCodes.Add(2202, "The specified username is invalid"); // ERROR_BAD_USERNAME
            humanReadableErrorCodes.Add(2202, Strings.WNetErrorAuthorizationFailed);
            errorCodes.Add(2250, "This network connection does not exist."); // ERROR_NOT_CONNECTED
            humanReadableErrorCodes.Add(2250, Strings.WNetErrorPathNotFound);
            errorCodes.Add(2401, "This network connection has files open or requests pending."); // ERROR_OPEN_FILES
            humanReadableErrorCodes.Add(2401, Strings.WNetErrorResourceBusy);
            errorCodes.Add(2404, "The device is in use by an active process and cannot be disconnected."); // ERROR_DEVICE_IN_USE
            humanReadableErrorCodes.Add(2404, Strings.WNetErrorResourceBusy);
        }


        protected int errorCode;
        /// <summary>
        /// Gets the error code returned by the windows API.
        /// </summary>
        public int ErrorCode { get { return this.errorCode; } }
        
        public override bool IsTemporaryError { get { return temporaryErrorCodes.Contains(this.errorCode); } }
        public override bool IsCredentialError { get { return credentialErrorCodes.Contains(this.errorCode); } }
        
        public override string HumanReadableMessage
        {
            get
            {
                if (humanReadableErrorCodes.ContainsKey(errorCode))
                    return humanReadableErrorCodes[errorCode] + " (Code " + errorCode + ")";
                else
                    return string.Format(Strings.WNetErrorWithCode, errorCode);
            }
        }


        private WNetApiException(string message, int errorCode)
            : base(message)
        {
            this.errorCode = errorCode;
        }


        public static WNetApiException FromErrorCode(int errorCode)
        {
            if (errorCodes.ContainsKey(errorCode))
                return new WNetApiException("WNetApi error code " + errorCode + ": " + errorCodes[errorCode], errorCode);
            else
                return new WNetApiException("WNetApi error code " + errorCode + ": Error message unknown. Please refer to microsoft help.", errorCode);
        }
    }

    class WebRequestException : NetworkDriveException
    {
        WebException exception;
        HttpStatusCode statusCode;
        public HttpStatusCode StatusCode { get { return this.statusCode; } }

        bool isTemporaryError;
        public override bool IsTemporaryError { get { return this.isTemporaryError; } }
        bool isCredentialError;
        public override bool IsCredentialError { get { return this.isCredentialError; } }

        public override string HumanReadableMessage
        {
            get
            {
                // web request could not be performed:
                switch(this.exception.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.NameResolutionFailure:
                        return Strings.WNetErrorPathNotFound;
                }
                // server response:
                switch (this.statusCode)
                {
                    case HttpStatusCode.Unauthorized: return Strings.WNetErrorAuthorizationFailed;
                    case HttpStatusCode.NotFound: return Strings.WNetErrorPathNotFound;
                    case HttpStatusCode.Forbidden: return Strings.WNetErrorAccessDenied;
                }
                // something else broke...
                return exception.Message;
            }
        }


        private WebRequestException(string message, WebException ex)
            : base(message)
        {
            this.exception = ex;
            if (ex.Response is HttpWebResponse) this.statusCode = ((HttpWebResponse)ex.Response).StatusCode;
            else this.statusCode = HttpStatusCode.ServiceUnavailable; // need to use some status code here...

            this.isTemporaryError = false;
            this.isCredentialError = false;
            switch (this.statusCode)
            {
                case HttpStatusCode.Unauthorized:
                    this.isCredentialError = true;
                    break;
            }
        }


        public static WebRequestException FromWebException(WebException ex)
        {
            return new WebRequestException(ex.Message, ex);
        }
    }

    class TimeoutException : NetworkDriveException
    {
        private NetworkDriveAction action;
        public NetworkDriveAction Action { get { return this.action; } }
        private int timeout;
        public int Timeout { get { return this.timeout; } }

        public override bool IsTemporaryError { get { return true; } }
        public override bool IsCredentialError { get { return false; } }

        public override string HumanReadableMessage
        {
            get
            {
                if (this.action == NetworkDriveAction.Disconnect)
                    return string.Format(Strings.ExceptionNetworkDriveDisconnectionTimedOut, (this.timeout / 1000).ToString());
                else
                    return string.Format(Strings.ExceptionNetworkDriveConnectionTimedOut, (this.timeout / 1000).ToString());
            }
        }


        public TimeoutException(NetworkDriveAction action, int timeout)
            : base("Action " + action + " timed out after " + timeout + "ms")
        {
            this.action = action;
            this.timeout = timeout;
        }
    }
    #endregion

    public class NetworkDrive : INotifyPropertyChanged
    {
        #region API Functions and Structures
        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NetResource lpNetResource, string lpPassword, string lpUsername, int dwFlags);
        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, int fForce);

        [StructLayout(LayoutKind.Sequential)]
        private struct NetResource
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpLocalName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRemoteName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpProvider;
        }

        // constants source: https://retep998.github.io/doc/winapi/winnetwk/index.html
        private const int RESOURCE_CONNECTED = 0x1;
        private const int RESOURCE_GLOBALNET = 0x2;
        private const int RESOURCE_REMEMBERED = 0x3;

        private const int RESOURCETYPE_DISK = 0x1;

        private const int RESOURCEDISPLAYTYPE_SHARE = 0x3;

        private const int RESOURCEUSAGE_CONNECTABLE = 0x1;

        private const int CONNECT_TEMPORARY = 0x4;
        private const int CONNECT_INTERACTIVE = 0x8;
        private const int CONNECT_PROMPT = 0x10;


        [DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        // constants source: http://www.pinvoke.net/default.aspx/Enums/SHChangeNotifyEventID.html
        private const int SHCNE_DRIVEADD = 0x100;
        private const int SHCNE_DRIVEREMOVED = 0x80;
        private const int SHCNF_IDLIST = 0;


        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const int WM_SETTINGCHANGE = 0x1a;
        #endregion

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        string localDriveLetter = null;
        public string LocalDriveLetter
        {
            get { return this.localDriveLetter; }
            set
            {
                if (value == null)
                    this.localDriveLetter = null;
                else
                {
                    if (value.Length == 0)
                        this.localDriveLetter = null;
                    else
                    {
                        if ((value[0] >= 'A' && value[0] <= 'Z') || (value[0] >= 'a' && value[0] <= 'z'))
                        {
                            if (value.Length == 1)
                                this.localDriveLetter = value.ToUpper() + ":";
                            else if (value.Length == 2 && value[1] == ':')
                                this.localDriveLetter = value.Substring(0, 2).ToUpper();
                            else
                                throw new ArgumentException(Strings.ExceptionDriveLetterFormat);
                        }
                        else
                            throw new ArgumentException(Strings.ExceptionDriveLetterOutOfRange);
                    }
                }
                NotifyPropertyChanged("LocalDriveLetter");
            }
        }

        public const int DefaultCheckConnectionTimeout = 4000; // wait 4 seconds before timeout
        const int CheckConnectionInterval = 500; // check every 0,5 seconds
        protected int checkConnectionTimeout;
        public int CheckConnectionTimeout
        {
            get { return this.checkConnectionTimeout; }
            set
            {
                this.checkConnectionTimeout = value;
                NotifyPropertyChanged("CheckConnectionTimeout");
            }
        }

        public const int DefaultActionTimeout = 30000; // wait 30 seconds for api-calls and other timeouts
        protected int actionTimeout;
        public int ActionTimeout
        {
            get { return this.actionTimeout; }
            set
            {
                this.actionTimeout = value;
                NotifyPropertyChanged("ActionTimeout");
            }
        }

        string remoteAddress = null;
        public string RemoteAddress
        {
            get { return this.remoteAddress; }
            set
            {
                this.remoteAddress = value;
                NotifyPropertyChanged("RemoteAddress");
                NotifyPropertyChanged("ExpandedRemoteAddress");
            }
        }
        /// <summary>
        /// Returns the remote address of this network drive with expanded environment variables.
        /// </summary>
        public string ExpandedRemoteAddress
        {
            get { return (this.remoteAddress == null ? null : Environment.ExpandEnvironmentVariables(this.remoteAddress)); }
        }

        string driveLabel = "";
        public string DriveLabel
        {
            get { return this.driveLabel; }
            set
            {
                this.driveLabel = (value == null ? "" : value);
                NotifyPropertyChanged("DriveLabel");
                NotifyPropertyChanged("ExpandedDriveLabel");
            }
        }
        public string ExpandedDriveLabel { get { return (this.driveLabel == null ? null : Environment.ExpandEnvironmentVariables(this.driveLabel)); } }

        string username = null;
        public string Username { get { return this.username; } }
        SecureString password = null;
        public bool HasCredentials { get { return (!string.IsNullOrEmpty(this.username) && (this.password != null) && (this.password.Length > 0)); } }
        public string HasCredentialsHumanReadable { get { return (HasCredentials ? Strings.Yes : Strings.No); } }

        protected bool connectOnStartup;
        public bool ConnectOnStartup
        {
            get { return this.connectOnStartup; }
            set
            {
                this.connectOnStartup = value;
                NotifyPropertyChanged("ConnectOnStartup");
            }
        }

        public NetworkDriveStates State
        {
            get
            {
                if (this.HasErrorMessage)
                    return NetworkDriveStates.Error;
                else if (this.isProcessing)
                    return NetworkDriveStates.Processing;
                else if (this.isConnected)
                    return NetworkDriveStates.Connected;
                else
                    return NetworkDriveStates.Disconnected;
            }
        }

        protected bool targetConnectivity;
        public bool TargetConnectivity
        {
            get { return this.targetConnectivity; }
            set
            {
                this.targetConnectivity = value;
                NotifyPropertyChanged("TargetConnectivity");
            }
        }

        private string lastErrorMessage = "";
        public string LastErrorMessage
        {
            get { return this.lastErrorMessage; }
            set
            {
                this.lastErrorMessage = value;
                NotifyPropertyChanged("LastErrorMessage");
                NotifyPropertyChanged("HasErrorMessage");
                NotifyPropertyChanged("State");
            }
        }
        public bool HasErrorMessage { get { return !string.IsNullOrEmpty(this.lastErrorMessage); } }
        #endregion

        #region Construction, Import and Export
        ILog log;


        private NetworkDrive()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            this.ConnectOnStartup = true;
            this.CheckConnectionTimeout = DefaultCheckConnectionTimeout;
            this.ActionTimeout = DefaultActionTimeout;
            this.targetConnectivity = false;
        }

        public NetworkDrive(string localDriveLetter)
            : this()
        {
            this.LocalDriveLetter = localDriveLetter; // assign to property to run normalization
            UpdateConnectivity(false);
            this.targetConnectivity = (this.connectOnStartup || this.isConnected);
        }
        public NetworkDrive(string localDriveLetter, string driveLabel, string remoteAddress, string username, SecureString password)
            : this(localDriveLetter)
        {
            this.remoteAddress = remoteAddress;
            this.DriveLabel = driveLabel;
            SetCredentials(username, password);
        }
        public NetworkDrive(string localDriveLetter, string driveLabel, string remoteAddress, string username, string password)
            : this(localDriveLetter)
        {
            this.remoteAddress = remoteAddress;
            this.DriveLabel = driveLabel;
            SetCredentials(username, password);
        }


        public NetworkDrive(JObject data)
            : this()
        {
            this.localDriveLetter = data["LocalDriveLetter"].Value<string>();
            this.remoteAddress = data["RemoteAddress"].Value<string>();
            if (data["DriveName"] == null)
            {
                if (data["DriveLabel"] == null) this.DriveLabel = this.remoteAddress;
                else this.DriveLabel = data["DriveLabel"].Value<string>();
            }
            else this.DriveLabel = data["DriveName"].Value<string>(); // read DriveName for backwards-compatibility
            this.ConnectOnStartup = (data["ConnectOnStartup"] == null ? data["MountOnStartup"].Value<bool>() : data["ConnectOnStartup"].Value<bool>()); // try to read MountOnStartup for backwards-compatibility

            if (data["Username"] != null) this.username = data["Username"].Value<string>();
            else this.username = null;

            if (data["Password"] == null || data["Password"].Value<string>() == null)
            {
                this.password = null;
            }
            else
            {
                SetPassword(Helper.UnprotectData(data["Password"].Value<string>()));
            }

            UpdateConnectivity(false);
            this.targetConnectivity = (this.connectOnStartup || this.isConnected);
        }

        public JObject ExportToJson(bool backwardCompatibility = false)
        {
            JObject data = new JObject();
            data.Add("LocalDriveLetter", this.localDriveLetter);
            data.Add("DriveLabel", this.DriveLabel);
            if (backwardCompatibility) data.Add("DriveName", this.DriveLabel);
            data.Add("RemoteAddress", this.remoteAddress);
            data.Add("Username", this.username);
            data.Add("Password", GetEncryptedPassword());
            data.Add("ConnectOnStartup", this.ConnectOnStartup);
            if (backwardCompatibility) data.Add("MountOnStartup", this.ConnectOnStartup);
            if (backwardCompatibility) data.Add("DisconnectOnShutdown", false);
            if (backwardCompatibility) data.Add("DismountOnShutdown", false);
            return data;
        }

        public void Update(NetworkDrive prototype)
        {
            this.localDriveLetter = prototype.localDriveLetter;
            this.driveLabel = prototype.driveLabel;
            this.remoteAddress = prototype.remoteAddress;
            SetCredentials(prototype.username, prototype.password);
            this.ConnectOnStartup = prototype.ConnectOnStartup;

            NotifyPropertyChanged("LocalDriveLetter");
            NotifyPropertyChanged("DriveLabel");
            NotifyPropertyChanged("ExpandedDriveLabel");
            NotifyPropertyChanged("RemoteAddress");
            NotifyPropertyChanged("ExpandedRemoteAddress");
            NotifyPropertyChanged("ConnectOnStartup");
        }

        public NetworkDrive Copy()
        {
            NetworkDrive copy = new NetworkDrive(this.localDriveLetter, this.driveLabel, this.remoteAddress, this.username, this.password);
            copy.connectOnStartup = this.ConnectOnStartup;
            return copy;
        }
        #endregion

        #region Credential management
        public void SetCredentials(string username, SecureString password)
        {
            this.username = username;
            this.password = (password == null ? null : password.Copy());

            NotifyPropertyChanged("Username");
            NotifyPropertyChanged("HasCredentials");
            NotifyPropertyChanged("HasCredentialsHumanReadable");
        }
        public void SetCredentials(string username, string password)
        {
            this.username = username;
            SetPassword(password);

            NotifyPropertyChanged("Username");
            NotifyPropertyChanged("HasCredentials");
            NotifyPropertyChanged("HasCredentialsHumanReadable");
        }

        public void DeleteCredentials()
        {
            this.username = null;
            if (this.password != null)
                this.password.Dispose();
            this.password = null;

            NotifyPropertyChanged("Username");
            NotifyPropertyChanged("HasCredentials");
            NotifyPropertyChanged("HasCredentialsHumanReadable");
        }

        protected void SetPassword(string password)
        {
            if (password == null)
            {
                this.password = null;
            }
            else
            {
                string rawPassword = Helper.UnprotectData(password);

                // build secure string for persistent storage
                this.password = new SecureString();
                foreach (char c in rawPassword)
                    this.password.AppendChar(c);
                this.password.MakeReadOnly();
            }
        }
        protected string GetEncryptedPassword()
        {
            if (this.password == null)
                return null;

            IntPtr ptrPassword = Marshal.SecureStringToGlobalAllocUnicode(this.password);
            string unsafePassword = Marshal.PtrToStringUni(ptrPassword);
            Marshal.ZeroFreeGlobalAllocUnicode(ptrPassword);
            return Helper.ProtectData(unsafePassword);
        }
        public string GetPassword()
        {
            if (this.password == null)
                return null;

            IntPtr ptrPassword = Marshal.SecureStringToGlobalAllocUnicode(this.password);
            string unsafePassword = Marshal.PtrToStringUni(ptrPassword);
            Marshal.ZeroFreeGlobalAllocUnicode(ptrPassword);
            return unsafePassword;
        }
        public SecureString GetSecurePassword()
        {
            return this.password;
        }
        #endregion

        #region Connecting and Disconnecting

        private bool IsFlagSet<T>(T flags, T checkFlag)
        {
            return ((int)(object)flags & (int)(object)checkFlag) != 0;
        }

        private int RunNetUse(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo("net", "use " + args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process p = Process.Start(psi);
            StringBuilder sb = new StringBuilder();
            while (!p.StandardError.EndOfStream)
                sb.Append(p.StandardError.ReadLine()).Append("\r\n");
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                Regex pattern = new Regex("(\\d+)");
                Match m = pattern.Match(sb.ToString());
                if (m.Success) return int.Parse(m.Value);
                else return -1;
            }
            return 0;
        }

        public void BeginConnect(ConnectDriveFlags flags = ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.Force)
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Connect(flags);
                }
                catch { } // error will be reported in the main list
            }));
            t.IsBackground = true;
            t.Start();
        }
        public void Connect(ConnectDriveFlags flags = ConnectDriveFlags.None)
        {
            SetProcessingState(true);
            Exception innerException = null;
            Thread t = new Thread(new ThreadStart(() =>
            {
                try
                {
                    ConnectInternal(flags);
                }
                catch (Exception ex)
                {
                    if (!IsFlagSet(flags, ConnectDriveFlags.DoNotUpdateTargetState))
                        this.TargetConnectivity = this.isConnected;
                    innerException = ex;
                }
                SetProcessingState(false);
            }));
            t.IsBackground = true;
            t.Start();

            if (IsFlagSet(flags, ConnectDriveFlags.NoTimeout))
            {
                t.Join();
            }
            else
            {
                if (!t.Join(this.ActionTimeout))
                {
                    innerException = new TimeoutException(NetworkDriveAction.Connect, this.ActionTimeout);
                }
            }

            if (innerException != null)
            {
                this.LastErrorMessage = Helper.GetUserMessage(innerException);
                throw innerException;
            }
            else
            {
                this.LastErrorMessage = "";
            }
        }
        private void ConnectInternal(ConnectDriveFlags flags = ConnectDriveFlags.None)
        {
            // check connection data
            if (this.localDriveLetter == null)
                throw new InvalidOperationException(Strings.ExceptionDriveLetterNotSet);
            if (this.remoteAddress == null)
                throw new InvalidOperationException(Strings.ExceptionRemoteAddressNotSet);

            if (IsFlagSet(flags, ConnectDriveFlags.Force))
            {
                try
                {
                    // close old connection to prevent "already connected" errors
                    WNetCancelConnection2(this.localDriveLetter, 0, 1);
                    WaitForConnectivity(false);
                }
                catch { }
            }

            if (!IsFlagSet(flags, ConnectDriveFlags.DoNotUpdateTargetState))
                this.TargetConnectivity = true;

            int start = Environment.TickCount;
            int result = 0;
            if (App.Current.ApplicationOptions.UseWindowsApi)
            {
                NetResource res = new NetResource();
                res.dwScope = RESOURCE_GLOBALNET;
                res.dwType = RESOURCETYPE_DISK;
                res.dwDisplayType = RESOURCEDISPLAYTYPE_SHARE;
                res.dwUsage = RESOURCEUSAGE_CONNECTABLE;
                res.lpRemoteName = this.ExpandedRemoteAddress;
                res.lpLocalName = this.localDriveLetter;
                // copy secure credentials temporarily to memory for the api call
                string unsafePassword = "";
                if (this.password != null)
                {
                    IntPtr ptrPassword = Marshal.SecureStringToGlobalAllocUnicode(this.password);
                    unsafePassword = Marshal.PtrToStringUni(ptrPassword);
                    Marshal.ZeroFreeGlobalAllocUnicode(ptrPassword);
                }
                result = WNetAddConnection2(ref res, unsafePassword, this.username, CONNECT_TEMPORARY);
            }
            else
            {
                string unsafePassword = "";
                if (this.password != null)
                {
                    IntPtr ptrPassword = Marshal.SecureStringToGlobalAllocUnicode(this.password);
                    unsafePassword = Marshal.PtrToStringUni(ptrPassword);
                    Marshal.ZeroFreeGlobalAllocUnicode(ptrPassword);
                }
                result = RunNetUse(this.LocalDriveLetter + " \"" + this.remoteAddress + "\" /user:" + this.username + " \"" + unsafePassword + "\"");
            }
            this.log.Debug("Drive \"" + this.localDriveLetter + "\" Connect API-call took " + (Environment.TickCount - start) + "ms -> " + result);

            if (result == 85 && !IsFlagSet(flags, ConnectDriveFlags.DoNotWaitForConnection))
            {
                // ignore this error when the drive is connected correctly
                if (!WaitForConnectivity(true))
                    CheckError(result, false);
            }

            if ((IsFlagSet(flags, ConnectDriveFlags.IgnoreAlreadyConnected) && result == 85) || !CheckError(result, true))
            {
                if (!IsFlagSet(flags, ConnectDriveFlags.DoNotWaitForConnection))
                {
                    if (!WaitForConnectivity(true))
                    {
                        Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected | DisconnectDriveFlags.DoNotUpdateTargetState | DisconnectDriveFlags.DoNotWaitForDisconnection);
                        throw new TimeoutException(NetworkDriveAction.Connect, this.CheckConnectionTimeout);
                    }
                }

                if (!IsFlagSet(flags, ConnectDriveFlags.DoNotSetDriveLabel))
                {
                    // Set Drive Label to Name. Set registry key always (even when not waiting for connection), so it exists when the drive
                    // is connected after a longer timeout
                    this.SetDriveLabel();
                }
            }
        }

        protected void SetDriveLabel()
        {
            if (!string.IsNullOrEmpty(this.driveLabel))
            {
                try
                {
                    // Gets the current drive from wmi
                    // ATTENTION: this line will block during api calls to Wnet-Add/Remove, thus limiting the benefit of multithreading
                    ManagementObjectSearcher disks = new ManagementObjectSearcher("Select * From Win32_LogicalDisk Where Name = '" + this.localDriveLetter + "'");

                    if (disks == null || disks.Get() == null || disks.Get().Count == 0)
                        throw new Exception(string.Format(Strings.ExceptionDiskNotFound, this.localDriveLetter));

                    foreach (ManagementObject disk in disks.Get())
                    {
                        disk.Get();
                        string providerName = disk["ProviderName"].ToString();

                        try
                        {
                            string expandedDriveLabel = this.ExpandedDriveLabel;

                            //Generates and sets the needed reg key in current user
                            providerName = providerName.Replace("\\", "#");
                            RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MountPoints2\\" + providerName);
                            key.SetValue("_LabelFromReg", expandedDriveLabel);
                            key.Close();

                            // generate key without DavWWWRoot as alternative
                            providerName = providerName.Replace("#DavWWWRoot", "");
                            key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\MountPoints2\\" + providerName);
                            key.SetValue("_LabelFromReg", expandedDriveLabel);
                            key.Close();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                }
                catch (ManagementException)
                {
                    // happens when wmi is not available because of application shutdowns -> just ignore this error
                    return;
                }

                if (App.Current.ApplicationOptions.SendSystemUpdateNotifications)
                    SendUpdateNotification();
            }
        }

        public static void SendUpdateNotification()
        {
            try
            {
                //TODO send notifications asynchronously (maybe even collectively after many connection changes)
                SHChangeNotify(SHCNE_DRIVEADD, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                SHChangeNotify(SHCNE_DRIVEREMOVED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                // source: https://msdn.microsoft.com/en-us/library/windows/desktop/ms725497(v=vs.85).aspx
                SendMessage(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, IntPtr.Zero);
            }
            catch { } // update-errors are not relevant
        }

        public void BeginDisconnect(DisconnectDriveFlags flags = DisconnectDriveFlags.IgnoreAlreadyDisconnected)
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Disconnect(flags);
                }
                catch { } // error will be reported in the main list
            }));
            t.IsBackground = true;
            t.Start();
        }
        public void Disconnect(DisconnectDriveFlags flags = DisconnectDriveFlags.None)
        {
            SetProcessingState(true);

            if (this.localDriveLetter == null)
                throw new InvalidOperationException(Strings.ExceptionDriveLetterNotSet);

            if (!IsFlagSet(flags, DisconnectDriveFlags.DoNotUpdateTargetState))
                this.TargetConnectivity = false;

            int result = 0;
            if (App.Current.ApplicationOptions.UseWindowsApi)
            {
                result = WNetCancelConnection2(this.localDriveLetter, 0, 1);
            }
            else
            {
                result = RunNetUse(this.LocalDriveLetter + " /delete /yes");
            }

            SetProcessingState(false);

            this.LastErrorMessage = "";
            if ((IsFlagSet(flags, DisconnectDriveFlags.IgnoreAlreadyDisconnected) && result == 2250) || !CheckError(result))
            {
                if (!IsFlagSet(flags, DisconnectDriveFlags.DoNotWaitForDisconnection))
                    if (!WaitForConnectivity(false))
                        throw new TimeoutException(NetworkDriveAction.Disconnect, this.CheckConnectionTimeout);
            }
        }

        protected bool CheckError(int result, bool disconnectOnError = false)
        {
            if (result != 0)
            {
                if (disconnectOnError)
                    Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected | DisconnectDriveFlags.DoNotUpdateTargetState | DisconnectDriveFlags.DoNotWaitForDisconnection);
                Exception ex = WNetApiException.FromErrorCode(result);
                this.LastErrorMessage = Helper.GetUserMessage(ex);
                throw ex;
            }
            else
                return false;
        }

        private bool WaitForConnectivity(bool connectivityState)
        {
            UpdateConnectivity(false);

            // already (dis)connected? do not wait
            if (this.isConnected == connectivityState)
            {
                NotifyConnectionStateChanged();
                return true;
            }
            // check every CheckConnectionInterval milliseconds for (dis)connection
            // but stop after CheckConnectionTimeout milliseconds and return false
            int startTick = Environment.TickCount;
            while ((Environment.TickCount - startTick) < this.CheckConnectionTimeout)
            {
                Thread.Sleep(CheckConnectionInterval);
                UpdateConnectivity(false);
                if (this.isConnected == connectivityState)
                {
                    NotifyConnectionStateChanged();
                    return true;
                }
            }
            NotifyConnectionStateChanged();
            return false;
        }


        private bool doNotChangeConnectionState;
        public bool DoNotChangeConnectionState { get { return this.doNotChangeConnectionState; } }


        protected bool isConnected;
        /// <summary>
        /// Returns the last known connectivity state of the drive.
        /// </summary>
        public bool IsConnectedCached
        {
            get { return this.isConnected; }

            // this property only exists so a TwoWay-Binding in WPF is possible
            // a OneWay-Binding is broken when the user changes the value manually
            set
            {
                if(value)
                {
                    BeginConnect();
                }
                else
                {
                    BeginDisconnect();
                }
            }
        }
        /// <summary>
        /// Determines the current connectivity state of the drives and returns it.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                UpdateConnectivity(true);
                return this.isConnected;
            }
        }
        private void NotifyConnectionStateChanged()
        {
            this.doNotChangeConnectionState = true;
            NotifyPropertyChanged("IsConnected");
            NotifyPropertyChanged("IsConnectedCached");
            NotifyPropertyChanged("State");
            this.doNotChangeConnectionState = false;
        }

        protected bool isProcessing = false;
        public bool IsProcessing
        {
            get { return this.isProcessing; }
        }
        public bool IsIdle
        {
            get { return !this.isProcessing; }
        }
        protected void SetProcessingState(bool isProcessing)
        {
            this.isProcessing = isProcessing;
            NotifyPropertyChanged("IsProcessing");
            NotifyPropertyChanged("IsIdle");
            NotifyPropertyChanged("State");
        }

        protected void UpdateConnectivity(bool notify)
        {
            bool oldIsConnected = this.isConnected;

            // a drive is connected when the drive is accessible and marked as network drive
            // correct address is not checked

            try
            {
                DriveInfo drive = new DriveInfo(this.localDriveLetter);
                if (!drive.IsReady)
                    this.isConnected = false;
                else
                    this.isConnected = (drive.DriveType == DriveType.Network);
            }
            catch { this.isConnected = false; }

            if (notify && (this.isConnected != oldIsConnected))
                NotifyConnectionStateChanged();
        }
        public void UpdateConnectivity()
        {
            UpdateConnectivity(true);
        }
        #endregion

        #region Connection Checking
        public bool IsHttpAddress
        {
            get
            {
                return (this.remoteAddress.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) || this.remoteAddress.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase));
            }
        }


        public bool CheckConnection(out Exception connectException)
        {
            // use default WNetApi
            try
            {
                CheckConnectionWNetApi();
            }
            catch (Exception ex)
            {
                connectException = ex;
                return false;
            }

            connectException = null;
            return true;
        }
        public bool CheckConnection()
        {
            Exception ex;
            return CheckConnection(out ex);
        }

        private void CheckConnectionWNetApi()
        {
            try
            {
                Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);
            }
            catch { }

            Connect(ConnectDriveFlags.IgnoreAlreadyConnected | ConnectDriveFlags.DoNotSetDriveLabel);

            try
            {
                Disconnect(DisconnectDriveFlags.IgnoreAlreadyDisconnected);
            }
            catch { }
        }

        private void CheckConnectionWebDav()
        {
            try
            {
                Helper.DownloadData(this.remoteAddress, new WebRequestAction((request) =>
                {
                    request.PreAuthenticate = true;
                    request.UseDefaultCredentials = false;
                    request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(this.username + ":" + GetPassword()));
                }), false); //TODO do not force ssl3 (tls often fails but takes some time...)
            }
            catch (WebException ex)
            {
                throw WebRequestException.FromWebException(ex);
            }
        }
        #endregion

        #region Input / Output
        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.remoteAddress))
                return this.localDriveLetter;
            else
                return this.localDriveLetter + " (" + this.remoteAddress + ")";
        }

        public static JArray ToArray(IEnumerable<NetworkDrive> drives)
        {
            JArray drivesArray = new JArray();
            foreach (NetworkDrive drive in drives)
                drivesArray.Add(drive.ExportToJson());
            return drivesArray;
        }

        public static NetworkDrive[] FromArray(JArray drivesArray, out bool requireSave)
        {
            // import drive data
            List<NetworkDrive> drives = new List<NetworkDrive>(drivesArray.Count);
            foreach (JObject data in drivesArray)
            {
                // try reading drive entry, but simply skip errorneous
                NetworkDrive drive;
                try
                {
                    drive = new NetworkDrive(data);
                }
                catch
                {
                    continue;
                }
                drives.Add(drive);
            }

            // set to true to encrypt plaintext values
            //TODO check for plaintext values instead of always encrypting
            requireSave = true;
            
            return drives.ToArray();
        }
        #endregion
    }
}
