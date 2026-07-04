using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Engine.Ctb;

namespace BS_CAD_STANDARD_V10_Plugin.Cad.Services
{
    /// <summary>
    /// CTB 桥接层 — cad → engine 的唯一切入点。
    /// </summary>
    public static class CtbBridge
    {
        private static readonly CtbEngine _engine = new();

        public static CtbCheckReport RunCheck(StandardConfig config)
        {
            return _engine.Check(config);
        }

        public static CtbExportReport Export(StandardConfig config)
        {
            return _engine.Export(config);
        }
    }
}
