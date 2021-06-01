namespace InstallerSuffixGenerator.Version14
{
    partial class QuasaroConfig14
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSave = new System.Windows.Forms.Button();
            this.tbxDomain = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblRemote = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(478, 142);
            this.btnSave.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(150, 44);
            this.btnSave.TabIndex = 0;
            this.btnSave.Text = "Create";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // tbxDomain
            // 
            this.tbxDomain.Location = new System.Drawing.Point(162, 87);
            this.tbxDomain.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.tbxDomain.Name = "tbxDomain";
            this.tbxDomain.Size = new System.Drawing.Size(462, 31);
            this.tbxDomain.TabIndex = 0;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(316, 142);
            this.btnClose.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(150, 44);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // lblRemote
            // 
            this.lblRemote.AutoSize = true;
            this.lblRemote.Location = new System.Drawing.Point(24, 92);
            this.lblRemote.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblRemote.Name = "lblRemote";
            this.lblRemote.Size = new System.Drawing.Size(85, 25);
            this.lblRemote.TabIndex = 5;
            this.lblRemote.Text = "Domain";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(24, 17);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(604, 63);
            this.label1.TabIndex = 6;
            this.label1.Text = "Create drives My and Projects for a Quasaro Domain like \"srv1.quasaro.de\":";
            // 
            // QuasaroConfig14
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(652, 210);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblRemote);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.tbxDomain);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "QuasaroConfig14";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Quasaro Config";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox tbxDomain;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblRemote;
        private System.Windows.Forms.Label label1;
    }
}