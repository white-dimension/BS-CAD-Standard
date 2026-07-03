using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_V10_Plugin.Utils;

namespace BS_CAD_STANDARD_V10_Plugin.Core
{
    public static class ConfigurationService
    {
        private const string V06StandardConfigFile = "BS_CAD_Standard_v0.6.json";
        private const string V10StandardConfigFile = "BS_CAD_Standard_V10.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string CurrentStandardConfigPath { get; private set; } = string.Empty;
        public static string CurrentDimStyleConfigPath { get; private set; } = string.Empty;
        public static string CurrentMigrationRulesPath { get; private set; } = string.Empty;

        public static StandardConfig? LoadStandardConfig(Editor ed)
        {
            string? v06Path = ResolveConfigFile(ed, "CAD standard config v0.6", V06StandardConfigFile, reportMissing: false);
            if (v06Path != null)
            {
                StandardConfig? v06Config = LoadStandardConfigV06(ed, v06Path);
                if (v06Config != null)
                {
                    CurrentStandardConfigPath = v06Path;
                    ed.WriteMessage($"\nLoaded CAD standard config:\n{ToDisplayConfigPath(v06Path)}");
                    return v06Config;
                }

                ed.WriteMessage("\n[Warning] v0.6 config failed to load, fallback to V10 config.");
            }
            else
            {
                ed.WriteMessage("\nv0.6 config not found, fallback to:\nconfig\\BS_CAD_Standard_V10.json");
            }

            string? path = ResolveConfigFile(ed, "CAD standard config V10", V10StandardConfigFile);
            CurrentStandardConfigPath = path ?? string.Empty;
            if (path == null) return null;

            StandardConfig? config = LoadJson<StandardConfig>(ed, path, "CAD standard config V10");
            if (config != null)
            {
                ed.WriteMessage($"\nLoaded CAD standard config:\n{ToDisplayConfigPath(path)}");
            }

            return config;
        }

        public static DimStyleStandardConfig? LoadDimStyleConfig(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "dimension style config", "BS_DimStyle_Standard_V10.json");
            CurrentDimStyleConfigPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<DimStyleStandardConfig>(ed, path, "dimension style config");
        }

        public static MigrationRulesConfig? LoadMigrationRules(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "layer migration rules config", "BS_Layer_Migration_Rules_V10.json");
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
            TryAddBackupPath(searchPaths, filename);
            TryAddPackagePath(searchPaths, filename);

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
                string? envRoot = Environment.GetEnvironmentVariable("BS_CAD_STANDARD_ROOT");
                if (!string.IsNullOrEmpty(envRoot))
                    paths.Add(Path.Combine(envRoot, "config", filename));
            }
            catch
            {
                // Ignore unreadable environment variables.
            }
        }

        private static void TryAddBackupPath(List<string> paths, string filename)
        {
            try
            {
                if (filename == V10StandardConfigFile)
                    paths.Add(StandardPaths.BackupConfigPath);
                else if (filename == "BS_DimStyle_Standard_V10.json")
                    paths.Add(StandardPaths.BackupDimConfigPath);
            }
            catch
            {
                // Ignore invalid backup paths.
            }
        }

        private static void TryAddPackagePath(List<string> paths, string filename)
        {
            try
            {
                paths.Add(Path.Combine(StandardPaths.PackageRoot, "config", filename));
            }
            catch
            {
                // Ignore invalid package paths.
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

        private static StandardConfig? LoadStandardConfigV06(Editor ed, string path)
        {
            try
            {
                string jsonContent = File.ReadAllText(path);
                BsCadStandardV06? source = JsonSerializer.Deserialize<BsCadStandardV06>(jsonContent, JsonOptions);
                if (source == null)
                {
                    ReportUtils.Error(ed, $"v0.6 CAD standard JSON deserialized to null: {path}");
                    return null;
                }

                StandardConfig config = new()
                {
                    Version = source.Version,
                    StandardName = source.StandardName,
                    PackageName = source.PackageName,
                    Ctb = source.Ctb
                };

                // Store categories from JSON
                config.ConfigCategories = source.Categories
                    .Select(c => new StandardCategory
                    {
                        CategoryNo = c.CategoryNo,
                        CategoryName = c.CategoryName
                    })
                    .ToList();

                for (int i = 0; i < source.Layers.Count; i++)
                {
                    config.Layers.Add(MapLayerV06(source.Layers[i]));
                }

                return config;
            }
            catch (JsonException ex)
            {
                ReportUtils.Error(ed, $"v0.6 CAD standard JSON parse failed: {path} ({ex.Message})");
            }
            catch (Exception ex)
            {
                ReportUtils.Exception(ed, $"Failed to read v0.6 CAD standard config: {path}", ex);
            }

            return null;
        }

        private static LayerConfig MapLayerV06(BsCadLayerV06 source)
        {
            int order = source.Order > 0 ? source.Order : 0;
            return new LayerConfig
            {
                Name = source.Name,
                Color = source.Color,
                Linetype = string.IsNullOrWhiteSpace(source.Linetype) ? "Continuous" : source.Linetype,
                Lineweight = ParseLineweight(source.Lineweight),
                Transparency = source.Transparency,
                Plot = ParsePlot(source.Plot),
                Core = true,
                Category = ResolveCategory(source),
                Description = source.Description,
                Order = order,
                CategoryNo = source.CategoryNo,
                CategoryName = source.CategoryName,
                Locked = ParseLocked(source.LockedJson),
                NewViewportFreezeRaw = ParseNewViewportFreeze(source.NewViewportFreezeJson),
                OrderIndex = order > 0 ? order : 0,
                LayerOrderSource = "JSON order field"
            };
        }

        private static double ParseLineweight(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return -1.0;

            string text = value.Trim();
            if (string.Equals(text, "ByCTB", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "ByLayer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Default", StringComparison.OrdinalIgnoreCase))
            {
                return -1.0;
            }

            text = text.Replace("mm", "", StringComparison.OrdinalIgnoreCase).Trim();
            return double.TryParse(text, out double mm) ? mm : -1.0;
        }

        private static bool ParseLocked(JsonElement locked)
        {
            if (locked.ValueKind == JsonValueKind.True) return true;
            if (locked.ValueKind == JsonValueKind.False) return false;
            return false;
        }

        private static string ParseNewViewportFreeze(JsonElement vpf)
        {
            if (vpf.ValueKind == JsonValueKind.True) return "true";
            if (vpf.ValueKind == JsonValueKind.False) return "false";
            if (vpf.ValueKind == JsonValueKind.String)
            {
                string? val = vpf.GetString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
            return string.Empty;
        }

        private static bool ParsePlot(JsonElement plot)
        {
            if (plot.ValueKind == JsonValueKind.False) return false;
            if (plot.ValueKind == JsonValueKind.True ||
                plot.ValueKind == JsonValueKind.Undefined ||
                plot.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (plot.ValueKind == JsonValueKind.String)
            {
                string? value = plot.GetString();
                if (string.Equals(value, "lightOrNoPlot", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.Equals(value, "onDemand", StringComparison.OrdinalIgnoreCase)) return true;
                if (bool.TryParse(value, out bool parsed)) return parsed;
            }

            return true;
        }

        private static string ResolveCategory(BsCadLayerV06 source)
        {
            if (!string.IsNullOrWhiteSpace(source.CategoryNo)) return source.CategoryNo;
            if (!string.IsNullOrWhiteSpace(source.CategoryCode)) return source.CategoryCode;
            if (!string.IsNullOrWhiteSpace(source.Category)) return source.Category;

            string[] parts = (source.Name ?? string.Empty).Split('-');
            return parts.Length >= 2 ? parts[1] : string.Empty;
        }

        private static string ToDisplayConfigPath(string path)
        {
            int index = path.LastIndexOf("\\config\\", StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? path.Substring(index + 1) : path;
        }
    }
}
