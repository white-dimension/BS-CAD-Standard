using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class CheckResult
    {
        public List<string> MissingCoreLayers { get; set; } = new();
        public List<string> PropertyDeviations { get; set; } = new();
        public List<string> ExtraLayers { get; set; } = new();
        public List<string> MissingTextStyles { get; set; } = new();
        public List<string> TextStyleFontDeviations { get; set; } = new();
        public List<string> MissingDimStyles { get; set; } = new();
        public bool MLeaderStyleExists { get; set; }
        public int CurrentUnits { get; set; }
        public string CurrentLayoutName { get; set; } = string.Empty;
        public string CurrentCtb { get; set; } = string.Empty;
    }

    public class CheckEngine
    {
        public static CheckResult RunFullCheck(StandardConfig config)
        {
            CheckResult result = new CheckResult();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 1 & 2 & 3. 图层检查
                    PerformLayerChecks(db, tr, config, result);

                    // 4. 文字样式
                    CheckTextStyles(db, tr, config, result);

                    // 5. 标注样式
                    CheckDimStyles(db, tr, config, result);

                    // 6. 多重引线样式
                    CheckMLeaderStyle(db, tr, result);

                    // 7. 单位
                    result.CurrentUnits = Convert.ToInt32(AcadUtils.SafeGetSystemVariable("INSUNITS") ?? 0);

                    // 8. 打印样式
                    CheckPlotStyle(db, tr, result);

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage($"\n[异常] 检查引擎运行出错: {ex.Message}");
                }
            }

            return result;
        }

        private static void PerformLayerChecks(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            HashSet<string> jsonLayerNames = new HashSet<string>(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            // 检查核心图层缺失和属性
            foreach (var layerConfig in config.Layers)
            {
                if (lt.Has(layerConfig.Name))
                {
                    LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[layerConfig.Name], OpenMode.ForRead);

                    // 如果是核心图层，检查属性
                    if (layerConfig.Core)
                    {
                        CheckSingleLayerProperties(ltr, layerConfig, result, tr);
                    }
                }
                else if (layerConfig.Core)
                {
                    result.MissingCoreLayers.Add(layerConfig.Name);
                }
            }

            // 检查额外非标准图层
            foreach (ObjectId id in lt)
            {
                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                string name = ltr.Name;

                if (IsStandardDefaultLayer(name)) continue;

                if (!jsonLayerNames.Contains(name))
                {
                    result.ExtraLayers.Add(name);
                }
            }
        }

        private static void CheckSingleLayerProperties(LayerTableRecord ltr, LayerConfig config, CheckResult result, Transaction tr)
        {
            // 1. 颜色
            if (ltr.Color.ColorIndex != config.Color)
            {
                result.PropertyDeviations.Add($"{ltr.Name}: Color 应为 {config.Color}, 实际为 {ltr.Color.ColorIndex}");
            }

            // 2. 线型
            LinetypeTableRecord ltr_lt = (LinetypeTableRecord)tr.GetObject(ltr.LinetypeObjectId, OpenMode.ForRead);
            if (!string.Equals(ltr_lt.Name, config.Linetype, StringComparison.OrdinalIgnoreCase))
            {
                result.PropertyDeviations.Add($"{ltr.Name}: Linetype 应为 {config.Linetype}, 实际为 {ltr_lt.Name}");
            }

            // 3. 线宽
            double currentLw = AcadUtils.LineWeightToMm(ltr.LineWeight);
            if (Math.Abs(currentLw - config.Lineweight) > 0.001)
            {
                result.PropertyDeviations.Add($"{ltr.Name}: Lineweight 应为 {config.Lineweight}mm, 实际为 {(currentLw < 0 ? "默认" : currentLw + "mm")}");
            }

            // 4. 打印
            if (ltr.IsPlottable != config.Plot)
            {
                result.PropertyDeviations.Add($"{ltr.Name}: Plot 应为 {config.Plot}, 实际为 {ltr.IsPlottable}");
            }
        }

        private static void CheckTextStyles(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            TextStyleTable st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            List<string> targets = (config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0)
                                   ? config.Styles.TextStyles
                                   : StandardDefaults.TextStyles;

            foreach (string style in targets)
            {
                if (!st.Has(style))
                {
                    result.MissingTextStyles.Add(style);
                    continue;
                }

                string? deviation = TextStyleService.GetFontDeviation(style);
                if (!string.IsNullOrWhiteSpace(deviation))
                {
                    result.TextStyleFontDeviations.Add(deviation);
                }
            }
        }

        private static void CheckDimStyles(Database db, Transaction tr, StandardConfig config, CheckResult result)
        {
            DimStyleTable dt = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            List<string> targets = (config.Styles.DimStyles != null && config.Styles.DimStyles.Count > 0)
                                   ? config.Styles.DimStyles
                                   : StandardDefaults.DimStyles;

            foreach (string style in targets)
            {
                if (!dt.Has(style))
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
            LayoutManager lm = LayoutManager.Current;
            ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
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
