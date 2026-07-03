using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public static class DimensionService
    {
        public static ObjectId CreateAlignedDimension(Point3d firstPoint, Point3d secondPoint, Point3d dimensionPoint, ObjectId styleId, ObjectId layerId)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        AlignedDimension dim = new AlignedDimension(firstPoint, secondPoint, dimensionPoint, string.Empty, styleId);
                        if (layerId != ObjectId.Null) dim.LayerId = layerId;

                        dim.ColorIndex = 256;
                        dim.LinetypeId = db.ByLayerLinetype;
                        dim.LineWeight = LineWeight.ByLayer;

                        ObjectId dimensionId = btr.AppendEntity(dim);
                        tr.AddNewlyCreatedDBObject(dim, true);

                        tr.Commit();
                        return dimensionId;
                    }
                    catch (System.Exception ex)
                    {
                        ReportUtils.Error(doc.Editor, $"创建标注对象失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }
    }
}
