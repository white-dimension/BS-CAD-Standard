using System.Collections.Generic;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Template
{
    public class TemplateCheckReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public int OkCount { get; set; }
        public int WarnCount { get; set; }
        public int InfoCount { get; set; }
        public int ErrorCount { get; set; }

        public List<string> Lines { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}
