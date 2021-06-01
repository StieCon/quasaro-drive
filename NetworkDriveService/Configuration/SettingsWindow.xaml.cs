using log4net;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QuasaroDRV.Configuration
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        ILog log;

        ApplicationOptions appOptions;
        MicrosoftOfficeConfiguration officeConfig;

        // keep track of changed values without the need to compare all values or just write all back at the end
        bool changedAutoStart, changedAutoUpdate, changedAllowBeta, changedOnlyReconnectActiveDrives, changedLanguage, changedPvExcel, changedPvWord, changedPvPowerPoint;
        public bool RequireRestart { get { return changedLanguage; } }

        public SettingsWindow(ApplicationOptions appOptions, MicrosoftOfficeConfiguration officeConfig)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();

            this.appOptions = appOptions;
            this.officeConfig = officeConfig;
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cbxAutoStart.IsChecked = this.appOptions.UseAutoStart;
                cbxAutoUpdate.IsEnabled = this.appOptions.AllowAutoUpdate;
                cbxAutoUpdate.IsChecked = (this.appOptions.AllowAutoUpdate && this.appOptions.UseAutoUpdate);
                cbxAllowBeta.IsChecked = this.appOptions.EnableBetaVersions;
                cbxOnlyReconnectActiveDrives.IsChecked = this.appOptions.OnlyReconnectActiveDrives;
                int selectedLanguageIndex = 0;
                for (int i = 0; i < cbxLanguage.Items.Count; i++)
                {
                    if (((ComboBoxItem)cbxLanguage.Items.GetItemAt(i)).Tag.Equals(this.appOptions.SelectedLanguage))
                    {
                        selectedLanguageIndex = i;
                        break;
                    }
                }
                cbxLanguage.SelectedIndex = selectedLanguageIndex;
                gbxOffice.IsEnabled = this.officeConfig.OfficeAvailable;
                if (this.officeConfig.OfficeAvailable)
                {
                    cbxPvExcel.IsChecked = this.officeConfig.ExcelDisableProtectedViewFromInternetSource;
                    cbxPvWord.IsChecked = this.officeConfig.WordDisableProtectedViewFromInternetSource;
                    cbxPvPowerPoint.IsChecked = this.officeConfig.PowerPointDisableProtectedViewFromInternetSource;
                }
                else
                {
                    gbxOffice.Header = Properties.Strings.SettingsWindowOfficeNotInstalledGroupTitle;
                }
                ButtonAdvanced.IsEnabled = App.Current.ApplicationOptions.AllowAdvancedSettings;

                // mark all as unchanged after the changed-events have been triggered during initialization
                this.changedAutoStart = false;
                this.changedAutoUpdate = false;
                this.changedAllowBeta = false;
                this.changedOnlyReconnectActiveDrives = false;
                this.changedLanguage = false;
                this.changedPvExcel = false;
                this.changedPvWord = false;
                this.changedPvPowerPoint = false;
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled Exception in SettingsWindow.Window_Loaded: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(string.Format(Properties.Strings.SettingsWindowLoadErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.SettingsWindowTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonOkay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.changedAutoStart) this.appOptions.UseAutoStart = cbxAutoStart.IsChecked.Value;
                if (this.changedAutoUpdate) this.appOptions.UseAutoUpdate = cbxAutoUpdate.IsChecked.Value;
                if (this.changedAllowBeta) this.appOptions.EnableBetaVersions = cbxAllowBeta.IsChecked.Value;
                if (this.changedOnlyReconnectActiveDrives) this.appOptions.OnlyReconnectActiveDrives = cbxOnlyReconnectActiveDrives.IsChecked.Value;
                if (this.changedLanguage) this.appOptions.SelectedLanguage = (string)((ComboBoxItem)cbxLanguage.SelectedItem).Tag;
                if (this.officeConfig.OfficeAvailable)
                {
                    if (this.changedPvExcel) this.officeConfig.ExcelDisableProtectedViewFromInternetSource = cbxPvExcel.IsChecked.Value;
                    if (this.changedPvWord) this.officeConfig.WordDisableProtectedViewFromInternetSource = cbxPvWord.IsChecked.Value;
                    if (this.changedPvPowerPoint) this.officeConfig.PowerPointDisableProtectedViewFromInternetSource = cbxPvPowerPoint.IsChecked.Value;
                }
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                this.log.Error("Unhandled Exception in SettingsWindow.ButtonOkay_Click: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                MessageBox.Show(string.Format(Properties.Strings.SettingsWindowSaveErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Branding.SettingsWindowTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void cbxAutoStart_Checked(object sender, RoutedEventArgs e)
        {
            this.changedAutoStart = true;
        }
        private void cbxAutoStart_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedAutoStart = true;
        }

        private void cbxAutoUpdate_Checked(object sender, RoutedEventArgs e)
        {
            this.changedAutoUpdate = true;
        }
        private void cbxAutoUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedAutoUpdate = true;
        }

        private void cbxAllowBeta_Checked(object sender, RoutedEventArgs e)
        {
            this.changedAllowBeta = true;
        }
        private void cbxAllowBeta_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedAllowBeta = true;
        }

        private void cbxOnlyReconnectActiveDrives_Checked(object sender, RoutedEventArgs e)
        {
            this.changedOnlyReconnectActiveDrives = true;
        }
        private void cbxOnlyReconnectActiveDrives_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedOnlyReconnectActiveDrives = true;
        }

        private void cbxLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.changedLanguage = true;
        }

        private void cbxPvExcel_Checked(object sender, RoutedEventArgs e)
        {
            this.changedPvExcel = true;
        }
        private void cbxPvExcel_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedPvExcel = true;
        }

        private void cbxPvWord_Checked(object sender, RoutedEventArgs e)
        {
            this.changedPvWord = true;
        }
        private void cbxPvWord_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedPvWord = true;
        }

        private void cbxPvPowerPoint_Checked(object sender, RoutedEventArgs e)
        {
            this.changedPvPowerPoint = true;
        }
        private void cbxPvPowerPoint_Unchecked(object sender, RoutedEventArgs e)
        {
            this.changedPvPowerPoint = true;
        }

        private void ButtonAdvanced_Click(object sender, RoutedEventArgs e)
        {
            AdvancedSettingsWindow dialog = new AdvancedSettingsWindow();
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}
