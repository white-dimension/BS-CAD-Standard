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
                // Determine export directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exportDir = Path.Combine(baseDir, "exports");
                report.ExportDirectory = exportDir;

                // Create directory if needed
                Directory.CreateDirectory(exportDir);

                // Sort rules by color
                var sortedRules = (config.CtbRules ?? new List<CtbRuleConfig>())
                    .OrderBy(r => r.Color)
                    .ToList();

                // Export Markdown
                string mdPath = Path.Combine(exportDir, "BS_CAD_STANDARD_CTB_RULES.md");
                ExportMarkdown(mdPath, config, sortedRules);
                report.MarkdownPath = mdPath;

                // Export CSV
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

        private static void ExportMarkdown(string path, StandardConfig config, List<CtbRuleConfig> rules)
        {
            using StreamWriter w = new(path, false, Encoding.UTF8);

            w.WriteLine("# BS CAD Standard CTB Rules");
            w.WriteLine();
            w.WriteLine($"Standard: {config.StandardName}");
            w.WriteLine($"Version: {config.Version}");
            w.WriteLine($"CTB: {config.Ctb}");
            w.WriteLine($"Rule count: {rules.Count}");
            w.WriteLine();
            w.WriteLine("> This file is exported from `config/BS_CAD_Standard_v0.6.json`.");
            w.WriteLine("> It is a documentation/export file only. It does not generate an AutoCAD `.ctb` file.");
            w.WriteLine();

            // Table header
            w.WriteLine("| Color | Preview | Screen Use | Plot Color | Plot Lineweight | Objects | Note |");
            w.WriteLine("|---:|---|---|---|---|---|---|");

            foreach (CtbRuleConfig rule in rules)
            {
                w.WriteLine(
                    $"| {rule.Color} " +
                    $"| {MdEscape(rule.Preview)} " +
                    $"| {MdEscape(rule.ScreenUse)} " +
                    $"| {MdEscape(rule.PlotColor)} " +
                    $"| {MdEscape(rule.PlotLineweight)} " +
                    $"| {MdEscape(rule.Objects)} " +
                    $"| {MdEscape(rule.Note)} |");
            }
        }

        private static void ExportCsv(string path, List<CtbRuleConfig> rules)
        {
            // UTF-8 with BOM for Excel
            using StreamWriter w = new(path, false, new UTF8Encoding(true));

            // Header
            w.WriteLine("Color,Preview,ScreenUse,PlotColor,PlotLineweight,Objects,Note");

            foreach (CtbRuleConfig rule in rules)
            {
                w.WriteLine(
                    $"{rule.Color}," +
                    $"{CsvEscape(rule.Preview)}," +
                    $"{CsvEscape(rule.ScreenUse)}," +
                    $"{CsvEscape(rule.PlotColor)}," +
                    $"{CsvEscape(rule.PlotLineweight)}," +
                    $"{CsvEscape(rule.Objects)}," +
                    $"{CsvEscape(rule.Note)}");
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
