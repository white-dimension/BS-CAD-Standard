using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class CheckCommands
    {
        [CommandMethod("BS_CHECK")]
        public void BS_Check()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                CheckResult result = CheckEngine.RunFullCheck(context.StandardConfig);
                PrintReport(ed, context, result);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_CHECK failed", ex);
            }
        }

        private void PrintReport(Editor ed, StandardContext context, CheckResult result)
        {
            StandardConfig config = context.StandardConfig;

            ed.WriteMessage("\n===== BS_CHECK - CAD Standard Layer Report =====");

            ed.WriteMessage("\n\n[Config]");
            ed.WriteMessage($"\nLoaded CAD standard config: {context.StandardConfigPath}");
            ed.WriteMessage($"\nStandard name: {config.StandardName}");
            ed.WriteMessage($"\nVersion: {config.Version}");
            ed.WriteMessage($"\nStandard layer count: {config.Layers.Count}");
            ed.WriteMessage($"\nCategory count: {config.Layers.Select(l => l.Category).Distinct().Count()}");

            ed.WriteMessage("\n\n[Layer Check]");
            PrintList(ed, "Non-standard layers", result.ExtraLayers);
            PrintList(ed, "Missing standard layers", result.MissingCoreLayers);
            PrintList(ed, "Color mismatches", result.ColorDeviations);
            PrintList(ed, "Linetype mismatches", result.LinetypeDeviations);
            PrintList(ed, "Transparency mismatches", result.TransparencyDeviations);
            PrintList(ed, "Plot mismatches", result.PlotDeviations);

            int layerIssueCount =
                result.ExtraLayers.Count +
                result.MissingCoreLayers.Count +
                result.ColorDeviations.Count +
                result.LinetypeDeviations.Count +
                result.TransparencyDeviations.Count +
                result.PlotDeviations.Count;
            ed.WriteMessage($"\n\nLayer issue total: {layerIssueCount}");

            PrintLegacyChecks(ed, config, result);
            ed.WriteMessage("\n\n==============================================");
        }

        private static void PrintLegacyChecks(Editor ed, StandardConfig config, CheckResult result)
        {
            List<string> textTargets = config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0
                ? config.Styles.TextStyles
                : StandardDefaults.TextStyles;

            ed.WriteMessage("\n\n[Text Styles]");
            ed.WriteMessage($"\nStandard: {textTargets.Count}");
            ed.WriteMessage($"\nExisting: {textTargets.Count - result.MissingTextStyles.Count}");
            PrintList(ed, "Missing", result.MissingTextStyles);
            PrintList(ed, "Font mismatches", result.TextStyleFontDeviations);

            List<string> dimTargets = config.Styles.DimStyles != null && config.Styles.DimStyles.Count > 0
                ? config.Styles.DimStyles
                : StandardDefaults.DimStyles;

            ed.WriteMessage("\n\n[Dim Styles]");
            ed.WriteMessage($"\nStandard: {dimTargets.Count}");
            ed.WriteMessage($"\nExisting: {dimTargets.Count - result.MissingDimStyles.Count}");
            PrintList(ed, "Missing", result.MissingDimStyles);

            ed.WriteMessage("\n\n[MLeader Style]");
            ed.WriteMessage($"\n{StandardDefaults.MLeaderStyleNote}: {(result.MLeaderStyleExists ? "exists" : "missing")}");

            ed.WriteMessage("\n\n[Units]");
            ed.WriteMessage($"\nINSUNITS = {result.CurrentUnits}");
            ed.WriteMessage($"\nResult: {(result.CurrentUnits == StandardDefaults.StandardUnits ? "OK" : "should be 4 (millimeters)")}");

            ed.WriteMessage("\n\n[Plot Style]");
            ed.WriteMessage($"\nCurrent layout: {result.CurrentLayoutName}");
            ed.WriteMessage($"\nCurrent CTB: {(string.IsNullOrEmpty(result.CurrentCtb) ? "none" : result.CurrentCtb)}");
            ed.WriteMessage($"\nStandard CTB: {StandardPaths.CtbFileName}");
            ed.WriteMessage($"\nResult: {(string.Equals(result.CurrentCtb, StandardPaths.CtbFileName, StringComparison.OrdinalIgnoreCase) ? "OK" : "mismatch")}");
        }

        private static void PrintList(Editor ed, string title, List<string> items)
        {
            ed.WriteMessage($"\n{title}: {items.Count}");
            foreach (string item in items)
            {
                ed.WriteMessage($"\n  - {item}");
            }
        }
    }
}
