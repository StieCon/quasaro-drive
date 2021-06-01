using System.Windows;

namespace QuasaroDRV.Configuration
{
    /// <summary>
    /// Interaktionslogik für AdvancedSettingsWindow.xaml
    /// </summary>
    public partial class AdvancedSettingsWindow : Window
    {
        public AdvancedSettingsWindow()
        {
            InitializeComponent();

            cbxShowRestartWebClientButton.IsChecked = App.Current.ApplicationOptions.ShowRestartWebClientButton;
            cbxEnableShowLogMenu.IsChecked = App.Current.ApplicationOptions.EnableShowLogMenu;
            cbxPreloadMainWindow.IsChecked = App.Current.ApplicationOptions.PreloadMainWindow;
            cbxUseWindowsAPI.IsChecked = App.Current.ApplicationOptions.UseWindowsApi;
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            App.Current.ApplicationOptions.ShowRestartWebClientButton = (cbxShowRestartWebClientButton.IsChecked == true);
            App.Current.ApplicationOptions.EnableShowLogMenu = (cbxEnableShowLogMenu.IsChecked == true);
            App.Current.ApplicationOptions.PreloadMainWindow = (cbxPreloadMainWindow.IsChecked == true);
            App.Current.ApplicationOptions.UseWindowsApi = (cbxUseWindowsAPI.IsChecked == true);
            this.DialogResult = true;
            this.Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
