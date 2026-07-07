using System;
using System.IO;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public static class StandardPaths
    {
        /// <summary>
        /// 解析标准包根目录。
        /// 优先级: BS_ROOT 环境变量 → BS_CAD_STANDARD_ROOT 环境变量 → AppDomain.BaseDirectory/standard
        /// </summary>
        public static string ResolveRoot()
        {
            string? envRoot = Environment.GetEnvironmentVariable("BS_ROOT")
                           ?? Environment.GetEnvironmentVariable("BS_CAD_STANDARD_ROOT");
            if (!string.IsNullOrEmpty(envRoot))
                return envRoot;

            // 回退到可执行文件目录下的 standard/
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fallback = Path.GetFullPath(Path.Combine(baseDir, "standard"));
            return fallback;
        }

        public static string ConfigDir => Path.Combine(ResolveRoot(), "config");
        public static string TemplatesDir => Path.Combine(ResolveRoot(), "templates");
        public static string PlotStylesDir => Path.Combine(ResolveRoot(), "plot_styles");

        // — JSON 配置路径 —
        public static string MainConfigPath => Path.Combine(ConfigDir, "BS_CAD_Standard_1.0.json");
        public static string BackupConfigPath => Path.Combine(ConfigDir, "BS_CAD_Standard_1.0.json");

        public static string DimConfigPath => Path.Combine(ConfigDir, "BS_DimStyle_Standard_1.0.json");
        public static string BackupDimConfigPath => Path.Combine(ConfigDir, "BS_DimStyle_Standard_1.0.json");

        public static string MigrationRulesConfigPath => Path.Combine(ConfigDir, "BS_Layer_Migration_Rules_1.0.json");

        // — CTB —
        public const string CtbFileName = "BS_CAD_STANDARD_1.0.ctb";

        // — DWT —
        public static string DwtPath => Path.Combine(TemplatesDir, "BS_CAD_STANDARD_1.0.dwt");
    }
}
