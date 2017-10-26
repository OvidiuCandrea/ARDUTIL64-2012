using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO = System.IO;
using WF = System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Interop;
using Autodesk.Civil.ApplicationServices;
//using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.DatabaseServices.Styles;
using Autodesk.Civil.Land.DatabaseServices;
using System.Reflection;
using Ovidiu.StringUtil;


namespace Ovidiu.x64.ARDUTIL
{
    public partial class Poze : WF.Form
    {
        bool disposed = false;
        //~Poze()
        //{
        //    MH.Uninstall();
        //    //trans.Dispose();
        //    //ed.WriteMessage("\nPoze a fost curatat de finalizor!; disposed: {0}", disposed);
        //    Dispose(false);
        //}

        Document acadDoc = Application.DocumentManager.MdiActiveDocument;
        CivilDocument civDoc;
        ObjectIdCollection idAxuri;
        Database db;
        Editor ed;
        Transaction trans;
        List<Alignment> Axuri = new List<Alignment>();
        Solid Senzor = null;
        ViewTableRecord VTRcur = null;
        SortedDictionary<int, string> imagini;
        SortedDictionary<int, string> imaginiCheie;
        List<string> listaFisiere;
        RamGecTools.MouseHook MH = new RamGecTools.MouseHook();
        DocumentLock DL = null;

        public Poze()
        {
            civDoc = CivilApplication.ActiveDocument;
            db = acadDoc.Database;
            ed = acadDoc.Editor;
            //trans = db.TransactionManager.StartTransaction();
            using (trans = db.TransactionManager.StartTransaction())
            {
                idAxuri = civDoc.GetAlignmentIds();
                InitializeComponent();
                imagini = new SortedDictionary<int, string>();
                imaginiCheie = new SortedDictionary<int, string>();
                listaFisiere = new List<string>();

                ActualizeazaAxuri();
                //comboBoxAx.SelectedIndex = 0;

                RegAppTable rat = (RegAppTable)trans.GetObject(db.RegAppTableId, OpenMode.ForWrite, false);
                if (!rat.Has("OVIDIU"))
                {
                    //rat.UpgradeOpen();
                    RegAppTableRecord ratr = new RegAppTableRecord();
                    ratr.Name = "OVIDIU";
                    rat.Add(ratr);
                    trans.AddNewlyCreatedDBObject(ratr, true);
                }
                trans.Commit();
            }

            //Instalare carlig pentru soarece
            MH.RightButtonDown += MH_RightButtonDown;
            MH.LeftButtonDown += MH_LeftButtonDown;
            if (checkBoxSincr.Checked) MH.Install();

            //HookManager.MouseClick += HookManager_MouseClick;
            //SelectieDirector();
            //AfisarePoze();

        }

        private void MH_LeftButtonDown(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            //MH.Uninstall();
            checkBoxSincr.Checked = false;
        }

        private void ActualizarePoze()
        {
            imagini.Clear();
            listaFisiere.Clear();
            Alignment Ax = comboBoxAx.SelectedItem as Alignment;
            if (Ax == null) WF.MessageBox.Show((string)comboBoxAx.SelectedItem);

            var fisiere = from string fisier in IO.Directory.GetFiles(textBoxDir.Text, "*.*", IO.SearchOption.AllDirectories)
                          where ".jpg.bmp.png.gif.jpeg.tiff".Contains(new IO.FileInfo(fisier).Extension.ToLower())
                          orderby fisier
                          select fisier;


            if (fisiere.Count() == 0)
            {
                WF.MessageBox.Show("Directorul nu contine imagini!");
                return;
            }
            else
            {
                listaFisiere.AddRange(fisiere);
            }


            double lungimeAx = Ax.EndingStation - Ax.StartingStation;
            if (imaginiCheie.Count == 0)
            {
                imaginiCheie.Add((int)(Ax.StartingStation * 1000), fisiere.First());
                imaginiCheie.Add((int)(Ax.EndingStation * 1000), fisiere.Last());
                //double interval = (Ax.EndingStation - Ax.StartingStation) / (fisiere.Count() - 1);
                //for (int i = 0; i < fisiere.Count(); i++)
                //{
                //    double km = i * interval;
                //    //WF.MessageBox.Show(string.Format("Lungime ax: {0}\nInterval intre poze: {1}\nPozitia curenta:{2}", lungimeAx, interval, km)); //Raport individual imagini
                //    imagini.Add((int)(km * 1000), fisiere.ElementAt(i));
                //}
            }
            else if (imaginiCheie.Count == 1)
            {
                if (imaginiCheie.First().Key < (int)(Ax.EndingStation * 1000))
                {
                    imaginiCheie.Add((int)(Ax.EndingStation * 1000), fisiere.Last());
                }
                else
                {
                    imaginiCheie.Add((int)(Ax.StartingStation * 1000), fisiere.First());
                }
            }
            for (int j = 1; j < imaginiCheie.Count; j++)
            {
                double interval = (double)(imaginiCheie.ElementAt(j).Key - imaginiCheie.ElementAt(j - 1).Key) /
                     (listaFisiere.IndexOf(imaginiCheie.ElementAt(j).Value) - listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) - 0);
                if (j == 1 && listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) > 0)
                {
                    for (int nr = 1; nr < listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) - 1; nr++)
                    {
                        int index = listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) - nr;
                        imagini.Add((int)(imaginiCheie.ElementAt(j - 1).Key - nr * interval), listaFisiere[index]);
                    }
                }
                for (int nr = 0; nr <= (listaFisiere.IndexOf(imaginiCheie.ElementAt(j).Value) - listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) - 0); nr++)
                {
                    int index = listaFisiere.IndexOf(imaginiCheie.ElementAt(j - 1).Value) + nr;
                    int cheie = (int)(imaginiCheie.ElementAt(j - 1).Key + nr * interval);
                    if (!imagini.Keys.Contains(cheie))
                    {
                        imagini.Add((int)(imaginiCheie.ElementAt(j - 1).Key + nr * interval), listaFisiere[index]);
                    }
                }
                if (j == imaginiCheie.Count - 1 && listaFisiere.IndexOf(imaginiCheie.ElementAt(j).Value) < listaFisiere.Count - 1)
                {
                    for (int nr = 1; nr < listaFisiere.Count - listaFisiere.IndexOf(imaginiCheie.ElementAt(j).Value) - 0; nr++)
                    {
                        int index = listaFisiere.IndexOf(imaginiCheie.ElementAt(j).Value) + nr;
                        imagini.Add((int)(imaginiCheie.ElementAt(j).Key + nr * interval), listaFisiere[index]);
                    }
                }
            }


            //Scriere rezultat calibrare
            IO.FileInfo calibrare = new IO.FileInfo(textBoxDir.Text + @"\CALIBRARE.TXT");
            using (IO.StreamWriter SR = calibrare.CreateText())
            {
                foreach (int cheie in imagini.Keys)
                {
                    string fix = string.Empty;
                    if (imaginiCheie.Keys.Contains(cheie)) fix = "********";
                    SR.WriteLine(string.Format("Km: {0} -> {1}{2}", cheie, imagini[cheie], fix));
                }
            }



            //WF.Binding Bind = new WF.Binding("Value", imagini, "Keys");
            //trackBar1.DataBindings.Add(Bind);
            //    trackBar1.Minimum = (int)(Ax.StartingStation * 1000);
            //trackBar1.Maximum = (int)(Ax.EndingStation * 1000) + 1000;
            trackBar1.Minimum = imagini.ElementAt(0).Key;
            trackBar1.Maximum = imagini.Last().Key;
            trackBar1.Value = trackBar1.Minimum;
            trackBar1.SmallChange = 5000;
            try
            {
                trackBar1.LargeChange = int.Parse(textBoxInterval.Text) * 1000;
            }
            catch
            {
                trackBar1.LargeChange = trackBar1.SmallChange * 5;
            }
            //trackBar1.SmallChange = (int)(interval * 1000);
            trackBar1.LargeChange = int.Parse(textBoxInterval.Text) * 1000;
            //trackBar1.LargeChange = trackBar1.SmallChange * 5;
            trackBar1.TickFrequency = trackBar1.SmallChange;
            trackBar1.TickStyle = System.Windows.Forms.TickStyle.BottomRight;

            textBoxKm.Leave += TextBoxKm_Leave;
            textBoxKm.KeyPress += TextBoxKm_KeyPress;
            textBoxSalt.KeyPress += TextBoxSalt_KeyPress;

            AfisarePoza();

        }

        private void TextBoxKm_KeyPress(object sender, WF.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)WF.Keys.Return)
            {
                trackBar1.Focus();
            }
        }

        private void TextBoxSalt_KeyPress(object sender, WF.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)WF.Keys.Return)
            {
                double val;
                if (double.TryParse(textBoxSalt.Text, out val))
                {
                    trackBar1.Value = (from k in imagini.Keys orderby Math.Abs((int)(val * 1000) - k) select k).First();
                    AfisarePoza();
                }
            }
        }

        private void TextBoxKm_Leave(object sender, EventArgs e)
        {
            int kmAnterior = (from k in imagini.Keys orderby Math.Abs(trackBar1.Value - k) select k).First();
            double kmNou = -999;
            double.TryParse(textBoxKm.Text, out kmNou);
            if (kmNou != -999)
            {
                if (imaginiCheie.Keys.Contains(kmAnterior))
                {
                    imaginiCheie.Remove(kmAnterior);
                }
                imaginiCheie.Add((int)(kmNou * 1000), pictureBox1.ImageLocation);
                var cheiSortate = from cheie in imaginiCheie orderby cheie.Key select cheie;
                //imaginiCheie.Clear();
                //foreach (var cheie in cheiSortate)
                //{
                //    imaginiCheie.Add(cheie.Key, cheie.Value);
                //}
                //WF.MessageBox.Show("Imagine cheie km: " + kmNou.ToString());
            }
            else
            {
                WF.MessageBox.Show("Pozitie kilometrica invalida!");
            }
            ActualizarePoze();
        }



        private void AfisarePoza()
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            pictureBox1.ImageLocation = imagini[trackBar1.Value];
            pictureBox1.SizeMode = WF.PictureBoxSizeMode.Zoom;
            pictureBox1.Load();
            if (imaginiCheie.Values.Contains(pictureBox1.ImageLocation))
            {
                textBoxKm.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                textBoxKm.ForeColor = System.Drawing.Color.Black;
            }
            textBoxKm.Text = ((double)trackBar1.Value / 1000).ToString();
            textBoxSalt.Text = ((double)trackBar1.Value / 1000).ToString();
        }

        private void CentrarePeSenzor()
        {
            if (Senzor != null && VTRcur != null)
            {
                double[] SenzorXs = { Senzor.GetPointAt(0).X, Senzor.GetPointAt(1).X, Senzor.GetPointAt(2).X };
                double[] SenzorYs = { Senzor.GetPointAt(0).Y, Senzor.GetPointAt(1).Y, Senzor.GetPointAt(2).Y };
                VTRcur = ed.GetCurrentView() as ViewTableRecord;
                VTRcur.CenterPoint = new Point2d(SenzorXs.Average(), SenzorYs.Average());
                ed.WriteMessage("\nCurrent CenterPoint: {0}", VTRcur.CenterPoint);
                ed.SetCurrentView(VTRcur);
            }
        }

        private void SelectieDirector()
        {
            WF.FolderBrowserDialog FBD = new WF.FolderBrowserDialog();
            if (FBD.ShowDialog() == WF.DialogResult.OK)
            {
                textBoxDir.Text = FBD.SelectedPath;
            }
            else
            {
                textBoxDir.Text = @"-";
            }
        }

        private void CalibrarePoze()
        {
            trackBar1.Value = (from k in imagini.Keys orderby Math.Abs(trackBar1.Value - k) select k).First();
        }

        private void buttonDir_Click(object sender, EventArgs e)
        {
            SelectieDirector();
        }

        private void textBoxDir_TextChanged(object sender, EventArgs e)
        {
            imagini.Clear();
            imaginiCheie.Clear();
            listaFisiere.Clear();
            pictureBox1.Image = null;

            if (IO.Directory.Exists(textBoxDir.Text))
            {
                //Asociere director cu aliniamentul
                using (acadDoc.LockDocument())
                using (Transaction trdir = db.TransactionManager.StartTransaction())
                using (ResultBuffer RB = new ResultBuffer(new TypedValue(1001, "OVIDIU"), new TypedValue(1000, "POZE->" + textBoxDir.Text)))
                {
                    //Alignment Ax = (Alignment)trans.GetObject(((Alignment)comboBoxAx.SelectedItem).Id, OpenMode.ForWrite);
                    string nume = ((Alignment)comboBoxAx.SelectedItem).Name;
                    Alignment Ax = null;
                    foreach (ObjectId idAx in civDoc.GetAlignmentIds())
                    {
                        Ax = (Alignment)trdir.GetObject(idAx, OpenMode.ForRead);
                        if (Ax.Name == nume)
                        {
                            Ax.UpgradeOpen();
                            Ax.XData = RB;
                        }
                    }
                    //Ax.UpgradeOpen();
                    //Ax.XData = RB;


                    //Capturare Senzor
                    CapturaSenzor();
                    //CapturaSenzor(trans, Ax);
                    //TypedValue[] TVs = new TypedValue[]
                    //{
                    //    new TypedValue(0, "SOLID"),
                    //    new TypedValue(8, "AR-MArker"),
                    //    new TypedValue(62, "3")
                    //};
                    //SelectionFilter SF = new SelectionFilter(TVs);
                    //PromptSelectionResult PSR = ed.SelectAll(SF);
                    //if (PSR.Status != PromptStatus.OK) return;
                    //SelectionSet SS = PSR.Value;
                    //foreach (SelectedObject SO in SS)
                    //{
                    //    Solid S = (Solid)trans.GetObject(SO.ObjectId, OpenMode.ForRead);
                    //    double[] Xs = { S.GetPointAt(0).X, S.GetPointAt(1).X, S.GetPointAt(2).X };
                    //    double[] Ys = { S.GetPointAt(0).Y, S.GetPointAt(1).Y, S.GetPointAt(2).Y };
                    //    double km = double.NaN;
                    //    double offset = double.NaN;
                    //    try
                    //    {
                    //        Ax.StationOffset(Xs.Average(), Ys.Average(), ref km, ref offset);
                    //    }
                    //    catch { }
                    //    if (offset != double.NaN)
                    //    {
                    //        if (Senzor == null) Senzor = S;
                    //        else
                    //        {
                    //            double[] SenzorXs = { Senzor.GetPointAt(0).X, Senzor.GetPointAt(1).X, Senzor.GetPointAt(2).X };
                    //            double[] SenzorYs = { Senzor.GetPointAt(0).Y, Senzor.GetPointAt(1).Y, Senzor.GetPointAt(2).Y };
                    //            double SenzorKm = double.NaN;
                    //            double SenzorOffset = double.NaN;
                    //            try
                    //            {
                    //                Ax.StationOffset(SenzorXs.Average(), SenzorYs.Average(), ref SenzorKm, ref SenzorOffset);
                    //                if (offset < SenzorOffset) Senzor = S;
                    //            }
                    //            catch { }
                    //        }
                    //    }
                    //}
                    //if (Senzor != null) checkBoxSincr.Checked = true; //

                    trdir.Commit();
                }//

                //Inregistrare vedere curenta
                VTRcur = ed.GetCurrentView().Clone() as ViewTableRecord;

                //Citire imaginiCheie din fisierul CALIBRARE.TXT daca acesta exista
                if (IO.File.Exists(textBoxDir.Text + @"\CALIBRARE.TXT"))
                {
                    using (IO.StreamReader reader = new IO.FileInfo(textBoxDir.Text + @"\CALIBRARE.TXT").OpenText())
                    {
                        string linie = null;
                        while ((linie = reader.ReadLine()) != null)
                        {
                            if (linie.Contains("********"))
                            {
                                try
                                {
                                    string[] componente = linie.Remove(0, 4).Replace("********", "").Split(new string[] { "-> " }, StringSplitOptions.None);
                                    imaginiCheie.Add(int.Parse(componente[0]), componente[1]);
                                }
                                catch { }
                            }
                        }
                    }
                    //WF.MessageBox.Show("Numar chei citite: " + imaginiCheie.Count.ToString() + ": " + imaginiCheie.ToString());
                }

                ActualizarePoze();
            }
            else
            {
                textBoxDir.Text = @"-";
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            try
            {
                trackBar1.Value = (from k in imagini.Keys orderby Math.Abs(trackBar1.Value - k) select k).First();
                AfisarePoza();
                //CentrarePeSenzor();
            }
            catch
            {
                WF.MessageBox.Show(string.Format("Nepotrivire km bara: {0}/ km poza: {1}", trackBar1.Value.ToString(), (from k in imagini.Keys orderby Math.Abs(trackBar1.Value - k) select k).First()));
            }
        }

        private void textBoxKm_TextChanged(object sender, EventArgs e)
        {
            int km = -999;
            int.TryParse(textBoxKm.Text, out km);
            //if (imaginiCheie.Keys.Contains(km))
            //{
            //    //imaginiCheie.Add(kmCheie, pictureBox1.ImageLocation);
            //    //WF.MessageBox.Show("Imagine cheie km: " + kmCheie.ToString());
            //    textBoxKm.ForeColor = System.Drawing.Color.Red;
            //}
            //else
            //{
            //    textBoxKm.ForeColor = System.Drawing.Color.Black;
            //}
        }

        private void comboBoxAx_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Incercare actualizare director din Xdata si incarcare poze
            Alignment Ax = (Alignment)comboBoxAx.SelectedItem;
            try
            {
                bool gasit = false;
                ResultBuffer RB = Ax.XData;
                foreach (TypedValue TV in RB)
                {
                    if (TV.TypeCode == 1000 && TV.Value is string)
                    {
                        string valoare = TV.Value as string;
                        if (valoare.StartsWith("POZE->"))
                        {
                            textBoxDir.Text = valoare.Remove(0, 6);
                            gasit = true;
                        }
                    }
                }
                if (!gasit)
                {
                    textBoxDir.Text = "-";
                }
            }
            catch
            {
                textBoxDir.Text = "-";
            }
        }

        private void MH_RightButtonDown(RamGecTools.MouseHook.MSLLHOOKSTRUCT mouseStruct)
        {
            Alignment Ax = comboBoxAx.SelectedItem as Alignment;

            //Mutarea Senzorului daca fereastra Civil 3d este activa
            //Window fereastra = acadDoc.Window;
            //WF.MessageBox.Show(string.Format("Pane 1: {0}\nDocument activ? {1}", Application.StatusBar.Panes[0].Text, acadDoc.IsActive));


            if (Senzor != null)
            {
                double[] Xs = { Senzor.GetPointAt(0).X, Senzor.GetPointAt(1).X, Senzor.GetPointAt(2).X };
                double[] Ys = { Senzor.GetPointAt(0).Y, Senzor.GetPointAt(1).Y, Senzor.GetPointAt(2).Y };
                double km = double.NaN;
                double offset = double.NaN;
                Ax.StationOffset(Xs.Average(), Ys.Average(), ref km, ref offset);
                if (km != double.NaN)
                {
                    trackBar1.Value = (from k in imagini.Keys orderby Math.Abs((int)(km * 1000) - k) select k).First();
                    AfisarePoza();
                    //CentrarePeSenzor();
                }
            }
        }

        private void checkBoxSincr_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSincr.Checked)
            {
                Senzor = null;
                CapturaSenzor(); //Ar trebui sa captureze Senzorul
                if (Senzor != null)
                {
                    //checkBoxSincr.Checked = true;
                    MH.Install();
                }
                else checkBoxSincr.Checked = false;
            }
            else MH.Uninstall();
        }

        //private void CapturaSenzor(Transaction trans, Alignment Ax)
        private void CapturaSenzor()
        {
            Alignment Ax = comboBoxAx.SelectedItem as Alignment;
            if (Ax != null)
            {
                TypedValue[] TVs = new TypedValue[]
                        {
                        new TypedValue(0, "SOLID"),
                        new TypedValue(8, "AR-MArker"),
                        new TypedValue(62, "3")
                        };
                SelectionFilter SF = new SelectionFilter(TVs);
                PromptSelectionResult PSR = ed.SelectAll(SF);
                if (PSR.Status != PromptStatus.OK) return;
                SelectionSet SS = PSR.Value;
                using (Transaction trSzr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject SO in SS)
                    {
                        if (SO.ObjectId.IsValid)
                        {
                            Solid S = null;
                            //try
                            //{
                                S = (Solid)trSzr.GetObject(SO.ObjectId, OpenMode.ForRead);
                            //}
                            //catch
                            //{
                            //    continue;
                            //}
                            double[] Xs = { S.GetPointAt(0).X, S.GetPointAt(1).X, S.GetPointAt(2).X };
                            double[] Ys = { S.GetPointAt(0).Y, S.GetPointAt(1).Y, S.GetPointAt(2).Y };
                            double km = double.NaN;
                            double offset = double.NaN;
                            try
                            {
                                Ax.StationOffset(Xs.Average(), Ys.Average(), ref km, ref offset);
                            }
                            catch { }
                            if (offset != double.NaN)
                            {
                                if (Senzor == null) Senzor = S;
                                else
                                {
                                    double[] SenzorXs = { Senzor.GetPointAt(0).X, Senzor.GetPointAt(1).X, Senzor.GetPointAt(2).X };
                                    double[] SenzorYs = { Senzor.GetPointAt(0).Y, Senzor.GetPointAt(1).Y, Senzor.GetPointAt(2).Y };
                                    double SenzorKm = double.NaN;
                                    double SenzorOffset = double.NaN;
                                    try
                                    {
                                        Ax.StationOffset(SenzorXs.Average(), SenzorYs.Average(), ref SenzorKm, ref SenzorOffset);
                                        if (offset < SenzorOffset) Senzor = S;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                //if (Senzor != null) checkBoxSincr.Checked = true;
            }
        }

        //private void CapturaSenzor()
        //{
        //    using (Transaction tr = HostApplicationServices.WorkingDatabase.TransactionManager.StartTransaction())
        //    {
        //        Alignment Ax = comboBoxAx.SelectedValue as Alignment;
        //        if (Ax != null)
        //        {
        //            CapturaSenzor(tr, Ax);
        //        }
        //    }
        //}

        private void ActualizeazaAxuri()
        {
            Axuri.Clear();
            comboBoxAx.Items.Clear();
            using (Transaction trAx = db.TransactionManager.StartTransaction())
            {
                if (idAxuri != null) foreach (ObjectId idAx in idAxuri)
                    {
                        Alignment A = (Alignment)trAx.GetObject(idAx, OpenMode.ForRead);
                        if (!checkBoxR.Checked || A.Description.ToUpper().Contains("R-")) //Aplica filtru R-
                        {
                            Axuri.Add(A);
                            comboBoxAx.Items.Add(Axuri.Last());
                        }
                    }
                if (comboBoxAx.Items.Count > 0) comboBoxAx.SelectedIndex = 0;
                //else ed.WriteMessage("\nLipsa axuri!");
            }
        }

        private void checkBoxR_CheckedChanged(object sender, EventArgs e)
        {
            ActualizeazaAxuri();
        }

        private void buttonStrgSzr_Click(object sender, EventArgs e)
        {
            TypedValue[] TVs = new TypedValue[]
                        {
                        new TypedValue(0, "SOLID"),
                        new TypedValue(8, "AR-MArker"),
                        new TypedValue(62, "3")
                        };
            SelectionFilter SF = new SelectionFilter(TVs);
            PromptSelectionResult PSR = ed.SelectAll(SF);
            if (PSR.Status != PromptStatus.OK) return;
            SelectionSet SS = PSR.Value;
            using (Transaction trStrgSzr = db.TransactionManager.StartTransaction())
            using (acadDoc.LockDocument())
            {
                foreach (SelectedObject SO in SS)
                {
                    if (SO.ObjectId.IsValid)
                    {
                        Solid S = null;
                        //try
                        //{
                        S = (Solid)trStrgSzr.GetObject(SO.ObjectId, OpenMode.ForWrite);
                        S.Erase();
                    }
                }
            trStrgSzr.Commit();
            }
        }
    }
}
