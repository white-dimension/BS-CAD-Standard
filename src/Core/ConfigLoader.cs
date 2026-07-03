using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.IO;

namespace BS_CAD_STANDARD_V10_Plugin.Core
{
    public class ConfigLoader
    {
        public static StandardConfig? LoadConfiguration()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            return ConfigurationService.LoadStandardConfig(ed);
        }

        public static string GetCurrentUsedPath()
        {
            if (!string.IsNullOrEmpty(ConfigurationService.CurrentStandardConfigPath))
            {
                return ConfigurationService.CurrentStandardConfigPath;
            }

            return File.Exists(StandardPaths.MainConfigPath)
                ? StandardPaths.MainConfigPath
                : StandardPaths.BackupConfigPath;
        }
    }
}
