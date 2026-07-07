using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
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
        private const string SnapshotDictionaryKey = "BS_LAYER_MODE_SNAPSHOT";
        private const string CurrentLayerPrefix = "CLAYER\t";

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

            using (DocumentLock dl = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    CaptureVisibilitySnapshot(db, tr, layerTable);

                    foreach (string layerName in mode.Layers)
                    {
                        if (!layerTable.Has(layerName))
                        {
                            report.MissingLayers.Add(layerName);
                        }
                    }
                    report.MissingCount = report.MissingLayers.Count;

                    ObjectId currentLayerId = db.Clayer;
                    string currentLayerName = string.Empty;
                    try
                    {
                        LayerTableRecord curRecord = (LayerTableRecord)tr.GetObject(currentLayerId, OpenMode.ForRead);
                        currentLayerName = curRecord.Name;
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(currentLayerName) &&
                        !LayerPropertyUtils.IsExcludedLayer(currentLayerName) &&
                        !visibleLayerNames.Contains(currentLayerName) &&
                        layerTable.Has("0"))
                    {
                        db.Clayer = layerTable["0"];
                    }

                    int actualVisible = 0;
                    int hiddenCount = 0;

                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);

                        if (LayerPropertyUtils.IsExcludedLayer(layerRecord.Name))
                        {
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
                            layerRecord.UpgradeOpen();
                            layerRecord.IsOff = !shouldShow;
                            layerRecord.DowngradeOpen();
                        }

                        if (shouldShow) actualVisible++;
                        else hiddenCount++;
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
            LayerModeReport report = new() { ModeName = "All Layers" };
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (DocumentLock dl = doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    if (RestoreVisibilitySnapshot(db, tr, layerTable, out int restoredFromSnapshot))
                    {
                        report.RestoredCount = restoredFromSnapshot;
                        tr.Commit();
                        return report;
                    }

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

        private static void CaptureVisibilitySnapshot(Database db, Transaction tr, LayerTable layerTable)
        {
            DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (nod.Contains(SnapshotDictionaryKey)) return;

            var values = new List<TypedValue>
            {
                new TypedValue((int)DxfCode.Text, CurrentLayerPrefix + GetCurrentLayerName(db, tr))
            };

            foreach (ObjectId id in layerTable)
            {
                LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                values.Add(new TypedValue((int)DxfCode.Text, $"{layerRecord.Name}\t{(layerRecord.IsOff ? "1" : "0")}"));
            }

            nod.UpgradeOpen();
            Xrecord snapshot = new() { Data = new ResultBuffer(values.ToArray()) };
            nod.SetAt(SnapshotDictionaryKey, snapshot);
            tr.AddNewlyCreatedDBObject(snapshot, true);
            nod.DowngradeOpen();
        }

        private static bool RestoreVisibilitySnapshot(Database db, Transaction tr, LayerTable layerTable, out int restoredCount)
        {
            restoredCount = 0;
            DBDictionary nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(SnapshotDictionaryKey)) return false;

            Xrecord snapshot = (Xrecord)tr.GetObject(nod.GetAt(SnapshotDictionaryKey), OpenMode.ForWrite);
            TypedValue[] values = snapshot.Data?.AsArray() ?? Array.Empty<TypedValue>();
            string currentLayerName = string.Empty;

            foreach (TypedValue value in values)
            {
                string? text = value.Value as string;
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (text.StartsWith(CurrentLayerPrefix, StringComparison.Ordinal))
                {
                    currentLayerName = text.Substring(CurrentLayerPrefix.Length);
                    continue;
                }

                int split = text.LastIndexOf('\t');
                if (split <= 0 || split >= text.Length - 1) continue;

                string layerName = text.Substring(0, split);
                bool wasOff = text.Substring(split + 1) == "1";
                if (!layerTable.Has(layerName)) continue;

                LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(layerTable[layerName], OpenMode.ForWrite);
                if (layerRecord.IsOff != wasOff)
                {
                    layerRecord.IsOff = wasOff;
                    restoredCount++;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentLayerName) && layerTable.Has(currentLayerName))
            {
                db.Clayer = layerTable[currentLayerName];
            }

            snapshot.Erase();
            return true;
        }

        private static string GetCurrentLayerName(Database db, Transaction tr)
        {
            try
            {
                LayerTableRecord currentLayer = (LayerTableRecord)tr.GetObject(db.Clayer, OpenMode.ForRead);
                return currentLayer.Name;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
