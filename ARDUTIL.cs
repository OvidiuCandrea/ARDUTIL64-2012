using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.IO;
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
            using (Database db = acadDoc.Database)
            {
                //List<string> listaNume = new List<string>();
                //acadDoc.LockDocument();

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
                    using (Database dbNoua = new Database(true, false))
                    {
                        //Importarea sablonului NVEX.dwt
                        try
                        {
                            AcadPreferences acPrefComObj = (AcadPreferences)Application.Preferences;
                            string caleDwt = acPrefComObj.Files.TemplateDwgPath + "\\NVEX.dwt";
                            //string caleDwt = @"C:\Users\user\AppData\Local\Autodesk\C3D 2012\enu\Template\NVEX.dwt";
                            ed.WriteMessage("\n" + caleDwt);
                            dbNoua.ReadDwgFile(caleDwt, FileOpenMode.OpenTryForReadShare, false, null);
                            dbNoua.CloseInput(false);
                            ed.WriteMessage("\nSablonul 'NVEX.dwt' a fost importat cu succes.");
                        }
                        catch
                        {
                            ed.WriteMessage("\nEroare la importarea sablonului 'NVEX.dwt'!");
                        }
                        dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                        dbNoua.CloseInput(true);
                    }


                    //Procesare Planse
                    BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                    double deltaX = 0;
                    double deltaY = 0;
                    double scara = 1;
                    if (formular.Filtru == "XS") scara = 0.1;
                    string numePrecedent = string.Empty;
                    foreach (string numePlansa in formular.listaNume)
                    {
                        using (Database dbNoua = new Database(false, true))
                        {
                            dbNoua.ReadDwgFile(caleNoua, FileOpenMode.OpenForReadAndWriteNoShare, false, null);
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
                            using (Database dbCur = new Database(true, false))
                            {
                                HostApplicationServices.WorkingDatabase = dbCur;
                                db.Wblock(dbCur, obiecte, Point3d.Origin, DuplicateRecordCloning.Ignore);
                                ObjectIdCollection objCur = new ObjectIdCollection();
                                using (Transaction trCur = dbCur.TransactionManager.StartTransaction())
                                {
                                    //HostApplicationServices.WorkingDatabase = dbCur;
                                    BlockTable btCur = (BlockTable)trCur.GetObject(dbCur.BlockTableId, OpenMode.ForRead);
                                    BlockTableRecord btrCur = (BlockTableRecord)trCur.GetObject(btCur[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                                    //Actualizeaza deltaX si deltaY daca e necesar
                                    if (numePrecedent != string.Empty &&
                                        numePrecedent.ToUpper().Substring(0, numePrecedent.LastIndexOf("-")) != numePlansa.ToUpper().Substring(0, numePlansa.LastIndexOf("-")))
                                    {
                                        deltaY -= scara * (layout.Extents.MaxPoint.Y + 80);
                                        deltaX = 0;
                                    }
                                    numePrecedent = numePlansa;

                                    if (deltaX == 0) //Notarea numelui plansei
                                    {
                                        DBText t1 = new DBText();
                                        t1.TextString = "Planse " + numePlansa.Substring(0, numePlansa.LastIndexOf(formular.Filtru) - 1);
                                        t1.Justify = AttachmentPoint.BottomRight;
                                        t1.Position = new Point3d(-500, 0, 0);
                                        t1.Layer = "0";
                                        t1.Height = 30;
                                        btrCur.AppendEntity(t1);
                                        trCur.AddNewlyCreatedDBObject(t1, true);
                                    }

                                    foreach (ObjectId objId in btrCur)
                                    {
                                        Entity ent = (Entity)trCur.GetObject(objId, OpenMode.ForWrite);
                                        if (ent is Viewport) continue;
                                        Matrix3d translatie = Matrix3d.Displacement(new Vector3d(deltaX, deltaY, 0));
                                        ent.TransformBy(translatie);
                                        Matrix3d scalare = Matrix3d.Scaling(scara, new Point3d(deltaX, deltaY, 0));
                                        ent.TransformBy(scalare);
                                        objCur.Add(objId);
                                    }

                                    //Actualizeaza deltaX
                                    deltaX += scara * (layout.Extents.MaxPoint.X + 80);


                                    trCur.Commit();
                                }
                                //HostApplicationServices.WorkingDatabase = db;

                                //Copierea obiectelor procesate in plansa finala
                                //dbCur.Wblock(dbNoua, objCur, Point3d.Origin, DuplicateRecordCloning.MangleName);
                                IdMapping iMap = new IdMapping();
                                dbNoua.WblockCloneObjects(objCur, dbNoua.CurrentSpaceId, iMap, DuplicateRecordCloning.Ignore, false);

                                //Crearea Desenului Nou
                                dbNoua.CloseInput(true);
                                dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                            }

                        }
                    } //capat foreach (string numePlansa in formular.listaNume)
                    HostApplicationServices.WorkingDatabase = db; //NECESAR CA SA NU CRAPE DIN CAND IN CAND ACADUL. ALTEREAZA IN SCHIMB CULORILE AFISATE
                    //ed.Regen(); //NU FUNCTIONEAZA

                    #region NU MAI E NECESAR ODATA CU FOLOSIREA WblockCloneObjects CU DuplicateRecordCloning.Ignore
                    /////////////////////////////////////////////////////////////////////////////////////////////////
                    ////Curatarea bazei de data de straturi si stiluri duplicat 
                    //using (Transaction trNoua = dbNoua.TransactionManager.StartTransaction())
                    //{
                    //    BlockTable btNou = (BlockTable)trNoua.GetObject(dbNoua.BlockTableId, OpenMode.ForWrite);
                    //    BlockTableRecord msNou = (BlockTableRecord)trNoua.GetObject(btNou[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    //    LayerTable lt = (LayerTable)trNoua.GetObject(dbNoua.LayerTableId, OpenMode.ForWrite);
                    //    LinetypeTable ltt = (LinetypeTable)trNoua.GetObject(dbNoua.LinetypeTableId, OpenMode.ForWrite);
                    //    DimStyleTable dst = (DimStyleTable)trNoua.GetObject(dbNoua.DimStyleTableId, OpenMode.ForWrite);

                    //    //Corectarea obiectelor
                    //    foreach (ObjectId objId in msNou)
                    //    {
                    //        Entity ent = trNoua.GetObject(objId, OpenMode.ForRead) as Entity;

                    //        //Straturi
                    //        string numeStrat = ent.Layer;
                    //        if (ent.Layer.StartsWith("$"))
                    //        {
                    //            try
                    //            {
                    //                string numeStratBun = ent.Layer.Substring(ent.Layer.Substring(0, ent.Layer.Length - 2).LastIndexOf('$') + 1);
                    //                //ed.WriteMessage("\n{0} ---> {1}", ent.Layer, numeStratBun);
                    //                ent.UpgradeOpen();
                    //                if (lt.Has(numeStratBun)) ent.Layer = numeStratBun;
                    //                else if (numeStratBun.Length != 0)
                    //                {
                    //                    LayerTableRecord stratRau = (LayerTableRecord)trNoua.GetObject(ent.LayerId, OpenMode.ForRead);
                    //                    LayerTableRecord stratBun = new LayerTableRecord();
                    //                    stratBun.CopyFrom(stratRau);
                    //                    stratBun.Name = numeStratBun;
                    //                    lt.Add(stratBun);
                    //                    trNoua.AddNewlyCreatedDBObject(stratBun, true);
                    //                    ent.Layer = numeStratBun;
                    //                }
                    //            }
                    //            catch (System.Exception)
                    //            {
                    //                ed.WriteMessage("\nProblema la procesarea obiectului {0}, din stratul {1}", ent.ToString(), ent.Layer);
                    //            }
                    //        }

                    //        //Tipuri de linie
                    //        if (ent.Linetype.StartsWith("$"))
                    //        {
                    //            string numeLinieBun = ent.Linetype.Substring(ent.Linetype.Substring(0, ent.Linetype.Length - 2).LastIndexOf('$') + 1);
                    //            ent.UpgradeOpen();
                    //            if (ltt.Has(numeLinieBun)) ent.Linetype = numeLinieBun;
                    //            else if (numeLinieBun.Length != 0)
                    //            {
                    //                LinetypeTableRecord linieRea = (LinetypeTableRecord)trNoua.GetObject(ent.LinetypeId, OpenMode.ForRead);
                    //                LinetypeTableRecord linieBuna = new LinetypeTableRecord();
                    //                linieBuna.CopyFrom(linieRea);
                    //                linieBuna.Name = numeLinieBun;
                    //                lt.Add(linieBuna);
                    //                trNoua.AddNewlyCreatedDBObject(linieBuna, true);
                    //                ent.Linetype = numeLinieBun;
                    //            }
                    //        }

                    //        //Stiluri de cotare
                    //        if (ent is Dimension && ((Dimension)ent).DimensionStyleName.StartsWith("$"))
                    //        {
                    //            string numeCotareBun = ((Dimension)ent).DimensionStyleName;
                    //            numeCotareBun = numeCotareBun.Substring(numeCotareBun.Substring(0, numeCotareBun.Length - 2).LastIndexOf('$') + 1);
                    //            if (dst.Has(numeCotareBun)) ((Dimension)ent).DimensionStyleName = numeCotareBun;
                    //            else if (numeCotareBun.Length != 0)
                    //            {
                    //                DimStyleTableRecord cotareRea = (DimStyleTableRecord)trNoua.GetObject(((Dimension)ent).DimensionStyle, OpenMode.ForRead);
                    //                DimStyleTableRecord cotareBuna = new DimStyleTableRecord();
                    //                cotareBuna.CopyFrom(cotareRea);
                    //                cotareBuna.Name = numeCotareBun;
                    //                dst.Add(cotareBuna);
                    //                trNoua.AddNewlyCreatedDBObject(cotareBuna, true);
                    //                ((Dimension)ent).DimensionStyleName = numeCotareBun;
                    //            }
                    //        }

                    //        //Blocuri NU MERGE!
                    //        //if (ent is BlockReference)
                    //        //{
                    //        //    BlockReference bloc = ent as BlockReference;
                    //        //    if (bloc.BlockName.Contains("$$"))
                    //        //    {
                    //        //        string numeBun = bloc.BlockName.Substring(bloc.BlockName.LastIndexOf("$") + 1);
                    //        //        ed.WriteMessage(numeBun);
                    //        //        ObjectId bId = ObjectId.Null;
                    //        //        try
                    //        //        {
                    //        //            bId = btNou[numeBun];
                    //        //            if (bId != ObjectId.Null) ed.WriteMessage("\n{0} exista!", numeBun);
                    //        //        }
                    //        //        catch { };
                    //        //        if (bId != ObjectId.Null)
                    //        //        {
                    //        //            bloc.BlockTableRecord = bId;
                    //        //        }
                    //        //        else
                    //        //        {
                    //        //            BlockTableRecord btrBun = (BlockTableRecord)trNoua.GetObject(bloc.BlockTableRecord, OpenMode.ForWrite);
                    //        //            btrBun.Name = numeBun;
                    //        //            ed.WriteMessage("\nS-a redenumit blocul? {0}", bloc.BlockName);
                    //        //            btrBun.DowngradeOpen();
                    //        //        }
                    //        //    }
                    //        //}
                    //    }

                    //    //Stergerea straturilor si stilurilor necorespunzatoare
                    //    foreach (ObjectId lId in lt)
                    //    {
                    //        LayerTableRecord ltr = (LayerTableRecord)trNoua.GetObject(lId, OpenMode.ForWrite);
                    //        if (ltr.Name.Contains('$') && !ltr.IsDependent)
                    //        {
                    //            ltr.Erase(true);
                    //        }
                    //    }
                    //    foreach (ObjectId ltId in ltt)
                    //    {
                    //        LinetypeTableRecord lttr = (LinetypeTableRecord)trNoua.GetObject(ltId, OpenMode.ForWrite);
                    //        if (lttr.Name.Contains('$') && !lttr.IsDependent)
                    //        {
                    //            lttr.Erase(true);
                    //        }
                    //    }
                    //    foreach (ObjectId dsId in dst)
                    //    {
                    //        DimStyleTableRecord dstr = (DimStyleTableRecord)trNoua.GetObject(dsId, OpenMode.ForWrite);
                    //        if (dstr.Name.Contains('$') && !dstr.IsDependent)
                    //        {
                    //            dstr.Erase(true);
                    //        }
                    //    }

                    //    trNoua.Commit();
                    //}

                    //Crearea Desenului Nou
                    //dbNoua.CloseInput(true);
                    //dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                    //dbNoua.Dispose();
                    #endregion

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

                        //HostApplicationServices.WorkingDatabase = db;
                        //acadDoc.LockDocument(DocumentLockMode.NotLocked, null, null, false);
                    }

                    //Nu functioneaza
                    //acadDoc.TransactionManager.EnableGraphicsFlush(true);
                    //acadDoc.TransactionManager.QueueForGraphicsFlush();
                    //Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
                    trans.Commit();
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
                if (alin.StartingStation / interval != Math.Truncate(alin.StartingStation / interval))
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



                // Setarea stratului
                string numeStrat = "Cross - " + alin.Name;
                if (!lt.Has(numeStrat))
                {
                    LayerTableRecord cross = new LayerTableRecord();
                    cross.Name = numeStrat;
                    cross.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                    ObjectId IdStrat = lt.Add(cross);
                    trans.AddNewlyCreatedDBObject(cross, true);
                    ed.WriteMessage("\n Stratul '{0}' a fost creat.", numeStrat);
                }
                else ed.WriteMessage("\nStratul '{0}' exista.", numeStrat);
                ////Verificarea existentei stratului "Cross"
                //if (!lt.Has("Cross"))
                //{
                //    LayerTableRecord cross = new LayerTableRecord();
                //    cross.Name = "Cross";
                //    cross.Color = Color.FromColorIndex(ColorMethod.ByAci, 171);
                //    lt.Add(cross);
                //    ed.WriteMessage("\n Stratul 'Cross' a fost creat.");
                //}
                //else ed.WriteMessage("\nStratul 'Cross' exista.");

                //Desenarea Liniilor
                foreach (double km in listaKm)
                {
                    double xStart = -999;
                    double yStart = -999;
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
                    Linie.Layer = numeStrat;
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

                string prefixNou = prefix.ToString();
                if (prefix == "Ch: ") prefixNou = "Km: ";
                else
                {
                    PrStrOpt.Message = "\nSpecifica prefixul nou: ";
                    PrStrRes = ed.GetString(PrStrOpt);
                    if (PrStrRes.Status == PromptStatus.OK) prefixNou = PrStrRes.StringResult;
                }

                int nrZecimale = 2;
                PromptIntegerOptions PrIntOpt = new PromptIntegerOptions("\nNumarul de zecimale: ");
                PrIntOpt.AllowNegative = false;
                PrIntOpt.DefaultValue = 2;
                PrIntOpt.UseDefaultValue = true;

                PromptIntegerResult PrIntRes = ed.GetInteger(PrIntOpt);
                if(PrIntRes.Status == PromptStatus.OK)
                {
                    nrZecimale = PrIntRes.Value;
                }

                int contor = 0;
                foreach (ObjectId objId in btr)
                {
                    Entity entCur = (Entity)trans.GetObject(objId, OpenMode.ForRead);
                    if (entCur != null && entCur.GetType() == typeof(DBText) && ((DBText)entCur).TextString.StartsWith(prefix))
                    {
                        //ed.WriteMessage("\nTextul analizat: <{0}> contine <{1}>?", ((DBText)entCur).TextString, prefix);
                        entCur.UpgradeOpen();
                        DBText textKm = entCur as DBText;
                        string textNou = textKm.TextString.Replace(prefix, "").Replace("+", "");
                        //ed.WriteMessage(" ---> Da! Valoarea kilometrajului este {0}", textNou);
                        try
                        {
                            //int nrZecimale = textNou.Length - textNou.LastIndexOf('.') - 1;
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
                        Dictionary<Autodesk.Civil.EntitySideType, AlignmentRegionCollection> Supralargiri = 
                            new Dictionary<Autodesk.Civil.EntitySideType, AlignmentRegionCollection>();
                        Dictionary<Autodesk.Civil.EntitySideType, double> OffsetNominal = new Dictionary<Autodesk.Civil.EntitySideType, double>();
                        OffsetNominal.Add(Autodesk.Civil.EntitySideType.Left, 0);
                        OffsetNominal.Add(Autodesk.Civil.EntitySideType.Right, 0);
                        foreach (ObjectId idOffset in Ax.GetChildOffsetAlignmentIds())
                        {
                            Alignment Offset = (Alignment)trans.GetObject(idOffset, OpenMode.ForRead);
                            Supralargiri.Add(Offset.OffsetAlignmentInfo.Side, Offset.OffsetAlignmentInfo.Regions);
                            OffsetNominal[Offset.OffsetAlignmentInfo.Side] = Offset.OffsetAlignmentInfo.NominalOffset;
                            //ed.WriteMessage("\nInformatii aliniamente offset: -->\n,{0}", Offset.OffsetAlignmentInfo.Side);
                        }
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
                                    parametri.Add("I-ID", IID.ToString() + " - " + Ax.Name);
                                    //Viteza de proiectare
                                    double IV = 20;
                                    if (Ax.DesignSpeeds.Count != 0)
                                    {
                                        //IV = Ax.DesignSpeeds.GetDesignSpeed(arc.StartStation).Value;
                                        foreach (DesignSpeed DS in Ax.DesignSpeeds.OrderBy<DesignSpeed, double>(D => D.Station))
                                        {
                                            if (arc.StartStation >= DS.Station) IV = DS.Value;
                                        }
                                    }
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
                                    //Valoarea ratei de suprainaltare
                                    //SuperelevationCurve Super = Ax.SuperelevationCurves.ToList().Find(C => ((C.StartStation + C.EndStation) / 2) > arc.StartStation &&
                                    //                                               ((C.StartStation + C.EndStation) / 2) < arc.EndStation);
                                    //if (Super != null) parametri.Add("R-E", "9.999");
                                    parametri.Add("R-E", "0.000");
                                    //Lungime tranzitie supralargire-suprainaltare
                                    Autodesk.Civil.EntitySideType Parte = Autodesk.Civil.EntitySideType.Left;
                                    Autodesk.Civil.EntitySideType Opus = Autodesk.Civil.EntitySideType.Right;
                                    if (arc.Clockwise)
                                    {
                                        Parte = Autodesk.Civil.EntitySideType.Right;
                                        Opus = Autodesk.Civil.EntitySideType.Left;
                                    }
                                    AlignmentRegionCollection Regiuni;
                                    AlignmentRegionCollection RegOpus;
                                    string s_ls = "0.000";
                                    string s_i = "0.000";
                                    string s_e = "0.000";
                                    if (Supralargiri.TryGetValue(Parte, out Regiuni))
                                    {
                                        foreach (AlignmentRegion Supralargire in Regiuni)
                                        {
                                            if (
                                                Supralargire.RegionType != AlignmentRegionType.Norminal &&
                                                (Supralargire.StartStation + Supralargire.EndStation) / 2 >= arc.StartStation &&
                                                (Supralargire.StartStation + Supralargire.EndStation) / 2 <= arc.EndStation)
                                            {
                                                double li = 0;
                                                double le = 0;
                                                double val = 0;
                                                try
                                                {
                                                    li = Supralargire.EntryTransition.TransitionDescription.Length;
                                                }
                                                catch { }
                                                try
                                                {
                                                    le = Supralargire.ExitTransition.TransitionDescription.Length;
                                                }
                                                catch { }
                                                try
                                                {
                                                    //val = Supralargire.IncreasedWidth;
                                                    val = Math.Abs(Math.Abs(Supralargire.Offset) - Math.Abs(OffsetNominal[Parte]));
                                                }
                                                catch { }
                                                s_ls = Math.Max(li, le).ToString("F3");
                                                if (val != 0)
                                                {
                                                    s_i = val.ToString("F3");
                                                    ed.WriteMessage("\nkm: {0},ls: {1}, s_i: {2}, type: {3}", arc.StartStation, s_ls, s_i, Supralargire.RegionType);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (Supralargiri.TryGetValue(Opus, out RegOpus))
                                    {
                                        foreach (AlignmentRegion Supralargire in RegOpus)
                                        {
                                            if (Supralargire.RegionType != AlignmentRegionType.Norminal &&
                                                (Supralargire.StartStation + Supralargire.EndStation) / 2 >= arc.StartStation &&
                                                (Supralargire.StartStation + Supralargire.EndStation) / 2 <= arc.EndStation)
                                            {
                                                //double li = 0;
                                                //double le = 0;
                                                double val = 0;
                                                //try
                                                //{
                                                //    li = Supralargire.EntryTransition.TransitionDescription.Length;
                                                //}
                                                //catch { }
                                                //try
                                                //{
                                                //    le = Supralargire.ExitTransition.TransitionDescription.Length;
                                                //}
                                                //catch { }
                                                try
                                                {
                                                    val = Supralargire.IncreasedWidth;
                                                }
                                                catch { }
                                                //s_ls = Math.Max(li, le).ToString("F3");
                                                if (val != 0)
                                                {
                                                    s_e = val.ToString("F3");
                                                    ed.WriteMessage("\nkm: {0},ls: {1}, s_e: {2}, type: {3}", arc.StartStation, s_ls, s_e, Supralargire.RegionType);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    parametri.Add("L-WD1", s_ls);
                                    //Valoare supralargire interior;
                                    parametri.Add("L-WI", s_i);
                                    //Valoare supralargire exterior;
                                    parametri.Add("L-WO", s_e);
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
                                    Xdata[26, 0] = "1040"; Xdata[26, 1] = s_ls;
                                    Xdata[27, 0] = "1000"; Xdata[27, 1] = "L-WT1"; //L-WT1   -Widening on tangent-in
                                    Xdata[28, 0] = "1040"; Xdata[28, 1] = s_ls;
                                    Xdata[29, 0] = "1000"; Xdata[29, 1] = "L-WD2"; //L-WD2   -Widening transition-out
                                    Xdata[30, 0] = "1040"; Xdata[30, 1] = s_ls;
                                    Xdata[31, 0] = "1000"; Xdata[31, 1] = "L-WD1"; //L-WD1   -Widening transition-in
                                    Xdata[32, 0] = "1040"; Xdata[32, 1] = s_ls;
                                    Xdata[33, 0] = "1000"; Xdata[33, 1] = "L-WO"; //L-WO    -Widening outer [wo]
                                    Xdata[34, 0] = "1040"; Xdata[34, 1] = s_e;
                                    Xdata[35, 0] = "1000"; Xdata[35, 1] = "L-WI"; //L-WI    -Widening inner [wi]
                                    Xdata[36, 0] = "1040"; Xdata[36, 1] = s_i;
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
                                    double L3P1X = 0; double L3P1Y = 0; Ax.PointLocation((kmCapat + kmStart) / 2, 0, ref L3P1X, ref L3P1Y);
                                    double L3P2X = 0; double L3P2Y = 0; Ax.PointLocation((kmCapat + kmStart) / 2, offset, ref L3P2X, ref L3P2Y);
                                    //L3P2X += 7.1; L3P2Y -= 6.7;
                                    Line L3 = new Line(new Point3d(L3P1X, L3P1Y, 0), new Point3d(L3P2X, L3P2Y, 0));
                                    L3.Layer = numeStrat;
                                    ms.AppendEntity(L3);
                                    trans.AddNewlyCreatedDBObject(L3, true);
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
                                        parametri.Add("CURBA", IID.ToString() + " - " + Ax.Name);
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

        [CommandMethod("acalin")] //Comanda pentru actualizarea Xdata dupa modificarea datelor din blocurile curbelor
        public void acalin()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            Database db = acadDoc.Database;
            CivilDocument civDoc = CivilApplication.ActiveDocument;

            ////Se actualizeaza dictionarul blocurilor de curbe
            //accurbe();
            //ed.WriteMessage("\nS-a rulat comanda <<accurbe>>");

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                //RegAppTable rat = (RegAppTable)trans.GetObject(db.RegAppTableId, OpenMode.ForRead);
                
                //lista aliniamente
                ObjectIdCollection idAxuri = civDoc.GetAlignmentIds();
                if (idAxuri.Count == 0)
                {
                    ed.WriteMessage("\nDesenul nu contine axuri Civil3D! Comanda intrerupta.");
                    return;
                }

                string numeBlocCurba = "HD-CURVE_DATA$RO-01$";
                if (!bt.Has(numeBlocCurba)) //Se verifica daca desenul contine blocuri curba
                {
                    ed.WriteMessage("\nDesenul nu contine blocuri pentru curbe! Comanda intrerupta.");
                    return;
                }
                //else if (!rat.Has("BNYCSM-HD-CURVE_DATA"))
                //{
                //    ed.WriteMessage("\nBlocurile curbelor nu contin date Xdata! Comanda intrerupta.");
                //    return;
                //}
                else
                {
                    //Selectarea blocului de curba
                    PromptEntityOptions PEO = new PromptEntityOptions("\nSelecteaza blocul de curba pentru actualizarea axului: ");
                    PEO.SetRejectMessage("\nObiectul selectat nu este de tip bloc");
                    PEO.AddAllowedClass(typeof(BlockReference), true);
                    PromptEntityResult PER = ed.GetEntity(PEO);
                    if (PER.Status == PromptStatus.OK)
                    {
                        BlockReference BR = (BlockReference)trans.GetObject(PER.ObjectId, OpenMode.ForRead);

                        //Citirea Xdata
                        List<TypedValue> TVs = BR.XData.AsArray().ToList();

                        //Verificarea validitatii blocului
                        if (BR.Name != numeBlocCurba || TVs.Count == 0)
                        {
                            ed.WriteMessage("\nBlocul selectat nu este corespunzator! Comanda intrerupta.");
                            return;
                        }

                        //Citirea atributelor
                        Dictionary<string, AttributeReference> parametri = new Dictionary<string, AttributeReference>();
                        parametri.Add("I-ID", null);
                        parametri.Add("L-WD2", null);
                        parametri.Add("L-WD1", null);
                        parametri.Add("L-WO", null);
                        parametri.Add("L-WI", null);
                        parametri.Add("R-E", null);
                        parametri.Add("L-S1", null);
                        parametri.Add("L-S2", null);
                        parametri.Add("I-V", null);
                        parametri.Add("P-VERT", null);
                        foreach (ObjectId idAR in BR.AttributeCollection)
                        {
                            DBObject obj = trans.GetObject(idAR, OpenMode.ForRead);
                            AttributeReference AR = obj as AttributeReference;
                            if (AR != null)
                            {
                                if (parametri.Keys.Contains(AR.Tag.ToUpper()))
                                {
                                    parametri[AR.Tag.ToUpper()] = AR;
                                }
                            }
                        }

                        //Gasirea axului corespunzator blocului
                        Alignment Ax = null;
                        foreach (ObjectId idAx in idAxuri)
                        {
                            Ax = (Alignment)trans.GetObject(idAx, OpenMode.ForRead);
                            string numeAx = parametri["I-ID"].TextString;
                            numeAx = numeAx.Remove(0, numeAx.IndexOf("-") + 2);
                            if (Ax.Name != numeAx) Ax = null;
                            else break;
                        }
                        if (Ax == null)
                        {
                            ed.WriteMessage("\nNu a fost gasit axul corespunzator blocului de curba selectat! Comanda intrerupta");
                            return;
                        }
                        else
                        {
                            ed.WriteMessage("\nAxul: " + Ax.Name);
                        }

                        //Verificarea existentei aliniamentelor offset
                        ObjectIdCollection idAxOffset = Ax.GetChildOffsetAlignmentIds();
                        if (idAxOffset.Count == 0)
                        {
                            ed.WriteMessage("\nAxul nu are aliniamente offset! Comanda intrerupta.");
                            return;
                        }

                        //Gasirea curbei corespunzatoare blocului
                        int nrCurba;
                        int.TryParse(parametri["I-ID"].TextString.Replace(" - " + Ax.Name, ""), out nrCurba);
                        double kmBloc = -999.999;
                        double offsetBloc = -999.999;
                        string[] coordVarf = parametri["P-VERT"].TextString.Split(new char[] { 'N', 'E'});
                        ed.WriteMessage("\n0: {0},1: {1},2: {2}", coordVarf[0], coordVarf[1], coordVarf[2]);
                        double xVarf = -999.999;
                        double.TryParse(coordVarf[1], out xVarf);
                        double yVarf = -999.999;
                        double.TryParse(coordVarf[0], out yVarf);
                        //ed.WriteMessage("\nCoordonate varf: {0}, {1}", xVarf, yVarf);
                        Ax.StationOffset(xVarf, yVarf, ref kmBloc, ref offsetBloc);
                        ed.WriteMessage("\nCurba cu numarul: {0}, la km: {1}", nrCurba, kmBloc);
                        AlignmentEntity EntitateCurba = Ax.Entities.EntityAtStation(kmBloc);
                        ed.WriteMessage("\nEntitate de tip: {0}, cu {1} subentitati.", EntitateCurba.EntityType, EntitateCurba.SubEntityCount);
                        double kmStart = EntitateCurba[0].StartStation;
                        double kmCapat = EntitateCurba[EntitateCurba.SubEntityCount - 1].EndStation;
                        int indiceSens = 1;
                        AlignmentSubEntityArc Arc = EntitateCurba[(int)Math.Truncate(EntitateCurba.SubEntityCount / 2.00)] as AlignmentSubEntityArc;
                        if (Arc != null && !Arc.Clockwise)
                        {
                            indiceSens = -1;
                        }

                        //Gasirea aliniamentelor offset
                        Dictionary<Autodesk.Civil.EntitySideType, Alignment> AxuriOffset = new Dictionary<Autodesk.Civil.EntitySideType, Alignment>();
                        foreach (ObjectId idOffset in idAxOffset)
                        {
                            Alignment AxOffset = (Alignment)trans.GetObject(idOffset, OpenMode.ForRead);
                            Autodesk.Civil.EntitySideType partea = AxOffset.OffsetAlignmentInfo.Side;
                            double offsetNominal = AxOffset.OffsetAlignmentInfo.NominalOffset;
                            //In cazul prezentei mai multor aliniamente offset pe aceasi parte se modifica aliniamentul cel mai apropiat de axul drumului
                            if (AxuriOffset.Keys.Contains(partea) && offsetNominal >= AxuriOffset[partea].OffsetAlignmentInfo.NominalOffset)
                            {
                                continue;
                            }
                            AxuriOffset[partea] = AxOffset;
                        }

                        //Adaugarea supralargirii stanga
                        OffsetAlignmentInfo OffsetStanga = AxuriOffset[Autodesk.Civil.EntitySideType.Left].OffsetAlignmentInfo;
                        double Stanga;
                        if (indiceSens == 1) double.TryParse(parametri["L-WO"].TextString, out Stanga);
                        else double.TryParse(parametri["L-WI"].TextString, out Stanga);
                        //Stanga = Math.Abs(OffsetStanga.NominalOffset - Stanga);
                        ed.WriteMessage("\nSupralargire stanga: {0}", Stanga);
                        if (Stanga > 0) OffsetStanga.AddWidening(kmStart, kmCapat, Stanga);

                        //Adaugarea supralargirii dreapta
                        OffsetAlignmentInfo OffsetDreapta = AxuriOffset[Autodesk.Civil.EntitySideType.Right].OffsetAlignmentInfo;
                        AutoWideningInfo AWI = new AutoWideningInfo();
                        double Dreapta;
                        if (indiceSens == 1)
                        {
                            double.TryParse(parametri["L-WI"].TextString, out Dreapta);
                            AWI.Side = Autodesk.Civil.Land.WideningSide.Inside;
                        }
                        else
                        {
                            double.TryParse(parametri["L-WO"].TextString, out Dreapta);
                            AWI.Side = Autodesk.Civil.Land.WideningSide.Outside;
                        }
                        //Dreapta = OffsetDreapta.NominalOffset + Dreapta;
                        ed.WriteMessage("\nSupralargire dreapta: {0}", Dreapta);

                        //if (Dreapta > 0) OffsetDreapta.AddWidening(kmStart, kmCapat, Dreapta);
                        //AlignmentTransitionCollection Tranzitii = OffsetDreapta.Transitions;
                        //int nr = 0;
                        //foreach( AlignmentTransition Tranzitie in Tranzitii)
                        //{
                        //    nr++;
                        //    ed.WriteMessage("\nTranzitia {0}, de tipul {1}, si criteriile {2}", nr, Tranzitie.TransitionType, Tranzitie.TransitionDescription);
                        //}
                        //AlignmentRegionCollection Regiuni = OffsetDreapta.Regions;
                        //AlignmentRegion Regiune = Regiuni.Last();
                        AWI.IncreasedWidth = Dreapta;
                        double Lcs;
                        double.TryParse(parametri["L-WD1"].TextString, out Lcs);
                        AWI.TransitionLength = Lcs;
                        OffsetDreapta.AddAutoWidenings(AWI, new AlignmentSubEntityArc[] { Arc });

                    }
                    else
                    {
                        ed.WriteMessage("\nComanda intrerupta!");
                    }
                    trans.Commit();
                }
            }
        }

        [CommandMethod("accurbe")] //Comanda pentru actualizarea Xdata dupa modificarea datelor din blocurile curbelor
        public void accurbe()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            Database db = acadDoc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                RegAppTable rat = (RegAppTable)trans.GetObject(db.RegAppTableId, OpenMode.ForRead);

                string numeBlocCurba = "HD-CURVE_DATA$RO-01$";
                if (!bt.Has(numeBlocCurba)) //Se verifica daca desenul contine blocuri curba
                {
                    ed.WriteMessage("\nDesenul nu contine blocuri pentru curbe! Comanda intrerupta.");
                    return;
                }
                else if (!rat.Has("BNYCSM-HD-CURVE_DATA"))
                {
                    ed.WriteMessage("\nBlocurile curbelor nu contin date Xdata! Comanda intrerupta.");
                    return;
                }
                else
                {
                    //Selectarea blocurilor curba
                    TypedValue[] TVS = new TypedValue[]
                    {
                        new TypedValue(0 , "INSERT"),
                        new TypedValue(2 , numeBlocCurba)
                    };
                    SelectionFilter SF = new SelectionFilter(TVS);
                    PromptSelectionResult PSR = ed.SelectAll(SF);
                    if (PSR.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nDesenul nu contine blocuri pentru curbe! Comanda intrerupta.");
                        return;
                    }
                    //Parcurgerea blocurilor
                    foreach (ObjectId idBloc in PSR.Value.GetObjectIds())
                    {
                        BlockReference BR = (BlockReference)trans.GetObject(idBloc, OpenMode.ForWrite);
                        //Citirea Xdata
                        List<TypedValue> TVs = BR.XData.AsArray().ToList();

                        //Citirea atributelor si actualizarea Xdata
                        Dictionary<string, int> parametri = new Dictionary<string, int>();
                        parametri.Add("L-WD2", 30);
                        parametri.Add("L-WD1", 32);
                        parametri.Add("L-WO", 34);
                        parametri.Add("L-WI", 36);
                        parametri.Add("R-E", 38);
                        parametri.Add("I-V", 42);
                        foreach (ObjectId idAR in BR.AttributeCollection)
                        {
                            DBObject obj = trans.GetObject(idAR, OpenMode.ForRead);
                            AttributeReference AR = obj as AttributeReference;
                            if (AR != null)
                            {
                                if (parametri.Keys.Contains(AR.Tag.ToUpper()))
                                {
                                    int code = TVs[parametri[AR.Tag.ToUpper()]].TypeCode;
                                    TVs[parametri[AR.Tag.ToUpper()]] = new TypedValue(code, AR.TextString);
                                    if (AR.Tag.ToUpper() == "L-WD1")
                                    {
                                        TVs[30] = new TypedValue(code, AR.TextString);
                                    }

                                }
                            }
                        }
                        BR.XData = new ResultBuffer(TVs.ToArray());
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
                    for (int i = 1; i < Vertecsi.Count; i++)
                    {
                        if (Vertecsi[i].Position.X >= Xnou)
                        {
                            if (Vertecsi[i].Position.X == Xnou)
                            {
                                Ynou = Vertecsi[i - 1].Position.Y;
                                break;
                            }
                            else
                            {
                                //ed.WriteMessage("\nPozitie Vertex inainte: {0}", Vertecsi[i - 1].Position);
                                Ynou = Vertecsi[i - 1].Position.Y +
                                    (Xnou - Vertecsi[i - 1].Position.X) / (Vertecsi[i].Position.X - Vertecsi[i - 1].Position.X) * (Vertecsi[i].Position.Y - Vertecsi[i - 1].Position.Y);
                                break;
                            }
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
                            if (i % 10 == 0)
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
                        chenExt.AddVertexAt(0, new Point2d(ins.X - 20, ins.Y - 5), 0, 0, 0);
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

        [CommandMethod("pagini2")] //Comanda pentru crearea paginilor
        public void pagini2()
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
                            if (i % 10 == 0)
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
                        hublou.Width = 350;
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
                        chenExt.AddVertexAt(0, new Point2d(ins.X - 15, ins.Y - 5), 0, 0, 0);
                        chenExt.AddVertexAt(1, new Point2d(ins.X + 405, ins.Y - 5), 0, 0, 0);
                        chenExt.AddVertexAt(2, new Point2d(ins.X + 405, ins.Y + 292), 0, 0, 0);
                        chenExt.AddVertexAt(3, new Point2d(ins.X - 15, ins.Y + 292), 0, 0, 0);
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

        [CommandMethod("pagini3")] //Comanda pentru crearea paginilor
        public void pagini3()
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
                            if (i % 10 == 0)
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
                        hublou.Width = 395;
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
                        chenExt.AddVertexAt(0, new Point2d(ins.X - 20, ins.Y - 5), 0, 0, 0);
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

        [CommandMethod("rapsant")] //Comanda pentru crearea unui raport cu tipurile de sant NETERMINATA
        public void rapsant()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = acadDoc.Editor;
            CivilDocument civDoc = CivilApplication.ActiveDocument;

            using (Database db = acadDoc.Database)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    //lista aliniamente
                    ObjectIdCollection idAxuri = civDoc.GetAlignmentIds();


                    foreach (ObjectId idAx in idAxuri)
                    {
                        Alignment Ax = (Alignment)trans.GetObject(idAx, OpenMode.ForRead);
                        //ed.WriteMessage("\nSe citesc etichetele axului {0}", Ax.Name);
                        List<Obiect3D<StationOffsetLabel>> Etichete = new List<Obiect3D<StationOffsetLabel>>();

                        //Gasirea tuturor etichetelor km-offset apartinant axului curent
                        foreach (ObjectId idEt in Ax.GetLabelIds())
                        {
                            StationOffsetLabel EtSTO = trans.GetObject(idEt, OpenMode.ForRead) as StationOffsetLabel;
                            if (EtSTO != null)
                            {
                                Obiect3D<StationOffsetLabel> obj = new Obiect3D<StationOffsetLabel>();
                                obj.X = EtSTO.Location.X;
                                obj.Y = EtSTO.Location.Y;
                                double km = 10, offset = 10;
                                try
                                {
                                    Ax.StationOffset(obj.X, obj.Y, ref km, ref offset);
                                    //ed.WriteMessage("\n{0} -> {1}", Ax.Name, km);
                                }
                                catch
                                {
                                    km = -99999;
                                    offset = -99999;
                                    Point3d p = new Point3d(obj.X, obj.Y, 0);
                                    Curve curba = Ax.BaseCurve;
                                    //Verificare punct apropiat de start start sau capat
                                    Line distS = new Line(p, Ax.StartPoint);
                                    Line distC = new Line(p, Ax.EndPoint);
                                    //Se creaza linii de constructie si se aproximeaza km si offsetul punctului aflat inafara axului
                                    Line per;
                                    Point3dCollection pintC = new Point3dCollection();
                                    if (distS.Length <= distC.Length)
                                    {
                                        double Xper = 0, Yper = 0;
                                        Ax.PointLocation(Ax.StartingStation, 10, ref Xper, ref Yper);
                                        per = new Line(Ax.StartPoint, new Point3d(Xper, Yper, 0));
                                        double semn = -1;
                                        if (Ax.StartPoint.DistanceTo(p) >= per.EndPoint.DistanceTo(p)) semn = 1;
                                        per.TransformBy(Matrix3d.Displacement(new Vector3d(p.X - Ax.StartPoint.X, p.Y - Ax.StartPoint.Y, 0)));
                                        per.IntersectWith(Ax, Intersect.ExtendBoth, pintC, new IntPtr(), new IntPtr());
                                        per.EndPoint = pintC[0];
                                        km = Ax.StartingStation - per.EndPoint.DistanceTo(Ax.StartPoint);
                                        offset = semn * per.Length;
                                        ed.WriteMessage("\nPunctul e in afara axului. Km: {0} / Offset: {1}", km, offset);
                                    }
                                    else
                                    {
                                        double Xper = 0, Yper = 0;
                                        Ax.PointLocation(Ax.EndingStation, 10, ref Xper, ref Yper);
                                        per = new Line(Ax.EndPoint, new Point3d(Xper, Yper, 0));
                                        double semn = -1;
                                        if (Ax.EndPoint.DistanceTo(p) >= per.EndPoint.DistanceTo(p)) semn = 1;
                                        per.TransformBy(Matrix3d.Displacement(new Vector3d(p.X - Ax.EndPoint.X, p.Y - Ax.EndPoint.Y, 0)));
                                        per.IntersectWith(Ax, Intersect.ExtendBoth, pintC, new IntPtr(), new IntPtr());
                                        per.EndPoint = pintC[0];
                                        km = Ax.EndingStation + per.EndPoint.DistanceTo(Ax.EndPoint);
                                        offset = semn * per.Length;
                                        ed.WriteMessage("\nPunctul e in afara axului. Km: {0} / Offset: {1}", km, offset);
                                    }
                                }
                                obj.KM = km;
                                obj.Offset = offset;
                                //ObjectIdCollection IdTexte = EtSTO.GetTextComponentIds();
                                //foreach (ObjectId IdText in IdTexte)
                                //{
                                //    LabelStyleTextComponent Text = (LabelStyleTextComponent)trans.GetObject(IdText, OpenMode.ForRead);
                                //    if (Text.Name == "Pichet")
                                //    {
                                //        EtSTO.UpgradeOpen();
                                //        EtSTO.SetTextComponentOverride(IdText, km.ToString("0+000.00"));
                                //    }
                                //    ed.WriteMessage("\nText eticheta: {0}", Text.Name);
                                //}
                                obj.ObAtasat = EtSTO;
                                Etichete.Add(obj);
                            }
                        }

                        //Sortarea etichetelor dupa, parte si km
                        //NU MERGE SORTAREA ASA
                        ////Sortarea etichetelor dupa parte (stanga - dreapta)
                        //Etichete.Sort((E1, E2) => Math.Sign(E1.Offset).CompareTo(Math.Sign(E2.Offset)));
                        ////Sortarea etichetelor dupa pozitia kilometrica
                        //Etichete.Sort((E1, E2) =>
                        //{
                        //    if (Math.Sign(E1.Offset) == Math.Sign(E2.Offset)) return E1.KM.CompareTo(E2.KM);
                        //    else return 0;
                        //});
                        //ASA MERGE
                        //    List<Obiect3D<StationOffsetLabel>> EtStg = new List<Obiect3D<StationOffsetLabel>>();
                        //    EtStg.AddRange(from E in Etichete where E.Offset < 0 orderby E.KM select E);
                        //    List<Obiect3D<StationOffsetLabel>> EtMij = new List<Obiect3D<StationOffsetLabel>>();
                        //    EtMij.AddRange(from E in Etichete where E.Offset == 0 orderby E.KM select E);
                        //    List<Obiect3D<StationOffsetLabel>> EtDr = new List<Obiect3D<StationOffsetLabel>>();
                        //    EtDr.AddRange(from E in Etichete where E.Offset > 0 orderby E.KM select E);
                        //    Etichete.Clear();
                        //    Etichete.AddRange(EtStg);
                        //    Etichete.AddRange(EtMij);
                        //    Etichete.AddRange(EtDr);


                        //    foreach (Obiect3D<StationOffsetLabel> E in Etichete)
                        //    {
                        //        ed.WriteMessage("\n{0} -> Km: {1}, Off: {2} ({3}/{4}/{5})", Ax.Name, E.KM, E.Offset, EtStg.Count, EtMij.Count, EtDr.Count);
                        //    }

                        //    //Tiparire date santuri
                        //    if (Etichete.Count > 0) ed.WriteMessage("\nSanturi pe axul {0}", Ax.Name);
                        //    if (EtStg.Count % 2 != 0)
                        //    {
                        //        ed.WriteMessage("\nNumar impar de etichete pe stanga!");
                        //    }
                        //    if (EtMij.Count % 2 != 0)
                        //    {
                        //        ed.WriteMessage("\nNumar impar de etichete pe mijloc!");
                        //    }
                        //    if (EtDr.Count % 2 != 0)
                        //    {
                        //        ed.WriteMessage("\nNumar impar de etichete pe dreapta!");
                        //    }
                        //    for (int i = 0; i < Etichete.Count; i = i + 2)
                        //    {
                        //        string semn = "stanga";
                        //        if (Math.Sign(Etichete[i].Offset) == 0) semn = "mijloc";
                        //        if (Math.Sign(Etichete[i].Offset) > 0) semn = "dreapta";
                        //        if (Math.Sign(Etichete[i].Offset) == Math.Sign(Etichete[i + 1].Offset))
                        //        {
                        //            ed.WriteMessage("\n{0}. {1} {2} intre km {3} - km {4}  L = {5}",
                        //              (i + 2) / 2,
                        //              Etichete[i].ObAtasat.StyleName,
                        //              semn,
                        //              Etichete[i].KM,
                        //              Etichete[i + 1].KM,
                        //              Etichete[i + 1].KM - Etichete[i].KM);
                        //        }
                        //    }


                        //PROCESAREA ETICHETELOR
                        var sortare = from obj in Etichete
                                      orderby obj.ObAtasat.StyleName, Math.Sign(obj.Offset), obj.KM
                                      group obj by obj.ObAtasat.StyleName into Tipuri
                                      orderby Tipuri.Key
                                      from obj2 in Tipuri
                                      group obj2 by Math.Sign(obj2.Offset) into Tipuri2
                                      select Tipuri2;

                        foreach (var grup in sortare)
                        {
                            ed.WriteMessage("\n{0} -> {1}", Ax.Name, (Parte)Math.Sign(grup.First().Offset));
                            if (grup.Count() % 2 != 0)
                            {
                                ed.WriteMessage("\nNumar impar de etichete!");
                            }
                            //foreach (var elem in grup)
                            //{

                            //    ed.WriteMessage("\n{0} -> {1}: {2}", Ax.Name, elem.ObAtasat.StyleName, elem.KM);
                            //}
                            if (grup.Count() < 2) continue;
                            else for (int i = 0; i < grup.Count(); i = i + 2)
                                {
                                    double lungime = grup.ElementAt(i + 1).KM - grup.ElementAt(i).KM;
                                    bool dinPoly = LungimeSant(trans, ed, grup.ElementAt(i), grup.ElementAt(i + 1), ref lungime);
                                    ed.WriteMessage("\n{0} -> Km {1:f2} - Km {2:f2}, L = {3:f2}{4}",
                                        grup.ElementAt(i).ObAtasat.StyleName,
                                        grup.ElementAt(i).KM,
                                        grup.ElementAt(i + 1).KM,
                                        lungime,
                                        dinPoly ? "" : "(!din Km)");
                                }
                        }

                    }

                    trans.Commit();
                }
            }
        }

        [CommandMethod("poze")] //Comanda pentru vizualizarea fotografiilor corelata cu vederea longitudinala ARD
        public void poze()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            CivilDocument civDoc = CivilApplication.ActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            ObjectIdCollection idAxuri = civDoc.GetAlignmentIds();

            if (idAxuri.Count == 0)
            {
                ed.WriteMessage("\nDesenul nu contine aliniamente Civil 3D. Comanda intrerupta!");
                return;
            }

            Poze F = new Poze();
            Application.ShowModelessDialog(F);



        }


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
                        foreach (MethodInfo m in CMA) ed.WriteMessage("\n" + m.Name);
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
            }
        }

        [CommandMethod("GXD")]
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

        [CommandMethod("Ferestre")]
        public void ferestre()
        {

        }

        [CommandMethod("trimal")]
        public void trimal()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            CivilDocument civDoc = Autodesk.Civil.ApplicationServices.CivilApplication.ActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                ObjectIdCollection idAxuri = civDoc.GetAlignmentIds();

                if (idAxuri.Count == 0)
                {
                    ed.WriteMessage("\nDesenul nu contine aliniamente Civil 3D! Comanda intrerupta.");
                    return;
                }

                PromptEntityOptions PrEntOpt = new PromptEntityOptions("\nSelecteaza axul de taiat: ");
                PrEntOpt.AllowNone = false;
                PrEntOpt.SetRejectMessage("\nObiectul selectat nu este de un aliniament Civil 3D!");
                PrEntOpt.AddAllowedClass(typeof(Alignment), true);

                PromptEntityResult PrEntRes = ed.GetEntity(PrEntOpt);
                if (PrEntRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nSelectie gresita! Comanda intrerupta.");
                    return;
                }

                Alignment Ax = (Alignment)trans.GetObject(PrEntRes.ObjectId, OpenMode.ForWrite);

                double kmStart = 0;
                double kmCapat = 0;
                kmStart = ed.GetDouble("\nSpecifica km de start al zonei de taiat: ").Value;
                kmCapat = ed.GetDouble("\nSpecifica km de capat al zonei de taiat: ").Value;

                AlignmentEntityCollection AEC = Ax.Entities;
                List<AlignmentEntity> DeSters = new List<AlignmentEntity>();
                for (int i = 0; i < AEC.Count; i++)
                {
                    try
                    {
                        if (AEC[i][0].StartStation >= kmStart
                            && AEC[i][AEC[i].SubEntityCount - 1].EndStation <= kmCapat
                            )
                        {
                            DeSters.Add(AEC[i]);
                        }
                    }
                    catch { }
                    //for (int j = 0; j < AEC[i].SubEntityCount;  j++)
                    //{
                    //    bool taiat = false;
                    //    if (AEC[i][j].StartStation >= kmStart && AEC[i][j].EndStation <= kmCapat)
                    //    {
                            
                    //    }
                    //}
                }
                foreach(AlignmentEntity E in DeSters)
                {
                    Ax.Entities.Remove(E);
                }

                trans.Commit();
            }
        }

        [CommandMethod("stergax")]
        public void stergax()
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

            //Afisarea formularului de selectie a axurilor pentru notat
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                List<Alignment> AxuriCivil = new List<Alignment>();
                List<Alignment> AxuriARD = new List<Alignment>();
                List<Alignment> AxuriDeSters = new List<Alignment>();
                ListaAxuri formular = new ListaAxuri();
                formular.Text = "Sterge aliniamente Civil 3D";
                formular.btnExec.Text = "Sterge";
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
                    AxuriDeSters.AddRange(formular.checkedListBox1.CheckedItems.OfType<Alignment>());
                };


                System.Windows.Forms.DialogResult Rezultat = formular.ShowDialog();

                if (Rezultat != System.Windows.Forms.DialogResult.OK)
                {
                    ed.WriteMessage("\nComanda anulata!");
                }

                //Stergere Aliniamente
                foreach (Alignment Ax in AxuriDeSters)
                {
                    Ax.UpgradeOpen();
                    Ax.Erase();
                }

                trans.Commit();
            }
        }

        [CommandMethod("ltsf")] //Fixeaza scara tipului de linie a unui obiect
        public void ltsf()
        {
            Document acadDoc = Application.DocumentManager.MdiActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;
            ed.WriteMessage("\nComanda seteaza o scara a tipului de linie fixa pentru obiectele selectate.");

            //Afisarea formularului de selectie a axurilor pentru notat
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)trans.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                PromptSelectionOptions PSO = new PromptSelectionOptions();
                PSO.MessageForAdding = "\nSelecteaza obiectele: ";

                PromptSelectionResult PSR = ed.GetSelection(PSO);
                if (PSR.Status == PromptStatus.OK)
                {
                    PromptDoubleResult PDR = ed.GetDouble("\nSpecifica scara tipului de linie: ");
                    if (PDR.Status == PromptStatus.OK)
                    {
                        double scara = PDR.Value;
                        foreach (ObjectId IdObiect in PSR.Value.GetObjectIds())
                        {
                            Entity ent = trans.GetObject(IdObiect, OpenMode.ForWrite) as Entity;
                           if (ent != null)
                            {
                                ent.LinetypeScale = scara;
                            }
                        }
                    }
                }

                trans.Commit();
            }
        }

        public static string StratSant(Tipuri_Sant tip)
        {
            FieldInfo fi = tip.GetType().GetField(tip.ToString());
            System.ComponentModel.DescriptionAttribute[] att =
                (System.ComponentModel.DescriptionAttribute[])fi.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
            return (att.Length > 0) ? att[0].Description : "0";
        }

        public static string EtichetaSant(Tipuri_Sant tip)
        {
            return StratSant(tip).Length > 5 ? StratSant(tip).Substring(5) : tip.ToString();
        }

        public static bool LungimeSant(Transaction trans, Editor ed, Obiect3D<StationOffsetLabel> E1, Obiect3D<StationOffsetLabel> E2, ref double Lungime)
        {
            try
            {
                BlockTable bt = (BlockTable)trans.GetObject(ed.Document.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                LabelStyle LS = (LabelStyle)trans.GetObject(E1.ObAtasat.StyleId, OpenMode.ForRead);
                ed.WriteMessage("\nLabel layer: {0}", LS.Properties.Label.Layer.Value);
                TypedValue[] TVs = new TypedValue[]
                {
                    new TypedValue(0, "LWPOLYLINE"),
                    new TypedValue(8 , LS.Properties.Label.Layer.Value)
                };
                SelectionFilter SF = new SelectionFilter(TVs);
                double tol = 0.01;
                Point3d p1 = new Point3d(E1.X - tol, E1.Y - tol, 0);
                Point3d p2 = new Point3d(E1.X + tol, E1.Y + tol, 0);

                //Point3d p3 = new Point3d(E1.X + tol, E1.Y - tol, 0);
                //Point3d p4 = new Point3d(E1.X - tol, E1.Y + tol, 0);
                //Point3dCollection Pol1 = new Point3dCollection(new Point3d[] { p1, p3, p2, p4 });
                Line CW1 = new Line(p1, p2);
                ms.AppendEntity(CW1);
                trans.AddNewlyCreatedDBObject(CW1, true);
                PromptSelectionResult PSR1 = ed.SelectCrossingWindow(p2, p1, SF);
                //PromptSelectionResult PSR1 = ed.SelectCrossingPolygon(Pol1, SF);
                p1 = new Point3d(E2.X - tol, E2.Y - tol, 0);
                p2 = new Point3d(E2.X + tol, E2.Y + tol, 0);
                //p3 = new Point3d(E2.X + tol, E2.Y - tol, 0);
                //p4 = new Point3d(E2.X - tol, E2.Y + tol, 0);
                //Point3dCollection Pol2 = new Point3dCollection(new Point3d[] { p1, p3, p2, p4 });
                Line CW2 = new Line(p1, p2);
                ms.AppendEntity(CW2);
                trans.AddNewlyCreatedDBObject(CW2, true);
                PromptSelectionResult PSR2 = ed.SelectCrossingWindow(p2, p1, SF);
                //PromptSelectionResult PSR2 = ed.SelectCrossingPolygon(Pol2, SF);
                Punct3D Err = null;
                if (PSR1.Status != PromptStatus.OK)
                {
                    Err = E1;
                    throw new System.Exception(string.Format("\nEroare la selectie: {0}", LS.Properties.Label.Layer.Value));
                }
                else
                {
                    foreach (ObjectId oId in PSR1.Value.GetObjectIds())
                    {
                        Entity ent = (Entity)trans.GetObject(oId, OpenMode.ForWrite);
                        ent.Layer = "0";
                        ent.Color = Color.FromColorIndex(ColorMethod.ByAci, 241);
                    }
                }
                if (PSR2.Status != PromptStatus.OK)
                {
                    Err = E2;
                    throw new System.Exception(string.Format("\nEroare la selectie: {0}", LS.Properties.Label.Layer.Value));
                }
                else
                {
                    foreach (ObjectId oId in PSR2.Value.GetObjectIds())
                    {
                        Entity ent = (Entity)trans.GetObject(oId, OpenMode.ForWrite);
                        ent.Layer = "0";
                        ent.Color = Color.FromColorIndex(ColorMethod.ByAci, 241);
                    }
                }
                //if (PSR1.Status != PromptStatus.OK || PSR2.Status != PromptStatus.OK)
                //{
                //    throw new System.Exception(string.Format("\nEroare la selectie: {0}", LS.Properties.Label.Layer.Value));
                //}
                //var sel = PSR1.Value.GetObjectIds().Intersect(PSR2.Value.GetObjectIds());
                var sel = from soId in PSR1.Value.GetObjectIds()
                          where PSR2.Value.GetObjectIds().Contains(soId)
                          select soId;
                if (sel.Count() == 1)
                {
                    Polyline p = (Polyline)trans.GetObject(sel.First(), OpenMode.ForRead);
                    Lungime = Math.Abs(p.GetDistAtPoint(new Point3d(E1.X, E1.Y, 0)) - p.GetDistAtPoint(new Point3d(E2.X, E2.Y, 0)));
                    return true;
                }
                else
                {
                    throw new System.Exception(string.Format("\nObiecte gasite: {0}", sel.Count()));
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nEroare la calcularea lungimii santului prin punctul {0}", E1.toString(Punct3D.Format.ENZ, Punct3D.DelimitedBy.Comma, 3, false));
                ed.WriteMessage(ex.Message);
                Line E1E2 = new Line(new Point3d(E1.X, E1.Y, -999), new Point3d(E2.X, E2.Y, -999));
                //HostApplicationServices.WorkingDatabase = ed.Document.Database;
                BlockTable bt = (BlockTable)trans.GetObject(ed.Document.Database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                ms.AppendEntity(E1E2);
                trans.AddNewlyCreatedDBObject(E1E2, true);
                return false;
            }
        }

    }
}

