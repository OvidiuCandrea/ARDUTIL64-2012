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
                string caleNoua = caleDocAcad.Replace(caleDocAcad.Substring(caleDocAcad.Length - 4), indicativ);
                Database dbNoua = new Database(true, false);
                ed.WriteMessage("\nFisierul Rezultate este " + caleNoua);

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
                    ObjectIdCollection idObiecte = new ObjectIdCollection();
                    List<Entity> obiecte = new List<Entity>();
                    foreach (ObjectId idObiect in btr)
                    {
                        idObiecte.Add(idObiect);

                    }

                    //Procesarea obiectelor din plansa curenta
                    Database dbCur = new Database(true, true);
                    ObjectIdCollection objCur = new ObjectIdCollection();
                    //db.Wblock(dbCur, obiecte, Point3d.Origin, DuplicateRecordCloning.Ignore);
                    
                    using (Transaction trCur = dbCur.TransactionManager.StartTransaction())
                    {
                        IdMapping iMap = new IdMapping();
                        BlockTable btCur = (BlockTable)trCur.GetObject(dbCur.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btrCur = (BlockTableRecord)trCur.GetObject(btCur[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                        dbCur.WblockCloneObjects(idObiecte, btCur[BlockTableRecord.ModelSpace], iMap, DuplicateRecordCloning.Ignore, false);
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
                    //dbCur.Wblock(dbNoua, objCur, Point3d.Origin, DuplicateRecordCloning.MangleName);
                    //using (Transaction trNoua = dbNoua.TransactionManager.StartTransaction())
                    //{
                    //    BlockTable btNou = (BlockTable)trNoua.GetObject(db.BlockTableId, OpenMode.ForWrite);
                    //    IdMapping iMap2 = new IdMapping();
                    //    dbNoua.WblockCloneObjects(objCur, btNou[BlockTableRecord.ModelSpace], iMap2, DuplicateRecordCloning.Ignore, false);
                    //    trNoua.Commit();
                    //}

                    //Actualizeaza deltaX
                    deltaX += scara * 500;
                }

                //Crearea Desenului Nou
                dbNoua.SaveAs(caleNoua, DwgVersion.Newest);
                dbNoua.Dispose();
                
            }


        }
    }
}

