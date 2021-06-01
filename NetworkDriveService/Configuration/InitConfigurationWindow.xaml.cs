using log4net;
using QuasaroDRV.DriveManagement;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Windows;

namespace QuasaroDRV.Configuration
{
    /// <summary>
    /// Interaction logic for InitConfigurationWindow.xaml
    /// </summary>
    public partial class InitConfigurationWindow : Window
    {
        private class DriveListEntry
        {
            public bool IsChecked { get; set; }
            List<string> availableDriveLetters;
            public List<string> AvailableDriveLetters { get { return this.availableDriveLetters; } }
            public DriveConfiguration14 DriveConf { get; set; }
            public string DriveLetter
            {
                get { return this.DriveConf.DriveLetter + ":"; }
                set { this.DriveConf.DriveLetter = value[0]; }
            }

            public DriveListEntry(DriveConfiguration14 drive)
            {
                this.IsChecked = true;
                this.availableDriveLetters = new List<string>();
                this.DriveConf = drive;
            }
        }


        ILog log;

        List<DriveListEntry> drives;
        List<NetworkDrive> targetList;
        
        
        public InitConfigurationWindow(ConfigurationSuffix14 suffix, List<NetworkDrive> targetList)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();
            
            this.targetList = targetList;
            SetupInitialConfig(suffix);
        }

        void SetupInitialConfig(ConfigurationSuffix14 suffix)
        {
            this.drives = new List<DriveListEntry>(suffix.Drives.Length);
            for (int i = 0; i < suffix.Drives.Length; i++)
            {
                this.drives.Add(new DriveListEntry(suffix.Drives[i]));
            }
            UpdateDriveLetters();
            lvwDrives.ItemsSource = this.drives;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            Activate();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DialogResult != true)
            {
                if (MessageBox.Show(Properties.Strings.InitialConfigWindowSkipConfigMessage, Branding.InitialConfigWindowTitle, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }


        private void UpdateDriveLetters()
        {
            foreach (DriveListEntry drive in this.drives)
                UpdateDriveLetters(drive);
        }

        private void UpdateDriveLetters(DriveListEntry drive)
        {
            // prepare array with all drive letters
            drive.AvailableDriveLetters.Clear();
            for (char c = 'A'; c <= 'Z'; c++)
                drive.AvailableDriveLetters.Add(c.ToString() + ":");

            // remove letters which are currently beeing used in the sytem (hard drives, optical drives, etc...)
            foreach (DriveInfo info in DriveInfo.GetDrives())
                drive.AvailableDriveLetters.Remove(char.ToUpper(info.Name[0]).ToString() + ":");

            if (drive.AvailableDriveLetters.Count > 0)
            {
                if (!drive.AvailableDriveLetters.Contains(drive.DriveLetter))
                    drive.DriveLetter = drive.AvailableDriveLetters[drive.AvailableDriveLetters.Count - 1];
            }
        }


        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            HashSet<char> usedLetters = new HashSet<char>();
            bool missingLetter = false;
            bool conflict = false;
            foreach (DriveListEntry entry in this.drives)
            {
                if (entry.IsChecked)
                {
                    if (entry.DriveConf.DriveLetter < 'A' || entry.DriveConf.DriveLetter > 'Z')
                        missingLetter = true;

                    if (usedLetters.Contains(entry.DriveConf.DriveLetter))
                        conflict = true;
                    else
                        usedLetters.Add(entry.DriveConf.DriveLetter);
                }
            }

            if(missingLetter)
            {
                MessageBox.Show(Properties.Strings.InitialConfigWindowMissingDriveLetterMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (conflict)
            {
                MessageBox.Show(Properties.Strings.InitialConfigWindowLettersConflictMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (usedLetters.Count == 0)
            {
                if (MessageBox.Show(Properties.Strings.InitialConfigWindowNoDrivesMessage, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
                    return;
            }
            
            foreach(DriveListEntry entry in this.drives)
            {
                if (entry.IsChecked)
                {
                    NetworkDrive drive = new NetworkDrive(entry.DriveConf.DriveLetter.ToString(), entry.DriveConf.DriveLabel, entry.DriveConf.RemoteAddress, null, (SecureString)null);
                    targetList.Add(drive);
                }
            }
            this.DialogResult = true;
            this.Close();
        }
    }
}
