using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.IO;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.Colors;
//using Autodesk.AutoCAD.Interop;
using Autodesk.Civil.ApplicationServices;
//using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;
using Autodesk.Civil.Land.DatabaseServices;


namespace Ovidiu.x64.ARDUTIL
{
    public class ARDUTIL : IExtensionApplication
    {
        #region IExtensionApplication Members
        public void Initialize()
        {
            //throw new NotImplementedException();
            //Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //ed.WriteMessage("\nLoaded AcadUTIL - small routines created by Ovidiu Candrea <ovidiucandrea@yahoo.com>\nFor help run command HELPUTIL");
        }

        public void Terminate()
        {
            //throw new NotImplementedException();
        }
        #endregion

        [CommandMethod("planse")] //Comanda care exporta continutul planselor dintr-un desen intr-un zona model a altui desen
        public void planse()
        {

            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            //List<string> listaNume = new List<string>();

            //Selectarea planselor din desen
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary planse = (DBDictionary)trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                Lista_Align formular = new Lista_Align();

                ed.WriteMessage("\nAu fost gasite urmatoarele planse: ");
                foreach (DBDictionaryEntry plansa in planse)
                {
                    if (plansa.Key == "Model")
                    {
                        ed.WriteMessage("\n " + plansa.Key);
                        formular.chkListBox1.Items.Add(plansa.Key, false);
                    }
                    else 
                    {
                        ed.WriteMessage("\n " + plansa.Key);
                        formular.chkListBox1.Items.Add(plansa.Key, true);
                    }
                }

                System.Windows.Forms.DialogResult Rezultat = formular.ShowDialog();
                if (Rezultat == System.Windows.Forms.DialogResult.OK && formular.listaNume.Length != 0)
                {
                    ed.WriteMessage("\nAu fost selectate urmatoarele planse: ");
                    foreach (string nume in formular.listaNume)
                    {
                        ed.WriteMessage("\n " + nume);
                    }
                }
                else
                {
                    ed.WriteMessage("\nComanda anulata!");
                    return;
                }

                //Calea Desenului Rezultat
                string caleDocAcad = HostApplicationServices.Current.FindFile(acadDoc.Name, db, FindFileHint.Default);
                string indicativ = "-Sectiuni.dwg";
                if (formular.Filtru == "PROFILE") indicativ = "-Profile.dwg";
                string caleNoua = caleDocAcad.Replace(caleDocAcad.Substring(caleDocAcad.Length - 4), indicativ);
                Database dbNoua = new Database(true, false);

                ////Importarea sablonului NVEX.dwt
                //try
                //{
                //    AcadPreferences acPrefComObj = (AcadPreferences)Application.Preferences;
                //    string caleDwt = acPrefComObj.Files.TemplateDwgPath + "\\NVEX.dwt";
                //    //string caleDwt = @"C:\Users\user\AppData\Local\Autodesk\C3D 2012\enu\Template\NVEX.dwt";
                //    ed.WriteMessage("\n" + caleDwt);
                //    dbNoua.ReadDwgFile(caleDwt, FileOpenMode.OpenTryForReadShare, false, null);
                //    dbNoua.CloseInput(false);
                //    ed.WriteMessage("\nSablonul 'NVEX.dwt' a fost importat cu succes.");
                //}
                //catch
                //{
                //    ed.WriteMessage("\nEroare la importarea sablonului 'NVEX.dwt'!");
                //}
                

                //Procesare Planse
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                double deltaX = 0;
                double scara = 1;
                if (formular.Filtru == "XS") scara = 0.1;
                foreach (string numePlansa in formular.listaNume)
                {
                    ed.WriteMessage("\nProcesarea plansei " + numePlansa);
                    Layout layout = (Layout)trans.GetObject((ObjectId)planse[numePlansa], OpenMode.ForRead);
                    BlockTableRecord btr = (BlockTableRecord)trans.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);
                    //Obtinerea Obiectelor din plansa curenta
                    ObjectIdCollection obiecte = new ObjectIdCollection();
                    foreach (ObjectId obiect in btr)
                    {
                        obiecte.Add(obiect);
                    }

                    //Procesarea obiectelor din plansa curenta
                    Database dbCur = new Database(true, true);
                    db.Wblock(dbCur, obiecte, Point3d.Origin, DuplicateRecordCloning.MangleName);
                    ObjectIdCollection objCur = new ObjectIdCollection();
                    using (Transaction trCur = dbCur.TransactionManager.StartTransaction())
                    {
                        BlockTable btCur = (BlockTable)trCur.GetObject(dbCur.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btrCur = (BlockTableRecord)trCur.GetObject(btCur[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        foreach (ObjectId objId in btrCur)
                        {
                            Entity ent = (Entity)trCur.GetObject(objId, OpenMode.ForWrite);
                            if (ent is Viewport) continue;
                            Matrix3d translatie = Matrix3d.Displacement(new Vector3d(deltaX, 0, 0));
                            ent.TransformBy(translatie);
                            Matrix3d scalare = Matrix3d.Scaling(scara, new Point3d(deltaX, 0, 0));
                            ent.TransformBy(scalare);
                            objCur.Add(objId);
                        }

                        //Actualizeaza deltaX
                        deltaX += scara * (layout.Extents.MaxPoint.X + 80);

                        trCur.Commit();
                        //dbCur.SaveAs(caleNoua, DwgVersion.Newest);
                    }

                    //Copierea obiectelor procesate in plansa finala
                    dbCur.Wblock(dbNoua, objCur, Point3d.Origin, DuplicateRecordCloning.MangleName);

                    
                }

                //Curatarea bazei de data de straturi si stiluri duplicat
                using (Transaction trNoua = dbNoua.TransactionManager.StartTransaction())
                {
                    BlockTable btNou = (BlockTable)trNoua.GetObject(dbNoua.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord msNou = (BlockTableRecord)trNoua.GetObject(btNou[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    LayerTable lt = (LayerTable)trNoua.GetObject(dbNoua.LayerTableId, OpenMode.ForWrite);
                    LinetypeTable ltt = (LinetypeTable)trNoua.GetObject(dbNoua.LinetypeTableId, OpenMode.ForWrite);
                    DimStyleTable dst = (DimStyleTable)trNoua.GetObject(dbNoua.DimStyleTableId, OpenMode.ForWrite);

                    //Corectarea obiectelor
                    foreach (ObjectId objId in msNou)
                    {
                        Entity ent = trNoua.GetObject(objId, OpenMode.ForRead) as Entity;
                        
                        //Straturi
                        string numeStrat = ent.Layer;
                        if (ent.Layer.StartsWith("$"))
                        {
                            try
                            {
                                string numeStratBun = ent.Layer.Substring(ent.Layer.Substring(0, ent.Layer.Length - 2).LastIndexOf('$') + 1);
                                //ed.WriteMessage("\n{0} ---> {1}", ent.Layer, numeStratBun);
                                ent.UpgradeOpen();
                                if (lt.Has(numeStratBun)) ent.Layer = numeStratBun;
                                else if (numeStratBun.Length != 0)
                                {
                                    LayerTableRecord stratRau = (LayerTableRecord)trNoua.GetObject(ent.LayerId, OpenMode.ForRead);
                                    LayerTableRecord stratBun = new LayerTableRecord();
                                    stratBun.CopyFrom(stratRau);
                                    stratBun.Name = numeStratBun;
                                    lt.Add(stratBun);
                                    trNoua.AddNewlyCreatedDBObject(stratBun, true);
                                    ent.Layer = numeStratBun;
                                }
                            }
                            catch (System.Exception)
                            {
                                ed.WriteMessage("\nProblema la procesarea obiectului {0}, din stratul {1}", ent.ToString(), ent.Layer);
                            }
                        }

                        //Tipuri de linie
                        if (ent.Linetype.StartsWith("$"))
                        {
                            string numeLinieBun = ent.Linetype.Substring(ent.Linetype.Substring(0, ent.Linetype.Length - 2).LastIndexOf('$') + 1);
                            ent.UpgradeOpen();
                            if (ltt.Has(numeLinieBun)) ent.Linetype = numeLinieBun;
                            else if (numeLinieBun.Length != 0)
                            {
                                LinetypeTableRecord linieRea = (LinetypeTableRecord)trNoua.GetObject(ent.LinetypeId, OpenMode.ForRead);
                                LinetypeTableRecord linieBuna = new LinetypeTableRecord();
                                linieBuna.CopyFrom(linieRea);
                                linieBuna.Name = numeLinieBun;
                                lt.Add(linieBuna);
                                trNoua.AddNewlyCreatedDBObject(linieBuna, true);
                                ent.Linetype = numeLinieBun;
                            }
                        }

                        //Stiluri de cotare
                        if (ent is Dimension && ((Dimension)ent).DimensionStyleName.StartsWith("$"))
                        {
                            string numeCotareBun = ((Dimension)ent).DimensionStyleName;
                            numeCotareBun = numeCotareBun.Substring(numeCotareBun.Substring(0, numeCotareBun.Length - 2).LastIndexOf('$') + 1);
                            if (dst.Has(numeCotareBun)) ((Dimension)ent).DimensionStyleName = numeCotareBun;
                            else if (numeCotareBun.Length != 0)
                            {
                                DimStyleTableRecord cotareRea = (DimStyleTableRecord)trNoua.GetObject(((Dimension)ent).DimensionStyle, OpenMode.ForRead);
                                DimStyleTableRecord cotareBuna = new DimStyleTableRecord();
                                cotareBuna.CopyFrom(cotareRea);
                                cotareBuna.Name = numeCotareBun;
                                dst.Add(cotareBuna);
                                trNoua.AddNewlyCreatedDBObject(cotareBuna, true);
                                ((Dimension)ent).DimensionStyleName = numeCotareBun;
                            }
                        }
                    }

                    //Stergerea straturilor si stilurilor necorespunzatoare
                    foreach (ObjectId lId in lt)
                    {
                        LayerTableRecord ltr = (LayerTableRecord)trNoua.GetObject(lId, OpenMode.ForWrite);
                        if (ltr.Name.Contains('$') && !ltr.IsDependent)
                        {
                            ltr.Erase(true);
                        }
                    }
                    foreach (ObjectId ltId in ltt)
                    {
                        LinetypeTableRecord lttr = (LinetypeTableRecord)trNoua.GetObject(ltId, OpenMode.ForWrite);
                        if (lttr.Name.Contains('$') && !lttr.IsDependent)
                        {
                            lttr.Erase(true);
                        }
                    }
                    foreach (ObjectId dsId in dst)
                    {
                        DimStyleTableRecord dstr = (DimStyleTableRecord)trNoua.GetObject(dsId, OpenMode.ForWrite);
                        if (dstr.Name.Contains('$') && !dstr.IsDependent)
                        {
                            dstr.Erase(true);
                        }
                    }

                    trNoua.Commit();
                }

                //Crearea Desenului Nou
                dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                //dbNoua.Dispose();
                ed.WriteMessage("\nFisierul Rezultat este " + caleNoua);

                //Deschide desenul rezultat
                if (formular.chkBox2.Checked)
                {
                    Document doc = Application.DocumentManager.Open(caleNoua, false);
                    Application.DocumentManager.MdiActiveDocument = doc;

                    //Application.DocumentManager.MdiActiveDocument.Editor.Document.SendStringToExecute(".ZOOM E", true, false, true);

                    //object acadObj = Application.AcadApplication;
                    //acadObj.GetType().InvokeMember("ZoomExtents", System.Reflection.BindingFlags.InvokeMethod, null, acadObj, null);


                    doc.EndDwgOpen += (object sender, DrawingOpenEventArgs e) =>
                    {
                        //Varianta 1
                        Manager gsm = doc.GraphicsManager;
                        using (View view = new View())
                        {
                            view.SetView(Point3d.Origin + Vector3d.ZAxis, Point3d.Origin, Vector3d.YAxis, 500, 500);
                            int vpn = Convert.ToInt32(Application.GetSystemVariable("CVPORT"));
                            gsm.SetViewportFromView(vpn, view, true, false, false);
                        }

                        //Varianta 2
                        doc.Editor.Document.SendStringToExecute(".ZOOM E", true, false, true);

                        //Varianta 3
                        object acadObj = Application.AcadApplication;
                        acadObj.GetType().InvokeMember("ZoomExtents", System.Reflection.BindingFlags.InvokeMethod, null, acadObj, null);

                        doc.Editor.WriteMessage("\nS-a zoom-at?");

                    };



                }
            }

        }

        [CommandMethod("xsl")] //Comanda care creaza linii de sectiune la intervalul specificat in stratul "Cross"
        public void xsl()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            CivilDocument doc = CivilApplication.ActiveDocument;
            ObjectIdCollection alignmentIds = doc.GetAlignmentIds(); //lista ID aliniamente
            if (alignmentIds.Count == 0)
            {
                ed.WriteMessage("\nNu au fost gasite aliniamente in desen! ");
                return;
            }

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);

                //Selectia aliniamentului
                PromptEntityOptions PrEntOpt = new PromptEntityOptions("\nSelecteaza aliniamentul pentru pichetare: ");
                PrEntOpt.SetRejectMessage("\nObiectul selectat nu este aliniament Civil 3d!");
                PrEntOpt.AllowNone = false;
                PrEntOpt.AddAllowedClass(typeof(Alignment), true);
                PromptEntityResult PrEntRes = ed.GetEntity(PrEntOpt);
                if (PrEntRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nComanda intrerupta!");
                    return;
                }
                Alignment alin = (Alignment)trans.GetObject(PrEntRes.ObjectId, OpenMode.ForRead);

                //Alegerea intervalului de pichetare
                PromptDoubleOptions PrDblOpt = new PromptDoubleOptions("\nSpecificati intervalul de pichetare: ");
                PrDblOpt.DefaultValue = 20;
                PrDblOpt.UseDefaultValue = true;
                PrDblOpt.AllowNegative = false;
                PrDblOpt.AllowZero = false;
                PromptDoubleResult PrDblRes = ed.GetDouble(PrDblOpt);
                if (PrDblRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nComanda intrerupta!");
                    return;
                }
                double interval = PrDblRes.Value;

                //Alegerea distantelor stanga/dreapta fata de ax
                PrDblOpt = new PromptDoubleOptions("\nDistanta stanga din ax: ");
                PrDblOpt.DefaultValue = -10;
                PrDblOpt.UseDefaultValue = true;
                PrDblRes = ed.GetDouble(PrDblOpt);
                if (PrDblRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nComanda intrerupta!");
                    return;
                }
                double stg = PrDblRes.Value;

                PrDblOpt = new PromptDoubleOptions("\nDistanta dreapta din ax: ");
                PrDblOpt.DefaultValue = 10;
                PrDblOpt.UseDefaultValue = true;
                PrDblRes = ed.GetDouble(PrDblOpt);
                if (PrDblRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nComanda intrerupta!");
                    return;
                }
                double dr = PrDblRes.Value;
                
                //Calcularea pozitiilor kilometrice
                List<double> listaKm = new List<double>();
                listaKm.Add(alin.StartingStation);
                ed.WriteMessage("\nLista km: {0}, ", alin.StartingStation);
                if (alin.StartingStation/interval != Math.Truncate(alin.StartingStation/interval))
                {
                    double rotunjire = (Math.Round(alin.StartingStation / interval, 0) + 1) * interval;
                    if (rotunjire < alin.EndingStation) listaKm.Add(rotunjire);
                    ed.WriteMessage("{0}, ", rotunjire);
                }
                double ultim = listaKm[listaKm.Count - 1];
                while (ultim + interval < alin.EndingStation)
                {
                    listaKm.Add(ultim + interval);
                    ultim = listaKm[listaKm.Count - 1];
                    ed.WriteMessage("{0}, ", ultim);
                }
                listaKm.Add(alin.EndingStation);
                ed.WriteMessage(listaKm[listaKm.Count - 1].ToString());

                

                //Verificarea existentei stratului "Cross"
                if (!lt.Has("Cross"))
                {
                    LayerTableRecord cross = new LayerTableRecord();
                    cross.Name = "Cross";
                    cross.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    lt.Add(cross);
                    ed.WriteMessage("\n Stratul 'Cross' a fost creat.");
                }
                else ed.WriteMessage("\nStratul 'Cross' exista.");
                
                //Desenarea Liniilor
                foreach (double km in listaKm)
                {
                    double xStart = -999;
                    double yStart= -999;
                    alin.PointLocation(km, stg, ref xStart, ref yStart);
                    if (xStart == -999 || yStart == -999)
                    {
                        ed.WriteMessage("\nComanda intrerupta!");
                        return;
                    }
                    Point3d Start = new Point3d(xStart, yStart, 0);
                    double xStop = -999;
                    double yStop = -999;
                    alin.PointLocation(km, dr, ref xStop, ref yStop);
                    if (xStop == -999 || yStop == -999)
                    {
                        ed.WriteMessage("\nComanda intrerupta!");
                        return;
                    }
                    Point3d Stop = new Point3d(xStop, yStop, 0);
                    Line Linie = new Line(Start, Stop);
                    Linie.Layer = "Cross";
                    ms.AppendEntity(Linie);
                    trans.AddNewlyCreatedDBObject(Linie, true);
                    ed.WriteMessage("\nPichet km {0} --> OK", km);
                }

                trans.Commit();
            }
        }

        [CommandMethod("km3d")] //Comanda pentru notarea pozitiilor kilometrice la cota profilului longitudinal !!!NETERMINTA!!!
        public void km3d()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            Autodesk.Civil.ApplicationServices.CivilDocument civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;

            //Gasire aliniamente
            ObjectIdCollection alignIds = civilDoc.GetAlignmentIds();
            if (alignIds.Count == 0)
            {
                ed.WriteMessage("\nNu au fost gasite aliniamente! Comanda intrerupta.");
                return;
            }

            
            Hashtable IdTable = new Hashtable();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //Gasire profiluri longitudinale (si filtrare lista aliniamente)
                foreach (ObjectId alignId in alignIds)
                {
                    Alignment align = (Alignment)trans.GetObject(alignId, OpenMode.ForRead);
                    ObjectIdCollection proflIds = align.GetProfileIds();
                    //if (proflIds.Count != 0) IdTable.Add(alignId, proflIds);
                    IdTable.Add(alignId, proflIds);
                }
                //if (IdTable.Count == 0)
                //{
                //    System.Windows.Forms.MessageBox.Show("Niciun aliniament nu are profiluri longitudinale. \nNotarea se va face pornind de la cota 0.00.");
                //}

                //Populare si afisare formular
                using (ListaAlign_Profl Formular = new ListaAlign_Profl())
                {
                    foreach (ObjectId align in alignIds)
                    {
                        Formular.listBoxAlign.Items.Add(align.ToString());
                    }

                    if (IdTable.ContainsKey(alignIds[0]))
                    {
                        foreach (ObjectId profl in (ObjectIdCollection)IdTable[alignIds[0]])
                        {
                            Formular.listBoxProfl.Items.Add(profl.ToString());
                        }
                    }
                    else Formular.listBoxProfl.Items.Add("Cota 0.00");

                    Formular.textBoxKm.Text = "20.00";
                    Formular.textBoxOffset.Text = "0.00";
                    Formular.textBoxNivel.Text = "0.00";

                    System.Windows.Forms.DialogResult DR = Application.ShowModalDialog(Formular);

                    if (DR == System.Windows.Forms.DialogResult.OK)
                    {
                        var alignRez = from ObjectId alId in alignIds
                                           where alId.ToString() == Formular.listBoxAlign.SelectedItem.ToString()
                                           select alId;
                        ObjectId alignId = alignRez.ElementAt<ObjectId>(0);
                        
                        double interval = 20;
                        double.TryParse(Formular.textBoxKm.Text, out interval);
                        double offset = 0;
                        double.TryParse(Formular.textBoxOffset.Text, out offset);
                        double difNivel = 0;
                        double.TryParse(Formular.textBoxNivel.Text, out difNivel);

                        Alignment align = (Alignment)trans.GetObject(alignId, OpenMode.ForRead);
                        ObjectId proflId = new ObjectId();
                        if (align.GetProfileIds().Count != 0)
                        {
                            var proflRez = from ObjectId prId in align.GetProfileIds()
                                           where prId.ToString() == Formular.listBoxProfl.SelectedItem.ToString()
                                           select prId;
                            proflId = proflRez.ElementAt<ObjectId>(0);
                        }
                        Profile profl = null;
                        if (!proflId.IsNull) profl = (Profile)trans.GetObject(proflId, OpenMode.ForRead);
                        double kmCurent = 0;
                        while (kmCurent < align.EndingStation)
                        {
                            double cota = 0;
                            try { if (profl != null) cota = profl.ElevationAt(kmCurent); }
                            catch { }
                            cota += difNivel;

                            ed.WriteMessage("\nKm: {0}; Offset: {1}; Cota: {2}", kmCurent, offset, difNivel);
                            kmCurent += interval;
                        }
                        kmCurent = align.EndingStation;
                        ed.WriteMessage("\nKm: {0}; Offset: {1}; Diferenta Nivel: {2}", kmCurent, offset, difNivel);
                    }
                }
            }
        }

        [CommandMethod("kmo?")] //Comanda pentru afisarea pozitiei kilometrice si offsetului unui punct specificat !!!NETERMINATA!!!
        public void kmo()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            CivilDocument civilDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                BlockTableRecord ms = bt[BlockTableRecord.ModelSpace].GetObject(OpenMode.ForRead) as BlockTableRecord;

            }
        }

        [CommandMethod("kmplus")] //Comanda pentru inserarea caracterului '+' la notatia kilometrajului
        public void kmplus()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForRead);

                string prefix = string.Empty;
                PromptStringOptions PrStrOpt = new PromptStringOptions("\nSpecifica prefixul kilometrajului: ");
                PrStrOpt.AllowSpaces = true;
                PrStrOpt.DefaultValue = "Ch: ";
                PrStrOpt.UseDefaultValue = true;
               
                PromptResult PrStrRes = ed.GetString(PrStrOpt);
                if (PrStrRes.Status != PromptStatus.OK || PrStrRes.StringResult == "")
                {
                    ed.WriteMessage("\nComanda Intrerupta!");
                    return;
                }
                else
                {
                    prefix = PrStrRes.StringResult;
                }

                string prefixNou = prefix;
                if (prefix == "Ch: ") prefixNou = "Km: ";

                int contor = 0;
                foreach (ObjectId objId in btr)
                {
                    Entity entCur = (Entity)trans.GetObject(objId, OpenMode.ForRead);
                    if (entCur != null && entCur.GetType() == typeof(DBText) && ((DBText)entCur).TextString.StartsWith(prefix))
                    {
                        entCur.UpgradeOpen();
                        DBText textKm = entCur as DBText;
                        string textNou = textKm.TextString.Replace(prefix, "");
                        try
                        {
                            int nrZecimale = textNou.Length - textNou.LastIndexOf('.') - 1;
                            textNou = prefixNou + double.Parse(textNou).ToString("0+000." + "".PadRight(nrZecimale, '0'));
                            textKm.TextString = textNou;
                            contor++;
                        }
                        catch
                        {
                        }
                        finally
                        {
                            entCur.DowngradeOpen();
                        }
                    }
                }
                trans.Commit();
                ed.WriteMessage("\nS-au completat {0} elemente text.", contor);
            }
        }

        [CommandMethod("curbe")]
        public void curbe()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;
            ObjectIdCollection IdAxuriCivil = civilDoc.GetAlignmentIds();

            //Obtinerea listei de aliniamente de drum ARD
            if (IdAxuriCivil.Count == 0)
            {
                ed.WriteMessage("\nDesenul nu contine aliniamente civil 3d! Comanda intrerupta.");
                return;
            }

            //Gasirea caii desenului care contine blocul
            string dllDir = (System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).Substring(6);
            //ed.WriteMessage("\nCale director: {0}", dllDir);
            string[] cautaBloc = System.IO.Directory.GetFiles(dllDir, "bloc curba.dwg");
            if (cautaBloc.Length == 0)
            {
                ed.WriteMessage("\nDesenul 'bloc curba.dwg' nu a fost gasit! Comanda intrerupta.");
                return;
            }
            string caleBloc = cautaBloc[0];
            //ed.WriteMessage("\n{0}", caleBloc);

            //Importarea definitiei blocului daca aceasta nu exista inca
            string numeBloc = "HD-CURVE_DATA$RO-01$";
            importaBloc(caleBloc, numeBloc);

            //Afisarea formularului de selectie a axurilor pentru notat
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                List<Alignment> AxuriCivil = new List<Alignment>();
                List<Alignment> AxuriARD = new List<Alignment>();
                List<Alignment> AxuriDeNotat = new List<Alignment>();
                ListaAxuri formular = new ListaAxuri();
                System.Windows.Forms.ListBox listBox = formular.checkedListBox1 as System.Windows.Forms.ListBox;
                //listBox.DataSource = NumeAxuriDeNotat;
                listBox.DisplayMember = "Name";

                foreach (ObjectId IdAx in IdAxuriCivil)
                {
                    Alignment Ax = (Alignment)trans.GetObject(IdAx, OpenMode.ForRead);
                    AxuriCivil.Add(Ax);
                    if (Ax.Description.ToUpper() == "R-")
                    {
                        formular.checkedListBox1.Items.Add(Ax);
                        AxuriARD.Add(Ax);
                    }
                }

                //Handler filtrare lista axuri
                formular.checkBox1.CheckedChanged += (object sender, EventArgs e) =>
                {
                    System.Windows.Forms.CheckBox cb = sender as System.Windows.Forms.CheckBox;
                    formular.checkedListBox1.Items.Clear();
                    if (cb.Checked)
                    {
                        formular.checkedListBox1.Items.AddRange(AxuriARD.ToArray());
                        for (int i = 0; i < formular.checkedListBox1.Items.Count; i++)
                        {
                            formular.checkedListBox1.SetItemChecked(i, true);
                        }
                    }
                    else
                    {
                        formular.checkedListBox1.Items.AddRange(AxuriCivil.ToArray());
                        for (int i = 0; i < formular.checkedListBox1.Items.Count; i++)
                        {
                            formular.checkedListBox1.SetItemChecked(i, true);
                        }
                    }
                };

                //Handler acceptare lista
                formular.btnExec.Click += (sender, e) =>
                    {
                        AxuriDeNotat.AddRange(formular.checkedListBox1.CheckedItems.OfType<Alignment>());
                    };

                
                System.Windows.Forms.DialogResult Rezultat = formular.ShowDialog();

                if (Rezultat != System.Windows.Forms.DialogResult.OK)
                {
                    ed.WriteMessage("\nComanda anulata!");
                }

                
                
                //Procesare aliniamente
                if (AxuriDeNotat.Count != 0)
                {
                    ms.UpgradeOpen();
                    BlockTableRecord btrBloc = (BlockTableRecord)trans.GetObject(bt[numeBloc], OpenMode.ForRead);
                    foreach (Alignment Ax in AxuriDeNotat)
                    {
                        ed.WriteMessage("\nSe noteaza axul '{0}'", Ax.Name);
                        AlignmentEntityCollection AEC = Ax.Entities;
                        List<AlignmentSubEntityArc> arce = new List<AlignmentSubEntityArc>();
                        //arce.AddRange(AEC.OfType<AlignmentArc>());
                        foreach (AlignmentEntity AE in AEC)
                        {
                            //ed.WriteMessage("\n-->EntityId: {0}; EntityType: {1}; Subentity count: {2}", AE.EntityId, AE.EntityType, AE.SubEntityCount);
                            for (int i = 0; i < AE.SubEntityCount; i++)
                            {
                                //ed.WriteMessage("\n----->SubEntityType: {0}; StartStation: {1}; EndStation: {2}", AE[i].SubEntityType, AE[i].StartStation, AE[i].EndStation);
                                if (AE[i].SubEntityType == AlignmentSubEntityType.Arc)
                                {
                                    arce.Add(AE[i] as AlignmentSubEntityArc);
                                }
                            }
                        }
                        arce.Sort((AlignmentSubEntityArc a1, AlignmentSubEntityArc a2) => a1.StartStation.CompareTo(a2.StartStation));

                        //Procesare arce
                        int contor = 0;
                        foreach (AlignmentSubEntityArc arc in arce)
                        {
                            contor++;
                            //Gasirea parametrilor arcului
                            ed.WriteMessage("\nSe proceseaza arcul '{0}-{1}', cu km initial: {2}", arc.CurveGroupIndex, arc.CurveGroupSubEntityIndex, arc.StartStation);
                            double est = 0;
                            double nord = 0;
                            double offset = 10;
                            if (!arc.Clockwise) offset = offset * -1;
                            Ax.PointLocation((arc.EndStation + arc.StartStation) /2, offset, ref est, ref nord);
                            Point3d pozitie = new Point3d(est, nord, 0);
                            Dictionary<string, string> parametri = new Dictionary<string,string>();
                            parametri.Add("I-ID", contor.ToString());
                            double viteza = 30;
                            foreach (DesignSpeed ds in Ax.DesignSpeeds)
                            {
                                if (ds.Station <= arc.EndStation) viteza = ds.Value;
                            }
                            parametri.Add("I-V", viteza.ToString("F0"));
                            //double U = (Math.Atan2(arc.StartPoint.Y-arc.PIPoint.Y, arc.StartPoint.X - arc.PIPoint.X) - 
                            //    Math.Atan2(arc.PIPoint.Y-arc.EndPoint.Y, arc.PIPoint.X - arc.EndPoint.X)) * 180 / Math.PI;
                            parametri.Add("A-U", arc.DeflectedAngle.ToString("F4") + "%%d");
                            parametri.Add("L-R", arc.Radius.ToString("F2"));
                            parametri.Add("L-C", arc.Length.ToString("F2"));
                            parametri.Add("P-VERTC", arc.PIPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                            parametri.Add("P-S1C", arc.StartPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                            parametri.Add("P-S2C", arc.EndPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                            parametri.Add("P-VERT", arc.PIPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                            parametri.Add("P-TAN1", arc.StartPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
                            parametri.Add("P-TAN2", arc.EndPoint.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));

                            //string mijloc = string.Format("{0},{1}", est, nord);
                            //ed.WriteMessage("\n-->Tip: {0}; Km1: {1}; Km2: {2}; R: {3}; L: {4}; T: {5}; Centru: {6}; Mijloc: {7}; Dupa: {8}; Inainte de: {9}",
                            //    arc.EntityType, arc.StartStation, arc.EndStation, arc.Radius, arc.Length, arc.ExternalTangent, arc.CenterPoint, mijloc, arc.EntityBefore, arc.EntityAfter);

                            //Inserarea blocului si completarea atributelor
                            using (BlockReference refBloc = new BlockReference(pozitie, btrBloc.ObjectId))
                            {
                                ms.AppendEntity(refBloc);
                                trans.AddNewlyCreatedDBObject(refBloc, true);
                                //Iterarea atributelor si completarea lor dupa caz
                                foreach (ObjectId idAtrib in btrBloc)
                                {
                                    DBObject objAtrib = trans.GetObject(idAtrib, OpenMode.ForRead);
                                    AttributeDefinition defAtrib = objAtrib as AttributeDefinition;
                                    if (defAtrib != null)
                                    {
                                        using (AttributeReference refAtrib = new AttributeReference())
                                        {
                                            refAtrib.SetAttributeFromBlock(defAtrib, refBloc.BlockTransform);
                                            if (parametri.ContainsKey(defAtrib.Tag))
                                            {
                                                refAtrib.TextString = parametri[defAtrib.Tag];
                                            }
                                            refBloc.AttributeCollection.AppendAttribute(refAtrib);
                                            trans.AddNewlyCreatedDBObject(refAtrib, true);
                                        }
                                    }
                                }
                            }
                        }

                    }
                }

                trans.Commit();
            }

        }

        public void importaBloc(string caleBloc, string numeBloc) //Functie pentru importarea unui bloc dintr-un fisier .dwg in desenul curent
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(numeBloc))
                {
                    try
                    {
                        bt.UpgradeOpen();
                        ed.WriteMessage("\nImportare definitie bloc '{0}' -->", numeBloc);
                        Database dbBloc = new Database(false, true);
                        dbBloc.ReadDwgFile(caleBloc, FileOpenMode.OpenForReadAndAllShare, true, "");
                        using (Transaction trBloc = dbBloc.TransactionManager.StartTransaction())
                        {
                            BlockTable btBloc = (BlockTable)trBloc.GetObject(dbBloc.BlockTableId, OpenMode.ForRead);
                            //BlockTableRecord bloc = (BlockTableRecord)trBloc.GetObject(btBloc[numeBloc], OpenMode.ForRead);

                            ObjectIdCollection colBloc = new ObjectIdCollection() { btBloc[numeBloc] };
                            IdMapping mapping = new IdMapping();
                            dbBloc.WblockCloneObjects(colBloc, db.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);
                            ed.WriteMessage(" --> OK");
                        }
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        ed.WriteMessage("\nEroare la importarea blocului: " + ex.Message);
                    }
                }
                else
                {
                    ed.WriteMessage("\nBlocul '{0}' este deja in desen.", numeBloc);
                }
                trans.Commit();
            }
        }
    }
}

