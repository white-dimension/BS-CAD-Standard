using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public static class MLeaderEntityService
    {
        public static ObjectId CreateMLeaderNote(
            string noteText,
            Point3d arrowPoint,
            Point3d textPoint,
            ObjectId styleId,
            ObjectId layerId,
            ObjectId textStyleId)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    MText mtext = new MText
                    {
                        Contents = noteText,
                        TextHeight = MLeaderStyleService.StandardTextHeight,
                        TextStyleId = textStyleId,
                        Location = textPoint,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, 256)
                    };

                    MLeader leader = new MLeader();
                    leader.SetDatabaseDefaults();
                    leader.ContentType = ContentType.MTextContent;
                    leader.MText = mtext;
                    leader.MLeaderStyle = styleId;
                    leader.LayerId = layerId;
                    leader.Color = Color.FromColorIndex(ColorMethod.ByAci, 256);
                    leader.LinetypeId = db.ByLayerLinetype;
                    leader.LineWeight = LineWeight.ByLayer;
                    leader.LeaderLineType = LeaderType.StraightLeader;
                    leader.EnableLanding = true;
                    leader.EnableDogleg = true;
                    leader.DoglegLength = MLeaderStyleService.StandardLandingDistance;
                    leader.LandingGap = MLeaderStyleService.StandardLandingGap;
                    leader.ArrowSize = MLeaderStyleService.StandardArrowSize;
                    ObjectId arrowId = MLeaderStyleService.ResolveDotArrowId(db, tr);
                    if (arrowId != ObjectId.Null)
                    {
                        leader.ArrowSymbolId = arrowId;
                    }
                    leader.TextHeight = MLeaderStyleService.StandardTextHeight;
                    leader.TextStyleId = textStyleId;

                    int leaderIndex = leader.AddLeader();
                    int leaderLineIndex = leader.AddLeaderLine(leaderIndex);
                    leader.AddFirstVertex(leaderLineIndex, arrowPoint);
                    leader.AddLastVertex(leaderLineIndex, textPoint);

                    ObjectId leaderId = ms.AppendEntity(leader);
                    tr.AddNewlyCreatedDBObject(leader, true);

                    tr.Commit();
                    return leaderId;
                }
                catch (System.Exception ex)
                {
                    ReportUtils.Exception(doc.Editor, "创建多重引线失败", ex);
                    return ObjectId.Null;
                }
            }
        }
    }
}
