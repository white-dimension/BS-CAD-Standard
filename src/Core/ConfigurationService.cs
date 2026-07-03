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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static string CurrentStandardConfigPath { get; private set; } = string.Empty;
        public static string CurrentDimStyleConfigPath { get; private set; } = string.Empty;
        public static string CurrentMigrationRulesPath { get; private set; } = string.Empty;

        public static StandardConfig? LoadStandardConfig(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "主配置文件", "BS_CAD_Standard_V10.json");
            CurrentStandardConfigPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<StandardConfig>(ed, path, "主配置文件");
        }

        public static DimStyleStandardConfig? LoadDimStyleConfig(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "标注样式配置文件", "BS_DimStyle_Standard_V10.json");
            CurrentDimStyleConfigPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<DimStyleStandardConfig>(ed, path, "标注样式配置文件");
        }

        public static MigrationRulesConfig? LoadMigrationRules(Editor ed)
        {
            string? path = ResolveConfigFile(ed, "图层迁移规则配置文件", "BS_Layer_Migration_Rules_V10.json");
            CurrentMigrationRulesPath = path ?? string.Empty;

            if (path == null) return null;

            return LoadJson<MigrationRulesConfig>(ed, path, "图层迁移规则配置文件");
        }

        public static StandardContext? CreateContext(Editor ed, bool includeDimStyleConfig = false)
        {
            StandardConfig? standardConfig = LoadStandardConfig(ed);
            if (standardConfig == null) return null;

            DimStyleStandardConfig? dimStyleConfig = includeDimStyleConfig ? LoadDimStyleConfig(ed) : null;
            if (includeDimStyleConfig && dimStyleConfig == null)
            {
                ReportUtils.Warning(ed, "标注样式配置文件缺失或解析失败，跳过标注样式初始化。");
            }

            return StandardContext.Create(standardConfig, dimStyleConfig, CurrentStandardConfigPath, CurrentDimStyleConfigPath);
        }

        /// <summary>
        /// 按优先级搜索配置文件。查找顺序（先命中先返回）：
        ///
        ///   ① DLL 所在目录的上一级 config
        ///      测试包场景: E:\BS_CAD_STANDARD_V10_TestPackage\config\
        ///
        ///   ② DLL 所在目录下的 config
        ///      测试包场景: E:\BS_CAD_STANDARD_V10_TestPackage\plugin\config\
        ///
        ///   ③ 环境变量 BS_CAD_STANDARD_ROOT 指向目录下的 config
        ///
        ///   ④ 插件项目 config 备用路径（硬编码，仅开发环境）
        ///      D:\01_DesignProjects\BS_CAD_STANDARD_V10_Plugin\config\
        ///
        ///   ⑤ 开发机固定标准包路径（硬编码，仅开发环境兜底）
        ///      D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\config\
        /// </summary>
        private static string? ResolveConfigFile(Editor ed, string label, string filename)
        {
            var searchPaths = new List<string>();

            // ── ① DLL 所在目录的上一级 config ──
            // 测试包 root/plugin/xxx.dll → root/config/xxx.json
            TryAddAssemblyRelativePath(searchPaths, filename, goUp: true);

            // ── ② DLL 所在目录下的 config ──
            // 测试包 root/plugin/xxx.dll → root/plugin/config/xxx.json
            TryAddAssemblyRelativePath(searchPaths, filename, goUp: false);

            // ── ③ 环境变量 BS_CAD_STANDARD_ROOT → config ──
            TryAddEnvPath(searchPaths, filename);

            // ── ④ 插件项目 config 备用路径（硬编码，开发环境）──
            TryAddBackupPath(searchPaths, filename);

            // ── ⑤ 开发机固定标准包路径（硬编码，最后兜底）──
            TryAddPackagePath(searchPaths, filename);

            // 去重搜索
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in searchPaths)
            {
                string normalized;
                try { normalized = Path.GetFullPath(raw); }
                catch { normalized = raw; }

                if (!visited.Add(normalized)) continue;

                if (File.Exists(normalized))
                {
                    ed.WriteMessage($"\n[信息] 找到 {label}: {normalized}");
                    return normalized;
                }
            }

            string searched = string.Join("\n  ", visited);
            ReportUtils.Error(ed, $"找不到 {label}。已检查路径:\n  {searched}");
            return null;
        }

        /// <summary>
        /// 添加相对于 DLL 所在目录的 config 路径。
        /// goUp=true  → DLL 的上一级目录下的 config（测试包包根）
        /// goUp=false → DLL 所在目录下的 config
        /// </summary>
        private static void TryAddAssemblyRelativePath(List<string> paths, string filename, bool goUp)
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(typeof(ConfigurationService).Assembly.Location) ?? "";
                if (string.IsNullOrEmpty(assemblyDir)) return;

                string baseDir = goUp
                    ? (Path.GetDirectoryName(assemblyDir) ?? assemblyDir)
                    : assemblyDir;

                paths.Add(Path.Combine(baseDir, "config", filename));
            }
            catch
            {
                // 静默跳过，当前环境可能无法读取 Assembly.Location
            }
        }

        /// <summary>
        /// 添加环境变量 BS_CAD_STANDARD_ROOT → config 下的路径
        /// </summary>
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
                // 静默跳过，环境变量可能不可读
            }
        }

        /// <summary>
        /// 添加插件项目备用路径（硬编码，仅开发环境）
        /// </summary>
        private static void TryAddBackupPath(List<string> paths, string filename)
        {
            try
            {
                if (filename == "BS_CAD_Standard_V10.json")
                    paths.Add(StandardPaths.BackupConfigPath);
                else if (filename == "BS_DimStyle_Standard_V10.json")
                    paths.Add(StandardPaths.BackupDimConfigPath);
            }
            catch
            {
                // 静默跳过
            }
        }

        /// <summary>
        /// 添加开发机固定标准包路径（硬编码，最后兜底）
        /// </summary>
        private static void TryAddPackagePath(List<string> paths, string filename)
        {
            try
            {
                paths.Add(Path.Combine(StandardPaths.PackageRoot, "config", filename));
            }
            catch
            {
                // 静默跳过
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
                    ReportUtils.Error(ed, $"{label} JSON 反序列化结果为空: {path}");
                }

                return config;
            }
            catch (JsonException ex)
            {
                ReportUtils.Error(ed, $"{label} JSON 解析失败: {path} ({ex.Message})");
            }
            catch (Exception ex)
            {
                ReportUtils.Exception(ed, $"读取{label}失败: {path}", ex);
            }

            return null;
        }
    }
}
