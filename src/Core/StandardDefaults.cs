using System.Collections.Generic;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public static class StandardDefaults
    {
        // 默认文字样式列表
        public static readonly List<string> TextStyles = new()
        {
            "BS_TEXT_CN",
            "BS_TEXT_EN",
            "BS_TEXT_TITLE",
            "BS_TEXT_TABLE",
            "BS_TEXT_NOTE"
        };

        // 默认标注样式列表
        public static readonly List<string> DimStyles = new()
        {
            "BS_DIM_100",
            "BS_DIM_50",
            "BS_DIM_DETAIL"
        };

        // 默认多重引线样式
        public const string MLeaderStyleNote = "BS_MLEADER_NOTE";

        // 标准单位 (4 = mm)
        public const int StandardUnits = 4;

        // 标准精度
        public const int StandardLinearPrecision = 2; // 0.00
        public const int StandardAngularPrecision = 0; // 0

        // 默认文字样式
        public const string DefaultTextStyle = "BS_TEXT_CN";

        // 默认文字图层名 (Fallback)
        public const string FallbackTextLayer = "16-TX-普通文字";

        // 默认文字高度
        public const double DefaultTextHeight = 250.0;
        // 默认标注图层名
        public const string FallbackDimLayer = "14-DM-尺寸标注";

        // 标注样式比例定义
        public static readonly Dictionary<string, double> DimStyleScales = new()
        {
            { "BS_DIM_100", 100.0 },
            { "BS_DIM_50", 50.0 },
            { "BS_DIM_DETAIL", 20.0 }
        };
    }
}
