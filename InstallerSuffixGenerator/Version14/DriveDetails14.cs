using System;
using System.Linq;
using System.Windows.Forms;

namespace InstallerSuffixGenerator.Version14
{
    public partial class DriveDetails14 : Form
    {
        DriveConfiguration14 drive;


        public DriveDetails14(DriveConfiguration14 drive)
        {
            InitializeComponent();

            for (char c = 'A'; c <= 'Z'; c++)
                cbxDriveLetter.Items.Add(c + ":");

            this.drive = drive;
            tbxRemoteAddress.Text = this.drive.RemoteAddress;
            cbxDriveLetter.Text = char.ToUpper(this.drive.DriveLetter) + ":";
            tbxDriveLabel.Text = this.drive.DriveLabel;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if(tbxRemoteAddress.Text.Contains(ConfigurationSuffix14.NameShorthand))
            {
                MessageBox.Show("The character '" + ConfigurationSuffix14.NameShorthand.ToString() + "' is not allowed in remote addresses.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (tbxDriveLabel.Text.Contains(ConfigurationSuffix14.NameShorthand))
            {
                MessageBox.Show("The character '" + ConfigurationSuffix14.NameShorthand.ToString() + "' is not allowed in drive labels.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            this.DialogResult = DialogResult.OK;
            this.drive.ShortRemoteAddress = tbxRemoteAddress.Text;
            this.drive.DriveLetter = cbxDriveLetter.Text[0];
            this.drive.DriveLabel = tbxDriveLabel.Text;
            this.Close();
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
