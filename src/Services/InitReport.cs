namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public class InitReport
    {
        // 图层
        public int ExistingLayers { get; set; }
        public int MissingCoreLayers { get; set; }
        public int CreatedLayers { get; set; }
        public int UserSkippedLayers { get; set; }
        public int FailedLayers { get; set; }

        // 文字样式
        public int ExistingTextStyles { get; set; }
        public int MissingTextStyles { get; set; }
        public int CreatedTextStyles { get; set; }
        public int SkippedTextStyles { get; set; }
        public int FailedTextStyles { get; set; }

        // 标注样式
        public int ExistingDimStyles { get; set; }
        public int MissingDimStyles { get; set; }
        public int CreatedDimStyles { get; set; }
        public int SkippedDimStyles { get; set; }
        public int FailedDimStyles { get; set; }

        // 多重引线样式
        public string MLeaderStatus { get; set; } = string.Empty; // "已存在" / "已创建" / "跳过" / "创建失败"

        // 单位
        public string OldUnits { get; set; } = string.Empty;
        public string NewUnits { get; set; } = string.Empty;
        public string OldPrecision { get; set; } = string.Empty;
        public string NewPrecision { get; set; } = string.Empty;

        // CTB
        public string CurrentCtb { get; set; } = string.Empty;
        public bool CtbCorrect { get; set; }

        // 默认状态
        public string CurrentLayer { get; set; } = string.Empty;
        public string CurrentTextStyle { get; set; } = string.Empty;
        public string CurrentDimStyle { get; set; } = string.Empty;
    }
}
