using System;
using System.Windows.Forms;

namespace InstallerSuffixGenerator.Version14
{
    public partial class Version14Form : Form
    {
        bool ignoreChanges = false;


        public Version14Form()
        {
            InitializeComponent();

            // set default configuration
            SuffixToConfig(ConfigurationSuffix14.SuffixInitiator.ToString(), true);
        }


        private void SuffixToConfig(string str, bool updateTextBox = false)
        {
            if (ignoreChanges)
                return;
            ignoreChanges = true;

            ConfigurationSuffix14 suffix = ConfigurationSuffix14.Parse(str);
            if (suffix == null)
                return;
            if (updateTextBox)
                tbxSuffix.Text = suffix.ToString();

            lvwDrives.Items.Clear();
            foreach(DriveConfiguration14 drive in suffix.Drives)
            {
                ListViewItem item = new ListViewItem(new string[] { drive.DriveLetter.ToString(), drive.RemoteAddress, drive.DriveLabel });
                item.Tag = drive;
                lvwDrives.Items.Add(item);
            }

            cbxDisablePvExcel.Checked = suffix.DisableProtectedViewExcel;
            cbxDisablePvWord.Checked = suffix.DisableProtectedViewWord;
            cbxDisablePvPowerPoint.Checked = suffix.DisableProtectedViewPowerPoint;
            cbxShowUserConfig.Checked = suffix.ShowConfigWindow;

            ignoreChanges = false;
        }

        private void tbxSuffix_TextChanged(object sender, EventArgs e)
        {
            lblChars.Text = tbxSuffix.Text.Length + " Character(s)";
            SuffixToConfig(tbxSuffix.Text);
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(tbxSuffix.Text);
        }


        private void ConfigToSuffix()
        {
            if (ignoreChanges)
                return;
            ignoreChanges = true;

            ConfigurationSuffix14 suffix = new ConfigurationSuffix14();

            foreach(ListViewItem item in lvwDrives.Items)
            {
                suffix.AddDrive(item.Tag as DriveConfiguration14);
            }

            suffix.DisableProtectedViewExcel = cbxDisablePvExcel.Checked;
            suffix.DisableProtectedViewWord = cbxDisablePvWord.Checked;
            suffix.DisableProtectedViewPowerPoint = cbxDisablePvPowerPoint.Checked;
            suffix.ShowConfigWindow = cbxShowUserConfig.Checked;
            tbxSuffix.Text = suffix.ToString();

            ignoreChanges = false;
        }


        private void btnAddDrive_Click(object sender, EventArgs e)
        {
            DriveConfiguration14 drive = new DriveConfiguration14();
            DriveDetails14 dialog = new DriveDetails14(drive);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ListViewItem item = new ListViewItem(new string[] { drive.DriveLetter.ToString(), drive.RemoteAddress, drive.DriveLabel });
                item.Tag = drive;
                lvwDrives.Items.Add(item);
                ConfigToSuffix();
            }
        }
        
        private void lvwDrives_DoubleClick(object sender, EventArgs e)
        {
            btnEditDrive.PerformClick();
        }

        private void btnEditDrive_Click(object sender, EventArgs e)
        {
            if (lvwDrives.SelectedItems.Count == 1)
            {
                ListViewItem selected = lvwDrives.SelectedItems[0];
                DriveConfiguration14 drive = (DriveConfiguration14)selected.Tag;
                DriveDetails14 dialog = new DriveDetails14(drive);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selected.SubItems[0].Text = drive.DriveLetter.ToString();
                    selected.SubItems[1].Text = drive.RemoteAddress;
                    selected.SubItems[2].Text = drive.DriveLabel;
                    ConfigToSuffix();
                }
            }
        }

        private void btnRemoveDrive_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = new ListViewItem[lvwDrives.SelectedItems.Count];
            lvwDrives.SelectedItems.CopyTo(items, 0);
            foreach (ListViewItem item in items)
                lvwDrives.Items.Remove(item);
            ConfigToSuffix();
        }

        private void btnS2N_Click(object sender, EventArgs e)
        {
            QuasaroConfig14 dialog = new QuasaroConfig14();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string domain = dialog.Domain;
                if (!domain.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) && !domain.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) && !domain.StartsWith("\\\\", StringComparison.InvariantCultureIgnoreCase))
                    domain = "https://" + domain;
                if (!domain.EndsWith("/"))
                    domain += "/";

                DriveConfiguration14[] drives = new DriveConfiguration14[2];
                drives[0] = new DriveConfiguration14(domain + "webdav/Projects", 'P', "Projects");
                drives[1] = new DriveConfiguration14(domain + "webdav/My", 'M', "My");

                ListViewItem[] items = new ListViewItem[drives.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = new ListViewItem(new string[] { drives[i].DriveLetter.ToString(), drives[i].RemoteAddress, drives[i].DriveLabel });
                    items[i].Tag = drives[i];
                }
                lvwDrives.Items.AddRange(items);
                ConfigToSuffix();
            }
        }

        private void lvwDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnEditDrive.Enabled = (lvwDrives.SelectedItems.Count == 1);
            btnRemoveDrive.Enabled = (lvwDrives.SelectedItems.Count > 0);
        }


        private void cbxDisablePvExcel_CheckedChanged(object sender, EventArgs e)
        {
            ConfigToSuffix();
        }

        private void cbxDisablePvWord_CheckedChanged(object sender, EventArgs e)
        {
            ConfigToSuffix();
        }

        private void cbxDisablePvPowerPoint_CheckedChanged(object sender, EventArgs e)
        {
            ConfigToSuffix();
        }

        private void cbxShowUserConfig_CheckedChanged(object sender, EventArgs e)
        {
            ConfigToSuffix();
        }

        private void tbxSuffix_MouseUp(object sender, MouseEventArgs e)
        {
            if (tbxSuffix.SelectionLength == 0)
                tbxSuffix.SelectAll();
        }

        private void tbxLicenseKey_TextChanged(object sender, EventArgs e)
        {
            ConfigToSuffix();
        }
    }
}
