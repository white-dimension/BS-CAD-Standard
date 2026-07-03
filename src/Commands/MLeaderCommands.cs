using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class MLeaderCommands
    {
        [CommandMethod("BS_MLEADER")]
        public void BS_MLeader()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed);
                if (context == null)
                {
                    return;
                }

                ObjectId textStyleId = EnsureTextStyle(ed);
                if (textStyleId == ObjectId.Null)
                {
                    ReportUtils.Error(ed, "无法创建文字样式 BS_TEXT_CN，请使用标准 DWT。");
                    return;
                }

                ObjectId styleId = EnsureMLeaderStyle(ed, textStyleId);
                if (styleId == ObjectId.Null)
                {
                    return;
                }

                ObjectId layerId = EnsureMLeaderLayer(context);
                if (layerId == ObjectId.Null)
                {
                    ReportUtils.Error(ed, "无法确定或创建多重引线图层，请使用标准 DWT。");
                    return;
                }

                string noteText = PromptUtils.GetString("\n输入说明文字: ", allowSpaces: true).Trim();
                if (string.IsNullOrEmpty(noteText))
                {
                    ReportUtils.Warning(ed, "说明文字为空，命令已取消。");
                    return;
                }

                PromptPointResult arrowRes = ed.GetPoint(new PromptPointOptions("\n指定箭头点: "));
                if (arrowRes.Status != PromptStatus.OK)
                {
                    return;
                }

                PromptPointOptions textPointOptions = new PromptPointOptions("\n指定文字放置点: ")
                {
                    UseBasePoint = true,
                    BasePoint = arrowRes.Value
                };
                PromptPointResult textRes = ed.GetPoint(textPointOptions);
                if (textRes.Status != PromptStatus.OK)
                {
                    return;
                }

                ObjectId leaderId = MLeaderEntityService.CreateMLeaderNote(
                    noteText,
                    arrowRes.Value,
                    textRes.Value,
                    styleId,
                    layerId,
                    textStyleId);

                if (leaderId != ObjectId.Null)
                {
                    ReportUtils.Info(ed, "多重引线说明创建成功。");
                }
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_MLEADER 执行失败", ex);
            }
        }

        private static ObjectId EnsureTextStyle(Editor ed)
        {
            ObjectId textStyleId = GetTextStyleId(MLeaderStyleService.StandardTextStyleName);
            if (textStyleId != ObjectId.Null)
            {
                return textStyleId;
            }

            textStyleId = TextStyleService.CreateStandardTextStyle(MLeaderStyleService.StandardTextStyleName);
            if (textStyleId == ObjectId.Null)
            {
                ReportUtils.Error(ed, "当前图纸缺少文字样式 BS_TEXT_CN，且自动创建失败。");
            }

            return textStyleId;
        }

        private static ObjectId EnsureMLeaderStyle(Editor ed, ObjectId textStyleId)
        {
            ObjectId styleId = MLeaderStyleService.GetMLeaderStyleId(MLeaderStyleService.StandardStyleName);
            if (styleId != ObjectId.Null)
            {
                return MLeaderStyleService.CreateStandardMLeaderStyle(MLeaderStyleService.StandardStyleName);
            }

            PromptResultType result = PromptUtils.ConfirmAction(
                "当前图纸缺少多重引线样式 BS_MLEADER_NOTE。\n是否使用基础参数创建？",
                "Y");

            if (result != PromptResultType.Yes)
            {
                ReportUtils.Warning(ed, "未创建 BS_MLEADER_NOTE，请使用标准 DWT。");
                return ObjectId.Null;
            }

            return MLeaderStyleService.CreateBasicStandardStyle(textStyleId);
        }

        private static ObjectId EnsureMLeaderLayer(StandardContext context)
        {
            Editor ed = context.Editor;
            LayerConfig? layerCfg = FindPreferredLayer(context.StandardConfig);
            if (layerCfg == null)
            {
                ReportUtils.Warning(ed, "未在 JSON 中找到引线说明图层，尝试使用当前图层。");
                return context.Database.Clayer;
            }

            ObjectId layerId = LayerService.GetLayerId(layerCfg.Name);
            if (layerId != ObjectId.Null)
            {
                return layerId;
            }

            layerId = LayerService.CreateLayerFromConfig(layerCfg);
            if (layerId == ObjectId.Null)
            {
                ReportUtils.Warning(ed, $"图层 [{layerCfg.Name}] 创建失败，尝试使用当前图层。");
                return context.Database.Clayer;
            }

            return layerId;
        }

        private static LayerConfig? FindPreferredLayer(StandardConfig config)
        {
            string[] exactNames =
            {
                "04-DM-引线说明",
                "14-DM-引线说明",
                "14-DM-引线标注",
                "04-DM-尺寸标注",
                "14-DM-尺寸标注",
                "16-TX-普通文字",
                "13-TX-普通文字"
            };

            foreach (string name in exactNames)
            {
                LayerConfig? exact = config.Layers.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    if (!name.Contains("引线"))
                    {
                        ReportUtils.Warning(Application.DocumentManager.MdiActiveDocument.Editor, $"未找到引线说明专用图层，使用 [{name}] 作为 fallback。");
                    }

                    return exact;
                }
            }

            LayerConfig? dmNoteLayer = config.Layers.FirstOrDefault(l =>
                string.Equals(l.Category, "DM", StringComparison.OrdinalIgnoreCase)
                && (l.Name.Contains("引线") || l.Description.Contains("引线") || l.Name.Contains("说明") || l.Description.Contains("说明")));

            if (dmNoteLayer != null)
            {
                return dmNoteLayer;
            }

            LayerConfig? dmLayer = config.Layers.FirstOrDefault(l => string.Equals(l.Category, "DM", StringComparison.OrdinalIgnoreCase));
            if (dmLayer != null)
            {
                ReportUtils.Warning(Application.DocumentManager.MdiActiveDocument.Editor, $"未找到明确的引线说明图层，使用 DM 分类图层 [{dmLayer.Name}] 作为 fallback。");
                return dmLayer;
            }

            LayerConfig? txLayer = config.Layers.FirstOrDefault(l =>
                string.Equals(l.Category, "TX", StringComparison.OrdinalIgnoreCase)
                && (l.Name.Contains("普通文字") || l.Description.Contains("普通文字")));

            if (txLayer != null)
            {
                ReportUtils.Warning(Application.DocumentManager.MdiActiveDocument.Editor, $"未找到 DM 图层，使用文字图层 [{txLayer.Name}] 作为 fallback。");
                return txLayer;
            }

            return null;
        }

        private static ObjectId GetTextStyleId(string styleName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable table = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                return table.Has(styleName) ? table[styleName] : ObjectId.Null;
            }
        }
    }
}
