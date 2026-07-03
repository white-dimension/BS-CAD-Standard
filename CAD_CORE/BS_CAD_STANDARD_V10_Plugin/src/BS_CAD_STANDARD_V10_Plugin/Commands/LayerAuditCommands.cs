using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class LayerAuditCommands
    {
        [CommandMethod("BS_LAYER_AUDIT")]
        public void BS_LayerAudit()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // 复用 BS_CHECK 相同的配置加载路径：ConfigurationService.CreateContext
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null)
                {
                    return;
                }

                // 加载迁移规则（可选，缺失时仅关键词匹配降级）
                MigrationRulesConfig? rules = ConfigurationService.LoadMigrationRules(ed);
                string rulesPath = ConfigurationService.CurrentMigrationRulesPath;

                ed.WriteMessage("\n正在分析图纸图层，请稍候...");

                LayerAuditResult result = LayerAuditEngine.Audit(context.StandardConfig, rules);
                result.RulesPath = rulesPath;

                PrintAuditReport(ed, result, context.StandardConfigPath);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_AUDIT 执行失败", ex);
            }
        }

        private void PrintAuditReport(Editor ed, LayerAuditResult result, string configPath)
        {
            ed.WriteMessage("\n\n===== BS_LAYER_AUDIT · 旧图层分析 =====");

            ed.WriteMessage($"\n\n标准配置：{configPath}");
            ed.WriteMessage($"\n迁移规则：{result.RulesPath}");
            ed.WriteMessage($"\n标准图层总数（JSON）：{result.StandardLayerCount}");
            ed.WriteMessage($"\n当前图纸图层总数：{result.TotalLayerCount}");
            ed.WriteMessage($"\n非标准图层数量：{result.NonStandardCount}");
            ed.WriteMessage($"\n外部参照图层数量：{result.XrefLayerCount}");
            ed.WriteMessage("\n忽略系统图层：0, Defpoints");

            if (result.NonStandardLayers.Count == 0)
            {
                ed.WriteMessage("\n\n未发现非标准图层，当前图纸图层符合 BS CAD Standard V10 标准。");
                ed.WriteMessage("\n\n====================================");
                return;
            }

            ed.WriteMessage($"\n\n[非标准图层]");

            for (int i = 0; i < result.NonStandardLayers.Count; i++)
            {
                var layer = result.NonStandardLayers[i];
                ed.WriteMessage($"\n\n{i + 1}. {layer.LayerName}");
                ed.WriteMessage($"\n   对象数：{layer.ObjectCount}");
                ed.WriteMessage($"\n   建议目标：{layer.SuggestedTarget}");
                ed.WriteMessage($"\n   规则：{layer.MatchRule}");
            }

            ed.WriteMessage("\n\n====================================");
        }
    }
}
