using System.Windows;

namespace QuasaroDRV.DialogWindows
{
    /// <summary>
    /// Interaktionslogik für EnterPasswordWindow.xaml
    /// </summary>
    public partial class EnterPasswordWindow : Window
    {
        public string Password { get { return tbxPassword.Password; } }


        public EnterPasswordWindow(string message)
        {
            InitializeComponent();
            lblMessage.Content = message;
            tbxPassword.Focus();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}
