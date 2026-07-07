using System.Collections.Generic;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Ctb
{
    public class CtbCheckReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public string CtbName { get; set; } = string.Empty;

        public int StandardLayerCount { get; set; }
        public int ExistingStandardLayerCount { get; set; }
        public int MissingStandardLayerCount { get; set; }
        public int CtbRuleColorCount { get; set; }

        public int ValidLayerColorCount { get; set; }
        public int InvalidLayerColorCount { get; set; }
        public int ColorMismatchCount { get; set; }
        public int NonStandardLayerCount { get; set; }
        public int NonStandardLayerInvalidColorCount { get; set; }

        public List<string> MissingStandardLayers { get; set; } = new();
        public List<string> ColorMismatches { get; set; } = new();
        public List<string> InvalidCtbColors { get; set; } = new();
        public List<string> NonStandardLayers { get; set; } = new();
        public List<string> NonStandardLayerInvalidColors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class CtbExportReport
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;

        public string CtbName { get; set; } = string.Empty;
        public int RuleCount { get; set; }

        public string ExportDirectory { get; set; } = string.Empty;
        public string MarkdownPath { get; set; } = string.Empty;
        public string CsvPath { get; set; } = string.Empty;

        public List<string> Warnings { get; set; } = new();
    }

    public class CtbEditorRow
    {
        public int Color { get; set; }
        public string EditorColor { get; set; } = "Use object color";
        public string Dither { get; set; } = "On";
        public string Grayscale { get; set; } = "Off";
        public string PenNumber { get; set; } = "Automatic";
        public string VirtualPen { get; set; } = "Automatic";
        public int Screening { get; set; } = 100;
        public string Linetype { get; set; } = "Use object linetype";
        public string Adaptive { get; set; } = "On";
        public string Lineweight { get; set; } = "0.18mm";
        public string EndStyle { get; set; } = "Use object end style";
        public string JoinStyle { get; set; } = "Use object join style";
        public string FillStyle { get; set; } = "Use object fill style";
        public string Objects { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
