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
    public class MissingReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public int CreatedCount { get; set; }
        public List<string> CreatedLayers { get; set; } = new();

        public int ExistingCount { get; set; }

        public int NonStandardCount { get; set; }
        public List<string> NonStandardLayers { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }

    public class LayerMissingService
    {
        public static MissingReport RunCreateMissing(StandardConfig config)
        {
            MissingReport report = new();
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

                    // 1. Create missing standard layers
                    foreach (LayerConfig layerConfig in config.Layers)
                    {
                        if (layerTable.Has(layerConfig.Name))
                        {
                            report.ExistingCount++;
                            continue;
                        }

                        CreateSingleLayer(tr, layerTable, layerConfig, report, ed);
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
                    report.NonStandardCount = report.NonStandardLayers.Count;

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_FIX_MISSING failed: {ex.Message}");
                }
            }

            return report;
        }

        private static void CreateSingleLayer(Transaction tr, LayerTable layerTable,
            LayerConfig config, MissingReport report, Editor ed)
        {
            try
            {
                // Ensure linetype is available before creating
                ObjectId linetypeId = LayerPropertyUtils.GetOrCreateLinetypeId(tr, config.Linetype, ed, report.Warnings);

                // Upgrade layer table for write
                layerTable.UpgradeOpen();

                LayerTableRecord ltr = new LayerTableRecord
                {
                    Name = config.Name,
                    Color = Color.FromColorIndex(ColorMethod.ByAci, (short)config.Color),
                    LinetypeObjectId = linetypeId,
                    LineWeight = AcadUtils.LineWeightFromMm(config.Lineweight),
                    IsPlottable = config.Plot
                };

                ObjectId layerId = layerTable.Add(ltr);
                tr.AddNewlyCreatedDBObject(ltr, true);

                // Set description
                try
                {
                    if (!string.IsNullOrEmpty(config.Description))
                        ltr.Description = config.Description;
                }
                catch { /* Best effort */ }

                // Set transparency (after add)
                if (config.Transparency > 0 && config.Transparency <= 90)
                {
                    LayerPropertyUtils.SetTransparencyPercent(ltr, config.Transparency);
                }

                // Set locked (last, so it doesn't block other property changes)
                if (config.Locked)
                {
                    ltr.IsLocked = true;
                }

                layerTable.DowngradeOpen();

                report.CreatedCount++;
                report.CreatedLayers.Add(config.Name);
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n[Warning] Failed to create layer [{config.Name}]: {ex.Message}");
                report.Warnings.Add($"Create failed for [{config.Name}]: {ex.Message}");
            }
        }
    }
}
