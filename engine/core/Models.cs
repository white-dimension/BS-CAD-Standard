using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Core
{
    public class CheckResult
    {
        public List<string> MissingCoreLayers { get; set; } = new();
        public List<string> PropertyDeviations { get; set; } = new();
        public List<string> ExtraLayers { get; set; } = new();
        public List<string> ColorDeviations { get; set; } = new();
        public List<string> LinetypeDeviations { get; set; } = new();
        public List<string> TransparencyDeviations { get; set; } = new();
        public List<string> PlotDeviations { get; set; } = new();
        public List<string> MissingTextStyles { get; set; } = new();
        public List<string> TextStyleFontDeviations { get; set; } = new();
        public List<string> MissingDimStyles { get; set; } = new();
        public bool MLeaderStyleExists { get; set; }
        public int CurrentUnits { get; set; }
        public string CurrentLayoutName { get; set; } = string.Empty;
        public string CurrentCtb { get; set; } = string.Empty;
    }
}
