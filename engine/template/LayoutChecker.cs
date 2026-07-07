using Autodesk.AutoCAD.DatabaseServices;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Template
{
    /// <summary>
    /// 布局检查器 — 检查布局数量和打印样式设置。
    /// </summary>
    public class LayoutChecker
    {
        public void Run(Database db, Transaction tr, TemplateCheckReport report)
        {
            CheckLayouts(db, tr, report);
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
                    if (!string.Equals(layout.LayoutName, "Model", System.StringComparison.OrdinalIgnoreCase))
                    {
                        string ps = string.IsNullOrEmpty(layout.CurrentStyleSheet) ? "未设置" : layout.CurrentStyleSheet;
                        AddInfo(report, $"  布局 \"{layout.LayoutName}\"：{ps}");
                    }
                }
                catch { }
            }
        }

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
