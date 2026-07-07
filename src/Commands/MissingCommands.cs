using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Commands
{
    public class MissingCommands
    {
        [CommandMethod("BS_FIX_MISSING")]
        public void BsFixMissing()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                ed.WriteMessage("\n===== BS_FIX_MISSING Report =====");

                // Config
                ed.WriteMessage($"\n\n[Config]");
                ed.WriteMessage($"\nLoaded CAD standard config: {GetConfigFileName(context.StandardConfigPath)}");
                ed.WriteMessage($"\nStandard name: {config.StandardName}");
                ed.WriteMessage($"\nVersion: {config.Version}");
                ed.WriteMessage($"\nStandard layer count: {config.Layers.Count}");

                // Run
                MissingReport report = LayerMissingService.RunCreateMissing(config);

                // Created
                ed.WriteMessage($"\n\n[Created]");
                ed.WriteMessage($"\nCreated missing standard layers: {report.CreatedCount}");
                foreach (string name in report.CreatedLayers)
                {
                    ed.WriteMessage($"\n  - {name}");
                }

                // Skipped
                ed.WriteMessage($"\n\n[Skipped]");
                ed.WriteMessage($"\nExisting standard layers: {report.ExistingCount}");
                ed.WriteMessage($"\nNew viewport freeze: skipped.");

                // Non-standard
                ed.WriteMessage($"\n\nNon-standard layers: {report.NonStandardCount}");
                foreach (string name in report.NonStandardLayers)
                {
                    ed.WriteMessage($"\n  - {name}");
                }

                // Warnings
                if (report.Warnings.Count > 0)
                {
                    ed.WriteMessage($"\n\n[Warnings]");
                    foreach (string w in report.Warnings)
                    {
                        ed.WriteMessage($"\n  - {w}");
                    }
                }

                // Result
                ed.WriteMessage($"\n\n[Result]");
                if (report.Success)
                {
                    ed.WriteMessage($"\nBS_FIX_MISSING completed.\n");
                }
                else
                {
                    ed.WriteMessage($"\nBS_FIX_MISSING failed.");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_FIX_MISSING 执行失败", ex);
            }
        }

        private static string GetConfigFileName(string path)
        {
            int index = path.LastIndexOf("\\config\\", System.StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + 1) : path;
        }
    }
}
