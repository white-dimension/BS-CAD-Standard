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
    public class CtbCheckReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public string CtbName { get; set; } = string.Empty;

        public int StandardLayerCount { get; set; }
        public int ExistingStandardLayerCount { get; set; }
        public int MissingStandardLayerCount { get; set; }
        public int CtbRuleColorCount { get; set; }

        public int ValidLayerColorCount { get; set; }
        public int InvalidLayerColorCount { get; set; }
        public int ColorMismatchCount { get; set; }
        public int NonStandardLayerCount { get; set; }
        public int NonStandardLayerInvalidColorCount { get; set; }

        public List<string> MissingStandardLayers { get; set; } = new();
        public List<string> ColorMismatches { get; set; } = new();
        public List<string> InvalidCtbColors { get; set; } = new();
        public List<string> NonStandardLayers { get; set; } = new();
        public List<string> NonStandardLayerInvalidColors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class CtbCheckService
    {
        public static CtbCheckReport RunCheck(StandardConfig config)
        {
            CtbCheckReport report = new()
            {
                CtbName = config.Ctb,
                StandardLayerCount = config.Layers.Count,
                CtbRuleColorCount = config.CtbRules?.Count ?? 0
            };

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            HashSet<string> standardLayerNames = new(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);
            HashSet<int> ctbColors = new((config.CtbRules ?? new List<CtbRuleConfig>()).Select(r => r.Color));

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                    // 1. Check standard layers
                    foreach (LayerConfig layerConfig in config.Layers)
                    {
                        if (!layerTable.Has(layerConfig.Name))
                        {
                            report.MissingStandardLayers.Add(layerConfig.Name);
                            continue;
                        }

                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(
                            layerTable[layerConfig.Name], OpenMode.ForRead);
                        short colorIndex = layerRecord.Color.ColorIndex;

                        // Color mismatch
                        if (colorIndex != layerConfig.Color)
                        {
                            report.ColorMismatches.Add(
                                $"{layerConfig.Name}: current={colorIndex}, expected={layerConfig.Color}");
                        }

                        // Invalid CTB color
                        if (!ctbColors.Contains(colorIndex))
                        {
                            report.InvalidCtbColors.Add(
                                $"{layerConfig.Name}: color={colorIndex} not defined in ctbRules");
                        }
                    }

                    // 2. Non-standard layers
                    foreach (ObjectId id in layerTable)
                    {
                        LayerTableRecord layerRecord = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        string name = layerRecord.Name;

                        // Skip excluded / xref
                        if (LayerPropertyUtils.IsExcludedLayer(name)) continue;
                        if (name.Contains("|")) continue;

                        if (standardLayerNames.Contains(name)) continue;

                        report.NonStandardLayers.Add(name);
                        short colorIndex = layerRecord.Color.ColorIndex;
                        if (!ctbColors.Contains(colorIndex))
                        {
                            report.NonStandardLayerInvalidColors.Add(
                                $"{name}: color={colorIndex} not defined in ctbRules");
                        }
                    }

                    // 3. Statistics
                    report.ExistingStandardLayerCount = config.Layers.Count - report.MissingStandardLayers.Count;
                    report.MissingStandardLayerCount = report.MissingStandardLayers.Count;
                    report.ColorMismatchCount = report.ColorMismatches.Count;
                    report.InvalidLayerColorCount = report.InvalidCtbColors.Count;
                    report.ValidLayerColorCount = report.ExistingStandardLayerCount - report.InvalidLayerColorCount;
                    report.NonStandardLayerCount = report.NonStandardLayers.Count;
                    report.NonStandardLayerInvalidColorCount = report.NonStandardLayerInvalidColors.Count;

                    tr.Commit();
                }
                catch (Exception ex)
                {
                    report.Success = false;
                    report.ErrorMessage = ex.Message;
                    ed.WriteMessage($"\n[Exception] BS_CTB_CHECK failed: {ex.Message}");
                }
            }

            return report;
        }
    }
}
