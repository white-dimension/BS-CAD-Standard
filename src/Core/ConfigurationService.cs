using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public static class ConfigurationService
    {
        private const string StandardConfigFile = "BS_CAD_Standard_1.0.json";
        private const string DimStyleConfigFile = "BS_DimStyle_Standard_1.0.json";
        private const string MigrationRulesConfigFile = "BS_Layer_Migration_Rules_1.0.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string CurrentStandardConfigPath { get; private set; } = string.Empty;
        public static string CurrentDimStyleConfigPath { get; private set; } = string.Empty;
        public static string CurrentMigrationRulesPath { get; private set; } = string.Empty;

        public static StandardConfig? LoadStandardConfig(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "CAD standard config 1.0", StandardConfigFile);
            CurrentStandardConfigPath = path ?? string.Empty;
            if (path == null) return null;

            StandardConfig? config = LoadJson<StandardConfig>(ed, path, "CAD standard config 1.0");
            if (config != null)
            {
                ed.WriteMessage($"\nLoaded CAD standard config:\n{Path.GetFileName(path)}");
            }

            return config;
        }

        public static DimStyleStandardConfig? LoadDimStyleConfig(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "dimension style config", DimStyleConfigFile);
            CurrentDimStyleConfigPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<DimStyleStandardConfig>(ed, path, "dimension style config");
        }

        public static MigrationRulesConfig? LoadMigrationRules(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "layer migration rules config", MigrationRulesConfigFile);
            CurrentMigrationRulesPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<MigrationRulesConfig>(ed, path, "layer migration rules config");
        }

        public static StandardContext? CreateContext(Editor ed, bool includeDimStyleConfig = false)
        {
            StandardConfig? standardConfig = LoadStandardConfig(ed);
            if (standardConfig == null) return null;

            DimStyleStandardConfig? dimStyleConfig = includeDimStyleConfig ? LoadDimStyleConfig(ed) : null;
            if (includeDimStyleConfig && dimStyleConfig == null)
            {
                ReportUtils.Warning(ed, "Dimension style config is missing or invalid; skipped dim style initialization.");
            }

            return StandardContext.Create(standardConfig, dimStyleConfig, CurrentStandardConfigPath, CurrentDimStyleConfigPath);
        }

        private static string? ResolveConfigFile(Editor ed, string label, string filename, bool reportMissing = true)
        {
            var searchPaths = new List<string>();

            TryAddAssemblyConfigPaths(searchPaths, filename);
            TryAddEnvPath(searchPaths, filename);
            TryAddStandardConfigDir(searchPaths, filename);

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in searchPaths)
            {
                string normalized;
                try { normalized = Path.GetFullPath(raw); }
                catch { normalized = raw; }

                if (!visited.Add(normalized)) continue;

                if (File.Exists(normalized))
                {
                    ed.WriteMessage($"\n[Info] Found {label}: {normalized}");
                    return normalized;
                }
            }

            if (reportMissing)
            {
                string searched = string.Join("\n  ", visited);
                ReportUtils.Error(ed, $"Cannot find {label}. Checked paths:\n  {searched}");
            }

            return null;
        }

        private static void TryAddAssemblyConfigPaths(List<string> paths, string filename)
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(typeof(ConfigurationService).Assembly.Location) ?? "";
                if (string.IsNullOrEmpty(assemblyDir)) return;

                DirectoryInfo? dir = new DirectoryInfo(assemblyDir);
                while (dir != null)
                {
                    paths.Add(Path.Combine(dir.FullName, "config", filename));
                    dir = dir.Parent;
                }
            }
            catch
            {
                // Ignore unreadable assembly locations.
            }
        }

        private static void TryAddEnvPath(List<string> paths, string filename)
        {
            try
            {
                string? envRoot = Environment.GetEnvironmentVariable("BS_ROOT")
                               ?? Environment.GetEnvironmentVariable("BS_CAD_STANDARD_ROOT");
                if (!string.IsNullOrEmpty(envRoot))
                    paths.Add(Path.Combine(envRoot, "config", filename));
            }
            catch
            {
                // Ignore unreadable environment variables.
            }
        }

        private static void TryAddStandardConfigDir(List<string> paths, string filename)
        {
            try
            {
                paths.Add(Path.Combine(StandardPaths.ConfigDir, filename));
            }
            catch
            {
                // Ignore invalid standard paths.
            }
        }

        private static T? LoadJson<T>(Editor ed, string path, string label) where T : class
        {
            try
            {
                string jsonContent = File.ReadAllText(path);
                T? config = JsonSerializer.Deserialize<T>(jsonContent, JsonOptions);

                if (config == null)
                {
                    ReportUtils.Error(ed, $"{label} JSON deserialized to null: {path}");
                }

                return config;
            }
            catch (JsonException ex)
            {
                ReportUtils.Error(ed, $"{label} JSON parse failed: {path} ({ex.Message})");
            }
            catch (Exception ex)
            {
                ReportUtils.Exception(ed, $"Failed to read {label}: {path}", ex);
            }

            return null;
        }
    }
}
