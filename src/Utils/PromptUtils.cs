using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Core;
using System;

namespace BS_CAD_STANDARD_V10_Plugin.Utils
{
    public enum PromptResultType
    {
        Yes,
        No,
        Cancel
    }

    public static class PromptUtils
    {
        public const string QuitCommand = "Q";
        public const string BackCommand = "X";

        public static string GetString(string message, bool allowSpaces = false)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptStringOptions opt = new PromptStringOptions(message)
            {
                AllowSpaces = allowSpaces
            };

            PromptResult res = ed.GetString(opt);
            return res.Status == PromptStatus.OK ? res.StringResult : string.Empty;
        }

        public static int SelectNumber(string message, int minValue, int maxValue, int defaultValue = -1)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptStringOptions opt = new PromptStringOptions(message)
            {
                AllowSpaces = false
            };

            while (true)
            {
                PromptResult res = ed.GetString(opt);
                if (res.Status != PromptStatus.OK) return -1;

                string input = res.StringResult.Trim().ToUpper();
                if (string.IsNullOrEmpty(input) && defaultValue >= minValue && defaultValue <= maxValue)
                {
                    return defaultValue;
                }

                if (input == QuitCommand) return -1;

                if (int.TryParse(input, out int index) && index >= minValue && index <= maxValue)
                {
                    return index;
                }

                ed.WriteMessage("\n无效输入，请重新输入。");
            }
        }

        public static string SelectCategory(List<CategoryInfo> categories)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n--- 分类选择 ---");
            for (int i = 0; i < categories.Count; i++)
            {
                var cat = categories[i];
                ed.WriteMessage($"\n[{i + 1:D2}] {cat.Code} — {cat.Description} ({cat.LayerCount}层)");
            }

            PromptStringOptions opt = new PromptStringOptions("\n输入编号或代码 (Q退出): ");
            opt.AllowSpaces = false;

            while (true)
            {
                PromptResult res = ed.GetString(opt);
                if (res.Status != PromptStatus.OK) return "Q";

                string input = res.StringResult.Trim().ToUpper();
                if (string.IsNullOrEmpty(input)) continue;
                if (input == QuitCommand) return QuitCommand;

                // 尝试匹配编号
                if (int.TryParse(input, out int index) && index > 0 && index <= categories.Count)
                {
                    return categories[index - 1].Code;
                }

                // 尝试匹配代码
                if (categories.Exists(c => string.Equals(c.Code, input, StringComparison.OrdinalIgnoreCase)))
                {
                    return input;
                }

                ed.WriteMessage("\n无效输入，请重新输入。");
            }
        }

        public static LayerConfig? SelectLayer(List<LayerConfig> layers)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n--- 图层选择 ---");
            for (int i = 0; i < layers.Count; i++)
            {
                var ly = layers[i];
                ed.WriteMessage($"\n[{i + 1:D2}] {ly.Name}  Color={ly.Color}  LineWeight={ly.Lineweight}");
            }

            PromptStringOptions opt = new PromptStringOptions("\n输入编号 (X返回, Q退出): ");
            opt.AllowSpaces = false;

            while (true)
            {
                PromptResult res = ed.GetString(opt);
                if (res.Status != PromptStatus.OK) return null;

                string input = res.StringResult.Trim().ToUpper();
                if (string.IsNullOrEmpty(input)) continue;
                if (input == QuitCommand) return null;
                if (input == BackCommand) return new LayerConfig { Name = "__BACK__" };

                if (int.TryParse(input, out int index) && index > 0 && index <= layers.Count)
                {
                    return layers[index - 1];
                }

                ed.WriteMessage("\n无效输入，请重新输入。");
            }
        }

        public static string SelectDimStyle(List<string> styles)
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            ed.WriteMessage("\n--- 标注样式选择 ---");
            for (int i = 0; i < styles.Count; i++)
            {
                ed.WriteMessage($"\n[{i + 1:D2}] {styles[i]}");
            }

            int selectedIndex = SelectNumber("\n输入编号 (默认 1, Q退出): ", 1, styles.Count, 1);
            return selectedIndex < 0 ? QuitCommand : styles[selectedIndex - 1];
        }

        public static PromptResultType ConfirmCreate(string layerName)
        {
            return ConfirmAction($"图层 [{layerName}] 不存在，是否按标准创建？", "N");
        }

        public static PromptResultType ConfirmAction(string message, string defaultVal = "Y")
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            PromptStringOptions opt = new PromptStringOptions($"\n{message} [Y/N] <{defaultVal.ToUpper()}>: ");
            opt.AllowSpaces = false;

            PromptResult res = ed.GetString(opt);
            if (res.Status != PromptStatus.OK) return PromptResultType.Cancel;

            string input = res.StringResult.Trim().ToLower();

            if (string.IsNullOrEmpty(input))
            {
                return defaultVal.ToUpper() == "Y" ? PromptResultType.Yes : PromptResultType.No;
            }

            // 处理 Yes
            string[] yesValues = { "y", "yes", "是", "确认" };
            foreach (var val in yesValues)
            {
                if (input == val) return PromptResultType.Yes;
            }

            // 处理 Cancel (Q)
            if (input == "q") return PromptResultType.Cancel;

            // 处理 No
            string[] noValues = { "n", "no", "否", "取消" };
            foreach (var val in noValues)
            {
                if (input == val) return PromptResultType.No;
            }

            // 默认返回 No
            return PromptResultType.No;
        }
    }
}
