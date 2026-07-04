using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class FixCommands
    {
        [CommandMethod("BS_FIX_LAYER")]
        public void BsFixLayer()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                ed.WriteMessage("\n===== BS_FIX_LAYER Report =====");

                // Config info
                ed.WriteMessage($"\n\n[Config]");
                ed.WriteMessage($"\nLoaded CAD standard config: {GetConfigFileName(context.StandardConfigPath)}");
                ed.WriteMessage($"\nStandard name: {config.StandardName}");
                ed.WriteMessage($"\nVersion: {config.Version}");
                ed.WriteMessage($"\nStandard layer count: {config.Layers.Count}");

                // Run fix
                FixReport report = LayerFixService.RunFix(config);

                // Fixed
                ed.WriteMessage($"\n\n[Fixed]");
                ed.WriteMessage($"\nFixed color count: {report.FixedColorCount}");
                ed.WriteMessage($"\nFixed linetype count: {report.FixedLinetypeCount}");
                ed.WriteMessage($"\nFixed transparency count: {report.FixedTransparencyCount}");
                ed.WriteMessage($"\nFixed plot count: {report.FixedPlotCount}");
                ed.WriteMessage($"\nFixed locked count: {report.FixedLockedCount}");
                ed.WriteMessage($"\nTotal fixed: {report.TotalFixed}");

                // Missing standard layers
                ed.WriteMessage($"\n\n[Skipped]");
                ed.WriteMessage($"\nMissing standard layers: {report.MissingStandardLayers.Count}");
                foreach (string name in report.MissingStandardLayers)
                {
                    ed.WriteMessage($"\n  - {name}");
                }
                if (report.MissingStandardLayers.Count > 0)
                {
                    ed.WriteMessage($"\n  Run BS_LAYER to create missing layers.");
                }

                // Non-standard layers
                ed.WriteMessage($"\n\nNon-standard layers: {report.NonStandardLayers.Count}");
                foreach (string name in report.NonStandardLayers)
                {
                    ed.WriteMessage($"\n  - {name}");
                }

                // VPF — skipped (AutoCAD 2027 managed API not supported)
                ed.WriteMessage($"\n\nNew viewport freeze: skipped, AutoCAD 2027 managed API not supported.");

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
                    ed.WriteMessage($"\nBS_FIX_LAYER completed.\n");
                }
                else
                {
                    ed.WriteMessage($"\nBS_FIX_LAYER failed.");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_FIX_LAYER 执行失败", ex);
            }
        }

        private static string GetConfigFileName(string path)
        {
            int index = path.LastIndexOf("\\config\\", System.StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + 1) : path;
        }
    }
}
