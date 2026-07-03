using System;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class CtbCommands
    {
        [CommandMethod("BS_CTB_CHECK")]
        public void BsCtbCheck()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                ed.WriteMessage("\n===== BS_CTB_CHECK Report =====");

                // Config
                ed.WriteMessage($"\n\n[Config]");
                ed.WriteMessage($"\nLoaded CAD standard config: {GetConfigFileName(context.StandardConfigPath)}");
                ed.WriteMessage($"\nStandard name: {config.StandardName}");
                ed.WriteMessage($"\nVersion: {config.Version}");
                ed.WriteMessage($"\nCTB name: {config.Ctb}");
                ed.WriteMessage($"\nCTB rule colors: {config.CtbRules?.Count ?? 0}");

                // Run check
                CtbCheckReport report = CtbCheckService.RunCheck(config);

                // Summary
                ed.WriteMessage($"\n\n[Summary]");
                ed.WriteMessage($"\nStandard layers: {report.StandardLayerCount}");
                ed.WriteMessage($"\nExisting standard layers: {report.ExistingStandardLayerCount}");
                ed.WriteMessage($"\nMissing standard layers: {report.MissingStandardLayerCount}");
                ed.WriteMessage($"\nColor mismatches: {report.ColorMismatchCount}");
                ed.WriteMessage($"\nInvalid CTB colors on standard layers: {report.InvalidLayerColorCount}");
                ed.WriteMessage($"\nNon-standard layers: {report.NonStandardLayerCount}");
                ed.WriteMessage($"\nInvalid CTB colors on non-standard layers: {report.NonStandardLayerInvalidColorCount}");

                // CTB rule colors
                if (config.CtbRules != null && config.CtbRules.Count > 0)
                {
                    ed.WriteMessage($"\n\n[CTB Rule Colors]");
                    ed.WriteMessage($"\n{string.Join(", ", config.CtbRules.Select(r => r.Color))}");
                }

                // Color mismatches
                ed.WriteMessage($"\n\n[Color Mismatches]");
                if (report.ColorMismatchCount > 0)
                {
                    foreach (string m in report.ColorMismatches)
                        ed.WriteMessage($"\n  - {m}");
                }
                else
                {
                    ed.WriteMessage($"\nNone");
                }

                // Invalid CTB colors - standard
                ed.WriteMessage($"\n\n[Invalid CTB Colors - Standard Layers]");
                if (report.InvalidLayerColorCount > 0)
                {
                    foreach (string m in report.InvalidCtbColors)
                        ed.WriteMessage($"\n  - {m}");
                }
                else
                {
                    ed.WriteMessage($"\nNone");
                }

                // Non-standard layers
                ed.WriteMessage($"\n\n[Non-standard Layers]");
                if (report.NonStandardLayerCount > 0)
                {
                    foreach (string m in report.NonStandardLayers)
                        ed.WriteMessage($"\n  - {m}");
                }
                else
                {
                    ed.WriteMessage($"\nNone");
                }

                // Invalid CTB colors - non-standard
                ed.WriteMessage($"\n\n[Invalid CTB Colors - Non-standard Layers]");
                if (report.NonStandardLayerInvalidColorCount > 0)
                {
                    foreach (string m in report.NonStandardLayerInvalidColors)
                        ed.WriteMessage($"\n  - {m}");
                }
                else
                {
                    ed.WriteMessage($"\nNone");
                }

                // Result
                ed.WriteMessage($"\n\n[Result]");
                if (report.Success)
                {
                    ed.WriteMessage($"\nBS_CTB_CHECK completed.\n");
                }
                else
                {
                    ed.WriteMessage($"\nBS_CTB_CHECK failed.");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_CTB_CHECK 执行失败", ex);
            }
        }

        [CommandMethod("BS_CTB_EXPORT")]
        public void BsCtbExport()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                if (config.CtbRules == null || config.CtbRules.Count == 0)
                {
                    ed.WriteMessage("\nNo CTB rules found in config.");
                    return;
                }

                ed.WriteMessage("\n===== BS_CTB_EXPORT Report =====");

                // Config
                ed.WriteMessage($"\n\n[Config]");
                ed.WriteMessage($"\nLoaded CAD standard config: {GetConfigFileName(context.StandardConfigPath)}");
                ed.WriteMessage($"\nStandard name: {config.StandardName}");
                ed.WriteMessage($"\nVersion: {config.Version}");
                ed.WriteMessage($"\nCTB name: {config.Ctb}");

                // Export
                CtbExportReport report = CtbExportService.ExportRules(config);

                if (report.Success)
                {
                    ed.WriteMessage($"\n\n[Export]");
                    ed.WriteMessage($"\nRules exported: {report.RuleCount}");
                    ed.WriteMessage($"\nFormat: CTB editor fields");
                    ed.WriteMessage($"\nMarkdown: {report.MarkdownPath}");
                    ed.WriteMessage($"\nCSV: {report.CsvPath}");
                    ed.WriteMessage($"\n\n[Result]");
                    ed.WriteMessage($"\nBS_CTB_EXPORT completed.\n");
                }
                else
                {
                    ed.WriteMessage($"\n\n[Result]");
                    ed.WriteMessage($"\nBS_CTB_EXPORT failed.");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_CTB_EXPORT 执行失败", ex);
            }
        }

        private static string GetConfigFileName(string path)
        {
            int index = path.LastIndexOf("\\config\\", System.StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + 1) : path;
        }
    }
}
