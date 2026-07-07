using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_1_0_Plugin.Utils;
using System.Linq;

namespace BS_CAD_STANDARD_1_0_Plugin.Commands
{
    public class LayerModeCommands
    {
        [CommandMethod("BS_LAYER_MODE")]
        public void BsLayerMode()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                StandardConfig config = context.StandardConfig;

                if (config.LoadModes == null || config.LoadModes.Count == 0)
                {
                    ed.WriteMessage("\nNo layer modes found in config.");
                    return;
                }

                // Display header
                ed.WriteMessage("\n===== BS_LAYER_MODE =====");
                ed.WriteMessage($"\n\nLoaded CAD standard config: {GetConfigFileName(context.StandardConfigPath)}");
                ed.WriteMessage($"\nStandard name: {config.StandardName}");
                ed.WriteMessage($"\nVersion: {config.Version}");
                ed.WriteMessage($"\n\nAvailable layer modes:\n");

                foreach (LoadModeConfig mode in config.LoadModes)
                {
                    string usage = !string.IsNullOrWhiteSpace(mode.Usage) ? $" - {mode.Usage}" : "";
                    ed.WriteMessage($"\n[{mode.Id}] {mode.Name}{usage}");
                }

                // Get user input
                ed.WriteMessage($"\n");
                var opt = new Autodesk.AutoCAD.EditorInput.PromptStringOptions("\nEnter mode id: ")
                {
                    AllowSpaces = false
                };
                var res = ed.GetString(opt);
                if (res.Status != PromptStatus.OK) return;

                string input = res.StringResult.Trim();
                if (string.IsNullOrEmpty(input)) return;

                // Match mode
                LoadModeConfig? matchedMode = MatchMode(config.LoadModes, input);
                if (matchedMode == null)
                {
                    ed.WriteMessage($"\nInvalid mode id: {input}");
                    return;
                }

                // Apply mode
                LayerModeReport report = LayerModeService.ApplyMode(config, matchedMode);

                // Output result
                PrintModeResult(ed, report);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_MODE 执行失败", ex);
            }
        }

        [CommandMethod("BS_LAYER_ALL")]
        public void BsLayerAll()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                LayerModeReport report = LayerModeService.ShowAllLayers();

                if (report.Success)
                {
                    ed.WriteMessage($"\n===== BS_LAYER_ALL Result =====");
                    ed.WriteMessage($"\nRestored layers: {report.RestoredCount}");
                    ed.WriteMessage($"\nBS_LAYER_ALL completed.\n");
                }
                else
                {
                    ed.WriteMessage($"\nBS_LAYER_ALL failed.");
                    ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_ALL 执行失败", ex);
            }
        }

        private static LoadModeConfig? MatchMode(System.Collections.Generic.List<LoadModeConfig> modes, string input)
        {
            // Priority 1: Exact match by Id
            LoadModeConfig? exact = modes.FirstOrDefault(m =>
                string.Equals(m.Id, input, System.StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // Priority 2: Normalize single digit to two-digit (e.g. "1" → "01")
            if (int.TryParse(input, out int num) && num >= 0 && num <= 99)
            {
                string padded = num.ToString("D2");
                LoadModeConfig? byPad = modes.FirstOrDefault(m =>
                    string.Equals(m.Id, padded, System.StringComparison.OrdinalIgnoreCase));
                if (byPad != null) return byPad;
            }

            // Priority 3: Match by name (case-insensitive)
            return modes.FirstOrDefault(m =>
                string.Equals(m.Name, input, System.StringComparison.OrdinalIgnoreCase));
        }

        private static void PrintModeResult(Editor ed, LayerModeReport report)
        {
            if (!report.Success)
            {
                ed.WriteMessage($"\n===== BS_LAYER_MODE Result =====");
                ed.WriteMessage($"\nBS_LAYER_MODE failed.");
                ed.WriteMessage($"\nError: {report.ErrorMessage}\n");
                return;
            }

            ed.WriteMessage($"\n===== BS_LAYER_MODE Result =====");
            ed.WriteMessage($"\n\nMode: [{report.ModeId}] {report.ModeName}");
            ed.WriteMessage($"\nExpected visible layers: {report.ExpectedVisibleCount}");
            ed.WriteMessage($"\nActual visible layers: {report.ActualVisibleCount}");
            ed.WriteMessage($"\nHidden layers: {report.HiddenCount}");
            ed.WriteMessage($"\nMissing layers: {report.MissingCount}");

            if (report.MissingLayers.Count > 0)
            {
                ed.WriteMessage($"\n\n[Missing]");
                foreach (string name in report.MissingLayers)
                {
                    ed.WriteMessage($"\n  - {name}");
                }
                ed.WriteMessage($"\n\nRun BS_FIX_MISSING first, then retry BS_LAYER_MODE.");
            }

            ed.WriteMessage($"\n\nBS_LAYER_MODE completed.\n");
        }

        private static string GetConfigFileName(string path)
        {
            int index = path.LastIndexOf("\\config\\", System.StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + 1) : path;
        }
    }
}
