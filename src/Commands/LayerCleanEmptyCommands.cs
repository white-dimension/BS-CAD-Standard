using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class LayerCleanEmptyCommands
    {
        [CommandMethod("BS_LAYER_CLEAN_EMPTY")]
        public void BS_LayerCleanEmpty()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                var standardLayerNames = new HashSet<string>(
                    context.StandardConfig.Layers.Select(l => l.Name),
                    StringComparer.OrdinalIgnoreCase);

                ed.WriteMessage("\n正在扫描空图层...");

                // 分析图层
                CleanEmptyResult analysis = AnalyzeLayers(context.Database, standardLayerNames);

                // 打印预览
                PrintPreview(ed, analysis, context.StandardConfig.Layers.Count);

                if (analysis.Candidates.Count == 0)
                {
                    ed.WriteMessage("\n\n没有可删除的空旧图层。");
                    ed.WriteMessage("\n=================================");
                    return;
                }

                // 确认
                var confirmResult = PromptUtils.ConfirmAction(
                    "\n即将删除空的非标准旧图层，请确认已备份当前 DWG。是否继续？",
                    "N");

                if (confirmResult != PromptResultType.Yes)
                {
                    ed.WriteMessage("\n用户取消。");
                    return;
                }

                // 执行删除
                DeleteResult deleteResult = ExecuteDelete(analysis.Candidates, context.Database, ed);

                // 输出结果
                PrintDeleteResult(ed, deleteResult);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_CLEAN_EMPTY 执行失败", ex);
            }
        }

        private static CleanEmptyResult AnalyzeLayers(Database db, HashSet<string> standardLayerNames)
        {
            var result = new CleanEmptyResult();
            var allLayerNames = new List<string>();
            var layerObjectCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var layerFlags = new Dictionary<string, LayerFlags>(StringComparer.OrdinalIgnoreCase);

            string currentLayerName = db.Clayer.IsValid
                ? db.Clayer.IsErased ? "" : GetLayerName(db, db.Clayer)
                : "";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId layerId in lt)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    string name = layer.Name;
                    bool isStandard = standardLayerNames.Contains(name);
                    bool isSystem = string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(name, "Defpoints", StringComparison.OrdinalIgnoreCase);
                    bool isXref = name.Contains('|');
                    bool isDependent = layer.IsDependent;
                    bool isLocked = layer.IsLocked;
                    bool isCurrent = string.Equals(name, currentLayerName, StringComparison.OrdinalIgnoreCase);

                    if (!isStandard && !isSystem && !isXref && !isDependent)
                    {
                        allLayerNames.Add(name);
                        layerObjectCount[name] = 0;
                    }

                    layerFlags[name] = new LayerFlags
                    {
                        IsStandard = isStandard,
                        IsSystem = isSystem,
                        IsXref = isXref,
                        IsDependent = isDependent,
                        IsLocked = isLocked,
                        IsCurrent = isCurrent
                    };
                }

                // 统计对象数
                CountObjectsByLayer(db, tr, layerObjectCount);

                tr.Commit();
            }

            result.TotalLayerCount = layerFlags.Count;
            result.CurrentLayerName = currentLayerName;

            foreach (string name in allLayerNames)
            {
                var flags = layerFlags[name];
                int objCount = layerObjectCount.GetValueOrDefault(name, 0);

                if (objCount == 0)
                {
                    result.Candidates.Add(name);
                }
                else
                {
                    result.NonEmptyCount++;
                }
            }

            // 统计汇总
            result.SkipSystemCount = layerFlags.Values.Count(f => f.IsSystem);
            result.SkipXrefCount = layerFlags.Values.Count(f => f.IsXref);
            result.SkipCurrentCount = 0;
            result.SkipLockedCount = 0;

            // 从候选者中排除附加条件
            var finalCandidates = new List<string>();
            foreach (string name in result.Candidates)
            {
                var flags = layerFlags[name];

                if (flags.IsCurrent)
                {
                    result.SkipCurrentCount++;
                    continue;
                }

                if (flags.IsLocked)
                {
                    result.SkipLockedCount++;
                    continue;
                }

                if (flags.IsDependent)
                {
                    continue;
                }

                finalCandidates.Add(name);
            }

            result.Candidates = finalCandidates;
            result.NonStandardCount = allLayerNames.Count;

            return result;
        }

        private static string GetLayerName(Database db, ObjectId layerId)
        {
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    return layer.Name;
                }
            }
            catch
            {
                return "";
            }
        }

        private static void CountObjectsByLayer(Database db, Transaction tr, Dictionary<string, int> counts)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            CountInBlock(bt[BlockTableRecord.ModelSpace], tr, counts);

            foreach (ObjectId btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (!btr.IsLayout) continue;
                if (btr.ObjectId == bt[BlockTableRecord.ModelSpace]) continue;
                CountInBlock(btr.ObjectId, tr, counts);
            }
        }

        private static void CountInBlock(ObjectId blockId, Transaction tr, Dictionary<string, int> counts)
        {
            var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
            foreach (ObjectId entityId in btr)
            {
                if (entityId.IsErased) continue;
                try
                {
                    var entity = (Entity)tr.GetObject(entityId, OpenMode.ForRead, false, false);
                    if (counts.ContainsKey(entity.Layer))
                        counts[entity.Layer]++;
                }
                catch { }
            }
        }

        private static DeleteResult ExecuteDelete(List<string> candidates, Database db, Editor ed)
        {
            var result = new DeleteResult();

            using (DocumentLock dl = Application.DocumentManager.MdiActiveDocument.LockDocument())
            {
                foreach (string layerName in candidates)
                {
                    try
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                            if (!lt.Has(layerName))
                            {
                                result.SkippedCount++;
                                continue;
                            }

                            ObjectId layerId = lt[layerName];
                            var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);

                            // 二次确认：必须为空且非当前图层
                            if (layer.IsLocked || layer.IsDependent)
                            {
                                result.SkippedCount++;
                                continue;
                            }

                            // 删除图层
                            lt.UpgradeOpen();
                            layer.UpgradeOpen();
                            layer.Erase(true);

                            tr.Commit();
                            result.SuccessfulLayers.Add(layerName);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[错误] 删除图层 [{layerName}] 失败: {ex.Message}");
                        result.FailedLayers.Add(layerName);
                    }
                }
            }

            return result;
        }

        private static void PrintPreview(Editor ed, CleanEmptyResult result, int standardLayerCount)
        {
            ed.WriteMessage("\n\n===== BS_LAYER_CLEAN_EMPTY =====");

            ed.WriteMessage($"\n\n标准图层总数：{standardLayerCount}");
            ed.WriteMessage($"\n当前图纸图层总数：{result.TotalLayerCount}");
            ed.WriteMessage($"\n非标准图层数量：{result.NonStandardCount}");
            ed.WriteMessage($"\n可删除空旧图层数量：{result.Candidates.Count}");
            ed.WriteMessage($"\n跳过仍有对象图层：{result.NonEmptyCount}");
            ed.WriteMessage($"\n跳过外部参照图层：{result.SkipXrefCount}");
            ed.WriteMessage($"\n跳过系统图层：{result.SkipSystemCount}");
            ed.WriteMessage($"\n跳过当前图层：{result.SkipCurrentCount}");
            ed.WriteMessage($"\n跳过锁定图层：{result.SkipLockedCount}");

            if (result.Candidates.Count > 0)
            {
                ed.WriteMessage("\n\n[可删除空旧图层]");
                for (int i = 0; i < result.Candidates.Count; i++)
                {
                    ed.WriteMessage($"\n{i + 1}. {result.Candidates[i]}");
                }
            }
        }

        private static void PrintDeleteResult(Editor ed, DeleteResult result)
        {
            ed.WriteMessage("\n\n[删除结果]");

            foreach (string name in result.SuccessfulLayers)
            {
                ed.WriteMessage($"\n\n{name}");
                ed.WriteMessage("\n   结果：成功");
            }

            foreach (string name in result.FailedLayers)
            {
                ed.WriteMessage($"\n\n{name}");
                ed.WriteMessage("\n   结果：失败");
            }

            ed.WriteMessage("\n\n[汇总]");
            ed.WriteMessage($"\n成功删除图层数：{result.SuccessfulCount}");
            ed.WriteMessage($"\n失败图层数：{result.FailedCount}");
            ed.WriteMessage("\n=================================");
        }

        // ── 内部数据模型 ──

        private class LayerFlags
        {
            public bool IsStandard { get; set; }
            public bool IsSystem { get; set; }
            public bool IsXref { get; set; }
            public bool IsDependent { get; set; }
            public bool IsLocked { get; set; }
            public bool IsCurrent { get; set; }
        }

        private class CleanEmptyResult
        {
            public int TotalLayerCount { get; set; }
            public int NonStandardCount { get; set; }
            public int NonEmptyCount { get; set; }
            public int SkipXrefCount { get; set; }
            public int SkipSystemCount { get; set; }
            public int SkipCurrentCount { get; set; }
            public int SkipLockedCount { get; set; }
            public string CurrentLayerName { get; set; } = string.Empty;
            public List<string> Candidates { get; set; } = new();
        }

        private class DeleteResult
        {
            public List<string> SuccessfulLayers { get; } = new();
            public List<string> FailedLayers { get; } = new();
            public int SkippedCount { get; set; }
            public int SuccessfulCount => SuccessfulLayers.Count;
            public int FailedCount => FailedLayers.Count;
        }
    }
}
