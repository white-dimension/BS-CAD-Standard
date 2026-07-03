using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System.Linq;
using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class CheckCommands
    {
        [CommandMethod("BS_CHECK")]
        public void BS_Check()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null)
                {
                    return;
                }

                // 执行检查
                CheckResult result = CheckEngine.RunFullCheck(context.StandardConfig);

                // 输出报告
                PrintReport(ed, context, result);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_CHECK 执行失败", ex);
            }
        }

        private void PrintReport(Editor ed, StandardContext context, CheckResult result)
        {
            StandardConfig config = context.StandardConfig;
            ed.WriteMessage("\n===== BS CHECK · BS CAD Standard V10 =====");

            // [配置]
            ed.WriteMessage("\n\n[配置]");
            ed.WriteMessage($"\nJSON路径: {context.StandardConfigPath}");
            ed.WriteMessage($"\n标准名称: {config.StandardName}");
            ed.WriteMessage($"\n版本号: {config.Version}");
            ed.WriteMessage($"\n图层总数: {config.Layers.Count}");
            ed.WriteMessage($"\n核心图层数量: {config.Layers.Count(l => l.Core)}");
            ed.WriteMessage($"\n分类数量: {config.Layers.Select(l => l.Category).Distinct().Count()}");

            // [图层]
            ed.WriteMessage("\n\n[图层]");
            int coreCount = config.Layers.Count(l => l.Core);
            ed.WriteMessage($"\n核心图层检查: 共 {coreCount} 层");
            ed.WriteMessage($"\n缺失: {result.MissingCoreLayers.Count}");
            foreach (var layer in result.MissingCoreLayers) ed.WriteMessage($"\n  - {layer}");

            ed.WriteMessage($"\n属性偏差: {result.PropertyDeviations.Count}");
            foreach (var dev in result.PropertyDeviations) ed.WriteMessage($"\n  - {dev}");

            ed.WriteMessage($"\n额外非标准图层: {result.ExtraLayers.Count}");
            if (result.ExtraLayers.Count > 0)
            {
                // 只列出前 10 个，避免刷屏
                foreach (var layer in result.ExtraLayers.Take(10)) ed.WriteMessage($"\n  - {layer}");
                if (result.ExtraLayers.Count > 10) ed.WriteMessage("\n  ... (等)");
            }

            // [文字样式]
            List<string> textTargets = (config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0)
                                       ? config.Styles.TextStyles : StandardDefaults.TextStyles;
            ed.WriteMessage("\n\n[文字样式]");
            ed.WriteMessage($"\n标准: {textTargets.Count}");
            ed.WriteMessage($"\n已存在: {textTargets.Count - result.MissingTextStyles.Count}");
            ed.WriteMessage($"\n缺失: {result.MissingTextStyles.Count}");
            foreach (var style in result.MissingTextStyles) ed.WriteMessage($"\n  - {style}");
            ed.WriteMessage($"\n字体偏差: {result.TextStyleFontDeviations.Count}");
            foreach (var dev in result.TextStyleFontDeviations) ed.WriteMessage($"\n  - {dev}");

            // [标注样式]
            List<string> dimTargets = (config.Styles.DimStyles != null && config.Styles.DimStyles.Count > 0)
                                       ? config.Styles.DimStyles : StandardDefaults.DimStyles;
            ed.WriteMessage("\n\n[标注样式]");
            ed.WriteMessage($"\n标准: {dimTargets.Count}");
            ed.WriteMessage($"\n已存在: {dimTargets.Count - result.MissingDimStyles.Count}");
            ed.WriteMessage($"\n缺失: {result.MissingDimStyles.Count}");
            foreach (var style in result.MissingDimStyles) ed.WriteMessage($"\n  - {style}");

            // [多重引线样式]
            ed.WriteMessage("\n\n[多重引线样式]");
            ed.WriteMessage($"\n{StandardDefaults.MLeaderStyleNote}: {(result.MLeaderStyleExists ? "已存在" : "缺失")}");

            // [单位]
            ed.WriteMessage("\n\n[单位]");
            ed.WriteMessage($"\nINSUNITS = {result.CurrentUnits}");
            ed.WriteMessage($"\n结果: {(result.CurrentUnits == StandardDefaults.StandardUnits ? "正确" : "应为 4（毫米）")}");

            // [打印样式]
            ed.WriteMessage("\n\n[打印样式]");
            ed.WriteMessage($"\n当前布局: {result.CurrentLayoutName}");
            ed.WriteMessage($"\n当前 CTB: {(string.IsNullOrEmpty(result.CurrentCtb) ? "无" : result.CurrentCtb)}");
            ed.WriteMessage($"\n标准 CTB: {StandardPaths.CtbFileName}");
            ed.WriteMessage($"\n结果: {(string.Equals(result.CurrentCtb, StandardPaths.CtbFileName, System.StringComparison.OrdinalIgnoreCase) ? "正确" : "不一致")}");

            ed.WriteMessage("\n\n========================================");
        }
    }
}
