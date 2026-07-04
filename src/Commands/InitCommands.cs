using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Services; // DEPRECATED_CALL — migrate to engine when available
using BS_CAD_STANDARD_V10_Plugin.Utils;
using System.Linq;

namespace BS_CAD_STANDARD_V10_Plugin.Commands
{
    public class InitCommands
    {
        [CommandMethod("BS_INIT")]
        public void BS_Init()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                StandardContext? context = ConfigurationService.CreateContext(ed, includeDimStyleConfig: true);
                if (context == null)
                {
                    // 主配置缺失时退出，ConfigurationService 已输出错误
                    return;
                }

                bool hasDimStyleConfig = context.DimStyleConfig != null;
                if (!hasDimStyleConfig)
                {
                    ed.WriteMessage("\n[信息] 标注样式配置文件缺失，跳过标注样式初始化。");
                }

                ed.WriteMessage("\n--- BS CAD Standard V10 环境初始化 ---");

                bool createLayers = AskCreateLayers(ed, context.StandardConfig);
                bool createText = AskCreateTextStyles(ed, context.StandardConfig);
                bool createDim = false;
                if (hasDimStyleConfig)
                    createDim = AskCreateDimStyles(ed, context.DimStyleConfig!);
                bool createMLeader = AskCreateMLeaderStyle(ed);
                bool setUnits = AskSetUnits(ed);
                bool setDefaults = PromptUtils.ConfirmAction("是否设置当前默认项（图层、文字样式、标注样式等）？", "Y") == PromptResultType.Yes;

                InitReport report = InitService.Initialize(
                    context.StandardConfig,
                    context.DimStyleConfig,
                    createLayers,
                    createText,
                    createDim,
                    createMLeader,
                    setUnits,
                    setDefaults);

                PrintInitReport(ed, report);
            }
            catch (System.Exception ex)
            {
                ReportUtils.Exception(ed, "BS_INIT 执行失败", ex);
            }
        }

        private bool AskCreateLayers(Editor ed, StandardConfig config)
        {
            int missing = config.Layers.Count(l => l.Core && !LayerService.LayerExists(l.Name));
            if (missing == 0)
            {
                ed.WriteMessage("\n所有核心标准图层已就绪。");
                return false;
            }

            string msg = $"当前图纸缺少 {missing} 个核心标准图层，是否按标准创建？";
            return PromptUtils.ConfirmAction(msg, "Y") == PromptResultType.Yes;
        }

        private bool AskCreateTextStyles(Editor ed, StandardConfig config)
        {
            var targets = (config.Styles.TextStyles != null && config.Styles.TextStyles.Count > 0)
                           ? config.Styles.TextStyles : StandardDefaults.TextStyles;

            int missing = targets.Count(s => AcadUtils.GetTextStyleId(s) == ObjectId.Null);
            if (missing == 0)
            {
                ed.WriteMessage("\n所有标准文字样式已就绪。");
                return false;
            }

            string msg = $"当前图纸缺少 {missing} 个标准文字样式，是否创建？";
            return PromptUtils.ConfirmAction(msg, "Y") == PromptResultType.Yes;
        }

        private bool AskCreateDimStyles(Editor ed, DimStyleStandardConfig dimConfig)
        {
            int missing = dimConfig.DimStyles.Count(s => DimStyleService.GetDimStyleId(s.Name) == ObjectId.Null);
            if (missing == 0)
            {
                ed.WriteMessage("\n所有标准标注样式已就绪。");
                return false;
            }

            string msg = $"当前图纸缺少 {missing} 个标准标注样式，是否创建？";
            return PromptUtils.ConfirmAction(msg, "Y") == PromptResultType.Yes;
        }

        private bool AskCreateMLeaderStyle(Editor ed)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary mlStyles = (DBDictionary)tr.GetObject(db.MLeaderStyleDictionaryId, OpenMode.ForRead);
                if (mlStyles.Contains(StandardDefaults.MLeaderStyleNote))
                {
                    ed.WriteMessage($"\n多重引线样式 {StandardDefaults.MLeaderStyleNote} 已存在。");
                    return false;
                }
            }

            string msg = $"多重引线样式 {StandardDefaults.MLeaderStyleNote} 不存在，是否创建？";
            return PromptUtils.ConfirmAction(msg, "Y") == PromptResultType.Yes;
        }

        private bool AskSetUnits(Editor ed)
        {
            int currentUnits = System.Convert.ToInt32(AcadUtils.SafeGetSystemVariable("INSUNITS") ?? 0);
            int currentLuPrec = System.Convert.ToInt32(AcadUtils.SafeGetSystemVariable("LUPREC") ?? 0);
            int currentAuPrec = System.Convert.ToInt32(AcadUtils.SafeGetSystemVariable("AUPREC") ?? 0);

            bool unitsMatch = currentUnits == StandardDefaults.StandardUnits;
            bool precMatch = (currentLuPrec == StandardDefaults.StandardLinearPrecision && currentAuPrec == StandardDefaults.StandardAngularPrecision);

            if (unitsMatch && precMatch)
            {
                ed.WriteMessage("\n单位与精度设置已符合标准。");
                return false;
            }

            string msg = $"\n单位设置不符：\n- INSUNITS = {currentUnits} (标准: {StandardDefaults.StandardUnits})\n- 精度 = {currentLuPrec}/{currentAuPrec} (标准: 2/0)\n是否按标准修正？";
            return PromptUtils.ConfirmAction(msg, "Y") == PromptResultType.Yes;
        }

        private void PrintInitReport(Editor ed, InitReport report)
        {
            ed.WriteMessage("\n\n===== BS_INIT · 初始化报告 =====");

            ed.WriteMessage("\n\n图层：");
            ed.WriteMessage($"\n  已存在：{report.ExistingLayers}");
            ed.WriteMessage($"\n  缺失核心：{report.MissingCoreLayers}");
            ed.WriteMessage($"\n  已创建：{report.CreatedLayers}");
            if (report.FailedLayers > 0)
                ed.WriteMessage($"\n  失败：{report.FailedLayers}");
            if (report.UserSkippedLayers > 0)
                ed.WriteMessage($"\n  用户跳过：{report.UserSkippedLayers}");

            ed.WriteMessage("\n\n文字样式：");
            ed.WriteMessage($"\n  已存在：{report.ExistingTextStyles}");
            ed.WriteMessage($"\n  缺失：{report.MissingTextStyles}");
            ed.WriteMessage($"\n  已创建：{report.CreatedTextStyles}");
            if (report.FailedTextStyles > 0)
                ed.WriteMessage($"\n  失败：{report.FailedTextStyles}");
            if (report.SkippedTextStyles > 0)
                ed.WriteMessage($"\n  用户跳过：{report.SkippedTextStyles}");

            ed.WriteMessage("\n\n标注样式：");
            ed.WriteMessage($"\n  已存在：{report.ExistingDimStyles}");
            ed.WriteMessage($"\n  缺失：{report.MissingDimStyles}");
            ed.WriteMessage($"\n  已创建：{report.CreatedDimStyles}");
            if (report.FailedDimStyles > 0)
                ed.WriteMessage($"\n  失败：{report.FailedDimStyles}");
            if (report.SkippedDimStyles > 0)
                ed.WriteMessage($"\n  用户跳过：{report.SkippedDimStyles}");

            ed.WriteMessage("\n\n多重引线样式：");
            ed.WriteMessage($"\n  {MLeaderStyleService.StandardStyleName}：{report.MLeaderStatus}");

            ed.WriteMessage("\n\n单位与精度：");
            ed.WriteMessage($"\n  INSUNITS：原值 {report.OldUnits} → 当前 {report.NewUnits}");
            ed.WriteMessage($"\n  精度 (L/A)：原值 {report.OldPrecision} → 当前 {report.NewPrecision}");

            ed.WriteMessage("\n\nCTB：");
            ed.WriteMessage($"\n  当前：{(string.IsNullOrEmpty(report.CurrentCtb) ? "无" : report.CurrentCtb)}");
            ed.WriteMessage($"\n  标准：{StandardPaths.CtbFileName}");
            ed.WriteMessage($"\n  结果：{(report.CtbCorrect ? "正确" : "需手动确认")}");

            if (!string.IsNullOrEmpty(report.CurrentLayer))
            {
                ed.WriteMessage("\n\n默认状态：");
                ed.WriteMessage($"\n  当前图层：{report.CurrentLayer}");
                ed.WriteMessage($"\n  当前文字样式：{report.CurrentTextStyle}");
                ed.WriteMessage($"\n  当前标注样式：{report.CurrentDimStyle}");
            }

            ed.WriteMessage("\n\n================================");
        }
    }
}
