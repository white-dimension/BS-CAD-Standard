using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System.Linq;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class TextCommands
    {
        [CommandMethod("BS_TEXT")]
        public void BS_Text()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;
                StandardConfig config = context.StandardConfig;

                // 1. 确定文字样式
                ObjectId styleId = AcadUtils.GetTextStyleId(StandardDefaults.DefaultTextStyle);
                if (styleId == ObjectId.Null)
                {
                    PromptResultType res = PromptUtils.ConfirmAction($"当前图纸缺少文字样式 {StandardDefaults.DefaultTextStyle}，是否按标准创建？", "Y");

                    if (res == PromptResultType.Yes)
                    {
                        styleId = TextStyleService.CreateStandardTextStyle(StandardDefaults.DefaultTextStyle);
                        if (styleId == ObjectId.Null) return;
                    }
                    else if (res == PromptResultType.No)
                    {
                        ed.WriteMessage($"\n已取消创建文字样式，BS_TEXT 结束。");
                        return;
                    }
                    else // Cancel
                    {
                        return;
                    }
                }
                else if (!TextStyleService.IsStandardTextStyle(StandardDefaults.DefaultTextStyle))
                {
                    ed.WriteMessage($"\n[警告] 文字样式 {StandardDefaults.DefaultTextStyle} 字体不符合 BS 标准，正在更新。");
                    styleId = TextStyleService.EnsureStandardTextStyle(StandardDefaults.DefaultTextStyle, updateExisting: true);
                    if (styleId == ObjectId.Null) return;
                }

                // 2. 确定图层
                LayerConfig? textLayerCfg = config.Layers.FirstOrDefault(l => l.Name == StandardDefaults.FallbackTextLayer);
                if (textLayerCfg == null)
                {
                    // 如果找不到完全匹配的，尝试模糊搜索 TX 分类的普通文字
                    textLayerCfg = config.Layers.FirstOrDefault(l => l.Category == "TX" && (l.Name.Contains("普通文字") || l.Description.Contains("普通文字")));
                }

                string targetLayerName = textLayerCfg?.Name ?? StandardDefaults.FallbackTextLayer;
                ObjectId layerId = LayerService.GetLayerId(targetLayerName);

                if (layerId == ObjectId.Null)
                {
                    if (textLayerCfg != null)
                    {
                        PromptResultType res = PromptUtils.ConfirmAction($"图层 [{targetLayerName}] 不存在，是否按标准创建？", "Y");
                        if (res == PromptResultType.Yes)
                        {
                            layerId = LayerService.CreateLayerFromConfig(textLayerCfg);
                        }
                        else if (res == PromptResultType.No)
                        {
                            ed.WriteMessage($"\n[警告] 未创建标准图层，文字将创建在当前图层。");
                        }
                        else // Cancel
                        {
                            return;
                        }
                    }
                }

                // 3. 用户输入文字内容
                PromptStringOptions textOpt = new PromptStringOptions("\n请输入文字内容: ");
                textOpt.AllowSpaces = true;
                PromptResult textRes = ed.GetString(textOpt);
                if (textRes.Status != PromptStatus.OK) return;
                string content = textRes.StringResult;

                // 4. 用户指定插入点
                PromptPointOptions ptOpt = new PromptPointOptions("\n请指定插入点: ");
                PromptPointResult ptRes = ed.GetPoint(ptOpt);
                if (ptRes.Status != PromptStatus.OK) return;
                Point3d position = ptRes.Value;

                // 5. 用户输入文字高度
                PromptDoubleOptions hOpt = new PromptDoubleOptions($"\n请输入文字高度 <{StandardDefaults.DefaultTextHeight}>: ");
                hOpt.DefaultValue = StandardDefaults.DefaultTextHeight;
                hOpt.UseDefaultValue = true;
                hOpt.AllowNone = true;
                PromptDoubleResult hRes = ed.GetDouble(hOpt);
                if (hRes.Status != PromptStatus.OK) return;
                double height = hRes.Value;

                // 6. 执行创建
                ObjectId textId = TextService.CreateStandardDBText(content, position, height, layerId, styleId);

                if (textId != ObjectId.Null)
                {
                    ed.WriteMessage($"\n已创建标准文字: {content}");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_TEXT 执行失败", ex);
            }
        }
    }
}
