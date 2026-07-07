using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Commands
{
    public class LayerMergeFromCsvCommands
    {
        // 安全模式白名单 — 只迁移这些低风险目标图层
        private static readonly HashSet<string> SafeModeTargets = new(StringComparer.OrdinalIgnoreCase)
        {
            "13-TX-普通文字",
            "13-TX-说明文字",
            "13-TX-标题文字",
            "14-DM-尺寸标注",
            "14-DM-引线标注",
            "14-DM-符号",
            "14-DM-标高",
            "15-FR-视口",
            "17-AN-不打印线",
            "17-AN-中心线",
            "17-AN-辅助线",
            "17-AN-临时对象"
        };

        [CommandMethod("BS_LAYER_MERGE_FROM_CSV")]
        public void BS_LayerMergeFromCsv()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // 1. 加载配置
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                var standardLayerMap = context.StandardConfig.Layers
                    .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

                // 2. 选择 CSV 文件
                string? csvPath = PromptForCsvFile(ed, "选择人工确认后的 CSV 文件");
                if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
                {
                    ed.WriteMessage("\n已取消或文件不存在。");
                    return;
                }

                // 3. 读取 CSV
                List<CsvRow> rows = ReadCsv(csvPath, ed);
                if (rows == null || rows.Count == 0)
                {
                    ed.WriteMessage("\nCSV 文件无有效数据或格式错误。");
                    return;
                }

                // 4. 校验与过滤
                var mergeRows = ValidateAndFilter(rows, standardLayerMap, context.StandardConfig, ed);

                // 5. 统计摘要
                int totalRead = rows.Count;
                int skippedEmpty = rows.Count(r => string.IsNullOrWhiteSpace(r.UserConfirmedTargetLayer));
                int skippedXref = rows.Count(r => r.IsXrefLayer);
                int skippedSystem = rows.Count(r => r.IsSystemLayer);
                int validRules = mergeRows.Count;

                ed.WriteMessage($"\n读取行数：{totalRead}");
                ed.WriteMessage($"\n跳过空目标：{skippedEmpty}");
                ed.WriteMessage($"\n跳过外部参照：{skippedXref}");
                ed.WriteMessage($"\n跳过系统图层：{skippedSystem}");
                ed.WriteMessage($"\n有效迁移规则：{validRules}");

                if (validRules == 0)
                {
                    ed.WriteMessage("\n没有需要迁移的规则，命令结束。");
                    return;
                }

                // 6. 选择迁移模式
                int mode = PromptForMode(ed);
                if (mode < 0)
                {
                    ed.WriteMessage("\n用户取消。");
                    return;
                }

                string modeLabel = mode == 1 ? "安全模式" : (mode == 2 ? "手动确认模式" : "强制模式");

                // 7. 按模式过滤/确认
                var finalRows = new List<MergeRow>();
                var highRiskSkipped = new List<string>();

                if (mode == 1)
                {
                    // 安全模式：只保留白名单中的目标
                    foreach (var row in mergeRows)
                    {
                        if (SafeModeTargets.Contains(row.TargetLayer))
                        {
                            finalRows.Add(row);
                        }
                        else
                        {
                            highRiskSkipped.Add($"{row.SourceLayer} → {row.TargetLayer}");
                        }
                    }

                    int highRiskCount = highRiskSkipped.Count;
                    ed.WriteMessage($"\n安全模式：有效 {finalRows.Count} 条，跳过高风险 {highRiskCount} 条");
                    if (highRiskCount > 0)
                    {
                        ed.WriteMessage("\n跳过高风险图层：");
                        foreach (string s in highRiskSkipped)
                            ed.WriteMessage($"\n  {s}");
                    }
                }
                else if (mode == 2)
                {
                    // 手动确认模式：逐条询问
                    foreach (var row in mergeRows)
                    {
                        string prompt = $"\n旧图层 {row.SourceLayer} → 目标图层 {row.TargetLayer}，是否迁移？";
                        var result = PromptUtils.ConfirmAction(prompt, "N");
                        if (result == PromptResultType.Yes)
                        {
                            finalRows.Add(row);
                        }
                        else
                        {
                            highRiskSkipped.Add($"{row.SourceLayer} → {row.TargetLayer}（用户跳过）");
                        }
                    }
                    ed.WriteMessage($"\n手动确认：确认 {finalRows.Count} 条，跳过 {highRiskSkipped.Count} 条");
                }
                else // mode == 3
                {
                    // 强制模式：全部迁移，但先警告
                    ed.WriteMessage("\n\n[高风险警告] 强制模式可能破坏布局视口冻结、图层透明度、");
                    ed.WriteMessage("\n颜色、线宽和打印表达。请确认已备份 DWG。");
                    var forceConfirm = PromptUtils.ConfirmAction("\n是否继续强制迁移？", "N");
                    if (forceConfirm != PromptResultType.Yes)
                    {
                        ed.WriteMessage("\n用户取消。");
                        return;
                    }
                    finalRows = mergeRows;
                }

                // 已有确认的情况下不再重复确认
                if (finalRows.Count == 0)
                {
                    ed.WriteMessage("\n没有需要迁移的规则，命令结束。");
                    return;
                }

                // 8. 执行迁移
                MergeResult mergeResult = ExecuteMerge(finalRows, standardLayerMap, context.StandardConfig, ed);
                mergeResult.ModeLabel = modeLabel;
                mergeResult.HighRiskSkipped = highRiskSkipped;

                // 9. 输出报告
                PrintMergeReport(ed, csvPath, totalRead, validRules, skippedEmpty, skippedXref, skippedSystem, mergeResult);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_MERGE_FROM_CSV 执行失败", ex);
            }
        }

        /// <summary>
        /// 选择迁移模式
        /// </summary>
        private static int PromptForMode(Editor ed)
        {
            ed.WriteMessage("\n\n请选择迁移模式：");
            ed.WriteMessage("\n  1 = 安全模式：只迁移文字、标注、符号、视口、不打印线等低风险图层");
            ed.WriteMessage("\n  2 = 手动确认模式：逐条确认每个旧图层是否迁移");
            ed.WriteMessage("\n  3 = 强制模式：按 CSV 全部迁移，高风险");

            var opt = new PromptStringOptions("\n\n输入编号 (默认 1): ");
            opt.AllowSpaces = false;
            PromptResult res = ed.GetString(opt);

            if (res.Status != PromptStatus.OK) return -1;

            string input = res.StringResult.Trim();
            if (string.IsNullOrEmpty(input)) return 1; // 默认安全模式

            if (input == "1") return 1;
            if (input == "2") return 2;
            if (input == "3") return 3;

            ed.WriteMessage("\n无效输入。");
            return -1;
        }

        /// <summary>
        /// 提示用户输入 CSV 文件路径
        /// </summary>
        private static string? PromptForCsvFile(Editor ed, string title)
        {
            PromptStringOptions opt = new PromptStringOptions($"\n{title}\n请输入 CSV 文件完整路径: ");
            opt.AllowSpaces = true;
            PromptResult res = ed.GetString(opt);

            if (res.Status == PromptStatus.OK)
            {
                string path = res.StringResult.Trim();
                // 去掉可能的首尾引号
                if (path.StartsWith("\"") && path.EndsWith("\""))
                    path = path.Substring(1, path.Length - 2);
                return path;
            }

            return null;
        }

        /// <summary>
        /// 读取 CSV，兼容 UTF-8 with BOM
        /// </summary>
        private static List<CsvRow> ReadCsv(string path, Editor ed)
        {
            var result = new List<CsvRow>();
            string[] lines;

            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[错误] 读取 CSV 失败: {ex.Message}");
                return result;
            }

            if (lines.Length < 2)
            {
                ed.WriteMessage("\n[错误] CSV 文件缺少表头或数据行。");
                return result;
            }

            // 解析表头
            string[] headers = ParseCsvLine(lines[0]);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                headerMap[headers[i].Trim()] = i;
            }

            // 校验必填字段
            if (!headerMap.ContainsKey("SourceLayer"))
            {
                ed.WriteMessage("\n[错误] CSV 缺少 SourceLayer 列。");
                return result;
            }
            if (!headerMap.ContainsKey("UserConfirmedTargetLayer"))
            {
                ed.WriteMessage("\n[错误] CSV 缺少 UserConfirmedTargetLayer 列。");
                return result;
            }

            int idxSource = headerMap["SourceLayer"];
            int idxTarget = headerMap["UserConfirmedTargetLayer"];
            int idxXref = headerMap.ContainsKey("IsXrefLayer") ? headerMap["IsXrefLayer"] : -1;
            int idxSystem = headerMap.ContainsKey("IsSystemLayer") ? headerMap["IsSystemLayer"] : -1;
            int idxObjectCount = headerMap.ContainsKey("ObjectCount") ? headerMap["ObjectCount"] : -1;
            int idxMatchRule = headerMap.ContainsKey("MatchRule") ? headerMap["MatchRule"] : -1;
            int idxNote = headerMap.ContainsKey("Note") ? headerMap["Note"] : -1;

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] fields = ParseCsvLine(line);

                string sourceLayer = GetFieldSafe(fields, idxSource);
                if (string.IsNullOrWhiteSpace(sourceLayer)) continue;

                string targetLayer = GetFieldSafe(fields, idxTarget);

                bool isXref = idxXref >= 0
                    && string.Equals(GetFieldSafe(fields, idxXref), "是", StringComparison.OrdinalIgnoreCase);

                bool isSystem = idxSystem >= 0
                    && string.Equals(GetFieldSafe(fields, idxSystem), "是", StringComparison.OrdinalIgnoreCase);

                int objCount = 0;
                if (idxObjectCount >= 0)
                    int.TryParse(GetFieldSafe(fields, idxObjectCount), out objCount);

                string matchRule = idxMatchRule >= 0 ? GetFieldSafe(fields, idxMatchRule) : "";
                string note = idxNote >= 0 ? GetFieldSafe(fields, idxNote) : "";

                result.Add(new CsvRow
                {
                    SourceLayer = sourceLayer,
                    UserConfirmedTargetLayer = targetLayer,
                    IsXrefLayer = isXref,
                    IsSystemLayer = isSystem,
                    ObjectCount = objCount,
                    MatchRule = matchRule,
                    Note = note
                });
            }

            return result;
        }

        /// <summary>
        /// 简单的 CSV 行解析，支持双引号包裹
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private static string GetFieldSafe(string[] fields, int index)
        {
            return (index >= 0 && index < fields.Length) ? (fields[index] ?? "").Trim() : "";
        }

        /// <summary>
        /// 校验并过滤需要迁移的行
        /// </summary>
        private static List<MergeRow> ValidateAndFilter(
            List<CsvRow> rows,
            Dictionary<string, LayerConfig> standardLayerMap,
            StandardConfig standardConfig,
            Editor ed)
        {
            var result = new List<MergeRow>();
            var errors = new List<string>();

            foreach (var row in rows)
            {
                string source = row.SourceLayer.Trim();

                // 跳过空目标
                if (string.IsNullOrWhiteSpace(row.UserConfirmedTargetLayer))
                    continue;

                string target = row.UserConfirmedTargetLayer.Trim();

                // 跳过系统图层
                if (row.IsSystemLayer)
                    continue;

                // 跳过外部参照
                if (row.IsXrefLayer)
                    continue;

                if (source.Contains('|'))
                    continue;

                // 跳过系统图层名
                if (string.Equals(source, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(source, "Defpoints", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 校验目标图层在标准 JSON 中
                if (!standardLayerMap.ContainsKey(target))
                {
                    errors.Add($"  目标图层 [{target}] 不在 BS 标准配置中，跳过。来源: {source}");
                    continue;
                }

                result.Add(new MergeRow
                {
                    SourceLayer = source,
                    TargetLayer = target
                });
            }

            if (errors.Count > 0)
            {
                ed.WriteMessage($"\n[校验结果] {errors.Count} 条规则被跳过：");
                foreach (var err in errors)
                    ed.WriteMessage($"\n{err}");
            }

            return result;
        }

        /// <summary>
        /// 执行迁移
        /// </summary>
        private static MergeResult ExecuteMerge(
            List<MergeRow> mergeRows,
            Dictionary<string, LayerConfig> standardLayerMap,
            StandardConfig standardConfig,
            Editor ed)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var result = new MergeResult();

            using (DocumentLock dl = doc.LockDocument())
            {
                foreach (var row in mergeRows)
                {
                    int migrated = 0;

                    try
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            // 确保目标图层存在
                            EnsureTargetLayerExists(row.TargetLayer, standardLayerMap, db, tr);

                            // 迁移实体
                            migrated = MigrateEntitiesToLayer(db, tr, row.SourceLayer, row.TargetLayer);

                            tr.Commit();
                        }

                        result.SuccessfulLayers.Add(new LayerMergeDetail
                        {
                            SourceLayer = row.SourceLayer,
                            TargetLayer = row.TargetLayer,
                            ObjectCount = migrated,
                            Success = true
                        });

                        result.TotalMigrated += migrated;
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[错误] 迁移图层 [{row.SourceLayer}] 失败: {ex.Message}");

                        result.FailedLayers.Add(new LayerMergeDetail
                        {
                            SourceLayer = row.SourceLayer,
                            TargetLayer = row.TargetLayer,
                            ObjectCount = 0,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 确保目标标准图层在 DWG 中存在
        /// </summary>
        private static void EnsureTargetLayerExists(
            string layerName,
            Dictionary<string, LayerConfig> standardLayerMap,
            Database db,
            Transaction tr)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(layerName)) return;

            // 从标准配置中查找
            if (!standardLayerMap.TryGetValue(layerName, out LayerConfig? cfg))
            {
                throw new System.Exception($"标准配置中找不到图层 [{layerName}] 的定义");
            }

            // 创建图层
            lt.UpgradeOpen();
            LayerTableRecord ltr = new LayerTableRecord { Name = cfg.Name };
            ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                Autodesk.AutoCAD.Colors.ColorMethod.ByAci, (short)cfg.Color);
            ltr.LineWeight = AcadUtils.LineWeightFromMm(cfg.Lineweight);
            ltr.IsPlottable = cfg.Plot;

            if (!string.IsNullOrEmpty(cfg.Linetype) &&
                !string.Equals(cfg.Linetype, "Continuous", StringComparison.OrdinalIgnoreCase))
            {
                if (AcadUtils.EnsureLinetypeLoaded(cfg.Linetype))
                {
                    LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                    if (ltt.Has(cfg.Linetype))
                        ltr.LinetypeObjectId = ltt[cfg.Linetype];
                }
            }

            ObjectId newId = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);

            // 设置描述
            if (!string.IsNullOrEmpty(cfg.Description))
            {
                try { ltr.Description = cfg.Description; }
                catch { }
            }
        }

        /// <summary>
        /// 遍历模型空间和图纸空间，将指定源图层上的实体迁移到目标图层
        /// </summary>
        private static int MigrateEntitiesToLayer(Database db, Transaction tr, string sourceLayer, string targetLayer)
        {
            int count = 0;
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            // 模型空间 + 所有布局
            var blockIds = new List<ObjectId> { bt[BlockTableRecord.ModelSpace] };
            foreach (ObjectId btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (!btr.IsLayout) continue;
                if (btr.ObjectId == bt[BlockTableRecord.ModelSpace]) continue;
                blockIds.Add(btrId);
            }

            foreach (ObjectId blockId in blockIds)
            {
                var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead, false);
                foreach (ObjectId entId in btr)
                {
                    if (entId.IsErased) continue;

                    try
                    {
                        var entity = (Entity)tr.GetObject(entId, OpenMode.ForWrite, false, false);
                        if (string.Equals(entity.Layer, sourceLayer, StringComparison.OrdinalIgnoreCase))
                        {
                            entity.Layer = targetLayer;
                            count++;
                        }
                    }
                    catch
                    {
                        // 跳过无法写入的对象
                    }
                }
            }

            return count;
        }

        private static void PrintMergeReport(
            Editor ed,
            string csvPath,
            int totalRead,
            int validRules,
            int skippedEmpty,
            int skippedXref,
            int skippedSystem,
            MergeResult mergeResult)
        {
            ed.WriteMessage("\n\n===== BS_LAYER_MERGE_FROM_CSV =====");
            ed.WriteMessage($"\n迁移模式：{mergeResult.ModeLabel}");
            ed.WriteMessage($"\nCSV路径：{csvPath}");
            ed.WriteMessage($"\n读取行数：{totalRead}");
            ed.WriteMessage($"\n有效迁移规则：{validRules}");
            ed.WriteMessage($"\n跳过空目标：{skippedEmpty}");
            ed.WriteMessage($"\n跳过外部参照：{skippedXref}");
            ed.WriteMessage($"\n跳过系统图层：{skippedSystem}");

            if (mergeResult.HighRiskSkipped.Count > 0)
            {
                ed.WriteMessage($"\n跳过高风险图层：{mergeResult.HighRiskSkipped.Count}");
                foreach (string s in mergeResult.HighRiskSkipped)
                    ed.WriteMessage($"\n  {s}");
            }

            if (mergeResult.SuccessfulLayers.Count > 0)
            {
                ed.WriteMessage("\n\n[迁移结果]");
                foreach (var detail in mergeResult.SuccessfulLayers)
                {
                    ed.WriteMessage($"\n\n{detail.SourceLayer} → {detail.TargetLayer}");
                    ed.WriteMessage($"\n   对象数：{detail.ObjectCount}");
                    ed.WriteMessage("\n   结果：成功");
                }
            }

            if (mergeResult.FailedLayers.Count > 0)
            {
                foreach (var detail in mergeResult.FailedLayers)
                {
                    ed.WriteMessage($"\n\n{detail.SourceLayer} → {detail.TargetLayer}");
                    ed.WriteMessage($"\n   结果：失败 - {detail.ErrorMessage}");
                }
            }

            ed.WriteMessage("\n\n[汇总]");
            ed.WriteMessage($"\n成功迁移图层数：{mergeResult.SuccessfulCount}");
            ed.WriteMessage($"\n成功迁移对象数：{mergeResult.TotalMigrated}");
            ed.WriteMessage($"\n失败图层数：{mergeResult.FailedCount}");
            ed.WriteMessage("\n旧图层未删除：是");
            ed.WriteMessage("\n对象属性未改 ByLayer：是");
            ed.WriteMessage("\n====================================");
        }

        // ── 内部数据模型 ──

        private class CsvRow
        {
            public string SourceLayer { get; set; } = string.Empty;
            public string UserConfirmedTargetLayer { get; set; } = string.Empty;
            public bool IsXrefLayer { get; set; }
            public bool IsSystemLayer { get; set; }
            public int ObjectCount { get; set; }
            public string MatchRule { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
        }

        private class MergeRow
        {
            public string SourceLayer { get; set; } = string.Empty;
            public string TargetLayer { get; set; } = string.Empty;
        }

        private class LayerMergeDetail
        {
            public string SourceLayer { get; set; } = string.Empty;
            public string TargetLayer { get; set; } = string.Empty;
            public int ObjectCount { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        private class MergeResult
        {
            public List<LayerMergeDetail> SuccessfulLayers { get; } = new();
            public List<LayerMergeDetail> FailedLayers { get; } = new();
            public int TotalMigrated { get; set; }
            public int SuccessfulCount => SuccessfulLayers.Count;
            public int FailedCount => FailedLayers.Count;
            public string ModeLabel { get; set; } = "";
            public List<string> HighRiskSkipped { get; set; } = new();
        }
    }
}
