using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

[assembly: ExtensionApplication(typeof(BS_CAD_STANDARD_1_0_Plugin.PluginEntry))]

namespace BS_CAD_STANDARD_1_0_Plugin
{
    public class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nBS CAD Standard 1.0 Plugin loaded.");
        }

        public void Terminate()
        {
            // 清理资源
        }
    }
}
