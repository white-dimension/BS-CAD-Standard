using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class LauncherCommands
    {
        [CommandMethod("BS")]
        public void BsLauncher()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                while (true)
                {
                    // Display menu
                    ed.WriteMessage("\n===== BS Toolkit =====");
                    ed.WriteMessage("\n");
                    ed.WriteMessage("\n[1] 生成标准图层        BS_LAYER");
                    ed.WriteMessage("\n[2] 检查图层标准        BS_CHECK");
                    ed.WriteMessage("\n[3] 修复图层属性        BS_FIX_LAYER");
                    ed.WriteMessage("\n[4] 补齐缺失图层        BS_FIX_MISSING");
                    ed.WriteMessage("\n[5] 图层模式切换        BS_LAYER_MODE");
                    ed.WriteMessage("\n[6] 恢复全部图层        BS_LAYER_ALL");
                    ed.WriteMessage("\n[7] 检查 CTB 颜色规则    BS_CTB_CHECK");
                    ed.WriteMessage("\n[8] 导出 CTB 规则说明    BS_CTB_EXPORT");
                    ed.WriteMessage("\n[9] 模板基础环境检查    BS_TEMPLATE_CHECK");
                    ed.WriteMessage("\n[0] 退出");
                    ed.WriteMessage("\n");

                    var opt = new PromptStringOptions("\nEnter option: ")
                    {
                        AllowSpaces = false
                    };

                    PromptResult res = ed.GetString(opt);
                    if (res.Status != PromptStatus.OK)
                    {
                        ed.WriteMessage("\nBS Toolkit canceled.\n");
                        return;
                    }

                    string input = res.StringResult.Trim();

                    switch (input)
                    {
                        case "1":
                            ed.WriteMessage("\n--- 启动 BS_LAYER ---");
                            new LayerCommands().BS_Layer();
                            break;
                        case "2":
                            ed.WriteMessage("\n--- 启动 BS_CHECK ---");
                            new CheckCommands().BS_Check();
                            break;
                        case "3":
                            ed.WriteMessage("\n--- 启动 BS_FIX_LAYER ---");
                            new FixCommands().BsFixLayer();
                            break;
                        case "4":
                            ed.WriteMessage("\n--- 启动 BS_FIX_MISSING ---");
                            new MissingCommands().BsFixMissing();
                            break;
                        case "5":
                            ed.WriteMessage("\n--- 启动 BS_LAYER_MODE ---");
                            new LayerModeCommands().BsLayerMode();
                            break;
                        case "6":
                            ed.WriteMessage("\n--- 启动 BS_LAYER_ALL ---");
                            new LayerModeCommands().BsLayerAll();
                            break;
                        case "7":
                            ed.WriteMessage("\n--- 启动 BS_CTB_CHECK ---");
                            new CtbCommands().BsCtbCheck();
                            break;
                        case "8":
                            ed.WriteMessage("\n--- 启动 BS_CTB_EXPORT ---");
                            new CtbCommands().BsCtbExport();
                            break;
                        case "9":
                            ed.WriteMessage("\n--- 启动 BS_TEMPLATE_CHECK ---");
                            new TemplateCommands().BsTemplateCheck();
                            break;
                        case "0":
                            ed.WriteMessage("\nBS Toolkit exited.\n");
                            return;
                        default:
                            ed.WriteMessage("\nInvalid option.\n");
                            continue;
                    }

                    // After command completes, return to menu
                    var cont = new PromptStringOptions("\n按回车返回菜单，或输入 Q 退出: ");
                    cont.AllowSpaces = false;
                    PromptResult contRes = ed.GetString(cont);
                    if (contRes.Status != PromptStatus.OK ||
                        string.Equals(contRes.StringResult?.Trim(), "Q", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ed.WriteMessage("\nBS Toolkit exited.\n");
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS 执行失败", ex);
            }
        }
    }
}
