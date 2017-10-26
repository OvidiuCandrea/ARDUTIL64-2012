namespace Ovidiu.x64.ARDUTIL
{
    partial class Poze
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
            //Dezinstalare carlig soarece
            MH.Uninstall();
            
            if (disposing) disposed = true;
            if (disposing && (components != null))
            {
                if (!trans.IsDisposed) trans.Dispose();
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
            this.btnAx = new System.Windows.Forms.Button();
            this.buttonDir = new System.Windows.Forms.Button();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBoxKm = new System.Windows.Forms.TextBox();
            this.labelKm = new System.Windows.Forms.Label();
            this.textBoxDir = new System.Windows.Forms.TextBox();
            this.labelInterval = new System.Windows.Forms.Label();
            this.textBoxInterval = new System.Windows.Forms.TextBox();
            this.comboBoxAx = new System.Windows.Forms.ComboBox();
            this.checkBoxSincr = new System.Windows.Forms.CheckBox();
            this.textBoxSalt = new System.Windows.Forms.TextBox();
            this.labelSalt = new System.Windows.Forms.Label();
            this.checkBoxR = new System.Windows.Forms.CheckBox();
            this.buttonStrgSzr = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnAx
            // 
            this.btnAx.Location = new System.Drawing.Point(15, 8);
            this.btnAx.Name = "btnAx";
            this.btnAx.Size = new System.Drawing.Size(69, 27);
            this.btnAx.TabIndex = 1;
            this.btnAx.Text = "Selectie Ax";
            this.btnAx.UseVisualStyleBackColor = true;
            // 
            // buttonDir
            // 
            this.buttonDir.Location = new System.Drawing.Point(454, 8);
            this.buttonDir.Name = "buttonDir";
            this.buttonDir.Size = new System.Drawing.Size(99, 27);
            this.buttonDir.TabIndex = 3;
            this.buttonDir.Text = "Director Foto";
            this.buttonDir.UseVisualStyleBackColor = true;
            this.buttonDir.Click += new System.EventHandler(this.buttonDir_Click);
            // 
            // trackBar1
            // 
            this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar1.Location = new System.Drawing.Point(13, 697);
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(1350, 45);
            this.trackBar1.TabIndex = 6;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(13, 42);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(1350, 649);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // textBoxKm
            // 
            this.textBoxKm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxKm.Location = new System.Drawing.Point(1046, 12);
            this.textBoxKm.Name = "textBoxKm";
            this.textBoxKm.Size = new System.Drawing.Size(81, 20);
            this.textBoxKm.TabIndex = 8;
            this.textBoxKm.Text = "0";
            this.textBoxKm.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxKm.TextChanged += new System.EventHandler(this.textBoxKm_TextChanged);
            // 
            // labelKm
            // 
            this.labelKm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelKm.AutoSize = true;
            this.labelKm.Location = new System.Drawing.Point(1012, 15);
            this.labelKm.Name = "labelKm";
            this.labelKm.Size = new System.Drawing.Size(28, 13);
            this.labelKm.TabIndex = 9;
            this.labelKm.Text = "Km: ";
            // 
            // textBoxDir
            // 
            this.textBoxDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDir.Location = new System.Drawing.Point(560, 12);
            this.textBoxDir.Name = "textBoxDir";
            this.textBoxDir.Size = new System.Drawing.Size(436, 20);
            this.textBoxDir.TabIndex = 10;
            this.textBoxDir.Text = "-";
            this.textBoxDir.TextChanged += new System.EventHandler(this.textBoxDir_TextChanged);
            // 
            // labelInterval
            // 
            this.labelInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInterval.AutoSize = true;
            this.labelInterval.Location = new System.Drawing.Point(1144, 15);
            this.labelInterval.Name = "labelInterval";
            this.labelInterval.Size = new System.Drawing.Size(48, 13);
            this.labelInterval.TabIndex = 12;
            this.labelInterval.Text = "Interval: ";
            // 
            // textBoxInterval
            // 
            this.textBoxInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInterval.Location = new System.Drawing.Point(1198, 12);
            this.textBoxInterval.Name = "textBoxInterval";
            this.textBoxInterval.Size = new System.Drawing.Size(30, 20);
            this.textBoxInterval.TabIndex = 11;
            this.textBoxInterval.Text = "20";
            this.textBoxInterval.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // comboBoxAx
            // 
            this.comboBoxAx.DisplayMember = "Name";
            this.comboBoxAx.FormattingEnabled = true;
            this.comboBoxAx.Location = new System.Drawing.Point(133, 12);
            this.comboBoxAx.Name = "comboBoxAx";
            this.comboBoxAx.Size = new System.Drawing.Size(315, 21);
            this.comboBoxAx.TabIndex = 13;
            this.comboBoxAx.ValueMember = "Name";
            this.comboBoxAx.SelectedIndexChanged += new System.EventHandler(this.comboBoxAx_SelectedIndexChanged);
            // 
            // checkBoxSincr
            // 
            this.checkBoxSincr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSincr.AutoSize = true;
            this.checkBoxSincr.Location = new System.Drawing.Point(1253, 14);
            this.checkBoxSincr.Name = "checkBoxSincr";
            this.checkBoxSincr.Size = new System.Drawing.Size(110, 17);
            this.checkBoxSincr.TabIndex = 15;
            this.checkBoxSincr.Text = "Sincronizare ARD";
            this.checkBoxSincr.UseVisualStyleBackColor = true;
            this.checkBoxSincr.CheckedChanged += new System.EventHandler(this.checkBoxSincr_CheckedChanged);
            // 
            // textBoxSalt
            // 
            this.textBoxSalt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSalt.Location = new System.Drawing.Point(679, 721);
            this.textBoxSalt.MinimumSize = new System.Drawing.Size(100, 20);
            this.textBoxSalt.Name = "textBoxSalt";
            this.textBoxSalt.Size = new System.Drawing.Size(100, 20);
            this.textBoxSalt.TabIndex = 16;
            // 
            // labelSalt
            // 
            this.labelSalt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSalt.AutoSize = true;
            this.labelSalt.Location = new System.Drawing.Point(614, 724);
            this.labelSalt.Name = "labelSalt";
            this.labelSalt.Size = new System.Drawing.Size(59, 13);
            this.labelSalt.TabIndex = 17;
            this.labelSalt.Text = "Sari la km: ";
            // 
            // checkBoxR
            // 
            this.checkBoxR.AutoSize = true;
            this.checkBoxR.Checked = true;
            this.checkBoxR.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxR.Location = new System.Drawing.Point(90, 14);
            this.checkBoxR.Name = "checkBoxR";
            this.checkBoxR.Size = new System.Drawing.Size(37, 17);
            this.checkBoxR.TabIndex = 18;
            this.checkBoxR.Text = "R-";
            this.checkBoxR.UseVisualStyleBackColor = true;
            this.checkBoxR.CheckedChanged += new System.EventHandler(this.checkBoxR_CheckedChanged);
            // 
            // buttonStrgSzr
            // 
            this.buttonStrgSzr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStrgSzr.Location = new System.Drawing.Point(1235, 724);
            this.buttonStrgSzr.Name = "buttonStrgSzr";
            this.buttonStrgSzr.Size = new System.Drawing.Size(128, 27);
            this.buttonStrgSzr.TabIndex = 19;
            this.buttonStrgSzr.Text = "Curata triunghi senzor";
            this.buttonStrgSzr.UseVisualStyleBackColor = true;
            this.buttonStrgSzr.Click += new System.EventHandler(this.buttonStrgSzr_Click);
            // 
            // Poze
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1375, 754);
            this.Controls.Add(this.buttonStrgSzr);
            this.Controls.Add(this.checkBoxR);
            this.Controls.Add(this.labelSalt);
            this.Controls.Add(this.textBoxSalt);
            this.Controls.Add(this.checkBoxSincr);
            this.Controls.Add(this.comboBoxAx);
            this.Controls.Add(this.labelInterval);
            this.Controls.Add(this.textBoxInterval);
            this.Controls.Add(this.textBoxDir);
            this.Controls.Add(this.labelKm);
            this.Controls.Add(this.textBoxKm);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.buttonDir);
            this.Controls.Add(this.btnAx);
            this.Name = "Poze";
            this.Text = "Vizualizare Instantanee Drum";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button btnAx;
        public System.Windows.Forms.Button buttonDir;
        public System.Windows.Forms.TrackBar trackBar1;
        public System.Windows.Forms.PictureBox pictureBox1;
        public System.Windows.Forms.TextBox textBoxKm;
        public System.Windows.Forms.Label labelKm;
        private System.Windows.Forms.TextBox textBoxDir;
        public System.Windows.Forms.Label labelInterval;
        public System.Windows.Forms.TextBox textBoxInterval;
        private System.Windows.Forms.ComboBox comboBoxAx;
        private System.Windows.Forms.CheckBox checkBoxSincr;
        private System.Windows.Forms.TextBox textBoxSalt;
        public System.Windows.Forms.Label labelSalt;
        private System.Windows.Forms.CheckBox checkBoxR;
        public System.Windows.Forms.Button buttonStrgSzr;
    }
}