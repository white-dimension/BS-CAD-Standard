using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System.IO;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public class DimStyleConfigLoader
    {
        public static DimStyleStandardConfig? LoadConfiguration()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            return ConfigurationService.LoadDimStyleConfig(ed);
        }

        public static string GetCurrentUsedPath()
        {
            if (!string.IsNullOrEmpty(ConfigurationService.CurrentDimStyleConfigPath))
            {
                return ConfigurationService.CurrentDimStyleConfigPath;
            }

            return StandardPaths.DimConfigPath;
        }
    }
}
