using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class LayerAuditExportCommands
    {
        [CommandMethod("BS_LAYER_AUDIT_EXPORT")]
        public void BS_LayerAuditExport()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null) return;

                MigrationRulesConfig? rules = ConfigurationService.LoadMigrationRules(ed);

                ed.WriteMessage("\n正在分析图纸图层...");

                // 复用 BS_LAYER_AUDIT 的分析逻辑
                LayerAuditResult result = LayerAuditEngine.Audit(context.StandardConfig, rules);

                // 确定导出路径
                string exportPath = GetExportPath(ed);

                // 生成 CSV
                string csvContent = BuildCsvContent(context.StandardConfig, rules);

                // 写入文件（UTF-8 with BOM）
                File.WriteAllText(exportPath, csvContent, new UTF8Encoding(true));

                // 输出报告
                PrintExportReport(ed, exportPath, result);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_LAYER_AUDIT_EXPORT 执行失败", ex);
            }
        }

        private static string GetExportPath(Editor ed)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            string dwgPath = doc.Name;
            string baseDir;

            if (!string.IsNullOrEmpty(dwgPath) && File.Exists(dwgPath))
            {
                baseDir = Path.GetDirectoryName(dwgPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            else
            {
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                ed.WriteMessage("\n[信息] 当前图纸未保存，导出到桌面。");
            }

            string dwgName = string.IsNullOrEmpty(dwgPath)
                ? "Unsaved"
                : Path.GetFileNameWithoutExtension(dwgPath);

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                dwgName = dwgName.Replace(c, '_');
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"BS_Layer_Audit_{dwgName}_{timestamp}.csv";

            return Path.Combine(baseDir, fileName);
        }

        private static string BuildCsvContent(StandardConfig config, MigrationRulesConfig? rules)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            var sb = new StringBuilder();

            var standardLayerNames = new HashSet<string>(
                config.Layers.Select(l => l.Name), StringComparer.OrdinalIgnoreCase);

            sb.AppendLine("SourceLayer,ObjectCount,SuggestedTargetLayer,MatchRule,IsXrefLayer,IsSystemLayer,UserConfirmedTargetLayer,Note");

            var layerObjectCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var layerIsXref = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var layerIsSystem = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var layerIsStandard = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                foreach (ObjectId layerId in lt)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    string layerName = layer.Name;

                    bool isSystem = string.Equals(layerName, "0", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(layerName, "Defpoints", StringComparison.OrdinalIgnoreCase);
                    bool isXref = layerName.Contains('|');
                    bool isStandard = standardLayerNames.Contains(layerName);

                    layerObjectCount[layerName] = 0;
                    layerIsXref[layerName] = isXref;
                    layerIsSystem[layerName] = isSystem;
                    layerIsStandard[layerName] = isStandard;
                }

                CountObjectsByLayer(db, tr, layerObjectCount);
                tr.Commit();
            }

            // 排序：标准图层在前（按名称），非标准在后
            var sortedLayers = layerObjectCount.Keys
                .OrderBy(l => layerIsStandard[l] ? 0 : 1)
                .ThenBy(l => l)
                .ToList();

            foreach (string layerName in sortedLayers)
            {
                int count = layerObjectCount[layerName];
                bool isXref = layerIsXref[layerName];
                bool isSystem = layerIsSystem[layerName];
                bool isStandard = layerIsStandard[layerName];

                string suggestedTarget;
                string matchRule;
                string note;

                if (isSystem)
                {
                    suggestedTarget = "系统图层，无需迁移";
                    matchRule = "系统保留图层";
                    note = "系统保留图层";
                }
                else if (isXref)
                {
                    suggestedTarget = "外部参照图层，无需迁移";
                    matchRule = "外部参照";
                    note = "外部参照图层";
                }
                else if (isStandard)
                {
                    suggestedTarget = layerName;
                    matchRule = "标准图层";
                    note = "";
                }
                else
                {
                    var match = TryMatchLayer(layerName, rules);
                    suggestedTarget = match.target;
                    matchRule = match.rule;
                    note = "需人工确认映射";
                }

                sb.AppendLine($"{EscapeCsv(layerName)},{count},{EscapeCsv(suggestedTarget)},{EscapeCsv(matchRule)},{EscapeCsv(isXref ? "是" : "")},{EscapeCsv(isSystem ? "是" : "")},,{EscapeCsv(note)}");
            }

            return sb.ToString();
        }

        private static (string target, string rule) TryMatchLayer(string layerName, MigrationRulesConfig? rules)
        {
            string upper = layerName.ToUpperInvariant();

            if (rules != null)
            {
                foreach (var rule in rules.Rules)
                {
                    foreach (string keyword in rule.Keywords)
                    {
                        if (upper.Contains(keyword.ToUpperInvariant()))
                        {
                            return (rule.TargetLayer, rule.Rule);
                        }
                    }
                }
            }

            return ("未识别，需要人工选择", "");
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

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static void PrintExportReport(Editor ed, string exportPath, LayerAuditResult result)
        {
            ed.WriteMessage("\n\n===== BS_LAYER_AUDIT_EXPORT =====");
            ed.WriteMessage($"\n导出完成：");
            ed.WriteMessage($"\n路径：{exportPath}");
            ed.WriteMessage($"\n非标准图层数量：{result.NonStandardCount}");
            ed.WriteMessage($"\n外部参照图层数量：{result.XrefLayerCount}");
            ed.WriteMessage("\n请人工检查 UserConfirmedTargetLayer 列后，再进行迁移。");
            ed.WriteMessage("\n=================================");
        }
    }
}
