using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Engine.Template
{
    /// <summary>
    /// 单位检查器 — 检查 INSUNITS / LUNITS / LUPREC / AUPREC。
    /// </summary>
    public class UnitChecker
    {
        public void Run(TemplateCheckReport report)
        {
            CheckUnits(report);
        }

        private static void CheckUnits(TemplateCheckReport report)
        {
            string InsUnitsStr = SafeSysVar("INSUNITS");
            string LunitsStr = SafeSysVar("LUNITS");
            string LuprecStr = SafeSysVar("LUPREC");
            string AunitsStr = SafeSysVar("AUNITS");
            string AuprecStr = SafeSysVar("AUPREC");

            if (InsUnitsStr == "4")
                AddOk(report, $"INSUNITS = {InsUnitsStr}，单位为毫米");
            else
                AddWarn(report, $"INSUNITS = {InsUnitsStr}，建议为 4（毫米）");

            if (LunitsStr == "2")
                AddOk(report, $"LUNITS = {LunitsStr}，十进制");
            else
                AddWarn(report, $"LUNITS = {LunitsStr}，建议为 2（十进制）");

            AddInfo(report, $"LUPREC = {LuprecStr}");
            AddInfo(report, $"AUNITS = {AunitsStr}, AUPREC = {AuprecStr}");
        }

        private static string SafeSysVar(string name)
        {
            object? val = AcadUtils.SafeGetSystemVariable(name);
            if (val == null) return "?";
            return val.ToString() ?? "?";
        }

        private static void AddOk(TemplateCheckReport r, string msg) { r.OkCount++; r.Lines.Add($"  [OK] {msg}"); }
        private static void AddWarn(TemplateCheckReport r, string msg) { r.WarnCount++; r.Lines.Add($"  [WARN] {msg}"); }
        private static void AddInfo(TemplateCheckReport r, string msg) { r.InfoCount++; r.Lines.Add($"  [INFO] {msg}"); }
    }
}
