using System.Windows;

namespace QuasaroDRV.AppInfoWindows
{
    /// <summary>
    /// Interaktionslogik für OpenSourceLicenseWindow.xaml
    /// </summary>
    public partial class OpenSourceLicenseWindow : Window
    {
        public OpenSourceLicenseWindow()
        {
            InitializeComponent();
            tbxLicenses.Text = Properties.Resources.openSourceLicenses;
        }
    }
}
