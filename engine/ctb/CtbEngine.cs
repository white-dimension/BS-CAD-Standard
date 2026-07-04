using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Ctb
{
    /// <summary>
    /// CTB 引擎 — CTB 校验和导出逻辑的唯一来源。
    /// </summary>
    public class CtbEngine
    {
        // ===== CTB 校验 =====

        public CtbCheckReport Check(StandardConfig config)
        {
            CtbCheckReport report = new()
            {
                CtbName = config.Ctb,
                StandardLayerCount = config.Layers.Count,
                CtbRuleColorCount = config.CtbRules?.Count ?? 0
            };

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);
            HashSet<int> ctbColors = new((config.CtbRules ?? new List<CtbRuleConfig>()).Select(r => r.Color));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    foreach (LayerConfig layerConfig in config.Layers)
                    {
                        if (!layerTable.Has(layerConfig.Name))
                        {
                            report.MissingStandardLayers.Add(layerConfig.Name);
                            continue;
                        }

                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(
                            layerTable[layerConfig.Name], OpenMode.ForRead);
                        short colorIndex = layerRecord.Color.ColorIndex;

                        if (colorIndex != layerConfig.Color)
                        {
                            report.ColorMismatches.Add(
                                $"{layerConfig.Name}: current={colorIndex}, expected={layerConfig.Color}");
                        }

                        if (!ctbColors.Contains(colorIndex))
                        {
                            report.InvalidCtbColors.Add(
                                $"{layerConfig.Name}: color={colorIndex} not defined in ctbRules");
                        }
                    }

                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        string name = layerRecord.Name;

                        if (LayerPropertyUtils.IsExcludedLayer(name)) continue;
                        if (name.Contains("|")) continue;

                        if (standardLayerNames.Contains(name)) continue;

                        report.NonStandardLayers.Add(name);
                        short colorIndex = layerRecord.Color.ColorIndex;
                        if (!ctbColors.Contains(colorIndex))
                        {
                            report.NonStandardLayerInvalidColors.Add(
                                $"{name}: color={colorIndex} not defined in ctbRules");
                        }
                    }

                    report.ExistingStandardLayerCount = config.Layers.Count - report.MissingStandardLayers.Count;
                    report.MissingStandardLayerCount = report.MissingStandardLayers.Count;
                    report.ColorMismatchCount = report.ColorMismatches.Count;
                    report.InvalidLayerColorCount = report.InvalidCtbColors.Count;
                    report.ValidLayerColorCount = report.ExistingStandardLayerCount - report.InvalidLayerColorCount;
                    report.NonStandardLayerCount = report.NonStandardLayers.Count;
                    report.NonStandardLayerInvalidColorCount = report.NonStandardLayerInvalidColors.Count;

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] CTB check failed: {ex.Message}");
                }
            }

            return report;
        }

        // ===== CTB 导出 =====

        public CtbExportReport Export(StandardConfig config)
        {
            CtbExportReport report = new()
            {
                CtbName = config.Ctb,
                RuleCount = config.CtbRules?.Count ?? 0
            };

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exportDir = Path.Combine(baseDir, "exports");
                report.ExportDirectory = exportDir;

                Directory.CreateDirectory(exportDir);

                var sortedRules = (config.CtbRules ?? new List<CtbRuleConfig>())
                    .OrderBy(r => r.Color)
                    .Select(r => BuildEditorRow(r))
                    .ToList();

                string mdPath = Path.Combine(exportDir, "BS_CAD_STANDARD_CTB_RULES.md");
                ExportMarkdown(mdPath, config, sortedRules);
                report.MarkdownPath = mdPath;

                string csvPath = Path.Combine(exportDir, "BS_CAD_STANDARD_CTB_RULES.csv");
                ExportCsv(csvPath, sortedRules);
                report.CsvPath = csvPath;
            }
            catch (Exception ex)
            {
                report.Success = false;
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        // ===== 导出帮助方法 =====

        private static CtbEditorRow BuildEditorRow(CtbRuleConfig rule)
        {
            CtbEditorRow row = new()
            {
                Color = rule.Color,
                Objects = rule.Objects,
                Note = rule.Note
            };

            if (!string.IsNullOrWhiteSpace(rule.EditorColor))
                row.EditorColor = rule.EditorColor;
            if (!string.IsNullOrWhiteSpace(rule.Dither))
                row.Dither = rule.Dither;
            if (!string.IsNullOrWhiteSpace(rule.Grayscale))
                row.Grayscale = rule.Grayscale;
            if (!string.IsNullOrWhiteSpace(rule.PenNumber))
                row.PenNumber = rule.PenNumber;
            if (!string.IsNullOrWhiteSpace(rule.VirtualPen))
                row.VirtualPen = rule.VirtualPen;
            if (rule.Screening.HasValue)
                row.Screening = rule.Screening.Value;
            if (!string.IsNullOrWhiteSpace(rule.Linetype))
                row.Linetype = rule.Linetype;
            if (!string.IsNullOrWhiteSpace(rule.Adaptive))
                row.Adaptive = rule.Adaptive;
            if (!string.IsNullOrWhiteSpace(rule.EndStyle))
                row.EndStyle = rule.EndStyle;
            if (!string.IsNullOrWhiteSpace(rule.JoinStyle))
                row.JoinStyle = rule.JoinStyle;
            if (!string.IsNullOrWhiteSpace(rule.FillStyle))
                row.FillStyle = rule.FillStyle;

            if (!string.IsNullOrWhiteSpace(rule.PlotLineweight))
            {
                string lw = rule.PlotLineweight.Trim();
                if (lw.StartsWith("0.") || lw.StartsWith("0,"))
                    row.Lineweight = lw;
            }

            ApplyColorFallback(rule.Color, rule.PlotColor, row);

            return row;
        }

        private static void ApplyColorFallback(int color, string? plotColor, CtbEditorRow row)
        {
            switch (color)
            {
                case 7:
                    row.EditorColor = "Black";
                    row.Screening = 100;
                    SetLineweight(row, "0.25mm");
                    break;

                case 8:
                    row.EditorColor = "Black";
                    row.Screening = 45;
                    SetLineweight(row, "0.13mm");
                    break;

                case 9:
                    row.EditorColor = "Black";
                    row.Screening = 30;
                    SetLineweight(row, "0.09mm");
                    break;

                case 250:
                    row.EditorColor = "Black";
                    row.Screening = 25;
                    SetLineweight(row, "0.09mm");
                    break;

                case 30:
                    row.EditorColor = "Use object color";
                    row.Screening = 100;
                    SetLineweight(row, "0.18mm");
                    break;

                case 94:
                case 140:
                case 160:
                case 180:
                    row.EditorColor = "Use object color";
                    row.Screening = 100;
                    SetLineweight(row, "0.18mm");
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(plotColor))
                    {
                        if (plotColor.Contains("黑") || plotColor.Contains("深灰"))
                        {
                            row.EditorColor = "Black";
                            row.Screening = 100;
                        }
                        else if (plotColor.Contains("淡") || plotColor.Contains("浅灰"))
                        {
                            row.EditorColor = "Black";
                            row.Screening = 30;
                        }
                    }
                    break;
            }
        }

        private static void SetLineweight(CtbEditorRow row, string lw)
        {
            if (row.Lineweight == "0.18mm" || string.IsNullOrWhiteSpace(row.Lineweight))
                row.Lineweight = lw;
        }

        private static void ExportMarkdown(string path, StandardConfig config, List<CtbEditorRow> rows)
        {
            using StreamWriter w = new(path, false, Encoding.UTF8);

            w.WriteLine("# BS_CAD_STANDARD.ctb 打印样式表编辑器设置表");
            w.WriteLine();
            w.WriteLine($"Standard: {config.StandardName}");
            w.WriteLine($"Version: {config.Version}");
            w.WriteLine($"CTB: {config.Ctb}");
            w.WriteLine($"Rule count: {rows.Count}");
            w.WriteLine();
            w.WriteLine("> This file is exported from standard config.");
            w.WriteLine("> It is a documentation/export file only. It does not generate an AutoCAD `.ctb` file.");
            w.WriteLine("> 本文件由 JSON ctbRules 生成，实际 CTB 修改后请同步更新 JSON。");
            w.WriteLine("> [INFO] 当前仅校验 CTB 文件名，不校验 CTB 内部规则。");
            w.WriteLine("> Format: CTB editor fields");
            w.WriteLine();

            w.WriteLine("| Color | EditorColor | Dither | Grayscale | PenNumber | VirtualPen | Screening | Linetype | Adaptive | Lineweight | EndStyle | JoinStyle | FillStyle | Objects | Note |");
            w.WriteLine("|---:|---|---|---|---|---|---:|---|---|---|---|---|---|---|");

            foreach (CtbEditorRow r in rows)
            {
                w.WriteLine(
                    $"| {r.Color} " +
                    $"| {MdEscape(r.EditorColor)} " +
                    $"| {MdEscape(r.Dither)} " +
                    $"| {MdEscape(r.Grayscale)} " +
                    $"| {MdEscape(r.PenNumber)} " +
                    $"| {MdEscape(r.VirtualPen)} " +
                    $"| {r.Screening} " +
                    $"| {MdEscape(r.Linetype)} " +
                    $"| {MdEscape(r.Adaptive)} " +
                    $"| {MdEscape(r.Lineweight)} " +
                    $"| {MdEscape(r.EndStyle)} " +
                    $"| {MdEscape(r.JoinStyle)} " +
                    $"| {MdEscape(r.FillStyle)} " +
                    $"| {MdEscape(r.Objects)} " +
                    $"| {MdEscape(r.Note)} |");
            }
        }

        private static void ExportCsv(string path, List<CtbEditorRow> rows)
        {
            using StreamWriter w = new(path, false, new UTF8Encoding(true));

            w.WriteLine("Color,EditorColor,Dither,Grayscale,PenNumber,VirtualPen,Screening,Linetype,Adaptive,Lineweight,EndStyle,JoinStyle,FillStyle,Objects,Note");

            foreach (CtbEditorRow r in rows)
            {
                w.WriteLine(
                    $"{r.Color}," +
                    $"{CsvEscape(r.EditorColor)}," +
                    $"{CsvEscape(r.Dither)}," +
                    $"{CsvEscape(r.Grayscale)}," +
                    $"{CsvEscape(r.PenNumber)}," +
                    $"{CsvEscape(r.VirtualPen)}," +
                    $"{r.Screening}," +
                    $"{CsvEscape(r.Linetype)}," +
                    $"{CsvEscape(r.Adaptive)}," +
                    $"{CsvEscape(r.Lineweight)}," +
                    $"{CsvEscape(r.EndStyle)}," +
                    $"{CsvEscape(r.JoinStyle)}," +
                    $"{CsvEscape(r.FillStyle)}," +
                    $"{CsvEscape(r.Objects)}," +
                    $"{CsvEscape(r.Note)}");
            }
        }

        private static string MdEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("|", "\\|");
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            bool needsQuotes = value.Contains(',') || value.Contains('"') ||
                               value.Contains('\r') || value.Contains('\n');

            if (!needsQuotes) return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
