using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace BS_CAD_STANDARD_1_0_Plugin.Core
{
    public class StandardContext
    {
        private StandardContext(
            Document document,
            Database database,
            Editor editor,
            StandardConfig standardConfig,
            DimStyleStandardConfig? dimStyleConfig,
            string standardConfigPath,
            string dimStyleConfigPath)
        {
            Document = document;
            Database = database;
            Editor = editor;
            StandardConfig = standardConfig;
            DimStyleConfig = dimStyleConfig;
            StandardConfigPath = standardConfigPath;
            DimStyleConfigPath = dimStyleConfigPath;
            PackageRoot = StandardPaths.ResolveRoot();
        }

        public Document Document { get; }
        public Database Database { get; }
        public Editor Editor { get; }
        public StandardConfig StandardConfig { get; }
        public DimStyleStandardConfig? DimStyleConfig { get; }
        public string StandardConfigPath { get; }
        public string DimStyleConfigPath { get; }
        public string PackageRoot { get; }

        public static StandardContext Create(
            StandardConfig standardConfig,
            DimStyleStandardConfig? dimStyleConfig,
            string standardConfigPath,
            string dimStyleConfigPath)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            return new StandardContext(doc, doc.Database, doc.Editor, standardConfig, dimStyleConfig, standardConfigPath, dimStyleConfigPath);
        }
    }
}
