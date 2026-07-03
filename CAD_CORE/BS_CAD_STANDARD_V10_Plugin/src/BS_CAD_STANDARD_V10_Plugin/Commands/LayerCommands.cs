using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class LayerCommands
    {
        [CommandMethod("BS_LAYER")]
        public void BS_Layer()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;
                List<CategoryInfo> categories = LayerService.GetCategories(config);
                bool keepRunning = true;

                while (keepRunning)
                {
                    // 1. 选择分类
                    string catCode = PromptUtils.SelectCategory(categories);
                    if (catCode == "Q") break;

                    // 2. 选择图层
                    List<LayerConfig> layers = LayerService.GetLayersByCategory(config, catCode);
                    bool backToCategories = false;

                    while (!backToCategories)
                    {
                        LayerConfig? selected = PromptUtils.SelectLayer(layers);
                        if (selected == null) // Q or Esc
                        {
                            keepRunning = false;
                            break;
                        }

                        if (selected.Name == "__BACK__")
                        {
                            backToCategories = true;
                            continue;
                        }

                        // 3. 处理图层切换或创建
                        ProcessLayerSelection(selected);
                        keepRunning = false; // 切换成功后退出命令
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER 执行失败", ex);
            }
        }

        private void ProcessLayerSelection(LayerConfig cfg)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ObjectId layerId = LayerService.GetLayerId(cfg.Name);

            if (layerId != ObjectId.Null)
            {
                if (LayerService.SwitchToLayer(layerId))
                {
                    ed.WriteMessage($"\n已切换到图层: {cfg.Name}");
                }
            }
            else
            {
                PromptResultType confirm = PromptUtils.ConfirmCreate(cfg.Name);

                if (confirm == PromptResultType.Yes)
                {
                    ObjectId newLayerId = LayerService.CreateLayerFromConfig(cfg);
                    if (newLayerId != ObjectId.Null)
                    {
                        if (LayerService.SwitchToLayer(newLayerId))
                        {
                            ed.WriteMessage($"\n图层 [{cfg.Name}] 已创建并切换。");
                        }
                    }
                }
                else if (confirm == PromptResultType.No)
                {
                    ed.WriteMessage("\n操作已取消。");
                }
                else // Cancel (Q/Esc)
                {
                    ed.WriteMessage("\n命令已取消。");
                }
            }
        }
    }
}
