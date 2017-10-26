using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;

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
            string caleNoua = string.Empty;

            //Selectarea planselor din desen
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                DBDictionary planse = (DBDictionary)trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                Lista_Planse formular = new Lista_Planse();

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
                caleNoua = caleDocAcad.Replace(caleDocAcad.Substring(caleDocAcad.Length - 4), indicativ);
                Database dbNoua = new Database(true, false);
                

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
                        trCur.Commit();
                        //dbCur.SaveAs(caleNoua, DwgVersion.Newest);
                    }

                    //Copierea obiectelor procesate in plansa finala
                    dbCur.Wblock(dbNoua, objCur, Point3d.Origin, DuplicateRecordCloning.MangleName);

                    //Actualizeaza deltaX
                    deltaX += scara * 500;
                }

                //Crearea Desenului Nou
                dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                dbNoua.Dispose();
                ed.WriteMessage("\nFisierul Rezultat este " + caleNoua);
            }

            using (Database dbNoua = new Database(false, true)
            {

                //Curatarea bazei de data de straturi si stiluri duplicat
                using (Transaction trNoua = dbNoua.TransactionManager.StartTransaction())
                {
                    BlockTable btNou = (BlockTable)trNoua.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord msNou = (BlockTableRecord)trNoua.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    LayerTable lt = (LayerTable)trNoua.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    LinetypeTable ltt = (LinetypeTable)trNoua.GetObject(db.LinetypeTableId, OpenMode.ForWrite);
                    DimStyleTable dst = (DimStyleTable)trNoua.GetObject(db.DimStyleTableId, OpenMode.ForWrite);

                    //Corectarea obiectelor
                    foreach (ObjectId objId in msNou)
                    {
                        Entity ent = trNoua.GetObject(objId, OpenMode.ForRead) as Entity;
                        
                        //Straturi
                        try
                        {
                            string numeStrat = ent.Layer;
                        }
                        catch
                        {
                            ed.WriteMessage("\nNu poti citi numele stratului!");
                        }
                        if (ent.Layer.StartsWith("$"))
                        {
                            try
                            {
                                string numeStratBun = ent.Layer.Substring(ent.Layer.Substring(0, ent.Layer.Length - 2).LastIndexOf('$') + 1);
                                ed.WriteMessage("\n{0} ---> {1}", ent.Layer, numeStratBun);
                                ent.UpgradeOpen();
                                if (lt.Has(numeStratBun)) ent.Layer = numeStratBun;
                                else if (numeStratBun.Length != 0)
                                {
                                    LayerTableRecord stratBun = new LayerTableRecord();
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
                                LinetypeTableRecord linieBuna = new LinetypeTableRecord();
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
                                DimStyleTableRecord cotareBuna = new DimStyleTableRecord();
                                cotareBuna.Name = numeCotareBun;
                                dst.Add(cotareBuna);
                                trans.AddNewlyCreatedDBObject(cotareBuna, true);
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

                ed.WriteMessage("\nS-a curatat desenul" + caleNoua);
            }
            }

        }
    }
}

