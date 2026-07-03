using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;

namespace BS_CAD_STANDARD_V10_Plugin.Utils
{
    public static class LayerPropertyUtils
    {
        public static ObjectId ResolveLinetypeId(Transaction tr, string linetypeName, Editor ed)
        {
            if (string.Equals(linetypeName, "Continuous", StringComparison.OrdinalIgnoreCase))
                return HostApplicationServices.WorkingDatabase.ContinuousLinetype;

            LinetypeTable lt = (LinetypeTable)tr.GetObject(
                HostApplicationServices.WorkingDatabase.LinetypeTableId, OpenMode.ForRead);

            if (lt.Has(linetypeName))
                return lt[linetypeName];

            // Try to load
            try
            {
                lt.UpgradeOpen();
                HostApplicationServices.WorkingDatabase.LoadLineTypeFile(linetypeName, "acadiso.lin");
                lt.DowngradeOpen();

                if (lt.Has(linetypeName))
                    return lt[linetypeName];
            }
            catch
            {
                try
                {
                    HostApplicationServices.WorkingDatabase.LoadLineTypeFile(linetypeName, "acad.lin");
                    if (lt.Has(linetypeName))
                        return lt[linetypeName];
                }
                catch
                {
                    ed.WriteMessage($"\n[Warning] Cannot load linetype '{linetypeName}'.");
                }
            }

            return ObjectId.Null;
        }

        public static string GetLinetypeName(Transaction tr, LayerTableRecord layerRecord)
        {
            try
            {
                LinetypeTableRecord ltr = (LinetypeTableRecord)tr.GetObject(
                    layerRecord.LinetypeObjectId, OpenMode.ForRead);
                return ltr.Name;
            }
            catch
            {
                return "Continuous";
            }
        }

        public static int GetTransparencyPercent(LayerTableRecord layerRecord)
        {
            try
            {
                byte alpha = layerRecord.Transparency.Alpha;
                int percent = 100 - (int)Math.Round(alpha * 100.0 / 255.0);
                if (percent < 0) return 0;
                if (percent > 90) return 90;
                return percent;
            }
            catch
            {
                return 0;
            }
        }

        public static void SetTransparencyPercent(LayerTableRecord layerRecord, int percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 90) percent = 90;
            byte alpha = (byte)(255 - (byte)Math.Round(percent * 255.0 / 100.0));
            layerRecord.Transparency = new Transparency(alpha);
        }

        public static bool IsExcludedLayer(string name)
        {
            string[] excluded = { "0", "Defpoints" };
            return excluded.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase));
        }

        public static ObjectId GetOrCreateLinetypeId(Transaction tr, string linetypeName, Editor ed, List<string> warnings)
        {
            ObjectId id = ResolveLinetypeId(tr, linetypeName, ed);
            if (id == ObjectId.Null && !string.Equals(linetypeName, "Continuous", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Linetype '{linetypeName}' could not be loaded; using Continuous.");
                return HostApplicationServices.WorkingDatabase.ContinuousLinetype;
            }
            return id;
        }
    }
}
