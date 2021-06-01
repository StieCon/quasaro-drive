using System.Security;
using System.Windows;

namespace QuasaroDRV.DialogWindows
{
    public class Credentials
    {
        public string Username { get; set; }
        public SecureString Password { get; set; }
        public bool IsValid { get { return !string.IsNullOrEmpty(this.Username) && this.Password != null; } }

        public Credentials(string username, SecureString password)
        {
            this.Username = username;
            this.Password = password;
        }
    }

    /// <summary>
    /// Interaction logic for CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : Window
    {
        public string Username
        {
            get { return tbxUsername.Text; }
            set
            {
                if (value == null)
                {
                    tbxUsername.Text = "";
                }
                else
                    tbxUsername.Text = value;
            }
        }
        public SecureString Password
        {
            get
            {
                return tbxPassword.SecurePassword;
            }
        }
        public Credentials Credentials { get { return new Credentials(this.Username, this.Password); } }
        public string Description
        {
            get { return lblDescription.Text.ToString(); }
            set { lblDescription.Text = value; }
        }


        public CredentialsWindow()
        {
            InitializeComponent();

            tbxUsername.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (tbxUsername.Text == "") tbxUsername.Focus();
            else tbxPassword.Focus();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
