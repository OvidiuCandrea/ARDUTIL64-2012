namespace Ovidiu.x64.ARDUTIL

{
    partial class Lista_Align
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        public string[] listaNume;

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
            this.chkListBox1 = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnAnuleaza = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioProfile = new System.Windows.Forms.RadioButton();
            this.radioSectiuni = new System.Windows.Forms.RadioButton();
            this.chkBox2 = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkListBox1
            // 
            this.chkListBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkListBox1.FormattingEnabled = true;
            this.chkListBox1.Location = new System.Drawing.Point(12, 110);
            this.chkListBox1.Name = "chkListBox1";
            this.chkListBox1.Size = new System.Drawing.Size(348, 304);
            this.chkListBox1.TabIndex = 0;
            this.chkListBox1.SelectedIndexChanged += new System.EventHandler(this.checkedListBox1_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(158, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Selecteaza plansele de exportat";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExport.Location = new System.Drawing.Point(12, 437);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(155, 41);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "Exporta";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnAnuleaza
            // 
            this.btnAnuleaza.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAnuleaza.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAnuleaza.Location = new System.Drawing.Point(205, 437);
            this.btnAnuleaza.Name = "btnAnuleaza";
            this.btnAnuleaza.Size = new System.Drawing.Size(155, 41);
            this.btnAnuleaza.TabIndex = 3;
            this.btnAnuleaza.Text = "Anuleaza";
            this.btnAnuleaza.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.radioProfile);
            this.groupBox1.Controls.Add(this.radioSectiuni);
            this.groupBox1.Location = new System.Drawing.Point(12, 43);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(348, 57);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Tip de planse";
            // 
            // radioProfile
            // 
            this.radioProfile.AutoSize = true;
            this.radioProfile.Location = new System.Drawing.Point(215, 19);
            this.radioProfile.Name = "radioProfile";
            this.radioProfile.Size = new System.Drawing.Size(125, 17);
            this.radioProfile.TabIndex = 1;
            this.radioProfile.Text = "Profiluri Longitudinale";
            this.radioProfile.UseVisualStyleBackColor = true;
            // 
            // radioSectiuni
            // 
            this.radioSectiuni.AutoSize = true;
            this.radioSectiuni.Checked = true;
            this.radioSectiuni.Location = new System.Drawing.Point(6, 19);
            this.radioSectiuni.Name = "radioSectiuni";
            this.radioSectiuni.Size = new System.Drawing.Size(127, 17);
            this.radioSectiuni.TabIndex = 0;
            this.radioSectiuni.TabStop = true;
            this.radioSectiuni.Text = "Sectiuni Transversale";
            this.radioSectiuni.UseVisualStyleBackColor = true;
            this.radioSectiuni.CheckedChanged += new System.EventHandler(this.radioSectiuni_CheckedChanged);
            // 
            // chkBox2
            // 
            this.chkBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkBox2.AutoSize = true;
            this.chkBox2.Location = new System.Drawing.Point(12, 493);
            this.chkBox2.Name = "chkBox2";
            this.chkBox2.Size = new System.Drawing.Size(148, 17);
            this.chkBox2.TabIndex = 5;
            this.chkBox2.Text = "Deschide desenul rezultat";
            this.chkBox2.UseVisualStyleBackColor = true;
            // 
            // Lista_Planse
            // 
            this.AcceptButton = this.btnExport;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAnuleaza;
            this.ClientSize = new System.Drawing.Size(372, 522);
            this.Controls.Add(this.chkBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnAnuleaza);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkListBox1);
            this.Name = "Lista_Planse";
            this.Text = "Lista Planse";
            this.Load += new System.EventHandler(this.Lista_Planse_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.CheckedListBox chkListBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnAnuleaza;
        private System.Windows.Forms.GroupBox groupBox1;
        internal System.Windows.Forms.RadioButton radioProfile;
        internal System.Windows.Forms.RadioButton radioSectiuni;
        internal System.Windows.Forms.CheckBox chkBox2;
    }
}