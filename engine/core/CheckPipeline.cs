using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Core
{
    /// <summary>
    /// 检查管道 — BS_CHECK 逻辑的唯一来源。
    /// </summary>
    public class CheckPipeline
    {
        public CheckResult Run(StandardConfig config)
        {
            CheckResult result = new();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    PerformLayerChecks(db, tr, config, result);
                    CheckTextStyles(db, tr, config, result);
                    CheckDimStyles(db, tr, config, result);
                    CheckMLeaderStyle(db, tr, result);
                    result.CurrentUnits = Convert.ToInt32(AcadUtils.SafeGetSystemVariable("INSUNITS") ?? 0);
                    CheckPlotStyle(db, tr, result);

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    doc.Editor.WriteMessage($"\n[Exception] Check pipeline failed: {ex.Message}");
                }
            }

            return result;
        }

        private static void PerformLayerChecks(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            foreach (LayerConfig layerConfig in config.Layers)
            {
                if (!layerTable.Has(layerConfig.Name))
                {
                    result.MissingCoreLayers.Add(layerConfig.Name);
                    continue;
                }

                LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(layerTable[layerConfig.Name], OpenMode.ForRead);
                CheckSingleLayerProperties(layerRecord, layerConfig, result, tr);
            }

            foreach (ObjectId id in layerTable)
            {
                LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                string name = layerRecord.Name;

                if (IsStandardDefaultLayer(name)) continue;
                if (!standardLayerNames.Contains(name))
                {
                    result.ExtraLayers.Add(name);
                }
            }
        }

        private static void CheckSingleLayerProperties(LayerTableRecord layerRecord, LayerConfig config, CheckResult result, Transaction tr)
        {
            if (layerRecord.Color.ColorIndex != config.Color)
            {
                AddLayerDeviation(
                    result.ColorDeviations,
                    result,
                    $"{layerRecord.Name}: expected color {config.Color}, actual {layerRecord.Color.ColorIndex}");
            }

            LinetypeTableRecord linetypeRecord = (LinetypeTableRecord)tr.GetObject(layerRecord.LinetypeObjectId, OpenMode.ForRead);
            if (!string.Equals(linetypeRecord.Name, config.Linetype, StringComparison.OrdinalIgnoreCase))
            {
                AddLayerDeviation(
                    result.LinetypeDeviations,
                    result,
                    $"{layerRecord.Name}: expected linetype {config.Linetype}, actual {linetypeRecord.Name}");
            }

            double currentLineweight = AcadUtils.LineWeightToMm(layerRecord.LineWeight);
            if (Math.Abs(currentLineweight - config.Lineweight) > 0.001)
            {
                result.PropertyDeviations.Add($"{layerRecord.Name}: lineweight expected {config.Lineweight}mm, actual {(currentLineweight < 0 ? "default" : currentLineweight + "mm")}");
            }

            int currentTransparency = GetTransparencyPercent(layerRecord);
            if (currentTransparency != config.Transparency)
            {
                AddLayerDeviation(
                    result.TransparencyDeviations,
                    result,
                    $"{layerRecord.Name}: expected transparency {config.Transparency}, actual {currentTransparency}");
            }

            if (layerRecord.IsPlottable != config.Plot)
            {
                AddLayerDeviation(
                    result.PlotDeviations,
                    result,
                    $"{layerRecord.Name}: expected plot {config.Plot}, actual {layerRecord.IsPlottable}");
            }
        }

        private static void AddLayerDeviation(List<string> category, CheckResult result, string message)
        {
            category.Add(message);
            result.PropertyDeviations.Add(message);
        }

        private static int GetTransparencyPercent(LayerTableRecord layerRecord)
        {
            try
            {
                byte alpha = layerRecord.Transparency.Alpha;
                int percent = 100 - (int)Math.Round(alpha * 100.0 / 255.0);
                if (percent < 0) return 0;
                if (percent > 90) return 90;
                return percent;
            }
            catch
            {
                return 0;
            }
        }

        private static void CheckTextStyles(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            TextStyleTable textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            List<string> targets = config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0
                ? config.Styles.TextStyles
                : StandardDefaults.TextStyles;

            foreach (string style in targets)
            {
                if (!textStyleTable.Has(style))
                {
                    result.MissingTextStyles.Add(style);
                }
            }
        }

        private static void CheckDimStyles(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            DimStyleTable dimStyleTable = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            List<string> targets = config.Styles.DimStyles != null && config.Styles.DimStyles.Count > 0
                ? config.Styles.DimStyles
                : StandardDefaults.DimStyles;

            foreach (string style in targets)
            {
                if (!dimStyleTable.Has(style))
                {
                    result.MissingDimStyles.Add(style);
                }
            }
        }

        private static void CheckMLeaderStyle(Database db, Transaction tr, CheckResult result)
        {
            DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
            result.MLeaderStyleExists = mlStyles.Contains(StandardDefaults.MLeaderStyleNote);
        }

        private static void CheckPlotStyle(Database db, Transaction tr, CheckResult result)
        {
            LayoutManager layoutManager = LayoutManager.Current;
            ObjectId layoutId = layoutManager.GetLayoutId(layoutManager.CurrentLayout);
            Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

            result.CurrentLayoutName = layout.LayoutName;
            result.CurrentCtb = layout.CurrentStyleSheet;
        }

        private static bool IsStandardDefaultLayer(string name)
        {
            string[] defaults = { "0", "Defpoints" };
            return defaults.Any(d => string.Equals(d, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
