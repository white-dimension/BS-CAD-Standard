using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_V10_Plugin.Core;
using System;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class TextService
    {
        public static ObjectId CreateStandardDBText(string content, Point3d position, double height, ObjectId layerId, ObjectId styleId)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        DBText text = new DBText();
                        text.TextString = content;
                        text.Position = position;
                        text.Height = height;

                        if (styleId != ObjectId.Null)
                            text.TextStyleId = styleId;

                        if (layerId != ObjectId.Null)
                            text.LayerId = layerId;

                        // 属性默认 ByLayer
                        text.ColorIndex = 256; // ByLayer
                        text.LinetypeId = db.ByLayerLinetype;
                        text.LineWeight = LineWeight.ByLayer;

                        ObjectId textId = btr.AppendEntity(text);
                        tr.AddNewlyCreatedDBObject(text, true);

                        tr.Commit();
                        return textId;
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage($"\n[错误] 创建文字失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }
    }
}
