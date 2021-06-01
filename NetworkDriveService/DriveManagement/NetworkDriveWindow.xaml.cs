using log4net;
using QuasaroDRV.DialogWindows;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QuasaroDRV.DriveManagement
{
    /// <summary>
    /// Interaction logic for NetworkDriveWindow.xaml
    /// </summary>
    public partial class NetworkDriveWindow : MahApps.Metro.Controls.MetroWindow
    {
        ILog log;

        NetworkDrive networkDrive;
        public NetworkDrive NetworkDrive
        {
            get { return this.networkDrive; }
            set
            {
                this.networkDrive = value;
                this.tbxDriveLabel.DataContext = this.networkDrive;
                this.cbxDriveLetter.DataContext = this.networkDrive;
                this.tbxRemoteAddress.DataContext = this.networkDrive;
                this.cbxConnectOnLogin.DataContext = this.networkDrive;
                string tmpUsername = this.networkDrive.Username; // changing the fields updates both username and password in the NetworkDrive-class. Save old value before it gets overwritten
                this.tbxPassword.Password = (this.networkDrive.HasCredentials ? this.networkDrive.GetPassword() : "");
                this.tbxUsername.Text = tmpUsername;
            }
        }


        public NetworkDriveWindow()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();

            for (char c = 'A'; c <= 'Z'; c++)
                cbxDriveLetter.Items.Add(c + ":");
        }

        public NetworkDriveWindow(char[] drives)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();

            foreach (char c in drives)
                cbxDriveLetter.Items.Add(char.ToUpper(c) + ":");
        }


        private void tbxUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.networkDrive.SetCredentials(tbxUsername.Text, tbxPassword.Password);
        }

        private void tbxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this.networkDrive.SetCredentials(tbxUsername.Text, tbxPassword.Password);
        }


        bool CheckInput()
        {
            if (cbxDriveLetter.SelectedIndex == -1)
            {
                MessageBox.Show(Properties.Strings.DriveWindowSelectDriveLetterMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            
            if (tbxRemoteAddress.Text == "")
            {
                MessageBox.Show(Properties.Strings.DriveWindowEnterRemoteAddressMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            if (tbxUsername.Text == "" || tbxPassword.Password == "")
            {
                if (MessageBox.Show(Properties.Strings.DriveWindowNoCredentialsMessage, this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Cancel)
                    return false;
            }

            return true;
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckInput())
                return;

            LoadingWindow.TryShowLoadingWindow(Properties.Strings.ProgressMessageCheckingConnection, new Action(() =>
            {
                // try connect
                Exception ex;
                if (!this.networkDrive.CheckConnection(out ex))
                {
                    log.Error("Connection check of \"" + this.networkDrive.LocalDriveLetter + "\" to \"" + this.networkDrive.ExpandedRemoteAddress + "\" with user \"" + this.networkDrive.Username + "\" failed: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Properties.Strings.MessageCouldNotConnect, this.networkDrive.LocalDriveLetter, Helper.GetUserMessage(ex)), Properties.Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }));
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), Properties.Strings.MessageConnectionSuccessfull, Properties.Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Information);
                    }));
                }
            }), this);
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckInput())
                return;

            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
