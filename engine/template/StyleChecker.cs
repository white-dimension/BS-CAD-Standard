using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Template
{
    /// <summary>
    /// 样式检查器 — 检查文字样式和标注样式是否完备。
    /// </summary>
    public class StyleChecker
    {
        public void Run(Transaction tr, Database db, StandardConfig config, TemplateCheckReport report)
        {
            CheckTextStyles(tr, db, config, report);
            CheckDimStyles(tr, db, config, report);
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

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
