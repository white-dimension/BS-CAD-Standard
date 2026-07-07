using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;
using System;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public class DimStyleService
    {
        public static ObjectId GetDimStyleId(string styleName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DimStyleTable dt = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                if (dt.Has(styleName)) return dt[styleName];
                return ObjectId.Null;
            }
        }

        public static ObjectId CreateOrUpdateStandardDimStyle(DimStyleConfig cfg)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        DimStyleTable dt = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForWrite);
                        DimStyleTableRecord dstr;

                        bool isUpdate = false;
                        if (dt.Has(cfg.Name))
                        {
                            dstr = (DimStyleTableRecord)tr.GetObject(dt[cfg.Name], OpenMode.ForWrite);
                            isUpdate = true;
                        }
                        else
                        {
                            dstr = new DimStyleTableRecord();
                            dstr.Name = cfg.Name;
                        }

                        // 1. 获取或创建文字样式
                        string textStyleName = string.IsNullOrEmpty(cfg.TextStyle) ? StandardDefaults.DefaultTextStyle : cfg.TextStyle;
                        ObjectId textStyleId = AcadUtils.GetTextStyleId(textStyleName);
                        if (textStyleId == ObjectId.Null)
                        {
                            textStyleId = TextStyleService.CreateStandardTextStyle(textStyleName);
                        }

                        if (textStyleId == ObjectId.Null) return ObjectId.Null;

                        // --- 参数精准覆盖 ---

                        // [线]
                        dstr.Dimclrd = AcadUtils.ColorFromIndex(256); // ByLayer
                        dstr.Dimclre = AcadUtils.ColorFromIndex(256);
                        dstr.Dimclrt = AcadUtils.ColorFromIndex(256);
                        dstr.Dimltype = db.ByLayerLinetype;
                        dstr.Dimltex1 = db.ByLayerLinetype;
                        dstr.Dimltex2 = db.ByLayerLinetype;
                        dstr.Dimlwd = LineWeight.ByLayer;
                        dstr.Dimlwe = LineWeight.ByLayer;
                        dstr.Dimdle = 0;
                        dstr.Dimdli = cfg.BaselineSpacing;
                        dstr.Dimexe = cfg.ExtendBeyondDimLines;
                        dstr.Dimexo = cfg.OffsetFromOrigin;
                        dstr.DimfxlenOn = cfg.UseFixedExtensionLines;
                        dstr.Dimfxlen = cfg.ExtensionLineLength;

                        // [符号和箭头]
                        // 强制通过系统变量加载 "_ArchTick" (建筑标记) 块，并获取其 ID
                        if (cfg.ArrowType == "ArchitecturalTick")
                        {
                            SetArrowToArchTick(db, tr, dstr);
                        }

                        dstr.Dimasz = cfg.ArrowSize;
                        dstr.Dimcen = cfg.CenterMarkSize;
                        dstr.Dimarcsym = 0;

                        // [文字]
                        dstr.Dimtxsty = textStyleId;
                        dstr.Dimtxt = cfg.TextHeight;
                        dstr.Dimgap = cfg.TextOffset;
                        dstr.Dimtad = 1;
                        dstr.Dimjust = 0;
                        dstr.Dimtih = false;
                        dstr.Dimtoh = false;

                        // [调整]
                        dstr.Dimatfit = 3;
                        dstr.Dimtmove = 2; // 尺寸线上方，不带引线

                        // [主单位]
                        dstr.Dimlunit = 2;
                        dstr.Dimdec = cfg.DecimalPrecision;
                        dstr.Dimdsep = '.';
                        dstr.Dimzin = cfg.TrailingZeroSuppression ? 8 : 0;
                        dstr.Dimadec = 0;

                        // [比例]
                        dstr.Dimscale = cfg.DimScale;

                        if (!isUpdate)
                        {
                            dt.Add(dstr);
                            tr.AddNewlyCreatedDBObject(dstr, true);
                        }

                        tr.Commit();
                        return dstr.ObjectId;
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage($"\n[错误] 标注样式标准同步失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }

        private static void SetArrowToArchTick(Database db, Transaction tr, DimStyleTableRecord dstr)
        {
            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                string arrowName = "_ArchTick";
                ObjectId arrowId = ObjectId.Null;

                if (bt.Has(arrowName))
                {
                    arrowId = bt[arrowName];
                    EnsureStandardArchTickBlock(tr, arrowId);
                }
                else
                {
                    arrowId = CreateArchTickBlock(bt, tr, arrowName);
                }

                if (arrowId != ObjectId.Null)
                {
                    dstr.Dimblk = arrowId;
                    dstr.Dimblk1 = arrowId;
                    dstr.Dimblk2 = arrowId;
                }
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\n[警告] 建筑标记箭头设置失败: {ex.Message}");
            }
        }

        private static ObjectId CreateArchTickBlock(BlockTable bt, Transaction tr, string arrowName)
        {
            bt.UpgradeOpen();

            BlockTableRecord tickBlock = new BlockTableRecord
            {
                Name = arrowName,
                Origin = Point3d.Origin
            };

            ObjectId arrowId = bt.Add(tickBlock);
            tr.AddNewlyCreatedDBObject(tickBlock, true);

            AddArchTickGeometry(tickBlock, tr);

            return arrowId;
        }

        private static void EnsureStandardArchTickBlock(Transaction tr, ObjectId arrowId)
        {
            BlockTableRecord tickBlock = (BlockTableRecord)tr.GetObject(arrowId, OpenMode.ForWrite);

            foreach (ObjectId entityId in tickBlock)
            {
                Entity entity = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);
                entity.Erase();
            }

            AddArchTickGeometry(tickBlock, tr);
        }

        private static void AddArchTickGeometry(BlockTableRecord tickBlock, Transaction tr)
        {
            Polyline tickLine = new Polyline(2)
            {
                Layer = "0",
                ColorIndex = 0,
                Linetype = "ByBlock",
                LineWeight = LineWeight.ByLayer,
                ConstantWidth = 0.15
            };
            tickLine.AddVertexAt(0, new Point2d(-0.5, -0.5), 0, 0.15, 0.15);
            tickLine.AddVertexAt(1, new Point2d(0.5, 0.5), 0, 0.15, 0.15);

            tickBlock.AppendEntity(tickLine);
            tr.AddNewlyCreatedDBObject(tickLine, true);
        }

        public static bool SetCurrentDimStyle(ObjectId styleId)
        {
            if (styleId == ObjectId.Null) return false;
            Database db = HostApplicationServices.WorkingDatabase;
            try
            {
                db.Dimstyle = styleId;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DimStyleTableRecord dstr = (DimStyleTableRecord)tr.GetObject(styleId, OpenMode.ForRead);
                    db.SetDimstyleData(dstr);
                    tr.Commit();
                }
                return true;
            }
            catch { return false; }
        }
    }
}
