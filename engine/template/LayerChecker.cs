using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Template
{
    /// <summary>
    /// 图层检查器 — 检查标准图层完整性、图框/视口、默认图层、非标准图层。
    /// </summary>
    public class LayerChecker
    {
        public void Run(Transaction tr, Database db, StandardConfig config,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            CheckLayers(tr, db, config, standardLayerNames, report);
            CheckFrameAndViewport(tr, db, standardLayerNames, report);
            CheckDefaultLayers(tr, db, report);
            CheckNonStandardLayers(tr, db, standardLayerNames, report);
        }

        private static void CheckLayers(Transaction tr, Database db, StandardConfig config,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            int totalAll = 0;
            int checkableCount = 0;
            int existingStandard = 0;
            List<string> missing = new();

            foreach (ObjectId id in layerTable)
            {
                totalAll++;
                LayerTableRecord lr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!LayerPropertyUtils.IsExcludedLayer(lr.Name))
                    checkableCount++;
            }

            foreach (LayerConfig lc in config.Layers)
            {
                if (layerTable.Has(lc.Name))
                    existingStandard++;
                else
                    missing.Add(lc.Name);
            }

            AddInfo(report, $"当前图层总数（含默认图层）：{totalAll}");
            AddInfo(report, $"参与标准检查图层数（不含 0 / Defpoints）：{checkableCount}");
            AddInfo(report, $"标准图层数量：{config.Layers.Count}");

            if (existingStandard == config.Layers.Count)
                AddOk(report, $"标准图层完整：{existingStandard} / {config.Layers.Count}");
            else
                AddWarn(report, $"已有标准图层：{existingStandard} / {config.Layers.Count}，缺失 {missing.Count} 个");

            if (missing.Count > 0)
            {
                foreach (string m in missing.Take(10))
                    report.Lines.Add($"  - {m}");
                if (missing.Count > 10)
                    report.Lines.Add($"  ... 及其他 {missing.Count - 10} 个");
            }
        }

        private static void CheckFrameAndViewport(Transaction tr, Database db,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            var frameKeywords = new[] { "图框", "FR", "FRAME", "标题", "TITLE", "SHEET" };
            var vpKeywords = new[] { "视口", "VP", "VPORT", "VIEWPORT" };

            string? frameLayerInDwg = null;
            string? vpLayerInDwg = null;

            foreach (string name in standardLayerNames)
            {
                if (!layerTable.Has(name)) continue;

                if (frameLayerInDwg == null && frameKeywords.Any(kw =>
                    name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    frameLayerInDwg = name;
                }

                if (vpLayerInDwg == null && vpKeywords.Any(kw =>
                    name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    vpLayerInDwg = name;
                }

                if (frameLayerInDwg != null && vpLayerInDwg != null) break;
            }

            if (frameLayerInDwg != null)
                AddOk(report, $"图框相关图层已存在：{frameLayerInDwg}");
            else
                AddWarn(report, "当前 DWG 未发现图框相关图层");

            if (vpLayerInDwg != null)
                AddOk(report, $"视口图层已存在：{vpLayerInDwg}");
            else
                AddWarn(report, "当前 DWG 未发现视口相关图层");
        }

        private static void CheckDefaultLayers(Transaction tr, Database db, TemplateCheckReport report)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

            if (lt.Has("0"))
                AddOk(report, "默认图层 0 存在");
            else
                AddWarn(report, "默认图层 0 不存在");

            if (lt.Has("Defpoints"))
                AddOk(report, "默认图层 Defpoints 存在");
            else
                AddInfo(report, "当前图纸未发现 Defpoints 图层");
        }

        private static void CheckNonStandardLayers(Transaction tr, Database db,
            HashSet<string> standardLayerNames, TemplateCheckReport report)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            List<string> extra = new();

            foreach (ObjectId id in lt)
            {
                LayerTableRecord lr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                string name = lr.Name;
                if (LayerPropertyUtils.IsExcludedLayer(name)) continue;
                if (standardLayerNames.Contains(name)) continue;
                extra.Add(name);
            }

            if (extra.Count == 0)
                AddOk(report, "未发现非标准图层");
            else
            {
                AddWarn(report, $"非标准图层：{extra.Count} 个");
                foreach (string e in extra.Take(15))
                    report.Lines.Add($"  - {e}");
                if (extra.Count > 15)
                    report.Lines.Add($"  ... 及其他 {extra.Count - 15} 个");
            }
        }

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
