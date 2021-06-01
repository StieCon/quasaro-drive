namespace InstallerSuffixGenerator.Version14
{
    partial class DriveDetails14
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
            this.tbxRemoteAddress = new System.Windows.Forms.TextBox();
            this.cbxDriveLetter = new System.Windows.Forms.ComboBox();
            this.tbxDriveLabel = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblRemote = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(239, 101);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // tbxRemoteAddress
            // 
            this.tbxRemoteAddress.Location = new System.Drawing.Point(118, 12);
            this.tbxRemoteAddress.Name = "tbxRemoteAddress";
            this.tbxRemoteAddress.Size = new System.Drawing.Size(196, 20);
            this.tbxRemoteAddress.TabIndex = 0;
            // 
            // cbxDriveLetter
            // 
            this.cbxDriveLetter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxDriveLetter.FormattingEnabled = true;
            this.cbxDriveLetter.Location = new System.Drawing.Point(118, 38);
            this.cbxDriveLetter.Name = "cbxDriveLetter";
            this.cbxDriveLetter.Size = new System.Drawing.Size(80, 21);
            this.cbxDriveLetter.TabIndex = 1;
            // 
            // tbxDriveLabel
            // 
            this.tbxDriveLabel.Location = new System.Drawing.Point(118, 65);
            this.tbxDriveLabel.Name = "tbxDriveLabel";
            this.tbxDriveLabel.Size = new System.Drawing.Size(196, 20);
            this.tbxDriveLabel.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(158, 101);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // lblRemote
            // 
            this.lblRemote.AutoSize = true;
            this.lblRemote.Location = new System.Drawing.Point(12, 15);
            this.lblRemote.Name = "lblRemote";
            this.lblRemote.Size = new System.Drawing.Size(85, 13);
            this.lblRemote.TabIndex = 5;
            this.lblRemote.Text = "Remote Address";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Drive Letter";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Drive Label";
            // 
            // DriveDetails134
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(326, 136);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblRemote);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.tbxDriveLabel);
            this.Controls.Add(this.cbxDriveLetter);
            this.Controls.Add(this.tbxRemoteAddress);
            this.Controls.Add(this.btnSave);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DriveDetails134";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Drive details";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox tbxRemoteAddress;
        private System.Windows.Forms.ComboBox cbxDriveLetter;
        private System.Windows.Forms.TextBox tbxDriveLabel;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblRemote;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}