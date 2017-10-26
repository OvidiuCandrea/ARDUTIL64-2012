using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ovidiu.x64.ARDUTIL
{
    public partial class ListaAxuri : Form
    {

        public ListaAxuri()
        {
            InitializeComponent();
        }

        private void ListaAxuri_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
                checkedListBox1.Focus();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void btnSel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void btnDesel_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, false);
            }
        }

        
        private void checkedListBox1_MouseHover(object sender, EventArgs e)
        {
            CheckedListBox cb = sender as CheckedListBox;
            cb.Focus();
        }

        private void checkedListBox1_MouseLeave(object sender, EventArgs e)
        {
            //this.btnExec.Focus();
        }
    }
}
