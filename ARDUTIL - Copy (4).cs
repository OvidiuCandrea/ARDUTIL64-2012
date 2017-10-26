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
using System.Reflection;

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
                    BlockTable btNou = (BlockTable)trNoua.GetObject(dbNoua.BlockTableId, OpenMode.ForWrite);
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

                        //Blocuri
                        if (ent is BlockReference)
                        {
                            BlockReference bloc = ent as BlockReference;
                            if (bloc.BlockName.Contains("$$"))
                            {
                                string numeBun = bloc.BlockName.Substring(bloc.BlockName.LastIndexOf("$") + 1);
                                ed.WriteMessage(numeBun);
                                ObjectId bId = ObjectId.Null;
                                try
                                {
                                    bId = btNou[numeBun];
                                    if (bId != ObjectId.Null) ed.WriteMessage("\n{0} exista!", numeBun);
                                }
                                catch { };
                                if (bId != ObjectId.Null)
                                {
                                    bloc.BlockTableRecord = bId;
                                }
                                else
                                {
                                    BlockTableRecord btrBun = (BlockTableRecord)trNoua.GetObject(bloc.BlockTableRecord, OpenMode.ForWrite);
                                    btrBun.Name = numeBun;
                                    ed.WriteMessage("\nS-a redenumit blocul? {0}", bloc.BlockName);
                                    btrBun.DowngradeOpen();
                                }
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
                    double rotunjire = (Math.Truncate(alin.StartingStation / interval) + 1) * interval;
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

        [CommandMethod("curbe")] //Comanda pentru notarea curbelor aliniamentelor din desen
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
            string numeBlocCurba = "HD-CURVE_DATA$RO-01$";
            string numeBlocFrantura = "FRANT-PS";
            importaBloc(caleBloc, numeBlocCurba);
            importaBloc(caleBloc, numeBlocFrantura);

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
                    RegAppTable rat = (RegAppTable)trans.GetObject(db.RegAppTableId, OpenMode.ForWrite);
                    BlockTableRecord btrBlocCurba = (BlockTableRecord)trans.GetObject(bt[numeBlocCurba], OpenMode.ForRead);
                    BlockTableRecord btrBlocFrantura = (BlockTableRecord)trans.GetObject(bt[numeBlocFrantura], OpenMode.ForRead);
                    foreach (Alignment Ax in AxuriDeNotat)
                    {
                        ed.WriteMessage("\nSe noteaza axul '{0}'", Ax.Name);

                        //Verificarea existentei stratului si crearea lui daca lipseste
                        LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForRead);
                        string numeStrat = string.Format("HD_ALGN-{0}-$", Ax.Name);
                        //string numeStrat = "HD_CURVE_DATA";
                        if (!lt.Has(numeStrat))
                        {
                            lt.UpgradeOpen();
                            LayerTableRecord ltr = new LayerTableRecord();
                            ltr.Name = numeStrat;
                            ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                            lt.Add(ltr);
                            trans.AddNewlyCreatedDBObject(ltr, true);
                        }

                        AlignmentEntityCollection AEC = Ax.Entities;
                        List<AlignmentSubEntityArc> arce = new List<AlignmentSubEntityArc>();
                        //arce.AddRange(AEC.OfType<AlignmentArc>());
                        int IID = 0;
                        for (int i = 0; i < AEC.Count; i++)
                        {
                            AlignmentEntity AE = AEC.GetEntityByOrder(i);
                            //ed.WriteMessage("\n-->EntityId: {0}; EntityType: {1}; Subentity count: {2}", AE.EntityId, AE.EntityType, AE.SubEntityCount);
                            for (int j = 0; j < AE.SubEntityCount; j++)
                            {
                                //ed.WriteMessage("\n----->SubEntityType: {0}; StartStation: {1}; EndStation: {2}", AE[i].SubEntityType, AE[i].StartStation, AE[i].EndStation);
                                //Procesare arce
                                if (AE[j].SubEntityType == AlignmentSubEntityType.Arc && AE[j].Length != 0)
                                {
                                    IID++;
                                    AlignmentSubEntityArc arc = AE[j] as AlignmentSubEntityArc;


                                    //Gasirea parametrilor arcului
                                    //ed.WriteMessage("\nSe proceseaza arcul '{0}-{1}', cu km initial: {2}", arc.CurveGroupIndex, arc.CurveGroupSubEntityIndex, arc.StartStation);
                                    double est = 0;
                                    double nord = 0;
                                    double offset = 10;
                                    if (!arc.Clockwise) offset = offset * -1;
                                    Ax.PointLocation((arc.EndStation + arc.StartStation) / 2, offset, ref est, ref nord);
                                    Point3d pozitie = new Point3d(est, nord, 0);
                                    Dictionary<string, string> parametri = new Dictionary<string, string>();

                                    //Numar curba
                                    parametri.Add("I-ID", IID.ToString());
                                    //Viteza de proiectare
                                    double IV = 25;
                                    //foreach (DesignSpeed ds in Ax.DesignSpeeds)
                                    //{
                                    //    if (ds.Station <= arc.EndStation) IV = ds.Value;
                                    //}
                                    parametri.Add("I-V", IV.ToString("F0"));
                                    //Unghiul inclus
                                    double AU = arc.DeflectedAngle;
                                    parametri.Add("A-U", (AU * 200 / Math.PI).ToString("F4") + "g");
                                    //Raza cercului
                                    double LR = arc.Radius;
                                    parametri.Add("L-R", LR.ToString("F3"));
                                    //Lungimea cercului
                                    double LC = arc.Length;
                                    parametri.Add("L-C", LC.ToString("F3"));

                                    //Coordonatele varfului cercului
                                    double PVERTCX = arc.PIPoint.X; double PVERTCY = arc.PIPoint.Y;
                                    string PVERTC = PVERTCY.ToString("F3") + "N " + PVERTCX.ToString("F3") + "E";
                                    parametri.Add("P-VERTC", PVERTC);
                                    //Coordonatele punctului Clotoida/Tangenta - Cerc
                                    string PS1C = arc.StartPoint.Y.ToString("F3") + "N " + arc.StartPoint.X.ToString("F3") + "E";
                                    Point2d Start = arc.StartPoint;
                                    parametri.Add("P-S1C", PS1C);
                                    //Coordonatele punctului Cerc - Clotoida/Tangenta
                                    string PS2C = arc.EndPoint.Y.ToString("F3") + "N " + arc.EndPoint.X.ToString("F3") + "E";
                                    Point2d Capat = arc.EndPoint;
                                    parametri.Add("P-S2C", PS2C);
                                    //Valoare suprainaltare
                                    parametri.Add("R-E", "0.000");
                                    //Lungime tranzitie supralargire-suprainaltare
                                    parametri.Add("L-WD1", "0.000");
                                    //Valoare supralargire intrare;
                                    parametri.Add("L-WI", "0.000");
                                    //Valoare supralargire iesire;
                                    parametri.Add("L-WO", "0.000");
                                    //Sens curba
                                    string STurn = "right"; if (!arc.Clockwise) STurn = "left";
                                    //Racordare progresiva intrare
                                    Point2d TC = arc.StartPoint;
                                    double LS1 = 0;
                                    double O1 = new Vector2d(Math.Sin(arc.StartDirection), Math.Cos(arc.StartDirection)).Angle;
                                    Vector2d vectorStart = new Vector2d(Math.Sin(arc.StartDirection), Math.Cos(arc.StartDirection));
                                    double kmStart = arc.StartStation;
                                    if (j > 0 && AE[j - 1].SubEntityType == AlignmentSubEntityType.Spiral && AE[j - 1].Length != 0)
                                    {
                                        AlignmentSubEntitySpiral clotoida = AE[j - 1] as AlignmentSubEntitySpiral;
                                        TC = clotoida.StartPoint;
                                        Start = clotoida.StartPoint;
                                        LS1 = clotoida.Length;
                                        vectorStart = new Vector2d(Math.Sin(clotoida.StartDirection), Math.Cos(clotoida.StartDirection));
                                        kmStart = clotoida.StartStation;
                                        O1 = clotoida.StartDirection;
                                    }
                                    string PTAN1 = TC.Y.ToString("F3") + "N " + TC.X.ToString("F3") + "E";
                                    parametri.Add("P-TAN1", PTAN1);
                                    parametri.Add("L-S1", LS1.ToString("F3"));
                                    //Racordare progresiva iesire
                                    Point2d CT = arc.EndPoint;
                                    double LS2 = 0;
                                    Vector2d vectorCapat = new Vector2d(Math.Sin(arc.EndDirection), Math.Cos(arc.EndDirection)).Negate();
                                    double kmCapat = arc.EndStation;
                                    if (j < AE.SubEntityCount - 1 && AE[j + 1].SubEntityType == AlignmentSubEntityType.Spiral && AE[j + 1].Length != 0)
                                    {
                                        AlignmentSubEntitySpiral clotoida = AE[j + 1] as AlignmentSubEntitySpiral;
                                        CT = clotoida.EndPoint;
                                        Capat = clotoida.EndPoint;
                                        LS2 = clotoida.Length;
                                        vectorCapat = new Vector2d(Math.Sin(clotoida.EndDirection), Math.Cos(clotoida.EndDirection)).Negate();
                                        kmCapat = clotoida.EndStation;
                                    }
                                    string PTAN2 = CT.Y.ToString("F3") + "N " + CT.X.ToString("F3") + "E";
                                    parametri.Add("L-S2", LS2.ToString("F3"));
                                    parametri.Add("P-TAN2", PTAN2);
                                    //Punct intersectie aliniamente
                                    Line2d LinStart = new Line2d(TC, vectorStart);
                                    Line2d LinCapat = new Line2d(CT, vectorCapat);
                                    //ed.WriteMessage("{0}------{1}", LinStart, LinCapat);
                                    Point2d[] intersectii = LinStart.IntersectWith(LinCapat);
                                    double PVERTX = PVERTCX; double PVERTY = PVERTCY; string PVERT = PVERTC;
                                    double LTAN1;
                                    double LTAN2;
                                    if (intersectii == null || intersectii.Length == 0)
                                    {
                                        parametri.Add("P-VERT", PVERT);
                                        LTAN1 = Math.Sqrt(Math.Pow((arc.PIPoint - arc.StartPoint).X, 2) + Math.Pow((arc.PIPoint - arc.StartPoint).Y, 2));
                                        parametri.Add("L-TAN1", LTAN1.ToString("F3"));
                                        LTAN2 = Math.Sqrt(Math.Pow((arc.PIPoint - arc.StartPoint).X, 2) + Math.Pow((arc.PIPoint - arc.StartPoint).Y, 2));
                                        parametri.Add("L-TAN2", LTAN2.ToString("F3"));
                                    }
                                    else
                                    {
                                        PVERTX = intersectii[0].X; PVERTY = intersectii[0].Y;
                                        PVERT = PVERTY.ToString("F3") + "N " + PVERTX.ToString("F3") + "E";
                                        parametri.Add("P-VERT", PVERT);
                                        LTAN1 = Math.Sqrt(Math.Pow((TC - intersectii[0]).X, 2) + Math.Pow((TC - intersectii[0]).Y, 2));
                                        parametri.Add("L-TAN1", LTAN1.ToString("F3"));
                                        LTAN2 = Math.Sqrt(Math.Pow((CT - intersectii[0]).X, 2) + Math.Pow((CT - intersectii[0]).Y, 2));
                                        parametri.Add("L-TAN2", LTAN2.ToString("F3"));
                                    }

                                    //ed.WriteMessage("\n-->Tip: {0}; Km1: {1}; Km2: {2}; R: {3}; L: {4}; T: {5}; Centru: {6}; Mijloc: {7}; Dupa: {8}; Inainte de: {9}",
                                    //    arc.EntityType, arc.StartStation, arc.EndStation, arc.Radius, arc.Length, arc.ExternalTangent, arc.CenterPoint, mijloc, arc.EntityBefore, arc.EntityAfter);


                                    //Completarea datelor extinse
                                    object[,] Xdata = new object[43, 2];
                                    Xdata[0, 0] = "1001"; Xdata[0, 1] = "BNYCSM-HD-CURVE_DATA";
                                    Xdata[1, 0] = "1000"; Xdata[1, 1] = "L-S2"; //L-S2    -Spiral-out length [So]
                                    Xdata[2, 0] = "1040"; Xdata[2, 1] = LS2.ToString();
                                    Xdata[3, 0] = "1000"; Xdata[3, 1] = "L-S1"; //L-S1    -Spiral-in length [Si]
                                    Xdata[4, 0] = "1040"; Xdata[4, 1] = LS1.ToString();
                                    Xdata[5, 0] = "1000"; Xdata[5, 1] = "A-U"; //A-U     -Curve tangents angle [U]
                                    Xdata[6, 0] = "1040"; Xdata[6, 1] = AU.ToString();
                                    Xdata[7, 0] = "1000"; Xdata[7, 1] = "L-R"; //L-R     -Curve radius [R]
                                    Xdata[8, 0] = "1040"; Xdata[8, 1] = LR.ToString();
                                    Xdata[9, 0] = "1000"; Xdata[9, 1] = "O-1"; //O-1     -Curve direction-in [dTi]
                                    Xdata[10, 0] = "1040"; Xdata[10, 1] = O1.ToString();
                                    Xdata[11, 0] = "1000"; Xdata[11, 1] = "P-Vert"; //P-Vert  -Curve tangents vertex [V]
                                    Xdata[12, 0] = "1010"; Xdata[12, 1] = new Point3d(PVERTX, PVERTY, 0);
                                    Xdata[13, 0] = "1000"; Xdata[13, 1] = "S-Turn"; //S-Turn  -Curve turn
                                    Xdata[14, 0] = "1000"; Xdata[14, 1] = STurn;

                                    //Xdata[15, 0] = "1000"; Xdata[15, 1] = "E-S1"; //Entitati din desen - aparent optionale
                                    //DBPoint ES1 = new DBPoint(new Point3d(0, 0, 0));
                                    //ES1.Layer = numeStrat;
                                    //ms.AppendEntity(ES1);
                                    //trans.AddNewlyCreatedDBObject(ES1, true);
                                    //Xdata[16, 0] = "1005"; Xdata[16, 1] = ES1.Handle.ToString();
                                    //Xdata[17, 0] = "1000"; Xdata[17, 1] = "E-S2";
                                    //DBPoint ES2 = new DBPoint(new Point3d(0, 0, 0));
                                    //ES2.Layer = numeStrat;
                                    //ms.AppendEntity(ES2);
                                    //trans.AddNewlyCreatedDBObject(ES2, true);
                                    //Xdata[18, 0] = "1005"; Xdata[18, 1] = ES2.Handle.ToString();
                                    //Xdata[19, 0] = "1000"; Xdata[19, 1] = "E-C";
                                    //DBPoint EC = new DBPoint(new Point3d(0, 0, 0));
                                    //EC.Layer = numeStrat;
                                    //ms.AppendEntity(EC);
                                    //trans.AddNewlyCreatedDBObject(EC, true);
                                    //Xdata[20, 0] = "1005"; Xdata[20, 1] = EC.Handle.ToString();
                                    //Xdata[21, 0] = "1000"; Xdata[21, 1] = "E-M1";
                                    //DBPoint EM1 = new DBPoint(new Point3d(0, 0, 0));
                                    //EM1.Layer = numeStrat;
                                    //ms.AppendEntity(EM1);
                                    //trans.AddNewlyCreatedDBObject(EM1, true);
                                    //Xdata[22, 0] = "1005"; Xdata[22, 1] = EM1.Handle.ToString();
                                    //Xdata[23, 0] = "1000"; Xdata[23, 1] = "E-M2";
                                    //DBPoint EM2 = new DBPoint(new Point3d(0, 0, 0));
                                    //EM2.Layer = numeStrat;
                                    //ms.AppendEntity(EM2);
                                    //trans.AddNewlyCreatedDBObject(EM2, true);
                                    //Xdata[24, 0] = "1005"; Xdata[24, 1] = EM2.Handle.ToString();

                                    Xdata[15, 0] = "1000"; Xdata[15, 1] = "E-S1"; //Entitati din desen - aparent optionale
                                    Xdata[16, 0] = "1005"; Xdata[16, 1] = null;
                                    Xdata[17, 0] = "1000"; Xdata[17, 1] = "E-S2";
                                    Xdata[18, 0] = "1005"; Xdata[18, 1] = null;
                                    Xdata[19, 0] = "1000"; Xdata[19, 1] = "E-C";
                                    Xdata[20, 0] = "1005"; Xdata[20, 1] = null;
                                    Xdata[21, 0] = "1000"; Xdata[21, 1] = "E-M1";
                                    Xdata[20, 0] = "1005"; Xdata[20, 1] = null;
                                    Xdata[21, 0] = "1000"; Xdata[21, 1] = "E-M1";
                                    Xdata[22, 0] = "1005"; Xdata[22, 1] = null;
                                    Xdata[23, 0] = "1000"; Xdata[23, 1] = "E-M2";
                                    Xdata[24, 0] = "1005"; Xdata[24, 1] = null;

                                    Xdata[25, 0] = "1000"; Xdata[25, 1] = "L-WT2"; //L-WT2   -Widening on tangent-out
                                    Xdata[26, 0] = "1040"; Xdata[26, 1] = "0";
                                    Xdata[27, 0] = "1000"; Xdata[27, 1] = "L-WT1"; //L-WT1   -Widening on tangent-in
                                    Xdata[28, 0] = "1040"; Xdata[28, 1] = "0";
                                    Xdata[29, 0] = "1000"; Xdata[29, 1] = "L-WD2"; //L-WD2   -Widening transition-out
                                    Xdata[30, 0] = "1040"; Xdata[30, 1] = "0";
                                    Xdata[31, 0] = "1000"; Xdata[31, 1] = "L-WD1"; //L-WD1   -Widening transition-in
                                    Xdata[32, 0] = "1040"; Xdata[32, 1] = "0";
                                    Xdata[33, 0] = "1000"; Xdata[33, 1] = "L-WO"; //L-WO    -Widening outer [wo]
                                    Xdata[34, 0] = "1040"; Xdata[34, 1] = "0";
                                    Xdata[35, 0] = "1000"; Xdata[35, 1] = "L-WI"; //L-WI    -Widening inner [wi]
                                    Xdata[36, 0] = "1040"; Xdata[36, 1] = "0";
                                    Xdata[37, 0] = "1000"; Xdata[37, 1] = "R-E"; //R-E     -Superelevation
                                    Xdata[38, 0] = "1040"; Xdata[38, 1] = "0";
                                    Xdata[39, 0] = "1000"; Xdata[39, 1] = "L-SD"; //L-SD    -Sight distance
                                    Xdata[40, 0] = "1040"; Xdata[40, 1] = "0";
                                    Xdata[41, 0] = "1000"; Xdata[41, 1] = "I-V"; //I-V     -Design speed
                                    Xdata[42, 0] = "1071"; Xdata[42, 1] = IV.ToString();


                                    //Inserarea blocului, completarea atributelor si a datelor extinse
                                    using (BlockReference refBloc = new BlockReference(pozitie, btrBlocCurba.ObjectId))
                                    {
                                        refBloc.ScaleFactors = new Scale3d(0.6, 0.6, 0.6);
                                        refBloc.Layer = numeStrat;

                                        //Verificarea existentei datelor extinse si completarea lor
                                        //string numeX = Ax.Name + "-Curba" + IID.ToString();
                                        string numeX = Xdata[0, 1] as string;
                                        if (!rat.Has(numeX))
                                        {
                                            RegAppTableRecord ratr = new RegAppTableRecord();
                                            ratr.Name = numeX;
                                            rat.Add(ratr);
                                            trans.AddNewlyCreatedDBObject(ratr, true);
                                        }
                                        ResultBuffer rb = new ResultBuffer();
                                        //rb.Add(new TypedValue(1001, numeX));
                                        //rb.Add(new TypedValue(int.Parse(Xdata[1, 0]), Xdata[1, 1]));
                                        for (int n = 0; n <= Xdata.GetUpperBound(0); n++)
                                        //for (int n = 0; n < 13; n++)
                                        {
                                            TypedValue tv = new TypedValue(int.Parse(Xdata[n, 0] as string), Xdata[n, 1]);
                                            rb.Add(tv);
                                        }
                                        refBloc.XData = rb;

                                        ms.AppendEntity(refBloc);
                                        trans.AddNewlyCreatedDBObject(refBloc, true);
                                        refBloc.XData = rb;
                                        //Iterarea atributelor si completarea lor dupa caz
                                        foreach (ObjectId idAtrib in btrBlocCurba)
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

                                    //Desenarea liniilor de start si capat ale curbei
                                    double L1P1X = Start.X; double L1P1Y = Start.Y;
                                    double L1P2X = 0; double L1P2Y = 0; Ax.PointLocation(kmStart, offset * 2, ref L1P2X, ref L1P2Y);
                                    double L2P1X = Capat.X; double L2P1Y = Capat.Y;
                                    double L2P2X = 0; double L2P2Y = 0; Ax.PointLocation(kmCapat, offset * 2, ref L2P2X, ref L2P2Y);
                                    Line L1 = new Line(new Point3d(L1P1X, L1P1Y, 0), new Point3d(L1P2X, L1P2Y, 0));
                                    L1.Layer = numeStrat;
                                    ms.AppendEntity(L1);
                                    trans.AddNewlyCreatedDBObject(L1, true);
                                    Line L2 = new Line(new Point3d(L2P1X, L2P1Y, 0), new Point3d(L2P2X, L2P2Y, 0));
                                    L2.Layer = numeStrat;
                                    ms.AppendEntity(L2);
                                    trans.AddNewlyCreatedDBObject(L2, true);
                                }

                                if (AE[j] is AlignmentSubEntityLine)
                                {
                                    //Verifica daca subentitatea precedenta e linie
                                    double? DI = null;
                                    if (j > 0 && AE[j - 1] is AlignmentSubEntityLine)
                                    {
                                        DI = ((AlignmentSubEntityLine)AE[j - 1]).Direction;
                                    }
                                    if (i > 0)
                                    {
                                        AlignmentEntity AEP = AEC.GetEntityByOrder(i - 1);
                                        int nrSE = AEP.SubEntityCount;
                                        if (AEP[nrSE - 1] is AlignmentSubEntityLine)
                                        {
                                            DI = ((AlignmentSubEntityLine)AEP[nrSE - 1]).Direction;
                                        }
                                    }

                                    //Proceseaza frantura
                                    if (DI != null)
                                    {
                                        IID++;
                                        AlignmentSubEntityLine linie = (AlignmentSubEntityLine)AE[j];
                                        //Gasirea parametrilor franturii
                                        Vector2d VI = new Vector2d(Math.Sin(DI.Value), Math.Cos(DI.Value));
                                        Vector2d VE = new Vector2d(Math.Sin(linie.Direction), Math.Cos(linie.Direction));
                                        double est = 0;
                                        double nord = 0;
                                        double offset = 10;
                                        Ax.PointLocation(linie.StartStation, offset, ref est, ref nord);
                                        Point3d pozitie = new Point3d(est, nord, 0);
                                        Dictionary<string, string> parametri = new Dictionary<string, string>();

                                        //Numar curba
                                        parametri.Add("CURBA", IID.ToString());
                                        //Unghi frantura
                                        //ed.WriteMessage("Coordonate varf: {0} //// Directie intrare: {1} //// Directie iesire: {2}", linie.StartPoint, DI.Value, linie.Direction);
                                        parametri.Add("U", (200 - (VI.GetAngleTo(VE) * 200 / Math.PI)).ToString("F4") + "g");
                                        //Coordonate frantura
                                        parametri.Add("EVARF", linie.StartPoint.X.ToString("F4"));
                                        parametri.Add("NVARF", linie.StartPoint.Y.ToString("F4"));


                                        //Inserarea blocului si completarea atributelor
                                        using (BlockReference refBloc = new BlockReference(pozitie, btrBlocFrantura.ObjectId))
                                        {
                                            refBloc.ScaleFactors = new Scale3d(0.6, 0.6, 0.6);
                                            refBloc.Layer = numeStrat;

                                            ms.AppendEntity(refBloc);
                                            trans.AddNewlyCreatedDBObject(refBloc, true);
                                            //Iterarea atributelor si completarea lor dupa caz
                                            foreach (ObjectId idAtrib in btrBlocFrantura)
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

                                        //Desenarea liniei de notare a franturii
                                        Line L1 = new Line(new Point3d(linie.StartPoint.X, linie.StartPoint.Y, 0), pozitie);
                                        L1.Layer = numeStrat;
                                        ms.AppendEntity(L1);
                                        trans.AddNewlyCreatedDBObject(L1, true);
                                    }

                                }

                            }
                        }
                    }
                }

                trans.Commit();
            }

        }

        [CommandMethod("plu")] //Comanda pentru pozitionarea blocurilor podet pe profilul longitudinal
        public void plu()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            Database db = acadDoc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                #region Selectia blocurilor de tip "Podet Plan 1"
                PromptSelectionOptions PrSelOpt = new PromptSelectionOptions();
                //PrSelOpt.Keywords.Add("Toate");
                //PrSelOpt.Keywords.Default = "Toate";
                PrSelOpt.MessageForAdding = "\nSelecteaza podetele de aliniat pe lung: "
                    //+ PrSelOpt.Keywords.GetDisplayString(false)
                    ;
                TypedValue[] tvs = new TypedValue[] 
                {
                    //new TypedValue((int)DxfCode.Operator, "<and"),
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue(8, "Podete Plan")
                    //new TypedValue((int)DxfCode.BlockName, "Podet Plan 1"),
                    //new TypedValue((int)DxfCode.Operator, "and>")
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                PromptSelectionResult PrSelRez = ed.GetSelection(PrSelOpt, sf);

                ObjectIdCollection BlockIds = new ObjectIdCollection();
                PromptStatus PrStatus = PrSelRez.Status;
                //if (PrStatus == PromptStatus.None || PrStatus == PromptStatus.Keyword)
                //{
                //    PromptSelectionResult PrSelAll = ed.SelectAll(sf);
                //    if (PrSelAll.Status == PromptStatus.OK)
                //    {
                //        foreach (ObjectId Id in PrSelAll.Value.GetObjectIds())
                //        {
                //            Ids.Add(Id);
                //        }
                //        PrStatus = PromptStatus.OK;
                //    }
                //}
                if (PrStatus != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSelectie incorecta (Prompt Status: {0})! Comanda intrerupta.", PrSelRez.Status);
                    return;
                }
                foreach (ObjectId Id in PrSelRez.Value.GetObjectIds())
                {
                    BlockIds.Add(Id);
                }
                //ed.WriteMessage("\nS-au selectat {0} blocuri.", BlockIds.Count);
                #endregion

                #region Selectia liniei rosii
                tvs = new TypedValue[] { new TypedValue(0, "POLYLINE"), new TypedValue(8, "L-Design") };
                sf = new SelectionFilter(tvs);
                PrSelOpt.MessageForAdding = "\nSelecteaza linia rosie: ";
                PrSelRez = ed.GetSelection(PrSelOpt, sf);
                if (PrSelRez.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSelectie incorecta (Prompt Status: {0})! Comanda intrerupta.", PrSelRez.Status);
                    return;
                }
                ObjectIdCollection PolyIds = new ObjectIdCollection();
                foreach (ObjectId Id in PrSelRez.Value.GetObjectIds())
                {
                    PolyIds.Add(Id);
                }
                //ed.WriteMessage("\nS-au selectat {0} polilinii.", PolyIds.Count);
                #endregion

                #region Ordonarea Poliliniilor
                List<Polyline2d> Polys = new List<Polyline2d>();
                foreach (ObjectId PolyId in PolyIds)
                {
                    Polys.Add((Polyline2d)trans.GetObject(PolyId, OpenMode.ForRead));
                }
                Polys.Sort((p1, p2) => p1.StartPoint.X.CompareTo(p2.StartPoint.X));
                #endregion

                #region Pozitionarea blocurilor
                foreach (ObjectId BlockId in BlockIds)
                {
                    BlockReference BR = (BlockReference)trans.GetObject(BlockId, OpenMode.ForWrite);

                    //Gasirea pozitiei kilometrice
                    double KM = -999;
                    foreach (ObjectId AttId in BR.AttributeCollection)
                    {
                        AttributeReference AttRef = (AttributeReference)trans.GetObject(AttId, OpenMode.ForRead);
                        if (AttRef.Tag == "KM") double.TryParse(AttRef.TextString.Replace("+", ""), out KM);
                    }
                    if (KM == -999) continue;

                    //Calcularea noilor coordonate
                    int nrPoly = 0;
                    double KMpartial = KM;
                    double deltaX = Polys[nrPoly].EndPoint.X - Polys[nrPoly].StartPoint.X;
                    while (KMpartial > deltaX)
                    {
                        KMpartial = KMpartial - deltaX;
                        nrPoly++;
                        deltaX = Polys[nrPoly].EndPoint.X - Polys[nrPoly].StartPoint.X;
                    }
                    double Xnou = Polys[nrPoly].StartPoint.X + KMpartial;
                    double Ynou = -999;
                    List<Vertex2d> Vertecsi = new List<Vertex2d>();
                    foreach (ObjectId VertexId in Polys[nrPoly])
                    {
                        Vertecsi.Add((Vertex2d)trans.GetObject(VertexId, OpenMode.ForRead));
                    }
                    for (int i = 1; i <= Vertecsi.Count; i++)
                    {
                        if (Vertecsi[i].Position.X > Xnou)
                        {
                            //ed.WriteMessage("\nPozitie Vertex inainte: {0}", Vertecsi[i - 1].Position);
                            Ynou = Vertecsi[i - 1].Position.Y +
                                (Xnou - Vertecsi[i - 1].Position.X) / (Vertecsi[i].Position.X - Vertecsi[i - 1].Position.X) * (Vertecsi[i].Position.Y - Vertecsi[i - 1].Position.Y);
                            break;
                        }
                    }

                    //Mutarea si rotirea blocului
                    //ed.WriteMessage("\nPozitie noua: {0},{1}", Xnou, Ynou);
                    double dX = Xnou - BR.Position.X;
                    double dY = Ynou - BR.Position.Y;
                    Point3d PozNoua = new Point3d(Xnou, Ynou, 0);
                    double dZ = 0;
                    BR.TransformBy(Matrix3d.Displacement(new Vector3d(dX, dY, dZ)));
                    //ed.WriteMessage("\nRotatie initiala: {0}", BR.Rotation);
                    double deltaU = -(BR.Rotation + 0.5 * Math.PI);
                    BR.TransformBy(Matrix3d.Rotation(deltaU, Vector3d.ZAxis, PozNoua));

                }
                #endregion

                trans.Commit();
            }
        }

        [CommandMethod("pagini")] //Comanda pentru crearea paginilor
        public void pagini()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            Database db = acadDoc.Database;
            LayoutManager lm = LayoutManager.Current;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                int anterior = 0;
                int ultPag = 0;
                Point3d ins = new Point3d(0, 0, 0);
                bool continua = true;
                bool primaPlansa = true;

                //Lista planse la inceputul comenzii
                DBDictionary dbd = (DBDictionary)trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                List<string> numeExistente = new List<string>();
                foreach (DBDictionaryEntry dbdEntry in dbd)
                {
                    if (dbdEntry.Key != "Model") numeExistente.Add(dbdEntry.Key);

                }
                foreach (string sUltPag in numeExistente)
                {
                    if (sUltPag.Contains("("))
                    {
                        try
                        {
                            string sNr = sUltPag.Substring(Math.Max(0, sUltPag.LastIndexOf("-") + 1));
                            sNr = sNr.TrimEnd(')');
                            //ed.WriteMessage("\nCalcularea ultimei pagini: se citeste {0}", sNr);
                            ultPag = Math.Max(ultPag, int.Parse(sNr));
                        }
                        catch
                        {
                            //ed.WriteMessage("\nNumarul paginii precedente necunoscut: ");
                        }
                        //finally
                        //{
                        //    ed.WriteMessage(sUltPag); 
                        //}
                    }
                }
                int pag0 = ultPag;
                PlotSettingsValidator PSV = PlotSettingsValidator.Current;
                System.Collections.Specialized.StringCollection PSList = PSV.GetPlotStyleSheetList();
                System.Collections.Specialized.StringCollection PDList = PSV.GetPlotDeviceList();

                while (continua)
                {
                    #region Selectia chenarelor
                    PromptSelectionOptions PrSelOpt = new PromptSelectionOptions();
                    //PrSelOpt.Keywords.Add("Toate");
                    //PrSelOpt.Keywords.Default = "Toate";
                    PrSelOpt.MessageForAdding = string.Format("\nSelecteaza chenarele de pus in pagina: (Anterior {0})", anterior);
                    //+ PrSelOpt.Keywords.GetDisplayString(false)
                    ;
                    TypedValue[] tvs = new TypedValue[]
                    {
                    new TypedValue(0, "LWPOLYLINE"),
                    new TypedValue(8, "SHEETS_MODEL"),
                    new TypedValue(70, "1") //Polilinii inchise
                    };
                    SelectionFilter sf = new SelectionFilter(tvs);
                    PromptSelectionResult PrSelRez = ed.GetSelection(PrSelOpt, sf);
                    if (PrSelRez.Status != PromptStatus.OK || PrSelRez.Value.Count == 0)
                    {
                        ed.WriteMessage("\nSelectie chenare incorecta! Comanda intrerupta.");
                        return;
                    }
                    if (anterior != 0 && anterior + PrSelRez.Value.Count > 10)
                    {
                        ed.WriteMessage("\nUltima selectie nu incape in limita de 10 pagini");
                        //continua = false;
                        continue;
                    }
                    //Completarea catalogului numar - pozitie a hublourilor (viewport)
                    List<Polyline> Chenare = new List<Polyline>();
                    foreach (ObjectId Id in PrSelRez.Value.GetObjectIds())
                    {
                        Polyline Chenar = (Polyline)trans.GetObject(Id, OpenMode.ForRead);

                        Chenare.Add(Chenar);
                    }
                    #endregion

                    #region Selectia axului
                    PromptSelectionOptions PrSelOpt2 = new PromptSelectionOptions();
                    PrSelOpt2.MessageForAdding = string.Format("\nSelecteaza axul care strabate chenarele: ");
                    //PrSelOpt2.SingleOnly = true;
                    
                    TypedValue[] tvs2 = new TypedValue[]
                    {
                    new TypedValue(0, "LWPOLYLINE"),
                    new TypedValue(8, "Ax proiectat"),
                    };
                    SelectionFilter sf2 = new SelectionFilter(tvs2);
                    PromptSelectionResult PrSelRez2 = ed.GetSelection(PrSelOpt2, sf2);
                    if (PrSelRez2.Status != PromptStatus.OK || PrSelRez2.Value.Count == 0)
                    {
                        ed.WriteMessage("\nSelectie ax incorecta! Comanda intrerupta.");
                        return;
                    }
                    Polyline Ax = (Polyline)trans.GetObject(PrSelRez2.Value.GetObjectIds()[0], OpenMode.ForRead);
                    #endregion

                    #region Sortare chenare
                    Chenare.Sort(new Comparison<Polyline>((Polyline P1, Polyline P2) =>
                    {
                        if (P1 == null || P2 == null)
                        {
                            return 0;
                        }
                        Point3dCollection int1 = new Point3dCollection();
                        Line stanga1 = new Line(new Point3d(P1.GetPoint3dAt(0).X, P1.GetPoint3dAt(0).Y, Ax.Elevation), new Point3d(P1.GetPoint3dAt(3).X, P1.GetPoint3dAt(3).Y, Ax.Elevation));
                        Line dreapta1 = new Line(new Point3d(P1.GetPoint3dAt(1).X, P1.GetPoint3dAt(1).Y, Ax.Elevation), new Point3d(P1.GetPoint3dAt(2).X, P1.GetPoint3dAt(2).Y, Ax.Elevation));
                        Line mijloc1 = new Line(new Point3d((P1.GetPoint3dAt(0).X + P1.GetPoint3dAt(1).X) / 2, (P1.GetPoint3dAt(0).Y + P1.GetPoint3dAt(1).Y) / 2, Ax.Elevation),
                            new Point3d((P1.GetPoint3dAt(2).X + P1.GetPoint3dAt(3).X) / 2, (P1.GetPoint3dAt(2).Y + P1.GetPoint3dAt(3).Y) / 2, Ax.Elevation));
                        Point3dCollection int2 = new Point3dCollection();
                        Line stanga2 = new Line(new Point3d(P2.GetPoint3dAt(0).X, P2.GetPoint3dAt(0).Y, Ax.Elevation), new Point3d(P2.GetPoint3dAt(3).X, P2.GetPoint3dAt(3).Y, Ax.Elevation));
                        Line dreapta2 = new Line(new Point3d(P2.GetPoint3dAt(1).X, P2.GetPoint3dAt(1).Y, Ax.Elevation), new Point3d(P2.GetPoint3dAt(2).X, P2.GetPoint3dAt(2).Y, Ax.Elevation));
                        Line mijloc2 = new Line(new Point3d((P2.GetPoint3dAt(0).X + P2.GetPoint3dAt(1).X) / 2, (P2.GetPoint3dAt(0).Y + P2.GetPoint3dAt(1).Y) / 2, Ax.Elevation),
                            new Point3d((P2.GetPoint3dAt(2).X + P2.GetPoint3dAt(3).X) / 2, (P2.GetPoint3dAt(2).Y + P2.GetPoint3dAt(3).Y) / 2, Ax.Elevation));
                        stanga1.IntersectWith(Ax, Intersect.OnBothOperands, int1, new IntPtr(), new IntPtr());
                        stanga2.IntersectWith(Ax, Intersect.OnBothOperands, int2, new IntPtr(), new IntPtr());
                        Point3d mid1 = new Point3d((stanga1.StartPoint.X + stanga1.EndPoint.X) / 2, (stanga1.StartPoint.Y + stanga1.EndPoint.Y) / 2, 0);
                        Point3d mid2 = new Point3d((stanga2.StartPoint.X + stanga2.EndPoint.X) / 2, (stanga2.StartPoint.Y + stanga2.EndPoint.Y) / 2, 0);
                        if (int1.Count == 0 || int2.Count == 0)
                        {
                            mijloc1.IntersectWith(Ax, Intersect.OnBothOperands, int1, new IntPtr(), new IntPtr());
                            mijloc2.IntersectWith(Ax, Intersect.OnBothOperands, int2, new IntPtr(), new IntPtr());
                            mid1 = new Point3d((mijloc1.StartPoint.X + mijloc1.EndPoint.X) / 2, (mijloc1.StartPoint.Y + mijloc1.EndPoint.Y) / 2, 0);
                            mid2 = new Point3d((mijloc2.StartPoint.X + mijloc2.EndPoint.X) / 2, (mijloc2.StartPoint.Y + mijloc2.EndPoint.Y) / 2, 0);
                        }
                        if (int1.Count == 0 || int2.Count == 0)
                        {
                            dreapta1.IntersectWith(Ax, Intersect.ExtendArgument, int1, new IntPtr(), new IntPtr());
                            dreapta2.IntersectWith(Ax, Intersect.ExtendArgument, int2, new IntPtr(), new IntPtr());
                            mid1 = new Point3d((dreapta1.StartPoint.X + dreapta1.EndPoint.X) / 2, (dreapta1.StartPoint.Y + dreapta1.EndPoint.Y) / 2, 0);
                            mid2 = new Point3d((dreapta2.StartPoint.X + dreapta2.EndPoint.X) / 2, (dreapta2.StartPoint.Y + dreapta2.EndPoint.Y) / 2, 0);
                        }
                        if (int1.Count == 0 || int2.Count == 0)
                        {
                            if (int1.Count == 0) ed.WriteMessage("\nSortarea chenarelor a esuat la pozitia {0}", mid1);
                            else ed.WriteMessage("\nSortarea chenarelor a esuat la pozitia {0}", mid2);
                            return 0;
                        }

                        //Selectia intersectiei celei mai apropiate de mijlocul chenarului
                        List<Point3d> I1 = new List<Point3d>(), I2 = new List<Point3d>();
                        foreach (Point3d p in int1) I1.Add(p);
                        foreach (Point3d p in int2) I2.Add(p);

                        //Sortarea chenarelor dupa distanta pe ax
                        I1.Sort((pt1, pt2) => pt1.DistanceTo(mid1).CompareTo(pt2.DistanceTo(mid1)));
                        I2.Sort((pt1, pt2) => pt1.DistanceTo(mid2).CompareTo(pt2.DistanceTo(mid2)));
                        
                        if (Ax.GetDistAtPoint(I1[0]) < Ax.GetDistAtPoint(I2[0])) return -1;
                        else return 1;
                    }));

                    //Compararea directiei axului cu cea a chenarului 1
                    double dXax = Ax.GetPoint2dAt(1).X - Ax.GetPoint2dAt(0).X;
                    double dYax = Ax.GetPoint2dAt(1).Y - Ax.GetPoint2dAt(0).Y;
                    double dXchen = Chenare[0].GetPoint2dAt(1).X - Chenare[0].GetPoint2dAt(0).X;
                    double dYchen = Chenare[0].GetPoint2dAt(1).Y - Chenare[0].GetPoint2dAt(0).Y;
                    if (dXchen != 0 && (dYchen == 0 || Math.Abs(dXax) >= Math.Abs(dYax)))
                    {
                        if (Math.Sign(dXax) != Math.Sign(dXchen)) Chenare.Reverse();
                    }
                    else
                    {
                        if (Math.Sign(dYax) != Math.Sign(dYchen)) Chenare.Reverse();
                    }
                    #endregion

                    //#region Stil plotare
                    //PlotSettings Setari = new PlotSettings(false);
                    //if (numeExistente.Count != 0) //Se incearca copierea setarilor planselor existente
                    //{
                    //    Layout existent = (Layout)trans.GetObject(lm.GetLayoutId(numeExistente[numeExistente.Count - 1]), OpenMode.ForRead);
                    //    Setari.CopyFrom(existent);
                    //}
                    //foreach (string PD in PDList) //Setarea imprimantei si a formatului hartiei
                    //{
                    //    //ed.WriteMessage("\n" + PD);
                    //    if (PD.Contains("Xerox") && PD.Contains("pc3"))
                    //    {
                    //        PSV.SetPlotConfigurationName(Setari, PD, "A3");
                    //        ed.WriteMessage("{0} --> Activat, format hartie: A3", PD);
                    //    }
                    //}

                    //#endregion

                    #region Denumirea plansei
                    //Plansa noua de inceput
                    string radacina = string.Empty;
                    if (primaPlansa)
                    {
                        PromptStringOptions PrStrOpt = new PromptStringOptions("\nNume layout: ");
                        PrStrOpt.AllowSpaces = false;
                        PromptResult PrStrRez = ed.GetString(PrStrOpt);
                        if (PrStrRez.Status != PromptStatus.OK) 
                        {
                            ed.WriteMessage("\nComanda intrerupta.");
                            return;
                        }
                        radacina = PrStrRez.StringResult;
                        string nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                        while (numeExistente.Contains(nume))
                        {
                            ultPag = ultPag + 10;
                            nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                        }
                        //ed.WriteMessage("\nSe creaza plansa {0}", nume);
                        lm.CreateLayout(nume);

                        Layout plansa = (Layout)trans.GetObject(lm.GetLayoutId(nume), OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(plansa.BlockTableRecordId, OpenMode.ForWrite);
                        DBText t1 = new DBText();
                        t1.TextString = "Strada " + radacina;
                        t1.Position = new Point3d(ins.X - 350, ins.Y + 135, 0);
                        t1.Layer = "TEXT_PLAN";
                        t1.Height = 30;
                        btr.AppendEntity(t1);
                        trans.AddNewlyCreatedDBObject(t1, true);

                        numeExistente.Add(nume);
                        primaPlansa = false;
                    }
                    //Sau se redenumeste plansa
                    else
                    {
                        string rad = numeExistente[numeExistente.Count - 1];
                        //nume = nume.Replace(nume.Substring(nume.LastIndexOf("-")), string.Format("-{0})", ultPag + Chenare.Count));
                        //rad = rad.Substring(0, Math.Max(1, rad.LastIndexOf("-") - 1));
                        //if (char.IsDigit(rad[rad.Length - 1]))
                        //{
                        //    rad = rad.Substring(0, rad.Length - 1);
                        //}
                        //string nume = rad + string.Format("{0}-{1}", ultPag + 1, ultPag + Chenare.Count);
                        //while (numeExistente.Contains(nume))
                        //{
                        //    ultPag = ultPag + 10;
                        //    nume = rad + string.Format("{0}-{1}", ultPag + 1, ultPag + Chenare.Count);
                        //}
                        string nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                        while (numeExistente.Contains(nume))
                        {
                            ultPag = ultPag + 10;
                            nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                        }

                        //ed.WriteMessage("\nSe redenumeste plansa din {0} in {1}", numeExistente[numeExistente.Count - 1], nume);
                        lm.RenameLayout(numeExistente[numeExistente.Count - 1], nume);
                        numeExistente[numeExistente.Count - 1] = nume;

                        Layout plansa = (Layout)trans.GetObject(lm.GetLayoutId(nume), OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(plansa.BlockTableRecordId, OpenMode.ForWrite);
                        DBText t1 = new DBText();
                        t1.TextString = "Strada ";
                        t1.Position = new Point3d(ins.X - 350, ins.Y + 135, 0);
                        t1.Layer = "TEXT_PLAN";
                        t1.Height = 30;
                        btr.AppendEntity(t1);
                        trans.AddNewlyCreatedDBObject(t1, true);
                    }
                    #endregion

                    #region Generarea hublourilor
                    for (int i = 0; i < Chenare.Count; i++)
                    {
                        //Calcularea pozitiei hubloului
                        if (i > 0)
                        {
                            if (i%10 == 0)
                            {
                                ins = new Point3d(0, 0, 0);
                                //string nume = numeExistente[numeExistente.Count - 1];
                                //nume = nume.Replace(nume.Substring(nume.LastIndexOf("(")), string.Format("(p{0}-{1})", i + 1, Math.Min(i + 10, ultPag + Chenare.Count)));
                                //while (numeExistente.Contains(nume))
                                //{
                                //    nume = nume + "(1)";
                                //}
                                string nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                                while (numeExistente.Contains(nume))
                                {
                                    ultPag = ultPag + 10;
                                    nume = radacina + "(p" + (ultPag + 1).ToString() + "-" + Math.Min(Chenare.Count + pag0, ultPag + 10) + ")";
                                }
                                //ed.WriteMessage("\nSe creaza plansa {0}", nume);
                                lm.CreateLayout(nume);

                                Layout pNoua = (Layout)trans.GetObject(lm.GetLayoutId(nume), OpenMode.ForRead);
                                BlockTableRecord btrNou = (BlockTableRecord)trans.GetObject(pNoua.BlockTableRecordId, OpenMode.ForWrite);
                                DBText t1 = new DBText();
                                t1.TextString = "Strada " + radacina;
                                t1.Position = new Point3d(ins.X - 350, ins.Y + 135, 0);
                                t1.Layer = "TEXT_PLAN";
                                t1.Height = 30;
                                btrNou.AppendEntity(t1);
                                trans.AddNewlyCreatedDBObject(t1, true);

                                numeExistente.Add(nume);
                                //ed.WriteMessage("\nPozitie noua {0} - Chenar {1}", ins, Chenare[i].StartPoint);
                            }
                            else
                            {
                                ins = new Point3d(ins.X + 420, ins.Y, 0);
                            }
                        }
                        //ed.WriteMessage("\nPozitie noua {0} - Chenar {1}", ins, Chenare[i].StartPoint);

                        //Deblocarea la scriere a plansei
                        string numePlansa = numeExistente[numeExistente.Count - 1];
                        Layout plansa = (Layout)trans.GetObject(lm.GetLayoutId(numePlansa), OpenMode.ForWrite);
                        BlockTableRecord btr = (BlockTableRecord)trans.GetObject(plansa.BlockTableRecordId, OpenMode.ForWrite);

                        //Se creaza hubloul initial daca este necesar (Paperspace viewport);
                        if (plansa.GetViewports().Count == 0)
                        {
                            Viewport psvp = new Viewport();
                            psvp.Layer = "0";
                            psvp.Height = 1;
                            psvp.Width = 1;
                            btr.AppendEntity(psvp);
                            trans.AddNewlyCreatedDBObject(psvp, true);
                        }

                        //Setarea formatului, scarii, stilului de plotare si imprimantei
                        //PSV.SetPlotPaperUnits(plansa, PlotPaperUnit.Millimeters);
                        //PSV.SetPlotOrigin(plansa, Point2d.Origin);
                        
                        //ed.WriteMessage("\nSe tiparesc stilurile de plotare:");
                        //foreach (string PS in PSList)
                        //{
                        //    //ed.WriteMessage("\n" + PS);
                        //    if (PS.Contains("Stiftzuordnung") && Application.GetSystemVariable("PSTYLEMODE").ToString().Equals("1"))
                        //    {
                        //        ed.WriteMessage("--> Activat");
                        //        PSV.SetCurrentStyleSheet(plansa, PS);
                        //    }
                        //}
                        
                        //ed.WriteMessage("\nSe tiparesc dispozitivele de plotare:");
                        
                        //PSV.SetPlotType(plansa, PlotType.Window);
                        //PSV.SetUseStandardScale(plansa, false);
                        PSV.SetStdScale(plansa, 1);
                        PSV.SetCustomPrintScale(plansa, new CustomScale(1, 1000));

                        //Crearea hubloului
                        Viewport hublou = new Viewport();
                        hublou.Layer = "Cartus";
                        Point3d P1 = Chenare[i].GetPoint3dAt(0), P2 = Chenare[i].GetPoint3dAt(1), P3 = Chenare[i].GetPoint3dAt(2);

                        hublou.ViewDirection = Vector3d.ZAxis;
                        hublou.Width = 345;
                        hublou.Height = 287;
                        hublou.CenterPoint = new Point3d(ins.X + hublou.Width / 2, ins.Y + hublou.Height / 2, 0);
                        hublou.CustomScale = hublou.Width / P1.DistanceTo(P2);

                        hublou.TwistAngle = -(new Line(P1, P2).Angle);
                        hublou.ViewTarget = new Point3d(0.5 * (P1.X + P3.X), 0.5 * (P1.Y + P3.Y), 0);

                        btr.AppendEntity(hublou);
                        trans.AddNewlyCreatedDBObject(hublou, true);
                        //ed.WriteMessage("\nS-a creat hubloul cu numarul {0}", hublou.Number);

                        //Desenarea chenarului exterior
                        Polyline chenExt = new Polyline();
                        chenExt.AddVertexAt(0, new Point2d(ins.X - 20, ins.Y - 5), 0,0,0);
                        chenExt.AddVertexAt(1, new Point2d(ins.X + 400, ins.Y - 5), 0, 0, 0);
                        chenExt.AddVertexAt(2, new Point2d(ins.X + 400, ins.Y + 292), 0, 0, 0);
                        chenExt.AddVertexAt(3, new Point2d(ins.X - 20, ins.Y + 292), 0, 0, 0);
                        chenExt.Closed = true;
                        chenExt.Layer = "Cartus";
                        btr.AppendEntity(chenExt);
                        trans.AddNewlyCreatedDBObject(chenExt, true);
                    }
                    #endregion

                    // Actualizare numar anterior si iesire sau tranzitie spre iteratie noua
                    anterior += Chenare.Count;
                    ultPag += Chenare.Count;
                    if (anterior >= 10) break; //iesire din comanda

                    PromptKeywordOptions PrKeyOpt = new PromptKeywordOptions("\nContinua adaugarea chenarelor? ");
                    PrKeyOpt.Keywords.Add("Da");
                    PrKeyOpt.Keywords.Add("Nu");
                    PrKeyOpt.Keywords.Default = "Da";
                    PrKeyOpt.AppendKeywordsToMessage = true;
                    PrKeyOpt.AllowArbitraryInput = false;

                    PromptResult PrKeyRez = ed.GetKeywords(PrKeyOpt);
                    if (PrKeyRez.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nComanda Intrerupta.");
                        return;
                    }
                    if (PrKeyRez.StringResult == "Nu") continua = false;

                    if (PrKeyRez.StringResult == "Da")
                    {
                        ins = new Point3d(0, ins.Y - 350, 0); //Daca se continua se coboara un rand
                    }
                }

                //TADAAA
                trans.Commit();
                ed.Regen();
            }
        }

        [CommandMethod("GXD")]

        [CommandMethod("Comenzi")]
        public void comenzi()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            
            //Gasirea fisierelor dll
            System.IO.DirectoryInfo dllDir = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6));
            System.IO.FileInfo[] dlluri = dllDir.GetFiles("*UTIL*.dll");

            foreach (System.IO.FileInfo dll in dlluri)
            {
                ed.WriteMessage("\n***--> Comenzile din fisierul {0} ({1}): ***", dll.Name, dll.FullName);
                try
                {
                    Assembly asm = Assembly.LoadFrom(dll.FullName);
                    var clase = from t in asm.GetTypes() where t.IsClass select t;
                    foreach (Type t in clase)
                    {
                        var CMA = from m in t.GetMethods() where m.GetCustomAttributes(typeof(CommandMethodAttribute), false).Length != 0 select m;
                        foreach (MethodInfo m in CMA) ed.WriteMessage("\n"+ m.Name);
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
            }
        }

        static public void GetXData()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            // Ask the user to select an entity
            // for which to retrieve XData
            PromptEntityOptions opt = new PromptEntityOptions("\nSelect entity: ");
            PromptEntityResult res = ed.GetEntity(opt);

            if (res.Status == PromptStatus.OK)
            {
                Transaction tr = doc.TransactionManager.StartTransaction();

                using (tr)
                {
                    DBObject obj = tr.GetObject(res.ObjectId, OpenMode.ForRead);
                    ResultBuffer rb = obj.XData;
                    if (rb == null)
                    {
                        ed.WriteMessage("\nEntity does not have XData attached.");
                    }

                    else
                    {
                        int n = 0;
                        foreach (TypedValue tv in rb)
                        {
                            ed.WriteMessage(

                              "\nTypedValue {0} - type: {1}, value: {2}",

                              n++,

                              tv.TypeCode,

                              tv.Value

                            );

                        }

                        rb.Dispose();

                    }

                }

            }

        }

    [CommandMethod("SXD")]

    static public void SetXData()

    {

      Document doc =

        Application.DocumentManager.MdiActiveDocument;

      Editor ed = doc.Editor;

      // Ask the user to select an entity

      // for which to set XData

      PromptEntityOptions opt =

        new PromptEntityOptions(

          "\nSelect entity: "

        );

      PromptEntityResult res =

        ed.GetEntity(opt);

      if (res.Status == PromptStatus.OK)

      {

        Transaction tr =

          doc.TransactionManager.StartTransaction();

        using (tr)

        {

          DBObject obj =

            tr.GetObject(

              res.ObjectId,

              OpenMode.ForWrite

            );

          AddRegAppTableRecord("KEAN");

          ResultBuffer rb =

            new ResultBuffer(

              new TypedValue(1001, "KEAN"),

              new TypedValue(1000, "This is a test string")

            );

          obj.XData = rb;

          rb.Dispose();

          tr.Commit();

        }

      }

    }

    static void AddRegAppTableRecord(string regAppName)
    {

        Document doc =

          Application.DocumentManager.MdiActiveDocument;

        Editor ed = doc.Editor;

        Database db = doc.Database;

        Transaction tr =

          doc.TransactionManager.StartTransaction();

        using (tr)
        {

            RegAppTable rat =

              (RegAppTable)tr.GetObject(

                db.RegAppTableId,

                OpenMode.ForRead,

                false

              );

            if (!rat.Has(regAppName))
            {

                rat.UpgradeOpen();

                RegAppTableRecord ratr =

                  new RegAppTableRecord();

                ratr.Name = regAppName;

                rat.Add(ratr);

                tr.AddNewlyCreatedDBObject(ratr, true);

            }

            tr.Commit();

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
                        ed.WriteMessage(string.Format("\nEroare la importarea blocului '{0}': {1}", numeBloc, ex.Message));
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

