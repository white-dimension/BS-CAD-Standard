using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Engine.Core;

namespace BS_CAD_STANDARD_V10_Plugin.Cad.Services
{
    /// <summary>
    /// 检查桥接层 — cad → engine 的唯一切入点。
    /// </summary>
    public static class CheckBridge
    {
        private static readonly CheckPipeline _pipeline = new();

        public static CheckResult Run(StandardConfig config)
        {
            return _pipeline.Run(config);
        }
    }
}
