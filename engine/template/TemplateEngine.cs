using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Template
{
    /// <summary>
    /// 模板检查引擎 — Template 检查逻辑的唯一来源。
    /// 编排 5 个子检查器完成完整模板环境检查。
    /// </summary>
    public class TemplateEngine
    {
        public UnitChecker Unit { get; } = new();
        public LayerChecker Layer { get; } = new();
        public CtbChecker Ctb { get; } = new();
        public StyleChecker Style { get; } = new();
        public LayoutChecker Layout { get; } = new();

        public TemplateCheckReport CheckTemplate(StandardConfig config)
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
                    report.Lines.Add("");
                    report.Lines.Add("[1] 单位检查");
                    Unit.Run(report);

                    report.Lines.Add("");
                    report.Lines.Add("[2] 图层检查");
                    Layer.Run(tr, db, config, standardLayerNames, report);

                    report.Lines.Add("");
                    report.Lines.Add("[3] CTB / 打印样式检查");
                    Ctb.Run(db, tr, report);

                    report.Lines.Add("");
                    report.Lines.Add("[4] 样式检查");
                    Style.Run(tr, db, config, report);

                    report.Lines.Add("");
                    report.Lines.Add("[5] 布局检查");
                    Layout.Run(db, tr, report);

                    BuildSuggestions(report);

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] Template check failed: {ex.Message}");
                }
            }

            return report;
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
                report.Suggestions.Add("如需设置标准 CTB，请使用 BS_CTB_EXPORT 导出规则后手动制作 / 设置 CTB");
            if (hasColorIssue)
                report.Suggestions.Add("如需检查图层颜色，请运行 BS_CTB_CHECK");
        }
    }
}
