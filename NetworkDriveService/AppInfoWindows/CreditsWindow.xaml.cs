using log4net;
using QuasaroDRV.Properties;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace QuasaroDRV.AppInfoWindows
{
    /// <summary>
    /// Interaktionslogik für CreditsWindow.xaml
    /// </summary>
    public partial class CreditsWindow : Window
    {
        ILog log;

        public CreditsWindow()
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            }
            catch (Exception ex)
            {
                log.Error("Unable to open link \"" + e.Uri.AbsoluteUri + "\": " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(string.Format(Strings.MessageGenericError, ex.GetType().Name, ex.Message), Branding.ApplicationName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true;
        }

        private void btnOsLicenses_Click(object sender, RoutedEventArgs e)
        {
            OpenSourceLicenseWindow osWindow = new OpenSourceLicenseWindow();
            osWindow.Owner = this;
            osWindow.ShowDialog();
        }
    }
}
