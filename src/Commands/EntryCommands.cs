using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class EntryCommands
    {
        [CommandMethod("BS_HELP")]
        public void BS_Help()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n===== BS CAD Standard V10 =====");
            ed.WriteMessage("\n  BS_CHECK    检查当前图纸标准");
            ed.WriteMessage("\n  BS_INIT     初始化图纸标准环境");
            ed.WriteMessage("\n  BS_DRAW     绘图辅助入口（图层/文字/标注/引线）");
            ed.WriteMessage("\n  BS_MIGRATE  旧图纸迁移入口（分析→导出→迁移→清理→检查）");
            ed.WriteMessage("\n  BS_CTB_CHECK  检查 CTB 颜色规则");

            ed.WriteMessage("\n\n--- 绘图辅助 ---");
            ed.WriteMessage("\n  BS_LAYER    标准图层切换与创建");
            ed.WriteMessage("\n  BS_TEXT     标准文字样式创建");
            ed.WriteMessage("\n  BS_DIM      标准标注样式创建");
            ed.WriteMessage("\n  BS_MLEADER  标准多重引线样式创建");

            ed.WriteMessage("\n\n--- 旧图纸迁移 ---");
            ed.WriteMessage("\n  BS_LAYER_AUDIT         分析旧图层");
            ed.WriteMessage("\n  BS_LAYER_AUDIT_EXPORT  导出迁移 CSV");
            ed.WriteMessage("\n  BS_LAYER_MERGE_FROM_CSV  按 CSV 迁移对象");
            ed.WriteMessage("\n  BS_LAYER_CLEAN_EMPTY   清理空旧图层");

            ed.WriteMessage("\n\n推荐流程：");
            ed.WriteMessage("\n  新图纸：BS_INIT → BS_CHECK");
            ed.WriteMessage("\n  旧图纸：BS_MIGRATE → 分析 → 导出 → 人工确认CSV → 迁移 → 清理 → 检查");
            ed.WriteMessage("\n====================================");
        }

        [CommandMethod("BS_DRAW")]
        public void BS_Draw()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            bool running = true;

            while (running)
            {
                ed.WriteMessage("\n===== BS_DRAW · 绘图辅助 =====");
                ed.WriteMessage("\n  1 = 图层切换与创建   (BS_LAYER)");
                ed.WriteMessage("\n  2 = 文字样式创建     (BS_TEXT)");
                ed.WriteMessage("\n  3 = 标注样式创建     (BS_DIM)");
                ed.WriteMessage("\n  4 = 多重引线样式创建 (BS_MLEADER)");
                ed.WriteMessage("\n");

                var opt = new PromptStringOptions("\n输入编号 (回车退出): ");
                opt.AllowSpaces = false;
                PromptResult res = ed.GetString(opt);

                if (res.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(res.StringResult))
                {
                    running = false;
                    continue;
                }

                string input = res.StringResult.Trim();

                switch (input)
                {
                    case "1":
                        ed.WriteMessage("\n--- 启动 BS_LAYER ---");
                        new LayerCommands().BS_Layer();
                        break;
                    case "2":
                        ed.WriteMessage("\n--- 启动 BS_TEXT ---");
                        new TextCommands().BS_Text();
                        break;
                    case "3":
                        ed.WriteMessage("\n--- 启动 BS_DIM ---");
                        new DimCommands().BS_Dim();
                        break;
                    case "4":
                        ed.WriteMessage("\n--- 启动 BS_MLEADER ---");
                        new MLeaderCommands().BS_MLeader();
                        break;
                    default:
                        ed.WriteMessage("\n无效输入，请输入 1-4 之间的编号。");
                        continue;
                }

                // 子命令执行完后询问是否继续
                var cont = new PromptStringOptions("\n按回车返回菜单，或输入 Q 退出: ");
                cont.AllowSpaces = false;
                PromptResult contRes = ed.GetString(cont);
                if (contRes.Status != PromptStatus.OK ||
                    string.Equals(contRes.StringResult?.Trim(), "Q", System.StringComparison.OrdinalIgnoreCase))
                {
                    running = false;
                }
            }
        }

        [CommandMethod("BS_MIGRATE")]
        public void BS_Migrate()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            bool running = true;

            while (running)
            {
                ed.WriteMessage("\n===== BS_MIGRATE · 旧图纸迁移 =====");
                ed.WriteMessage("\n  1 = 分析旧图层         (BS_LAYER_AUDIT)");
                ed.WriteMessage("\n  2 = 导出迁移 CSV       (BS_LAYER_AUDIT_EXPORT)");
                ed.WriteMessage("\n  3 = 按 CSV 执行迁移     (BS_LAYER_MERGE_FROM_CSV)");
                ed.WriteMessage("\n  4 = 清理空旧图层       (BS_LAYER_CLEAN_EMPTY)");
                ed.WriteMessage("\n  5 = 迁移后检查         (BS_CHECK)");
                ed.WriteMessage("\n");

                var opt = new PromptStringOptions("\n输入编号 (回车退出): ");
                opt.AllowSpaces = false;
                PromptResult res = ed.GetString(opt);

                if (res.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(res.StringResult))
                {
                    running = false;
                    continue;
                }

                string input = res.StringResult.Trim();

                switch (input)
                {
                    case "1":
                        ed.WriteMessage("\n--- 启动 BS_LAYER_AUDIT ---");
                        new LayerAuditCommands().BS_LayerAudit();
                        break;
                    case "2":
                        ed.WriteMessage("\n--- 启动 BS_LAYER_AUDIT_EXPORT ---");
                        new LayerAuditExportCommands().BS_LayerAuditExport();
                        break;
                    case "3":
                        ed.WriteMessage("\n--- 启动 BS_LAYER_MERGE_FROM_CSV ---");
                        new LayerMergeFromCsvCommands().BS_LayerMergeFromCsv();
                        break;
                    case "4":
                        ed.WriteMessage("\n--- 启动 BS_LAYER_CLEAN_EMPTY ---");
                        new LayerCleanEmptyCommands().BS_LayerCleanEmpty();
                        break;
                    case "5":
                        ed.WriteMessage("\n--- 启动 BS_CHECK ---");
                        new CheckCommands().BS_Check();
                        break;
                    default:
                        ed.WriteMessage("\n无效输入，请输入 1-5 之间的编号。");
                        continue;
                }

                var cont = new PromptStringOptions("\n按回车返回菜单，或输入 Q 退出: ");
                cont.AllowSpaces = false;
                PromptResult contRes = ed.GetString(cont);
                if (contRes.Status != PromptStatus.OK ||
                    string.Equals(contRes.StringResult?.Trim(), "Q", System.StringComparison.OrdinalIgnoreCase))
                {
                    running = false;
                }
            }
        }
    }
}
