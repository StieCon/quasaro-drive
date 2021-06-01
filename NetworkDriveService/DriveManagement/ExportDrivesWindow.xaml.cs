using log4net;
using QuasaroDRV.DialogWindows;
using QuasaroDRV.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace QuasaroDRV.DriveManagement
{
    /// <summary>
    /// Interaktionslogik für ExportDrivesWindow.xaml
    /// </summary>
    public partial class ExportDrivesWindow : Window
    {
        ILog log;

        NetworkDrive[] exportDrives;


        public ExportDrivesWindow(NetworkDrive[] drives)
        {
            this.log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            InitializeComponent();

            this.exportDrives = drives;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (cbxUseEncryption.IsChecked == true)
            {
                if (tbxEncryptionPassword.Password != tbxEncryptionPasswordRepeat.Password)
                {
                    MessageBox.Show(Strings.MessagePasswordsDoNotMatch, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                if (string.IsNullOrEmpty(tbxEncryptionPassword.Password))
                {
                    MessageBox.Show(Strings.ExportWindowNoPasswordMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            if (cbxStorePassword.IsChecked == true && cbxUseEncryption.IsChecked != true)
            {
                MessageBox.Show(Strings.ExportWindowPasswordWithoutEncryptionMessage, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".drives";
            dialog.Title = this.Title;
            dialog.Filter = "*.drives|*.drives";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool storeUsername = (cbxStoreUsername.IsChecked == true);
                bool storePassword = (cbxStorePassword.IsChecked == true);
                bool useEncryption = (cbxUseEncryption.IsChecked == true);
                string password = tbxEncryptionPassword.Password;

                bool close = false;
                LoadingWindow.TryShowLoadingWindow(Strings.MainWindowExportProgressMessage, new Action(() =>
                {
                    try
                    {
                        JArray drivesList = new JArray();
                        foreach (NetworkDrive drive in this.exportDrives)
                        {
                            JObject obj = drive.ExportToJson();

                            if (!storeUsername)
                            {
                                // remove credentials
                                obj["Username"] = null;
                                obj["Password"] = null;
                            }
                            else if (storePassword)
                            {
                                // write unencrypted password to file
                                obj["Password"] = drive.GetPassword();
                            }
                            else
                            {
                                obj["Password"] = null;
                            }

                            if (useEncryption)
                            {
                                foreach (KeyValuePair<string, JToken> kvp in obj)
                                {
                                    if (kvp.Value.Type == JTokenType.String)
                                    {
                                        obj[kvp.Key] = Helper.EncryptValue(kvp.Value.Value<string>(), password);
                                    }
                                }
                            }

                            drivesList.Add(obj);
                        }

                        JObject exportObj = new JObject();
                        exportObj.Add("IsEncrypted", useEncryption);
                        if (useEncryption)
                            // save salted checksum of the password to check for the correct password
                            exportObj.Add("Check", Helper.GeneratePasswordStore(password));
                        exportObj.Add("Drives", drivesList);

                        log.Info("Export " + drivesList.Count + " drives to \"" + dialog.FileName + "\"");
                        File.WriteAllText(dialog.FileName, exportObj.ToString(), Encoding.UTF8);

                        close = true;
                    }
                    catch (Exception ex)
                    {
                        log.Error("Unable to export drives: " + ex.Message + " (" + ex.GetType().Name + ")", ex);
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show(LoadingWindow.GetStaticLoadingWindow(), string.Format(Strings.MainWindowExportErrorMessage, ex.GetType().Name, ex.Message, ex.StackTrace), Strings.TitleCheckConnection, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }));
                    }
                }));

                if (close)
                {
                    this.DialogResult = true;
                    Close();
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
