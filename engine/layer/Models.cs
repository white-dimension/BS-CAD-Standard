using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Engine.Layer
{
    public class CategoryInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryNo { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int LayerCount { get; set; }
        public int OrderIndex { get; set; }
    }
}
