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
    public class TemplateCheckReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public int OkCount { get; set; }
        public int WarnCount { get; set; }
        public int InfoCount { get; set; }
        public int ErrorCount { get; set; }

        public List<string> Lines { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public static class TemplateCheckService
    {
        public static TemplateCheckReport RunCheck(StandardConfig config)
        {
            TemplateCheckReport report = new();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // ============ 1. 单位检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[1] 单位检查");
                    CheckUnits(report);

                    // ============ 2. 图层检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[2] 图层检查");
                    CheckLayers(tr, db, config, standardLayerNames, report);

                    // ============ 3. CTB / 打印样式检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[3] CTB / 打印样式检查");
                    CheckPlotStyle(db, tr, report);

                    // ============ 4. 文字样式检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[4] 文字样式检查");
                    CheckTextStyles(tr, db, config, report);

                    // ============ 5. 标注样式检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[5] 标注样式检查");
                    CheckDimStyles(tr, db, config, report);

                    // ============ 6. 布局检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[6] 布局检查");
                    CheckLayouts(db, tr, report);

                    // ============ 7. 图框 / 视口检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[7] 图框 / 视口检查");
                    CheckFrameAndViewport(tr, db, standardLayerNames, report);

                    // ============ 8. 默认图层检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[8] 默认图层检查");
                    CheckDefaultLayers(tr, db, report);

                    // ============ 9. 非标准图层检查 ============
                    report.Lines.Add("");
                    report.Lines.Add("[9] 非标准图层检查");
                    CheckNonStandardLayers(tr, db, standardLayerNames, report);

                    // Suggestions
                    BuildSuggestions(report);

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_TEMPLATE_CHECK failed: {ex.Message}");
                }
            }

            return report;
        }

        private static void CheckUnits(TemplateCheckReport report)
        {
            string InsUnitsStr = SafeSysVar("INSUNITS");
            string LunitsStr = SafeSysVar("LUNITS");
            string LuprecStr = SafeSysVar("LUPREC");
            string AunitsStr = SafeSysVar("AUNITS");
            string AuprecStr = SafeSysVar("AUPREC");

            if (InsUnitsStr == "4")
                AddOk(report, $"INSUNITS = {InsUnitsStr}，单位为毫米");
            else
                AddWarn(report, $"INSUNITS = {InsUnitsStr}，建议为 4（毫米）");

            if (LunitsStr == "2")
                AddOk(report, $"LUNITS = {LunitsStr}，十进制");
            else
                AddWarn(report, $"LUNITS = {LunitsStr}，建议为 2（十进制）");

            AddInfo(report, $"LUPREC = {LuprecStr}");
            AddInfo(report, $"AUNITS = {AunitsStr}, AUPREC = {AuprecStr}");
        }

        private static string SafeSysVar(string name)
        {
            object? val = AcadUtils.SafeGetSystemVariable(name);
            if (val == null) return "?";
            return val.ToString() ?? "?";
        }

        private static void CheckLayers(Transaction tr, Database db, StandardConfig config,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            int totalAll = 0;
            int checkableCount = 0;
            int existingStandard = 0;
            List<string> missing = new();

            foreach (ObjectId id in layerTable)
            {
                totalAll++;
                LayerTableRecord lr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!LayerPropertyUtils.IsExcludedLayer(lr.Name))
                    checkableCount++;
            }

            foreach (LayerConfig lc in config.Layers)
            {
                if (layerTable.Has(lc.Name))
                    existingStandard++;
                else
                    missing.Add(lc.Name);
            }

            AddInfo(report, $"当前图层总数（含默认图层）：{totalAll}");
            AddInfo(report, $"参与标准检查图层数（不含 0 / Defpoints）：{checkableCount}");
            AddInfo(report, $"标准图层数量：{config.Layers.Count}");

            if (existingStandard == config.Layers.Count)
                AddOk(report, $"标准图层完整：{existingStandard} / {config.Layers.Count}");
            else
                AddWarn(report, $"已有标准图层：{existingStandard} / {config.Layers.Count}，缺失 {missing.Count} 个");

            if (missing.Count > 0)
            {
                foreach (string m in missing.Take(10))
                    report.Lines.Add($"  - {m}");
                if (missing.Count > 10)
                    report.Lines.Add($"  ... 及其他 {missing.Count - 10} 个");
            }
        }

        private static void CheckPlotStyle(Database db, Transaction tr, TemplateCheckReport report)
        {
            object? pstyleMode = AcadUtils.SafeGetSystemVariable("PSTYLEMODE");
            AddInfo(report, $"PSTYLEMODE = {pstyleMode ?? "?"}");

            try
            {
                LayoutManager lm = LayoutManager.Current;
                ObjectId layoutId = lm.GetLayoutId(lm.CurrentLayout);
                Layout layout = (Layout)tr.GetObject(layoutId, OpenMode.ForRead);

                string styleSheet = layout.CurrentStyleSheet ?? string.Empty;
                string canonicalMedia = "";
                try { canonicalMedia = layout.CanonicalMediaName ?? ""; } catch { }

                if (string.IsNullOrEmpty(styleSheet))
                    AddWarn(report, "当前布局未设置 CTB / STB");
                else if (!styleSheet.Contains("BS_", StringComparison.OrdinalIgnoreCase))
                    AddWarn(report, $"当前布局打印样式不是 BS 标准 CTB：{styleSheet}");
                else
                    AddOk(report, $"当前布局打印样式：{styleSheet}");
            }
            catch
            {
                AddInfo(report, "无法读取当前布局打印样式");
            }
        }

        private static void CheckTextStyles(Transaction tr, Database db, StandardConfig config, TemplateCheckReport report)
        {
            TextStyleTable tst = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            int found = 0;

            var targets = config.Styles.TextStyles.Count > 0
                ? config.Styles.TextStyles
                : StandardDefaults.TextStyles;

            foreach (string style in targets)
            {
                if (tst.Has(style))
                {
                    AddOk(report, $"文字样式 {style} 已存在");
                    found++;
                }
                else
                {
                    AddWarn(report, $"缺失文字样式 {style}");
                }
            }

            if (found == 0)
                AddWarn(report, "未发现任何 BS 标准文字样式");
        }

        private static void CheckDimStyles(Transaction tr, Database db, StandardConfig config, TemplateCheckReport report)
        {
            DimStyleTable dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
            int found = 0;

            var targets = config.Styles.DimStyles.Count > 0
                ? config.Styles.DimStyles
                : StandardDefaults.DimStyles;

            foreach (string style in targets)
            {
                if (dst.Has(style))
                {
                    AddOk(report, $"标注样式 {style} 已存在");
                    found++;
                }
                else
                {
                    AddWarn(report, $"缺失标注样式 {style}");
                }
            }

            if (found == 0)
                AddWarn(report, "未发现任何 BS 标准标注样式");
        }

        private static void CheckLayouts(Database db, Transaction tr, TemplateCheckReport report)
        {
            DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
            int count = layoutDict.Count;

            AddInfo(report, $"当前布局数量：{count}");

            if (count <= 1)
                AddWarn(report, "当前图纸没有布局空间（仅有 Model）");
            else
                AddOk(report, $"当前图纸已存在布局空间：{count - 1} 个（含 Model）");

            foreach (var entry in layoutDict)
            {
                try
                {
                    Layout layout = (Layout)tr.GetObject(entry.Value, OpenMode.ForRead);
                    if (!string.Equals(layout.LayoutName, "Model", StringComparison.OrdinalIgnoreCase))
                    {
                        string ps = string.IsNullOrEmpty(layout.CurrentStyleSheet) ? "未设置" : layout.CurrentStyleSheet;
                        AddInfo(report, $"  布局 \"{layout.LayoutName}\"：{ps}");
                    }
                }
                catch { }
            }
        }

        private static void CheckFrameAndViewport(Transaction tr, Database db,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            // Candidate layers from standard config
            var frameKeywords = new[] { "图框", "FR", "FRAME", "标题", "TITLE", "SHEET" };
            var vpKeywords = new[] { "视口", "VP", "VPORT", "VIEWPORT" };

            string? frameLayerInDwg = null;
            string? vpLayerInDwg = null;

            foreach (string name in standardLayerNames)
            {
                if (!layerTable.Has(name)) continue;

                if (frameLayerInDwg == null && frameKeywords.Any(kw =>
                    name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    frameLayerInDwg = name;
                }

                if (vpLayerInDwg == null && vpKeywords.Any(kw =>
                    name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    vpLayerInDwg = name;
                }

                if (frameLayerInDwg != null && vpLayerInDwg != null) break;
            }

            if (frameLayerInDwg != null)
                AddOk(report, $"图框相关图层已存在：{frameLayerInDwg}");
            else
                AddWarn(report, "当前 DWG 未发现图框相关图层");

            if (vpLayerInDwg != null)
                AddOk(report, $"视口图层已存在：{vpLayerInDwg}");
            else
                AddWarn(report, "当前 DWG 未发现视口相关图层");
        }

        private static void CheckDefaultLayers(Transaction tr, Database db, TemplateCheckReport report)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (lt.Has("0"))
                AddOk(report, "默认图层 0 存在");
            else
                AddWarn(report, "默认图层 0 不存在");

            if (lt.Has("Defpoints"))
                AddOk(report, "默认图层 Defpoints 存在");
            else
                AddInfo(report, "当前图纸未发现 Defpoints 图层");
        }

        private static void CheckNonStandardLayers(Transaction tr, Database db,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            List<string> extra = new();

            foreach (ObjectId id in lt)
            {
                LayerTableRecord lr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                string name = lr.Name;
                if (LayerPropertyUtils.IsExcludedLayer(name)) continue;
                if (standardLayerNames.Contains(name)) continue;
                extra.Add(name);
            }

            if (extra.Count == 0)
                AddOk(report, "未发现非标准图层");
            else
            {
                AddWarn(report, $"非标准图层：{extra.Count} 个");
                foreach (string e in extra.Take(15))
                    report.Lines.Add($"  - {e}");
                if (extra.Count > 15)
                    report.Lines.Add($"  ... 及其他 {extra.Count - 15} 个");
            }
        }

        private static void BuildSuggestions(TemplateCheckReport report)
        {
            bool hasMissingLayers = report.Lines.Any(l => l.Contains("[WARN]") && l.Contains("缺失"));
            bool hasCtbIssue = report.Lines.Any(l => l.Contains("[WARN]") &&
                (l.Contains("CTB") || l.Contains("打印样式")));
            bool hasColorIssue = report.Lines.Any(l => l.Contains("Invalid CTB") || l.Contains("not defined in ctbRules"));

            if (hasMissingLayers)
                report.Suggestions.Add("如缺失标准图层，请运行 BS_FIX_MISSING");
            if (hasCtbIssue)
                report.Suggestions.Add("如需设置标准 CTB，请使用 BS_CTB_EXPORT 导出规则后手动制作 / 设置 BS_CAD_STANDARD.ctb");
            if (hasColorIssue)
                report.Suggestions.Add("如需检查图层颜色，请运行 BS_CTB_CHECK");
        }

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
