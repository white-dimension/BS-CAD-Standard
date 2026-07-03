using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using System;

namespace BS_CAD_STANDARD_V10_Plugin.Utils
{
    public static class AcadUtils
    {
        /// <summary>
        /// 将 AutoCAD LineWeight 转换为毫米数值
        /// </summary>
        public static double LineWeightToMm(LineWeight lw)
        {
            if (lw == LineWeight.ByLayer || lw == LineWeight.ByBlock || lw == LineWeight.ByLineWeightDefault)
                return -1.0;

            return (double)lw / 100.0;
        }

        /// <summary>
        /// 将毫米数值转换为最接近的 AutoCAD LineWeight 枚举
        /// </summary>
        public static LineWeight LineWeightFromMm(double mm)
        {
            if (mm < 0) return LineWeight.ByLayer;

            int val = (int)Math.Round(mm * 100.0);

            // AutoCAD 标准线宽值（1/100 mm）
            int[] standardWeights = { 0, 5, 9, 13, 15, 18, 20, 25, 30, 35, 40, 50, 53, 60, 70, 80, 90, 100, 106, 120, 140, 158, 200, 211 };

            int closest = standardWeights[0];
            int minDiff = Math.Abs(val - closest);

            foreach (int sw in standardWeights)
            {
                int diff = Math.Abs(val - sw);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = sw;
                }
            }

            return (LineWeight)closest;
        }

        /// <summary>
        /// 根据 ACI 索引获取颜色对象
        /// </summary>
        public static Color ColorFromIndex(int aci)
        {
            return Color.FromColorIndex(ColorMethod.ByAci, (short)aci);
        }

        /// <summary>
        /// 确保线型已加载
        /// </summary>
        public static bool EnsureLinetypeLoaded(string linetypeName)
        {
            if (string.Equals(linetypeName, "Continuous", StringComparison.OrdinalIgnoreCase))
                return true;

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LinetypeTable lt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (lt.Has(linetypeName)) return true;
                tr.Commit();
            }

            // 尝试加载
            try
            {
                db.LoadLineTypeFile(linetypeName, "acadiso.lin");
                return true;
            }
            catch
            {
                try
                {
                    db.LoadLineTypeFile(linetypeName, "acad.lin");
                    return true;
                }
                catch
                {
                    doc.Editor.WriteMessage($"\n[警告] 无法加载线型: {linetypeName}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 获取文字样式的 ObjectId
        /// </summary>
        public static ObjectId GetTextStyleId(string styleName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TextStyleTable st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                if (st.Has(styleName)) return st[styleName];
                return ObjectId.Null;
            }
        }

        /// <summary>
        /// 安全获取系统变量
        /// </summary>
        public static object? SafeGetSystemVariable(string name)
        {
            try
            {
                return Application.GetSystemVariable(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
