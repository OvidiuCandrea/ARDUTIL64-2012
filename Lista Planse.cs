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
    public partial class Lista_Align : Form
    {
        private List<string> listaInitiala = new List<string>();
        public string Filtru = "XS";

        public void FiltreazaPlansele(string filtru)
        {
            chkListBox1.Items.Clear();
            foreach (string item in listaInitiala)
            {
                if (item.Contains(filtru))
                {
                    chkListBox1.Items.Add(item, true);
                }
            }
        }
        
        public Lista_Align()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            listaNume = new string[chkListBox1.CheckedItems.Count];
            for (int i = 0; i < chkListBox1.CheckedItems.Count; i++)
            {
                listaNume[i] = chkListBox1.CheckedItems[i].ToString();
            }
            if (radioProfile.Checked) Filtru = "PROFILE";

        }

        private void Lista_Planse_Load(object sender, EventArgs e)
        {
            foreach (object item in chkListBox1.Items)
            {
                listaInitiala.Add((string)item);
            }
            listaInitiala.Sort((n1, n2) =>
            {
                if (n1 == null || n2 == null) return 0;
            try
            {
                if (n1.Substring(0, n1.LastIndexOf('-') + 1).CompareTo(n2.Substring(0, n2.LastIndexOf('-') + 1)) != 0)
                {
                        //int i1 = 0, i2 = 0;
                        //string nr1 = new String((from char c in n1.Substring(0, n1.LastIndexOf('-') + 1) where char.IsDigit(c) select c).ToArray<char>());
                        //string nr2 = new String((from char c in n2.Substring(0, n2.LastIndexOf('-') + 1) where char.IsDigit(c) select c).ToArray<char>());
                        //bool r1 = int.TryParse(n1.Substring(0, n1.LastIndexOf('-') + 1), out i1);
                        //bool r2 = int.TryParse(n2.Substring(0, n2.LastIndexOf('-') + 1), out i2);
                        //if (r1 && r2) return i1.CompareTo(i2);
                        return n1.CompareTo(n2);
                    }
                else
                {
                    string nr1 = new String((from char c in n1.Substring(n1.LastIndexOf('-') + 1) where char.IsDigit(c) select c).ToArray<char>());
                    string nr2 = new String((from char c in n2.Substring(n2.LastIndexOf('-') + 1) where char.IsDigit(c) select c).ToArray<char>());
                    return int.Parse(nr1).CompareTo(int.Parse(nr2));
                    //return int.Parse(n1.Substring(n1.LastIndexOf('-') + 1)).CompareTo(int.Parse(n2.Substring(n2.LastIndexOf('-') + 1)));
                }
                }
                catch { return 1; }
            });
            string filtru = "XS";
            if (radioProfile.Checked) filtru = "PROFILE";
            FiltreazaPlansele(filtru);
            btnExport.Focus();
        }

        private void radioSectiuni_CheckedChanged(object sender, EventArgs e)
        {
            string filtru = "XS";
            if (radioProfile.Checked) filtru = "PROFILE";
            FiltreazaPlansele(filtru);
        }
    }
}
