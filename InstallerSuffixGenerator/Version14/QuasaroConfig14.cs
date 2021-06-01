using System;
using System.Windows.Forms;

namespace InstallerSuffixGenerator.Version14
{
    public partial class QuasaroConfig14 : Form
    {
        public string Domain
        {
            get { return tbxDomain.Text; }
            set { tbxDomain.Text = value; }
        }


        public QuasaroConfig14()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
