using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BS_CAD_STANDARD_V10_Plugin.Core
{
    public class StandardConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("standardName")]
        public string StandardName { get; set; } = string.Empty;

        [JsonPropertyName("packageName")]
        public string PackageName { get; set; } = string.Empty;

        [JsonPropertyName("ctb")]
        public string Ctb { get; set; } = string.Empty;

        [JsonPropertyName("ctbRules")]
        public List<CtbRuleConfig> CtbRules { get; set; } = new();

        [JsonPropertyName("layers")]
        public List<LayerConfig> Layers { get; set; } = new();

        [JsonPropertyName("styles")]
        public StylesConfig Styles { get; set; } = new();

        [JsonPropertyName("loadModes")]
        public List<LoadModeConfig> LoadModes { get; set; } = new();

        [JsonIgnore]
        public List<StandardCategory> ConfigCategories { get; set; } = new();
    }

    public class StylesConfig
    {
        [JsonPropertyName("textStyles")]
        public List<string> TextStyles { get; set; } = new();

        [JsonPropertyName("dimStyles")]
        public List<string> DimStyles { get; set; } = new();
    }

    public class LayerConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("linetype")]
        public string Linetype { get; set; } = "Continuous";

        [JsonPropertyName("lineweight")]
        public double Lineweight { get; set; }

        [JsonPropertyName("transparency")]
        public int Transparency { get; set; }

        [JsonPropertyName("plot")]
        public bool Plot { get; set; } = true;

        [JsonPropertyName("core")]
        public bool Core { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("categoryNo")]
        public string CategoryNo { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonIgnore]
        public bool Locked { get; set; }

        [JsonIgnore]
        public string NewViewportFreezeRaw { get; set; } = string.Empty;

        [JsonIgnore]
        public int OrderIndex { get; set; }

        [JsonIgnore]
        public string LayerOrderSource { get; set; } = string.Empty;
    }

    public class BsCadCategoryV06
    {
        [JsonPropertyName("categoryNo")]
        public string CategoryNo { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("firstOrder")]
        public int FirstOrder { get; set; }

        [JsonPropertyName("layerCount")]
        public int LayerCount { get; set; }
    }

    public class BsCadStandardV06
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("standardName")]
        public string StandardName { get; set; } = string.Empty;

        [JsonPropertyName("packageName")]
        public string PackageName { get; set; } = string.Empty;

        [JsonPropertyName("ctb")]
        public string Ctb { get; set; } = string.Empty;

        [JsonPropertyName("layerCount")]
        public int LayerCount { get; set; }

        [JsonPropertyName("categoryCount")]
        public int CategoryCount { get; set; }

        [JsonPropertyName("categories")]
        public List<BsCadCategoryV06> Categories { get; set; } = new();

        [JsonPropertyName("layers")]
        public List<BsCadLayerV06> Layers { get; set; } = new();

        [JsonPropertyName("ctbRules")]
        public List<CtbRuleConfig> CtbRules { get; set; } = new();

        [JsonPropertyName("loadModes")]
        public List<LoadModeConfig> LoadModes { get; set; } = new();
    }

    public class BsCadLayerV06
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public short Color { get; set; }

        [JsonPropertyName("linetype")]
        public string Linetype { get; set; } = "Continuous";

        [JsonPropertyName("lineweight")]
        public string Lineweight { get; set; } = "ByCTB";

        [JsonPropertyName("transparency")]
        public int Transparency { get; set; }

        [JsonPropertyName("plot")]
        public JsonElement Plot { get; set; }

        [JsonPropertyName("core")]
        public bool Core { get; set; } = true;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("categoryCode")]
        public string CategoryCode { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("categoryNo")]
        public string CategoryNo { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("locked")]
        public JsonElement LockedJson { get; set; }

        [JsonPropertyName("newViewportFreeze")]
        public JsonElement NewViewportFreezeJson { get; set; }
    }

    public class StandardCategory
    {
        public string CategoryNo { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }

    public class CtbRuleConfig
    {
        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("preview")]
        public string Preview { get; set; } = string.Empty;

        [JsonPropertyName("screenUse")]
        public string ScreenUse { get; set; } = string.Empty;

        [JsonPropertyName("plotColor")]
        public string PlotColor { get; set; } = string.Empty;

        [JsonPropertyName("plotLineweight")]
        public string PlotLineweight { get; set; } = string.Empty;

        [JsonPropertyName("objects")]
        public string Objects { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;

        // CTB editor fields (optional, with fallback in export)
        [JsonPropertyName("editorColor")]
        public string? EditorColor { get; set; }

        [JsonPropertyName("dither")]
        public string? Dither { get; set; }

        [JsonPropertyName("grayscale")]
        public string? Grayscale { get; set; }

        [JsonPropertyName("penNumber")]
        public string? PenNumber { get; set; }

        [JsonPropertyName("virtualPen")]
        public string? VirtualPen { get; set; }

        [JsonPropertyName("screening")]
        public int? Screening { get; set; }

        [JsonPropertyName("linetype")]
        public string? Linetype { get; set; }

        [JsonPropertyName("adaptive")]
        public string? Adaptive { get; set; }

        [JsonPropertyName("endStyle")]
        public string? EndStyle { get; set; }

        [JsonPropertyName("joinStyle")]
        public string? JoinStyle { get; set; }

        [JsonPropertyName("fillStyle")]
        public string? FillStyle { get; set; }
    }

    public class LoadModeConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public string Usage { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("futureDetail")]
        public string FutureDetail { get; set; } = string.Empty;

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;

        [JsonPropertyName("layers")]
        public List<string> Layers { get; set; } = new();
    }
}
