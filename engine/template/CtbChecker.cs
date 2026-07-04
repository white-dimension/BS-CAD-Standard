using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Template
{
    /// <summary>
    /// CTB 检查器 — 检查当前布局打印样式设置。
    /// </summary>
    public class CtbChecker
    {
        public void Run(Database db, Transaction tr, TemplateCheckReport report)
        {
            CheckPlotStyle(db, tr, report);
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

                if (string.IsNullOrEmpty(styleSheet))
                    AddWarn(report, "当前布局未设置 CTB / STB");
                else if (!string.Equals(styleSheet, StandardPaths.CtbFileName, System.StringComparison.OrdinalIgnoreCase))
                    AddWarn(report, $"当前布局打印样式不是 {StandardPaths.CtbFileName}：当前为 {styleSheet}");
                else
                    AddOk(report, $"当前布局打印样式：{StandardPaths.CtbFileName}");
            }
            catch
            {
                AddInfo(report, "无法读取当前布局打印样式");
            }
        }

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
