using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;

namespace BS_CAD_STANDARD_V10_Plugin.Services
{
    public class LayerAuditResult
    {
        public int StandardLayerCount { get; set; }
        public int TotalLayerCount { get; set; }
        public int NonStandardCount { get; set; }
        public int XrefLayerCount { get; set; }
        public string RulesPath { get; set; } = string.Empty;
        public List<NonStandardLayerInfo> NonStandardLayers { get; set; } = new();
    }

    public class NonStandardLayerInfo
    {
        public string LayerName { get; set; } = string.Empty;
        public int ObjectCount { get; set; }
        public string SuggestedTarget { get; set; } = string.Empty;
        public string MatchRule { get; set; } = string.Empty;
    }

    public class LayerAuditEngine
    {
        private static readonly HashSet<string> SystemLayers = new(StringComparer.OrdinalIgnoreCase)
        {
            "0", "Defpoints"
        };

        public static LayerAuditResult Audit(StandardConfig config, MigrationRulesConfig? rulesConfig)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            // 构建标准图层名称集合（快速查找）
            var standardLayerNames = new HashSet<string>(config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            var result = new LayerAuditResult
            {
                StandardLayerCount = config.Layers.Count
            };

            var layerObjectCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 打开图层表
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                result.TotalLayerCount = lt.Cast<ObjectId>().Count();

                foreach (ObjectId layerId in lt)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    string layerName = layer.Name;

                    // 排除系统图层
                    if (SystemLayers.Contains(layerName)) continue;

                    // 排除外部参照图层
                    if (layerName.Contains('|'))
                    {
                        result.XrefLayerCount++;
                        continue;
                    }

                    // 判断是否标准图层
                    if (standardLayerNames.Contains(layerName))
                    {
                        // 已在标准列表中，跳过
                        continue;
                    }

                    // 非标准图层，先记录，后续统计对象数
                    result.NonStandardCount++;
                    layerObjectCount[layerName] = 0;
                }

                // 遍历所有块表记录统计对象数量
                CountObjectsByLayer(db, tr, layerObjectCount);

                tr.Commit();
            }

            // 对每个非标准图层生成建议
            foreach (var kvp in layerObjectCount)
            {
                var info = new NonStandardLayerInfo
                {
                    LayerName = kvp.Key,
                    ObjectCount = kvp.Value,
                };

                // 尝试匹配关键词规则（JSON 中已按优先级从高到低排列）
                if (rulesConfig != null && rulesConfig.Rules.Count > 0)
                {
                    string upperName = kvp.Key.ToUpperInvariant();
                    bool matched = false;

                    foreach (var rule in rulesConfig.Rules)
                    {
                        foreach (string keyword in rule.Keywords)
                        {
                            if (upperName.Contains(keyword.ToUpperInvariant()))
                            {
                                info.SuggestedTarget = rule.TargetLayer;
                                info.MatchRule = rule.Rule;
                                matched = true;
                                break;
                            }
                        }

                        if (matched) break;
                    }

                    if (!matched)
                    {
                        info.SuggestedTarget = "未识别，需要人工选择";
                        info.MatchRule = "无匹配规则";
                    }
                }
                else
                {
                    info.SuggestedTarget = "未识别，需要人工选择";
                    info.MatchRule = "规则文件未加载";
                }

                result.NonStandardLayers.Add(info);
            }

            // 按对象数降序排列
            result.NonStandardLayers = result.NonStandardLayers
                .OrderByDescending(l => l.ObjectCount)
                .ToList();

            return result;
        }

        /// <summary>
        /// 遍历模型空间及所有布局，统计每个图层上的对象数量
        /// </summary>
        private static void CountObjectsByLayer(Database db, Transaction tr, Dictionary<string, int> counts)
        {
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            // 模型空间
            CountObjectsInBlock(bt[BlockTableRecord.ModelSpace], tr, counts);

            // 所有布局（图纸空间）
            foreach (ObjectId btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (!btr.IsLayout) continue;
                if (btr.ObjectId == bt[BlockTableRecord.ModelSpace]) continue;

                CountObjectsInBlock(btr.ObjectId, tr, counts);
            }
        }

        private static void CountObjectsInBlock(ObjectId blockId, Transaction tr, Dictionary<string, int> counts)
        {
            var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
            foreach (ObjectId entityId in btr)
            {
                if (entityId.IsErased) continue;

                try
                {
                    var entity = (Entity)tr.GetObject(entityId, OpenMode.ForRead, false, false);
                    string layerName = entity.Layer;

                    if (counts.ContainsKey(layerName))
                    {
                        counts[layerName]++;
                    }
                }
                catch
                {
                    // 跳过无法读取的对象
                }
            }
        }
    }
}
