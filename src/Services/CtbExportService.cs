using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BS_CAD_STANDARD_V10_Plugin.Core;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class CtbExportReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public string CtbName { get; set; } = string.Empty;
        public int RuleCount { get; set; }

        public string ExportDirectory { get; set; } = string.Empty;
        public string MarkdownPath { get; set; } = string.Empty;
        public string CsvPath { get; set; } = string.Empty;

        public List<string> Warnings { get; set; } = new();
    }

    public class CtbEditorRow
    {
        public int Color { get; set; }
        public string EditorColor { get; set; } = "Use object color";
        public string Dither { get; set; } = "On";
        public string Grayscale { get; set; } = "Off";
        public string PenNumber { get; set; } = "Automatic";
        public string VirtualPen { get; set; } = "Automatic";
        public int Screening { get; set; } = 100;
        public string Linetype { get; set; } = "Use object linetype";
        public string Adaptive { get; set; } = "On";
        public string Lineweight { get; set; } = "0.18mm";
        public string EndStyle { get; set; } = "Use object end style";
        public string JoinStyle { get; set; } = "Use object join style";
        public string FillStyle { get; set; } = "Use object fill style";
        public string Objects { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public static class CtbExportService
    {
        public static CtbExportReport ExportRules(StandardConfig config)
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

        private static CtbEditorRow BuildEditorRow(CtbRuleConfig rule)
        {
            CtbEditorRow row = new()
            {
                Color = rule.Color,
                Objects = rule.Objects,
                Note = rule.Note
            };

            // Priority: JSON field > fallback by color
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

            // Fallback by plotLineweight
            if (!string.IsNullOrWhiteSpace(rule.PlotLineweight))
            {
                string lw = rule.PlotLineweight.Trim();
                if (lw.StartsWith("0.") || lw.StartsWith("0,"))
                    row.Lineweight = lw;
            }

            // Fallback by color number
            ApplyColorFallback(rule.Color, rule.PlotColor, row);

            return row;
        }

        private static void ApplyColorFallback(int color, string? plotColor, CtbEditorRow row)
        {
            switch (color)
            {
                case 7:
                    // 主体亮灰 → 打印黑
                    row.EditorColor = "Black";
                    row.Screening = 100;
                    SetLineweight(row, "0.25mm");
                    break;

                case 8:
                    // 弱化灰 → 打印浅灰
                    row.EditorColor = "Black";
                    row.Screening = 45;
                    SetLineweight(row, "0.13mm");
                    break;

                case 9:
                    // 参照灰 → 打印淡灰
                    row.EditorColor = "Black";
                    row.Screening = 30;
                    SetLineweight(row, "0.09mm");
                    break;

                case 250:
                    // 淡灰 → 打印极淡
                    row.EditorColor = "Black";
                    row.Screening = 25;
                    SetLineweight(row, "0.09mm");
                    break;

                case 30:
                    // 橙色系 — 保留颜色
                    row.EditorColor = "Use object color";
                    row.Screening = 100;
                    SetLineweight(row, "0.18mm");
                    break;

                case 94:
                case 140:
                case 160:
                case 180:
                    // 保留色类
                    row.EditorColor = "Use object color";
                    row.Screening = 100;
                    SetLineweight(row, "0.18mm");
                    break;

                default:
                    // 其他 — 检查 plotColor 是否含灰阶暗示
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
            // Only set if no explicit plotLineweight was already applied
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
            w.WriteLine("> This file is exported from `config/BS_CAD_Standard_v0.6.json`.");
            w.WriteLine("> It is a documentation/export file only. It does not generate an AutoCAD `.ctb` file.");
            w.WriteLine("> 本文件由 JSON ctbRules 生成，实际 CTB 修改后请同步更新 JSON。");
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
