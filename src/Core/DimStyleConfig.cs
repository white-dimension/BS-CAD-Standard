using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public class DimStyleStandardConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("standardName")]
        public string StandardName { get; set; } = string.Empty;

        [JsonPropertyName("defaultDimStyle")]
        public string DefaultDimStyle { get; set; } = string.Empty;

        [JsonPropertyName("defaultLayer")]
        public string DefaultLayer { get; set; } = string.Empty;

        [JsonPropertyName("dimStyles")]
        public List<DimStyleConfig> DimStyles { get; set; } = new();
    }

    public class DimStyleConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("textStyle")]
        public string TextStyle { get; set; } = string.Empty;

        [JsonPropertyName("textHeight")]
        public double TextHeight { get; set; }

        [JsonPropertyName("textOffset")]
        public double TextOffset { get; set; } // DIMGAP (图片=1)

        [JsonPropertyName("arrowType")]
        public string ArrowType { get; set; } = string.Empty;

        [JsonPropertyName("arrowSize")]
        public double ArrowSize { get; set; } // DIMASZ (图片=2)

        [JsonPropertyName("centerMarkSize")]
        public double CenterMarkSize { get; set; } // DIMCEN (图片=1)

        [JsonPropertyName("baselineSpacing")]
        public double BaselineSpacing { get; set; } // DIMDLI (图片=12)

        [JsonPropertyName("extendBeyondDimLines")]
        public double ExtendBeyondDimLines { get; set; } // DIMEXE (图片=1.5)

        [JsonPropertyName("offsetFromOrigin")]
        public double OffsetFromOrigin { get; set; } // DIMEXO (图片=2.0)

        [JsonPropertyName("useFixedExtensionLines")]
        public bool UseFixedExtensionLines { get; set; } // DIMFXLEN_ON (图片=勾选)

        [JsonPropertyName("extensionLineLength")]
        public double ExtensionLineLength { get; set; } // DIMFXL (图片=5.0)

        [JsonPropertyName("dimScale")]
        public double DimScale { get; set; }

        [JsonPropertyName("decimalPrecision")]
        public int DecimalPrecision { get; set; }

        [JsonPropertyName("unitFormat")]
        public string UnitFormat { get; set; } = "Decimal";

        [JsonPropertyName("trailingZeroSuppression")]
        public bool TrailingZeroSuppression { get; set; } // DIMZIN (图片=勾选后续)

        [JsonPropertyName("colorMode")]
        public string ColorMode { get; set; } = "ByLayer";

        [JsonPropertyName("lineweightMode")]
        public string LineweightMode { get; set; } = "ByLayer";

        [JsonPropertyName("linetypeMode")]
        public string LinetypeMode { get; set; } = "ByLayer";

        [JsonPropertyName("annotative")]
        public bool Annotative { get; set; }
    }
}
