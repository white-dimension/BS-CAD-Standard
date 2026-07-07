using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public class FixReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public int FixedColorCount { get; set; }
        public int FixedLinetypeCount { get; set; }
        public int FixedLineweightCount { get; set; }
        public int FixedTransparencyCount { get; set; }
        public int FixedPlotCount { get; set; }
        public int FixedLockedCount { get; set; }
        public int TotalFixed => FixedColorCount + FixedLinetypeCount + FixedLineweightCount + FixedTransparencyCount + FixedPlotCount + FixedLockedCount;

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

            using (DocumentLock dl = doc.LockDocument())
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
                        if (LayerPropertyUtils.IsExcludedLayer(name)) continue;
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
            string currentLinetype = LayerPropertyUtils.GetLinetypeName(tr, layerRecord);
            bool linetypeMismatch = !string.Equals(currentLinetype, config.Linetype, StringComparison.OrdinalIgnoreCase);

            // Lineweight
            double currentLineweight = AcadUtils.LineWeightToMm(layerRecord.LineWeight);
            bool lineweightMismatch = Math.Abs(currentLineweight - config.Lineweight) > 0.001;

            // Transparency
            int currentTransparency = LayerPropertyUtils.GetTransparencyPercent(layerRecord);
            bool transparencyMismatch = currentTransparency != config.Transparency;

            // Plot
            bool plotMismatch = layerRecord.IsPlottable != config.Plot;

            // Locked
            bool lockedMismatch = layerRecord.IsLocked != config.Locked;

            bool hasFix = (linetypeMismatch || lineweightMismatch || transparencyMismatch || plotMismatch || lockedMismatch ||
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
                    ObjectId linetypeId = LayerPropertyUtils.ResolveLinetypeId(tr, config.Linetype, ed);
                    if (linetypeId != ObjectId.Null)
                    {
                        layerRecord.LinetypeObjectId = linetypeId;
                        report.FixedLinetypeCount++;
                    }
                }

                // Fix lineweight
                if (lineweightMismatch)
                {
                    layerRecord.LineWeight = AcadUtils.LineWeightFromMm(config.Lineweight);
                    report.FixedLineweightCount++;
                }

                // Fix transparency
                if (transparencyMismatch)
                {
                    LayerPropertyUtils.SetTransparencyPercent(layerRecord, config.Transparency);
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

    }
}
