using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class FixReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public int FixedColorCount { get; set; }
        public int FixedLinetypeCount { get; set; }
        public int FixedTransparencyCount { get; set; }
        public int FixedPlotCount { get; set; }
        public int FixedLockedCount { get; set; }
        public int TotalFixed => FixedColorCount + FixedLinetypeCount + FixedTransparencyCount + FixedPlotCount + FixedLockedCount;

        public List<string> MissingStandardLayers { get; set; } = new();
        public List<string> NonStandardLayers { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class LayerFixService
    {
        public static FixReport RunFix(StandardConfig config)
        {
            FixReport report = new();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    // 1. Fix existing standard layers
                    foreach (LayerConfig layerConfig in config.Layers)
                    {
                        if (!layerTable.Has(layerConfig.Name))
                        {
                            report.MissingStandardLayers.Add(layerConfig.Name);
                            continue;
                        }

                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(
                            layerTable[layerConfig.Name], OpenMode.ForRead);

                        FixSingleLayer(tr, layerRecord, layerConfig, report, ed);
                    }

                    // 2. Detect non-standard layers
                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        string name = layerRecord.Name;
                        if (IsExcludedLayer(name)) continue;
                        if (!standardLayerNames.Contains(name))
                        {
                            report.NonStandardLayers.Add(name);
                        }
                    }

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_FIX_LAYER failed: {ex.Message}");
                }
            }

            return report;
        }

        private static void FixSingleLayer(Transaction tr, LayerTableRecord layerRecord,
            LayerConfig config, FixReport report, Editor ed)
        {
            // Color
            // Linetype
            string currentLinetype = GetLinetypeName(tr, layerRecord);
            bool linetypeMismatch = !string.Equals(currentLinetype, config.Linetype, StringComparison.OrdinalIgnoreCase);

            // Transparency
            int currentTransparency = GetTransparencyPercent(layerRecord);
            bool transparencyMismatch = currentTransparency != config.Transparency;

            // Plot
            bool plotMismatch = layerRecord.IsPlottable != config.Plot;

            // Locked
            bool lockedMismatch = layerRecord.IsLocked != config.Locked;

            bool hasFix = (linetypeMismatch || transparencyMismatch || plotMismatch || lockedMismatch ||
                           layerRecord.Color.ColorIndex != config.Color);

            if (!hasFix) return;

            // Upgrade to write
            try
            {
                // If layer is locked, unlock temporarily to allow attribute changes
                if (layerRecord.IsLocked)
                {
                    layerRecord.UpgradeOpen();
                    layerRecord.IsLocked = false;
                    layerRecord.DowngradeOpen();
                }

                layerRecord.UpgradeOpen();

                // Fix color
                if (layerRecord.Color.ColorIndex != config.Color)
                {
                    layerRecord.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)config.Color);
                    report.FixedColorCount++;
                }

                // Fix linetype
                if (linetypeMismatch)
                {
                    ObjectId linetypeId = ResolveLinetypeId(tr, config.Linetype, ed);
                    if (linetypeId != ObjectId.Null)
                    {
                        layerRecord.LinetypeObjectId = linetypeId;
                        report.FixedLinetypeCount++;
                    }
                }

                // Fix transparency
                if (transparencyMismatch)
                {
                    SetTransparencyPercent(layerRecord, config.Transparency);
                    report.FixedTransparencyCount++;
                }

                // Fix plot
                if (plotMismatch)
                {
                    layerRecord.IsPlottable = config.Plot;
                    report.FixedPlotCount++;
                }

                // Fix locked
                if (lockedMismatch)
                {
                    layerRecord.IsLocked = config.Locked;
                    report.FixedLockedCount++;
                }

                layerRecord.DowngradeOpen();
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n[Warning] Failed to fix layer [{layerRecord.Name}]: {ex.Message}");
                report.Warnings.Add($"Fix failed for [{layerRecord.Name}]: {ex.Message}");
            }
        }

        private static ObjectId ResolveLinetypeId(Transaction tr, string linetypeName, Editor ed)
        {
            if (string.Equals(linetypeName, "Continuous", StringComparison.OrdinalIgnoreCase))
            {
                return HostApplicationServices.WorkingDatabase.ContinuousLinetype;
            }

            LinetypeTable lt = (LinetypeTable)tr.GetObject(
                HostApplicationServices.WorkingDatabase.LinetypeTableId, OpenMode.ForRead);

            if (lt.Has(linetypeName))
            {
                return lt[linetypeName];
            }

            // Try to load
            try
            {
                lt.UpgradeOpen();
                HostApplicationServices.WorkingDatabase.LoadLineTypeFile(linetypeName, "acadiso.lin");
                lt.DowngradeOpen();

                if (lt.Has(linetypeName))
                {
                    return lt[linetypeName];
                }
            }
            catch
            {
                try
                {
                    HostApplicationServices.WorkingDatabase.LoadLineTypeFile(linetypeName, "acad.lin");
                    if (lt.Has(linetypeName))
                    {
                        return lt[linetypeName];
                    }
                }
                catch
                {
                    ed.WriteMessage($"\n[Warning] Cannot load linetype '{linetypeName}' for layer fix.");
                }
            }

            return ObjectId.Null;
        }

        private static string GetLinetypeName(Transaction tr, LayerTableRecord layerRecord)
        {
            try
            {
                LinetypeTableRecord ltr = (LinetypeTableRecord)tr.GetObject(
                    layerRecord.LinetypeObjectId, OpenMode.ForRead);
                return ltr.Name;
            }
            catch
            {
                return "Continuous";
            }
        }

        private static int GetTransparencyPercent(LayerTableRecord layerRecord)
        {
            try
            {
                byte alpha = layerRecord.Transparency.Alpha;
                int percent = 100 - (int)Math.Round(alpha * 100.0 / 255.0);
                if (percent < 0) return 0;
                if (percent > 90) return 90;
                return percent;
            }
            catch
            {
                return 0;
            }
        }

        private static void SetTransparencyPercent(LayerTableRecord layerRecord, int percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 90) percent = 90;
            byte alpha = (byte)(255 - (byte)Math.Round(percent * 255.0 / 100.0));
            layerRecord.Transparency = new Transparency(alpha);
        }

        private static bool IsExcludedLayer(string name)
        {
            string[] excluded = { "0", "Defpoints" };
            return excluded.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
