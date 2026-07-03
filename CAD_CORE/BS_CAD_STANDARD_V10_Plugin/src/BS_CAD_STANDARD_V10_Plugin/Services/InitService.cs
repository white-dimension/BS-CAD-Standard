using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class InitService
    {
        public static InitReport Initialize(StandardConfig mainConfig, DimStyleStandardConfig? dimConfig, bool createLayers, bool createText, bool createDim, bool createMLeader, bool setUnits, bool setDefaults)
        {
            InitReport report = new InitReport();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            ProcessLayers(mainConfig, createLayers, report);
            ProcessTextStyles(mainConfig, createText, report);
            ProcessDimStyles(dimConfig, createDim, report);
            ProcessMLeaderStyle(createMLeader, report);
            ProcessUnits(setUnits, report);
            ProcessCtbCheck(report);

            if (setDefaults)
            {
                ProcessSetDefaults(mainConfig, report);
            }

            return report;
        }

        private static void ProcessLayers(StandardConfig config, bool create, InitReport report)
        {
            var coreLayers = config.Layers.Where(l => l.Core).ToList();

            foreach (var cfg in coreLayers)
            {
                bool exists = LayerService.LayerExists(cfg.Name);

                if (exists)
                {
                    report.ExistingLayers++;
                    continue;
                }

                report.MissingCoreLayers++;

                if (!create)
                {
                    report.UserSkippedLayers++;
                    continue;
                }

                if (LayerService.CreateLayerFromConfig(cfg) != ObjectId.Null)
                    report.CreatedLayers++;
                else
                    report.FailedLayers++;
            }
        }

        private static void ProcessTextStyles(StandardConfig config, bool create, InitReport report)
        {
            List<string> targets = (config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0)
                                   ? config.Styles.TextStyles : StandardDefaults.TextStyles;

            foreach (string style in targets)
            {
                bool exists = AcadUtils.GetTextStyleId(style) != ObjectId.Null;

                if (exists)
                {
                    report.ExistingTextStyles++;

                    if (create && !TextStyleService.IsStandardTextStyle(style))
                    {
                        if (TextStyleService.EnsureStandardTextStyle(style, updateExisting: true) == ObjectId.Null)
                            report.FailedTextStyles++;
                    }

                    continue;
                }

                report.MissingTextStyles++;

                if (!create)
                {
                    report.SkippedTextStyles++;
                    continue;
                }

                if (TextStyleService.CreateStandardTextStyle(style) != ObjectId.Null)
                    report.CreatedTextStyles++;
                else
                    report.FailedTextStyles++;
            }
        }

        private static void ProcessDimStyles(DimStyleStandardConfig? config, bool create, InitReport report)
        {
            if (config == null) return;

            foreach (var cfg in config.DimStyles)
            {
                bool exists = DimStyleService.GetDimStyleId(cfg.Name) != ObjectId.Null;

                if (exists)
                {
                    report.ExistingDimStyles++;
                    continue;
                }

                report.MissingDimStyles++;

                if (!create)
                {
                    report.SkippedDimStyles++;
                    continue;
                }

                if (DimStyleService.CreateOrUpdateStandardDimStyle(cfg) != ObjectId.Null)
                    report.CreatedDimStyles++;
                else
                    report.FailedDimStyles++;
            }
        }

        private static void ProcessMLeaderStyle(bool create, InitReport report)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            bool exists = false;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
                exists = mlStyles.Contains(StandardDefaults.MLeaderStyleNote);
            }

            if (exists)
            {
                if (create && MLeaderStyleService.CreateStandardMLeaderStyle(StandardDefaults.MLeaderStyleNote) != ObjectId.Null)
                    report.MLeaderStatus = "已更新";
                else
                    report.MLeaderStatus = "已存在";
            }
            else if (create)
            {
                if (MLeaderStyleService.CreateStandardMLeaderStyle(StandardDefaults.MLeaderStyleNote) != ObjectId.Null)
                    report.MLeaderStatus = "已创建";
                else
                    report.MLeaderStatus = "创建失败";
            }
            else
            {
                report.MLeaderStatus = "跳过";
            }
        }

        private static void ProcessUnits(bool set, InitReport report)
        {
            int currentUnits = Convert.ToInt32(AcadUtils.SafeGetSystemVariable("INSUNITS") ?? 0);
            report.OldUnits = currentUnits.ToString();

            if (set && currentUnits != StandardDefaults.StandardUnits)
            {
                try
                {
                    Application.SetSystemVariable("INSUNITS", StandardDefaults.StandardUnits);
                    report.NewUnits = StandardDefaults.StandardUnits.ToString();
                }
                catch { report.NewUnits = currentUnits.ToString(); }
            }
            else
            {
                report.NewUnits = currentUnits.ToString();
            }

            int currentLuPrec = Convert.ToInt32(AcadUtils.SafeGetSystemVariable("LUPREC") ?? 0);
            int currentAuPrec = Convert.ToInt32(AcadUtils.SafeGetSystemVariable("AUPREC") ?? 0);
            report.OldPrecision = $"L:{currentLuPrec}, A:{currentAuPrec}";

            if (set)
            {
                try
                {
                    if (currentLuPrec != StandardDefaults.StandardLinearPrecision)
                        Application.SetSystemVariable("LUPREC", StandardDefaults.StandardLinearPrecision);
                    if (currentAuPrec != StandardDefaults.StandardAngularPrecision)
                        Application.SetSystemVariable("AUPREC", StandardDefaults.StandardAngularPrecision);

                    report.NewPrecision = $"L:{StandardDefaults.StandardLinearPrecision}, A:{StandardDefaults.StandardAngularPrecision}";
                }
                catch { report.NewPrecision = report.OldPrecision; }
            }
            else
            {
                report.NewPrecision = report.OldPrecision;
            }
        }

        private static void ProcessCtbCheck(InitReport report)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayoutManager lm = LayoutManager.Current;
                ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
                Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                report.CurrentCtb = layout.CurrentStyleSheet;
                report.CtbCorrect = string.Equals(report.CurrentCtb, StandardPaths.CtbFileName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void ProcessSetDefaults(StandardConfig config, InitReport report)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    string targetLayer = "01-AR-墙体";
                    if (!config.Layers.Any(l => l.Name == targetLayer))
                    {
                        targetLayer = config.Layers.FirstOrDefault(l => l.Core)?.Name ?? "0";
                    }

                    ObjectId layerId = LayerService.GetLayerId(targetLayer);
                    if (layerId != ObjectId.Null)
                    {
                        db.Clayer = layerId;
                        report.CurrentLayer = targetLayer;
                    }

                    ObjectId textId = AcadUtils.GetTextStyleId(StandardDefaults.DefaultTextStyle);
                    if (textId != ObjectId.Null)
                    {
                        db.Textstyle = textId;
                        report.CurrentTextStyle = StandardDefaults.DefaultTextStyle;
                    }

                    ObjectId dimId = DimStyleService.GetDimStyleId("BS_DIM_100");
                    if (dimId != ObjectId.Null)
                    {
                        db.Dimstyle = dimId;
                        DimStyleTableRecord dstr = (DimStyleTableRecord)tr.GetObject(dimId, OpenMode.ForRead);
                        db.SetDimstyleData(dstr);
                        report.CurrentDimStyle = "BS_DIM_100";
                    }

                    tr.Commit();
                }
            }
        }
    }
}
