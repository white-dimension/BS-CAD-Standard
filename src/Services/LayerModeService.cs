using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class LayerModeReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public string ModeId { get; set; } = string.Empty;
        public string ModeName { get; set; } = string.Empty;

        public int ExpectedVisibleCount { get; set; }
        public int ActualVisibleCount { get; set; }
        public int HiddenCount { get; set; }
        public int MissingCount { get; set; }

        public List<string> MissingLayers { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int RestoredCount { get; set; }
    }

    public class LayerModeService
    {
        public static LayerModeReport ApplyMode(StandardConfig config, LoadModeConfig mode)
        {
            LayerModeReport report = new()
            {
                ModeId = mode.Id,
                ModeName = mode.Name,
                ExpectedVisibleCount = mode.Layers.Count
            };

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            HashSet<string> visibleLayerNames = new(mode.Layers, StringComparer.OrdinalIgnoreCase);
            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    // Check missing layers
                    foreach (string layerName in mode.Layers)
                    {
                        if (!layerTable.Has(layerName))
                        {
                            report.MissingLayers.Add(layerName);
                        }
                    }
                    report.MissingCount = report.MissingLayers.Count;

                    // If current layer will be turned off, switch to layer 0 first
                    ObjectId currentLayerId = db.Clayer;
                    string currentLayerName = string.Empty;
                    try
                    {
                        LayerTableRecord curRecord = (LayerTableRecord)tr.GetObject(currentLayerId, OpenMode.ForRead);
                        currentLayerName = curRecord.Name;
                    }
                    catch { /* ignore */ }

                    if (!string.IsNullOrEmpty(currentLayerName) &&
                        !LayerPropertyUtils.IsExcludedLayer(currentLayerName) &&
                        !visibleLayerNames.Contains(currentLayerName))
                    {
                        if (layerTable.Has("0"))
                        {
                            db.Clayer = layerTable["0"];
                        }
                    }

                    // Apply visibility
                    int actualVisible = 0;
                    int hiddenCount = 0;

                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);

                        if (LayerPropertyUtils.IsExcludedLayer(layerRecord.Name))
                        {
                            // 0 / Defpoints — always visible
                            if (layerRecord.IsOff)
                            {
                                layerRecord.UpgradeOpen();
                                layerRecord.IsOff = false;
                                layerRecord.DowngradeOpen();
                            }
                            actualVisible++;
                            continue;
                        }

                        bool shouldShow = visibleLayerNames.Contains(layerRecord.Name);

                        if (layerRecord.IsOff == shouldShow)
                        {
                            // Needs change
                            layerRecord.UpgradeOpen();
                            layerRecord.IsOff = !shouldShow;
                            layerRecord.DowngradeOpen();
                        }

                        if (shouldShow)
                            actualVisible++;
                        else
                            hiddenCount++;
                    }

                    report.ActualVisibleCount = actualVisible;
                    report.HiddenCount = hiddenCount;

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_LAYER_MODE failed: {ex.Message}");
                }
            }

            return report;
        }

        public static LayerModeReport ShowAllLayers()
        {
            LayerModeReport report = new()
            {
                ModeName = "All Layers"
            };

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    int restored = 0;

                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (layerRecord.IsOff)
                        {
                            layerRecord.UpgradeOpen();
                            layerRecord.IsOff = false;
                            layerRecord.DowngradeOpen();
                            restored++;
                        }
                    }

                    report.RestoredCount = restored;

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_LAYER_ALL failed: {ex.Message}");
                }
            }

            return report;
        }
    }
}
