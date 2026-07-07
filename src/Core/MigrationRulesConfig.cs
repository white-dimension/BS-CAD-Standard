using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public class MigrationRulesConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("standardName")]
        public string StandardName { get; set; } = string.Empty;

        [JsonPropertyName("rules")]
        public List<MigrationRule> Rules { get; set; } = new();
    }

    public class MigrationRule
    {
        [JsonPropertyName("rule")]
        public string Rule { get; set; } = string.Empty;

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        [JsonPropertyName("targetLayer")]
        public string TargetLayer { get; set; } = string.Empty;
    }
}
