namespace InstallerSuffixGenerator.Version14
{
    partial class Version14Form
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
            this.tbxSuffix = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbxDisablePvPowerPoint = new System.Windows.Forms.CheckBox();
            this.cbxDisablePvWord = new System.Windows.Forms.CheckBox();
            this.cbxDisablePvExcel = new System.Windows.Forms.CheckBox();
            this.btnCopy = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnS2N = new System.Windows.Forms.Button();
            this.btnRemoveDrive = new System.Windows.Forms.Button();
            this.btnEditDrive = new System.Windows.Forms.Button();
            this.btnAddDrive = new System.Windows.Forms.Button();
            this.lvwDrives = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbxShowUserConfig = new System.Windows.Forms.CheckBox();
            this.lblChars = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbxSuffix
            // 
            this.tbxSuffix.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxSuffix.Location = new System.Drawing.Point(24, 23);
            this.tbxSuffix.Margin = new System.Windows.Forms.Padding(6);
            this.tbxSuffix.Name = "tbxSuffix";
            this.tbxSuffix.Size = new System.Drawing.Size(1342, 31);
            this.tbxSuffix.TabIndex = 0;
            this.tbxSuffix.TextChanged += new System.EventHandler(this.tbxSuffix_TextChanged);
            this.tbxSuffix.MouseUp += new System.Windows.Forms.MouseEventHandler(this.tbxSuffix_MouseUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 67);
            this.label1.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(672, 50);
            this.label1.TabIndex = 1;
            this.label1.Text = "The suffix is placed between filename and extension of the installer:\r\n-> Example" +
    ": DriveInstaller@%B%B192.168.100.3%B#Z+Daten~=.msi";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.cbxDisablePvPowerPoint);
            this.groupBox1.Controls.Add(this.cbxDisablePvWord);
            this.groupBox1.Controls.Add(this.cbxDisablePvExcel);
            this.groupBox1.Location = new System.Drawing.Point(968, 140);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox1.Size = new System.Drawing.Size(564, 169);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Office Configuration";
            // 
            // cbxDisablePvPowerPoint
            // 
            this.cbxDisablePvPowerPoint.AutoSize = true;
            this.cbxDisablePvPowerPoint.Location = new System.Drawing.Point(12, 125);
            this.cbxDisablePvPowerPoint.Margin = new System.Windows.Forms.Padding(6);
            this.cbxDisablePvPowerPoint.Name = "cbxDisablePvPowerPoint";
            this.cbxDisablePvPowerPoint.Size = new System.Drawing.Size(387, 29);
            this.cbxDisablePvPowerPoint.TabIndex = 8;
            this.cbxDisablePvPowerPoint.Text = "PowerPoint: Disable Protected View";
            this.cbxDisablePvPowerPoint.UseVisualStyleBackColor = true;
            this.cbxDisablePvPowerPoint.CheckedChanged += new System.EventHandler(this.cbxDisablePvPowerPoint_CheckedChanged);
            // 
            // cbxDisablePvWord
            // 
            this.cbxDisablePvWord.AutoSize = true;
            this.cbxDisablePvWord.Location = new System.Drawing.Point(12, 81);
            this.cbxDisablePvWord.Margin = new System.Windows.Forms.Padding(6);
            this.cbxDisablePvWord.Name = "cbxDisablePvWord";
            this.cbxDisablePvWord.Size = new System.Drawing.Size(329, 29);
            this.cbxDisablePvWord.TabIndex = 7;
            this.cbxDisablePvWord.Text = "Word: Disable Protected View";
            this.cbxDisablePvWord.UseVisualStyleBackColor = true;
            this.cbxDisablePvWord.CheckedChanged += new System.EventHandler(this.cbxDisablePvWord_CheckedChanged);
            // 
            // cbxDisablePvExcel
            // 
            this.cbxDisablePvExcel.AutoSize = true;
            this.cbxDisablePvExcel.Location = new System.Drawing.Point(12, 37);
            this.cbxDisablePvExcel.Margin = new System.Windows.Forms.Padding(6);
            this.cbxDisablePvExcel.Name = "cbxDisablePvExcel";
            this.cbxDisablePvExcel.Size = new System.Drawing.Size(331, 29);
            this.cbxDisablePvExcel.TabIndex = 6;
            this.cbxDisablePvExcel.Text = "Excel: Disable Protected View";
            this.cbxDisablePvExcel.UseVisualStyleBackColor = true;
            this.cbxDisablePvExcel.CheckedChanged += new System.EventHandler(this.cbxDisablePvExcel_CheckedChanged);
            // 
            // btnCopy
            // 
            this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopy.Location = new System.Drawing.Point(1382, 19);
            this.btnCopy.Margin = new System.Windows.Forms.Padding(6);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(150, 44);
            this.btnCopy.TabIndex = 10;
            this.btnCopy.Text = "Copy";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.btnS2N);
            this.groupBox2.Controls.Add(this.btnRemoveDrive);
            this.groupBox2.Controls.Add(this.btnEditDrive);
            this.groupBox2.Controls.Add(this.btnAddDrive);
            this.groupBox2.Controls.Add(this.lvwDrives);
            this.groupBox2.Location = new System.Drawing.Point(24, 140);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox2.Size = new System.Drawing.Size(932, 365);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Drives Configuration";
            // 
            // btnS2N
            // 
            this.btnS2N.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnS2N.Location = new System.Drawing.Point(770, 310);
            this.btnS2N.Margin = new System.Windows.Forms.Padding(6);
            this.btnS2N.Name = "btnS2N";
            this.btnS2N.Size = new System.Drawing.Size(150, 44);
            this.btnS2N.TabIndex = 5;
            this.btnS2N.Text = "Quasaro";
            this.btnS2N.UseVisualStyleBackColor = true;
            this.btnS2N.Click += new System.EventHandler(this.btnS2N_Click);
            // 
            // btnRemoveDrive
            // 
            this.btnRemoveDrive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemoveDrive.Enabled = false;
            this.btnRemoveDrive.Location = new System.Drawing.Point(770, 148);
            this.btnRemoveDrive.Margin = new System.Windows.Forms.Padding(6);
            this.btnRemoveDrive.Name = "btnRemoveDrive";
            this.btnRemoveDrive.Size = new System.Drawing.Size(150, 44);
            this.btnRemoveDrive.TabIndex = 4;
            this.btnRemoveDrive.Text = "Remove";
            this.btnRemoveDrive.UseVisualStyleBackColor = true;
            this.btnRemoveDrive.Click += new System.EventHandler(this.btnRemoveDrive_Click);
            // 
            // btnEditDrive
            // 
            this.btnEditDrive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnEditDrive.Enabled = false;
            this.btnEditDrive.Location = new System.Drawing.Point(770, 92);
            this.btnEditDrive.Margin = new System.Windows.Forms.Padding(6);
            this.btnEditDrive.Name = "btnEditDrive";
            this.btnEditDrive.Size = new System.Drawing.Size(150, 44);
            this.btnEditDrive.TabIndex = 3;
            this.btnEditDrive.Text = "Edit";
            this.btnEditDrive.UseVisualStyleBackColor = true;
            this.btnEditDrive.Click += new System.EventHandler(this.btnEditDrive_Click);
            // 
            // btnAddDrive
            // 
            this.btnAddDrive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddDrive.Location = new System.Drawing.Point(770, 37);
            this.btnAddDrive.Margin = new System.Windows.Forms.Padding(6);
            this.btnAddDrive.Name = "btnAddDrive";
            this.btnAddDrive.Size = new System.Drawing.Size(150, 44);
            this.btnAddDrive.TabIndex = 2;
            this.btnAddDrive.Text = "Add";
            this.btnAddDrive.UseVisualStyleBackColor = true;
            this.btnAddDrive.Click += new System.EventHandler(this.btnAddDrive_Click);
            // 
            // lvwDrives
            // 
            this.lvwDrives.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvwDrives.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.lvwDrives.FullRowSelect = true;
            this.lvwDrives.GridLines = true;
            this.lvwDrives.HideSelection = false;
            this.lvwDrives.Location = new System.Drawing.Point(12, 37);
            this.lvwDrives.Margin = new System.Windows.Forms.Padding(6);
            this.lvwDrives.Name = "lvwDrives";
            this.lvwDrives.Size = new System.Drawing.Size(742, 314);
            this.lvwDrives.TabIndex = 1;
            this.lvwDrives.UseCompatibleStateImageBehavior = false;
            this.lvwDrives.View = System.Windows.Forms.View.Details;
            this.lvwDrives.SelectedIndexChanged += new System.EventHandler(this.lvwDrives_SelectedIndexChanged);
            this.lvwDrives.DoubleClick += new System.EventHandler(this.lvwDrives_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Letter";
            this.columnHeader1.Width = 30;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Remote Address";
            this.columnHeader2.Width = 230;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Label";
            this.columnHeader3.Width = 80;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.cbxShowUserConfig);
            this.groupBox3.Location = new System.Drawing.Point(968, 321);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(6);
            this.groupBox3.Size = new System.Drawing.Size(564, 185);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Other settings";
            // 
            // cbxShowUserConfig
            // 
            this.cbxShowUserConfig.AutoSize = true;
            this.cbxShowUserConfig.Location = new System.Drawing.Point(12, 37);
            this.cbxShowUserConfig.Margin = new System.Windows.Forms.Padding(6);
            this.cbxShowUserConfig.Name = "cbxShowUserConfig";
            this.cbxShowUserConfig.Size = new System.Drawing.Size(352, 29);
            this.cbxShowUserConfig.TabIndex = 9;
            this.cbxShowUserConfig.Text = "Show user configuration window";
            this.cbxShowUserConfig.UseVisualStyleBackColor = true;
            this.cbxShowUserConfig.CheckedChanged += new System.EventHandler(this.cbxShowUserConfig_CheckedChanged);
            // 
            // lblChars
            // 
            this.lblChars.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblChars.Location = new System.Drawing.Point(1160, 69);
            this.lblChars.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lblChars.Name = "lblChars";
            this.lblChars.Size = new System.Drawing.Size(372, 65);
            this.lblChars.TabIndex = 11;
            this.lblChars.Text = "0 Character(s)";
            this.lblChars.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Version14Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1556, 529);
            this.Controls.Add(this.lblChars);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbxSuffix);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(1554, 538);
            this.Name = "Version14Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Drive Installer Suffix Generator (Version 1.4 and above)";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbxSuffix;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.CheckBox cbxDisablePvPowerPoint;
        private System.Windows.Forms.CheckBox cbxDisablePvWord;
        private System.Windows.Forms.CheckBox cbxDisablePvExcel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnRemoveDrive;
        private System.Windows.Forms.Button btnEditDrive;
        private System.Windows.Forms.Button btnAddDrive;
        private System.Windows.Forms.ListView lvwDrives;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.CheckBox cbxShowUserConfig;
        private System.Windows.Forms.Button btnS2N;
        private System.Windows.Forms.Label lblChars;
    }
}

