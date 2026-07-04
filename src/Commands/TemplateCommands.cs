using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Cad.Services;
using BS_CAD_STANDARD_V10_Plugin.Engine.Template;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class TemplateCommands
    {
        [CommandMethod("BS_TEMPLATE_CHECK")]
        public void BsTemplateCheck()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                ed.WriteMessage("\n========================================");
                ed.WriteMessage("\nBS_TEMPLATE_CHECK - 模板基础环境检查");
                ed.WriteMessage("\nVersion: v0.8-template");
                ed.WriteMessage("\n========================================");

                TemplateCheckReport report = TemplateBridge.RunCheck(config);

                if (!report.Success)
                {
                    ed.WriteMessage($"\n\nBS_TEMPLATE_CHECK 执行失败。");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                    return;
                }

                foreach (string line in report.Lines)
                    ed.WriteMessage($"\n{line}");

                ed.WriteMessage("\n\n----------------------------------------");
                ed.WriteMessage($"\n检查完成：");
                ed.WriteMessage($"\n  OK：{report.OkCount} 项");
                ed.WriteMessage($"\n  WARN：{report.WarnCount} 项");
                ed.WriteMessage($"\n  INFO：{report.InfoCount} 项");
                ed.WriteMessage($"\n  ERROR：{report.ErrorCount} 项");

                if (report.Suggestions.Count > 0)
                {
                    ed.WriteMessage($"\n\n建议下一步：");
                    foreach (string s in report.Suggestions)
                        ed.WriteMessage($"\n  - {s}");
                }

                ed.WriteMessage("\n========================================\n");
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_TEMPLATE_CHECK 执行失败", ex);
            }
        }
    }
}
