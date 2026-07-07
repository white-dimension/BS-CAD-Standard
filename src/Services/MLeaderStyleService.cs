using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_1_0_Plugin.Utils;
using System;
using System.Reflection;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public static class MLeaderStyleService
    {
        public const string StandardStyleName = "BS_MLEADER_NOTE";
        public const string StandardTextStyleName = "BS_TEXT_CN";

        public const double StandardTextHeight = 2.5;
        public const double StandardLandingDistance = 10.0;
        public const double StandardLandingGap = 1.0;
        public const double StandardArrowSize = 5.0;
        public const double StandardScale = 100.0;

        public static ObjectId GetMLeaderStyleId(string styleName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
                return mlStyles.Contains(styleName) ? mlStyles.GetAt(styleName) : ObjectId.Null;
            }
        }

        public static ObjectId CreateBasicStandardStyle(ObjectId textStyleId)
        {
            return CreateStandardMLeaderStyle(StandardStyleName);
        }

        public static ObjectId CreateStandardMLeaderStyle(string styleName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);

                        MLeaderStyle mls;
                        bool isNew = false;
                        if (mlStyles.Contains(styleName))
                        {
                            mls = (MLeaderStyle)tr.GetObject(mlStyles.GetAt(styleName), OpenMode.ForWrite);
                        }
                        else
                        {
                            mls = new MLeaderStyle();
                            isNew = true;
                        }

                        // --- 1. 引线格式 ---
                        mls.LeaderLineType = LeaderType.StraightLeader;
                        mls.LeaderLineColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 256);

                        // 显式修正：线型设为 ByLayer (截图显示 ByBlock 是错误的)
                        mls.LeaderLineTypeId = db.ByLayerLinetype;
                        mls.LeaderLineWeight = LineWeight.ByLayer;

                        // 显式修正：打断大小设为 0 (截图显示 0.125 是错误的)
                        try { mls.BreakSize = 0.0; } catch { }

                        // 显式修正：箭头设为“小点” (截图显示实心闭合是错误的)
                        SetArrowToDot(db, tr, mls);
                        mls.ArrowSize = StandardArrowSize;

                        // --- 2. 引线结构 ---
                        SetPropertyValue(mls, "MaxPoints", 2);
                        SetPropertyValue(mls, "MaxLeaderPoints", 2);

                        mls.EnableLanding = true;
                        mls.EnableDogleg = true;
                        mls.DoglegLength = StandardLandingDistance;

                        mls.Annotative = AnnotativeStates.False;
                        mls.Scale = StandardScale;

                        // --- 3. 内容 ---
                        mls.ContentType = ContentType.MTextContent;

                        ObjectId tsId = TextStyleService.CreateStandardTextStyle(StandardTextStyleName);
                        if (tsId != ObjectId.Null)
                        {
                            mls.TextStyleId = tsId;
                        }
                        mls.TextAngleType = TextAngleType.HorizontalAngle;
                        mls.TextColor = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 256);
                        mls.TextHeight = StandardTextHeight;

                        // 显式修正：连接位置设为“第一行中间”
                        // 修复右侧加下划线的问题
                        SetAttachmentTypeRobust(mls, "MiddleOfTopLine");
                        SetAttachmentTypeRobust(mls, "AttachmentMiddleOfTop");
                        SetAttachmentTypeRobust(mls, "MiddleOfTop");

                        mls.LandingGap = StandardLandingGap;
                        mls.ExtendLeaderToText = true;

                        if (isNew)
                        {
                            mlStyles.UpgradeOpen();
                            mlStyles.SetAt(styleName, mls);
                            tr.AddNewlyCreatedDBObject(mls, true);
                        }

                        tr.Commit();
                        return mls.ObjectId;
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage($"\n[错误] 更新多重引线样式 {styleName} 失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }

        private static void SetAttachmentTypeRobust(MLeaderStyle mls, string typeName)
        {
            try
            {
                // 1. 设置主属性
                SetEnumProperty(mls, "TextAttachmentType", typeName);

                // 2. 使用反射动态调用 SetTextAttachmentType，不直接引用枚举名
                MethodInfo[] methods = mls.GetType().GetMethods();
                foreach (var m in methods)
                {
                    if (m.Name == "SetTextAttachmentType" && m.GetParameters().Length == 2)
                    {
                        var parms = m.GetParameters();
                        Type attachmentType = parms[0].ParameterType;
                        Type directionType = parms[1].ParameterType;

                        if (attachmentType.IsEnum && directionType.IsEnum)
                        {
                            try
                            {
                                object attrVal = Enum.Parse(attachmentType, typeName, true);
                                object leftDir = Enum.Parse(directionType, "LeftLeader", true);
                                object rightDir = Enum.Parse(directionType, "RightLeader", true);

                                m.Invoke(mls, new[] { attrVal, leftDir });
                                m.Invoke(mls, new[] { attrVal, rightDir });
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void SetPropertyValue(object target, string propName, object value)
        {
            try
            {
                PropertyInfo? prop = target.GetType().GetProperty(propName);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value);
                }
            }
            catch { }
        }

        private static void SetEnumProperty(object target, string propName, string enumValueName)
        {
            try
            {
                PropertyInfo? prop = target.GetType().GetProperty(propName);
                if (prop != null && prop.CanWrite)
                {
                    Type enumType = prop.PropertyType;
                    if (enumType.IsEnum)
                    {
                        object val = Enum.Parse(enumType, enumValueName, true);
                        prop.SetValue(target, val);
                    }
                }
            }
            catch { }
        }

        public static ObjectId ResolveDotArrowId(Database db, Transaction tr)
        {
            Editor? ed = Application.DocumentManager.MdiActiveDocument?.Editor;

            // AutoCAD 2027 对内置“小点”箭头的 DIMBLK / DIMBLKID 解析不稳定，
            // 会产生大量 eInvalidInput 警告。BS 标准插件优先使用自定义箭头块，
            // 确保跨电脑、空白图纸、标准 DWT 都能得到稳定的点状箭头效果。
            ObjectId arrowId = EnsureCustomDotArrowBlock(db, tr, ed);
            if (arrowId != ObjectId.Null)
            {
                return arrowId;
            }

            // 如果自定义块创建失败，再尝试查找图纸中已有的点状箭头块。
            arrowId = ResolveExistingArrowBlock(db, tr, ed);
            if (arrowId != ObjectId.Null)
            {
                return arrowId;
            }

            ReportUtils.Warning(ed!, "未能创建或解析小点箭头，将使用 AutoCAD 默认箭头。请在 MLEADERSTYLE 中手动确认。");
            return ObjectId.Null;
        }

        private static ObjectId ResolveExistingArrowBlock(Database db, Transaction tr, Editor? ed)
        {
            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (string arrowName in GetDotArrowCandidateNames(includeCustom: true))
                {
                    if (bt.Has(arrowName))
                    {
                        return bt[arrowName];
                    }
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Warning(ed!, $"查找已有小点箭头块失败: {ex.Message}");
            }

            return ObjectId.Null;
        }

        private static ObjectId ResolveArrowIdByDimblk(Editor? ed)
        {
            object? oldDimblk = null;

            try
            {
                oldDimblk = Application.GetSystemVariable("DIMBLK");
            }
            catch (System.Exception ex)
            {
                ReportUtils.Warning(ed!, $"读取当前 DIMBLK 失败: {ex.Message}");
            }

            foreach (string arrowName in GetDotArrowCandidateNames(includeCustom: false))
            {
                try
                {
                    Application.SetSystemVariable("DIMBLK", arrowName);
                    object dimblkIdValue = Application.GetSystemVariable("DIMBLKID");
                    ObjectId arrowId = ToObjectId(dimblkIdValue);
                    if (arrowId != ObjectId.Null)
                    {
                        RestoreDimblk(oldDimblk, ed);
                        return arrowId;
                    }

                    ReportUtils.Warning(ed!, $"DIMBLK={arrowName} 未返回有效箭头 ObjectId。");
                }
                catch (System.Exception ex)
                {
                    ReportUtils.Warning(ed!, $"通过 DIMBLK 解析箭头 {arrowName} 失败: {ex.Message}");
                }
            }

            RestoreDimblk(oldDimblk, ed);
            return ObjectId.Null;
        }

        private static ObjectId EnsureCustomDotArrowBlock(Database db, Transaction tr, Editor? ed)
        {
            const string customBlockName = "BS_ARROW_DOT";

            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(customBlockName))
                {
                    return bt[customBlockName];
                }

                bt.UpgradeOpen();

                BlockTableRecord btr = new BlockTableRecord
                {
                    Name = customBlockName,
                    Origin = Point3d.Origin
                };

                ObjectId blockId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                // AutoCAD 箭头块会按 ArrowSize 缩放。这里用较小的实心菱形近似“小点”，
                // 视觉上比默认“实心闭合”箭头更接近 BS_MLEADER_NOTE 的点状箭头。
                Solid dot = new Solid(
                    new Point3d(0.0, 0.45, 0.0),
                    new Point3d(0.45, 0.0, 0.0),
                    new Point3d(0.0, -0.45, 0.0),
                    new Point3d(-0.45, 0.0, 0.0));
                dot.SetDatabaseDefaults();
                dot.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 0);
                btr.AppendEntity(dot);
                tr.AddNewlyCreatedDBObject(dot, true);

                Circle outline = new Circle(Point3d.Origin, Vector3d.ZAxis, 0.45);
                outline.SetDatabaseDefaults();
                outline.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 0);
                btr.AppendEntity(outline);
                tr.AddNewlyCreatedDBObject(outline, true);

                return blockId;
            }
            catch (System.Exception ex)
            {
                ReportUtils.Warning(ed!, $"创建自定义小点箭头块 BS_ARROW_DOT 失败: {ex.Message}");
                return ObjectId.Null;
            }
        }

        private static void RestoreDimblk(object? oldDimblk, Editor? ed)
        {
            if (oldDimblk == null)
            {
                return;
            }

            try
            {
                Application.SetSystemVariable("DIMBLK", oldDimblk);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Warning(ed!, $"恢复 DIMBLK 失败: {ex.Message}");
            }
        }

        private static ObjectId ToObjectId(object value)
        {
            if (value is ObjectId objectId)
            {
                return objectId;
            }

            if (value is IntPtr ptr && ptr != IntPtr.Zero)
            {
                try
                {
                    return new ObjectId(ptr);
                }
                catch
                {
                    return ObjectId.Null;
                }
            }

            return ObjectId.Null;
        }

        private static string[] GetDotArrowCandidateNames(bool includeCustom)
        {
            string[] builtInNames =
            {
                "_DOTSMALL",
                "_DotSmall",
                "DOTSMALL",
                "DotSmall",
                "_DOT",
                "_Dot",
                "DOT",
                "Dot"
            };

            if (!includeCustom)
            {
                return builtInNames;
            }

            string[] names = new string[builtInNames.Length + 1];
            names[0] = "BS_ARROW_DOT";
            Array.Copy(builtInNames, 0, names, 1, builtInNames.Length);
            return names;
        }

        private static void SetArrowToDot(Database db, Transaction tr, MLeaderStyle mls)
        {
            Editor? ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ObjectId arrowId = ResolveDotArrowId(db, tr);
            if (arrowId != ObjectId.Null)
            {
                mls.ArrowSymbolId = arrowId;
            }
            else
            {
                ReportUtils.Warning(ed!, "BS_MLEADER_NOTE ArrowSymbolId = ObjectId.Null，小点箭头设置失败。");
            }
        }
    }
}
