using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using System;
using System.Collections.Generic;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public class TextStyleService
    {
        private const string ChineseFontFace = "SimHei";
        private const string ChineseFontFile = "simhei.ttf";
        private const string EnglishFontFace = "Arial";
        private const string EnglishFontFile = "arial.ttf";

        private static readonly Dictionary<string, string> StandardFontFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "BS_TEXT_CN", ChineseFontFile },
            { "BS_TEXT_CN ", ChineseFontFile },
            { "BS_TEXT_TITLE", ChineseFontFile },
            { "BS_TEXT_TABLE", ChineseFontFile },
            { "BS_TEXT_NOTE", ChineseFontFile },
            { "BS_TEXT_EN", EnglishFontFile }
        };

        private static readonly Dictionary<string, string> StandardFontFaces = new(StringComparer.OrdinalIgnoreCase)
        {
            { "BS_TEXT_CN", ChineseFontFace },
            { "BS_TEXT_TITLE", ChineseFontFace },
            { "BS_TEXT_TABLE", ChineseFontFace },
            { "BS_TEXT_NOTE", ChineseFontFace },
            { "BS_TEXT_EN", EnglishFontFace }
        };

        public static ObjectId CreateStandardTextStyle(string styleName)
        {
            return EnsureStandardTextStyle(styleName, updateExisting: true);
        }

        public static ObjectId EnsureStandardTextStyle(string styleName, bool updateExisting = true)
        {
            string safeName = styleName.Trim();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (DocumentLock dl = doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        TextStyleTable st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                        ObjectId styleId;

                        if (st.Has(safeName))
                        {
                            styleId = st[safeName];

                            if (updateExisting)
                            {
                                TextStyleTableRecord existing = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForWrite);
                                ApplyStandardFont(existing, safeName, doc.Editor);
                                ApplyCommonSettings(existing);
                                doc.Editor.WriteMessage($"\n[信息] 已更新文字样式 {safeName} -> {GetExpectedFontFile(safeName)}");
                            }

                            tr.Commit();
                            return styleId;
                        }

                        st.UpgradeOpen();
                        TextStyleTableRecord record = new TextStyleTableRecord
                        {
                            Name = safeName
                        };

                        ApplyStandardFont(record, safeName, doc.Editor);
                        ApplyCommonSettings(record);

                        styleId = st.Add(record);
                        tr.AddNewlyCreatedDBObject(record, true);

                        doc.Editor.WriteMessage($"\n[信息] 已创建文字样式 {safeName} -> {GetExpectedFontFile(safeName)}");
                        tr.Commit();
                        return styleId;
                    }
                    catch (System.Exception ex)
                    {
                        doc.Editor.WriteMessage($"\n[错误] 创建/更新文字样式 {safeName} 失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }

        public static void EnsureDefaultTextStyles(bool updateExisting = true)
        {
            foreach (string styleName in StandardDefaults.TextStyles)
            {
                EnsureStandardTextStyle(styleName, updateExisting);
            }
        }

        public static bool IsStandardTextStyle(string styleName)
        {
            string safeName = styleName.Trim();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (!st.Has(safeName)) return false;

                TextStyleTableRecord record = (TextStyleTableRecord)tr.GetObject(st[safeName], OpenMode.ForRead);
                return IsFontMatched(record, safeName);
            }
        }

        public static string? GetFontDeviation(string styleName)
        {
            string safeName = styleName.Trim();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (!st.Has(safeName)) return null;

                TextStyleTableRecord record = (TextStyleTableRecord)tr.GetObject(st[safeName], OpenMode.ForRead);
                if (IsFontMatched(record, safeName)) return null;

                return $"{safeName}: 字体应为 {GetExpectedFontFile(safeName)} / {GetExpectedFontFace(safeName)}，实际为 {GetCurrentFontDescription(record)}";
            }
        }

        private static void ApplyStandardFont(TextStyleTableRecord record, string styleName, Autodesk.AutoCAD.EditorInput.Editor? editor)
        {
            string trimmed = styleName.Trim();
            string face = GetExpectedFontFace(trimmed);
            string file = GetExpectedFontFile(trimmed);

            // Step 1: FontDescriptor first
            try
            {
                record.Font = new FontDescriptor(face, false, false, 0, 0);
            }
            catch (System.Exception ex)
            {
                editor?.WriteMessage($"\n[警告] 设置文字样式 {trimmed} 的字体族 {face} 失败: {ex.Message}");
            }

            // Step 2: Set FileName (FontDescriptor setter sometimes overwrites it)
            try
            {
                record.FileName = file;
            }
            catch (System.Exception ex)
            {
                editor?.WriteMessage($"\n[警告] 设置文字样式 {trimmed} 的字体文件 {file} 失败: {ex.Message}");
            }

            // Step 3: 再次强制 FileName，确保 FontDescriptor 不会覆盖为非预期的字体
            try
            {
                record.FileName = file;
            }
            catch
            {
                // 静默，上一步已报过警告
            }

            try
            {
                record.BigFontFileName = string.Empty;
            }
            catch
            {
                // 部分环境下清空 BigFont 可能失败，不影响主字体标准。
            }
        }

        private static void ApplyCommonSettings(TextStyleTableRecord record)
        {
            record.ObliquingAngle = 0;
            record.XScale = 1;
            record.IsVertical = false;
            record.TextSize = 0;
        }

        private static string GetExpectedFontFile(string styleName)
        {
            return StandardFontFiles.TryGetValue(styleName.Trim(), out string? file) ? file : ChineseFontFile;
        }

        private static string GetExpectedFontFace(string styleName)
        {
            return StandardFontFaces.TryGetValue(styleName.Trim(), out string? face) ? face : ChineseFontFace;
        }

        private static bool IsFontMatched(TextStyleTableRecord record, string styleName)
        {
            string safeName = styleName.Trim();
            string fileName = record.FileName ?? string.Empty;
            string typeFace = string.Empty;

            try
            {
                typeFace = record.Font.TypeFace ?? string.Empty;
            }
            catch
            {
                // 忽略读取 FontDescriptor 失败，继续用 FileName 判断。
            }

            // BS_TEXT_EN: 只接受 Arial / arial.ttf / arial
            if (string.Equals(safeName, "BS_TEXT_EN", StringComparison.OrdinalIgnoreCase))
            {
                return fileName.Contains("arial", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(typeFace, "Arial", StringComparison.OrdinalIgnoreCase);
            }

            // 中文样式 (BS_TEXT_CN / BS_TEXT_TITLE / BS_TEXT_TABLE / BS_TEXT_NOTE):
            // 只接受 SimHei / simhei.ttf / 黑体
            return fileName.Contains("simhei", StringComparison.OrdinalIgnoreCase)
                || string.Equals(typeFace, "SimHei", StringComparison.OrdinalIgnoreCase)
                || typeFace.Contains("黑体");
        }

        private static string GetCurrentFontDescription(TextStyleTableRecord record)
        {
            string fileName = record.FileName ?? string.Empty;
            string typeFace = string.Empty;

            try
            {
                typeFace = record.Font.TypeFace ?? string.Empty;
            }
            catch
            {
                // ignore
            }

            if (string.IsNullOrWhiteSpace(fileName) && string.IsNullOrWhiteSpace(typeFace))
                return "未知";

            if (string.IsNullOrWhiteSpace(typeFace))
                return fileName;

            if (string.IsNullOrWhiteSpace(fileName))
                return typeFace;

            return $"{fileName} / {typeFace}";
        }
    }
}
