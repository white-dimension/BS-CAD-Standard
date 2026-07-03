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

        [JsonPropertyName("layers")]
        public List<LayerConfig> Layers { get; set; } = new();

        [JsonPropertyName("styles")]
        public StylesConfig Styles { get; set; } = new();
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

        [JsonPropertyName("layers")]
        public List<BsCadLayerV06> Layers { get; set; } = new();
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
    }
}
