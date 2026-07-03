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

                    string? command = input switch
                    {
                        "1" => "BS_LAYER ",
                        "2" => "BS_CHECK ",
                        "3" => "BS_FIX_LAYER ",
                        "4" => "BS_FIX_MISSING ",
                        "5" => "BS_LAYER_MODE ",
                        "6" => "BS_LAYER_ALL ",
                        "7" => "BS_CTB_CHECK ",
                        "8" => "BS_CTB_EXPORT ",
                        "9" => "BS_TEMPLATE_CHECK ",
                        "0" => null,
                        _ => "INVALID"
                    };

                    if (command == "INVALID")
                    {
                        ed.WriteMessage("\nInvalid option.\n");
                        continue;
                    }

                    if (command == null)
                    {
                        ed.WriteMessage("\nBS Toolkit exited.\n");
                        return;
                    }

                    ed.WriteMessage($"\nRunning {command.Trim()}...\n");
                    Document doc = Application.DocumentManager.MdiActiveDocument;
                    doc.SendStringToExecute(command, true, false, false);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS 执行失败", ex);
            }
        }
    }
}
