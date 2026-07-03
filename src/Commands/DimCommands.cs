using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System.Linq;
using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class DimCommands
    {
        [CommandMethod("BS_DIM")]
        public void BS_Dim()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // 1. 加载主标准配置和标注样式专用配置
                StandardContext? context = ConfigurationService.CreateContext(ed, includeDimStyleConfig: true);
                if (context == null || context.DimStyleConfig == null)
                {
                    return;
                }

                StandardConfig mainConfig = context.StandardConfig;
                DimStyleStandardConfig dimConfig = context.DimStyleConfig;

                // 2. 选择标注样式
                List<string> styleNames = dimConfig.DimStyles.Select(s => s.Name).ToList();
                string selectedName = PromptUtils.SelectDimStyle(styleNames);
                if (selectedName == "Q") return;

                DimStyleConfig? selectedCfg = dimConfig.DimStyles.FirstOrDefault(s => s.Name == selectedName);
                if (selectedCfg == null) return;

                // 3. 检查 / 创建 / 更新
                ObjectId styleId = DimStyleService.GetDimStyleId(selectedName);
                if (styleId == ObjectId.Null)
                {
                    PromptResultType res = PromptUtils.ConfirmAction($"当前图纸缺少标注样式 {selectedName}，是否按标准创建？", "Y");
                    if (res == PromptResultType.Yes)
                    {
                        styleId = DimStyleService.CreateOrUpdateStandardDimStyle(selectedCfg);
                    }
                    else return;
                }
                else
                {
                    PromptResultType updateRes = PromptUtils.ConfirmAction($"标注样式 {selectedName} 已存在，是否按 V10 标准更新参数？", "Y");
                    if (updateRes == PromptResultType.Yes)
                    {
                        styleId = DimStyleService.CreateOrUpdateStandardDimStyle(selectedCfg);
                    }
                }

                if (styleId == ObjectId.Null) return;

                // 4. 确定图层 (优先从标注配置读取， fallback 到主配置)
                string targetLayerName = dimConfig.DefaultLayer;
                LayerConfig? layerCfg = mainConfig.Layers.FirstOrDefault(l => l.Name == targetLayerName);

                ObjectId layerId = LayerService.GetLayerId(targetLayerName);
                if (layerId == ObjectId.Null && layerCfg != null)
                {
                    PromptResultType res = PromptUtils.ConfirmAction($"标注图层 [{targetLayerName}] 不存在，是否按标准创建？", "Y");
                    if (res == PromptResultType.Yes)
                    {
                        layerId = LayerService.CreateLayerFromConfig(layerCfg);
                    }
                }

                // 5. 应用设置
                DimStyleService.SetCurrentDimStyle(styleId);
                if (layerId != ObjectId.Null)
                {
                    LayerService.SwitchToLayer(layerId);
                }

                // 6. 输出报告
                OutputDimConfigDetails(ed, selectedCfg, targetLayerName);

                // 7. 询问创建标注
                PromptResultType createRes = PromptUtils.ConfirmAction("是否立即创建线性标注？", "Y");
                if (createRes == PromptResultType.Yes)
                {
                    CreateAlignedDimension(styleId, layerId);
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_DIM 执行失败", ex);
            }
        }

        private void OutputDimConfigDetails(Editor ed, DimStyleConfig cfg, string layerName)
        {
            ed.WriteMessage("\n--- BS CAD 标注标准参数 ---");
            ed.WriteMessage($"\n样式名称: {cfg.Name}");
            ed.WriteMessage($"\n文字样式: {cfg.TextStyle}");
            ed.WriteMessage($"\n文字高度: {cfg.TextHeight}");
            ed.WriteMessage($"\n箭头大小: {cfg.ArrowSize}");
            ed.WriteMessage($"\n全局比例: {cfg.DimScale}");
            ed.WriteMessage($"\n小数精度: {cfg.DecimalPrecision}");
            ed.WriteMessage($"\n目标图层: {layerName}");
            ed.WriteMessage("\n---------------------------");
        }

        private void CreateAlignedDimension(ObjectId styleId, ObjectId layerId)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            PromptPointOptions pt1Opt = new PromptPointOptions("\n指定第一条尺寸界线原点: ");
            PromptPointResult pt1Res = ed.GetPoint(pt1Opt);
            if (pt1Res.Status != PromptStatus.OK) return;

            PromptPointOptions pt2Opt = new PromptPointOptions("\n指定第二条尺寸界线原点: ");
            pt2Opt.UseBasePoint = true;
            pt2Opt.BasePoint = pt1Res.Value;
            PromptPointResult pt2Res = ed.GetPoint(pt2Opt);
            if (pt2Res.Status != PromptStatus.OK) return;

            PromptPointOptions dimPtOpt = new PromptPointOptions("\n指定尺寸线位置: ");
            dimPtOpt.UseBasePoint = true;
            dimPtOpt.BasePoint = pt2Res.Value;
            PromptPointResult dimPtRes = ed.GetPoint(dimPtOpt);
            if (dimPtRes.Status != PromptStatus.OK) return;

            ObjectId dimensionId = DimensionService.CreateAlignedDimension(pt1Res.Value, pt2Res.Value, dimPtRes.Value, styleId, layerId);
            if (dimensionId != ObjectId.Null)
            {
                ed.WriteMessage("\n标注创建成功。");
            }
        }
    }
}
