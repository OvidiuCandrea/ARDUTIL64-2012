namespace Ovidiu.x64.ARDUTIL
{
    partial class ListaAxuri
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
            this.label1 = new System.Windows.Forms.Label();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.btnSel = new System.Windows.Forms.Button();
            this.btnDesel = new System.Windows.Forms.Button();
            this.btnExec = new System.Windows.Forms.Button();
            this.btnAnuleaza = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(206, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Selecteaza axurile pentru notarea curbelor";
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkedListBox1.CheckOnClick = true;
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Location = new System.Drawing.Point(12, 40);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(528, 304);
            this.checkedListBox1.TabIndex = 1;
            this.checkedListBox1.MouseHover += new System.EventHandler(this.checkedListBox1_MouseHover);
            this.checkedListBox1.MouseLeave += new System.EventHandler(this.checkedListBox1_MouseLeave);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(241, 13);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(62, 17);
            this.checkBox1.TabIndex = 2;
            this.checkBox1.Text = "Filtru R-";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // btnSel
            // 
            this.btnSel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnSel.Location = new System.Drawing.Point(16, 373);
            this.btnSel.Name = "btnSel";
            this.btnSel.Size = new System.Drawing.Size(111, 43);
            this.btnSel.TabIndex = 3;
            this.btnSel.Text = "Selecteaza tot";
            this.btnSel.UseVisualStyleBackColor = true;
            this.btnSel.Click += new System.EventHandler(this.btnSel_Click);
            // 
            // btnDesel
            // 
            this.btnDesel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDesel.Location = new System.Drawing.Point(153, 373);
            this.btnDesel.Name = "btnDesel";
            this.btnDesel.Size = new System.Drawing.Size(111, 43);
            this.btnDesel.TabIndex = 4;
            this.btnDesel.Text = "Deselecteaza tot";
            this.btnDesel.UseVisualStyleBackColor = true;
            this.btnDesel.Click += new System.EventHandler(this.btnDesel_Click);
            // 
            // btnExec
            // 
            this.btnExec.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExec.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExec.Location = new System.Drawing.Point(290, 373);
            this.btnExec.Name = "btnExec";
            this.btnExec.Size = new System.Drawing.Size(111, 43);
            this.btnExec.TabIndex = 5;
            this.btnExec.Text = "Executa";
            this.btnExec.UseVisualStyleBackColor = true;
            // 
            // btnAnuleaza
            // 
            this.btnAnuleaza.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAnuleaza.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAnuleaza.Location = new System.Drawing.Point(429, 373);
            this.btnAnuleaza.Name = "btnAnuleaza";
            this.btnAnuleaza.Size = new System.Drawing.Size(111, 43);
            this.btnAnuleaza.TabIndex = 6;
            this.btnAnuleaza.Text = "Anuleaza";
            this.btnAnuleaza.UseVisualStyleBackColor = true;
            // 
            // ListaAxuri
            // 
            this.AcceptButton = this.btnExec;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAnuleaza;
            this.ClientSize = new System.Drawing.Size(552, 438);
            this.Controls.Add(this.btnAnuleaza);
            this.Controls.Add(this.btnExec);
            this.Controls.Add(this.btnDesel);
            this.Controls.Add(this.btnSel);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.checkedListBox1);
            this.Controls.Add(this.label1);
            this.MinimumSize = new System.Drawing.Size(568, 468);
            this.Name = "ListaAxuri";
            this.Text = "ListaAxuri";
            this.Load += new System.EventHandler(this.ListaAxuri_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnSel;
        private System.Windows.Forms.Button btnDesel;
        private System.Windows.Forms.Button btnAnuleaza;
        internal System.Windows.Forms.CheckedListBox checkedListBox1;
        internal System.Windows.Forms.CheckBox checkBox1;
        internal System.Windows.Forms.Button btnExec;
    }
}