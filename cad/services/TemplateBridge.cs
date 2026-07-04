using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Engine.Template;

namespace BS_CAD_STANDARD_V10_Plugin.Cad.Services
{
    /// <summary>
    /// 模板桥接层 — cad → engine 的唯一切入点。
    /// </summary>
    public static class TemplateBridge
    {
        private static readonly TemplateEngine _engine = new();

        public static TemplateCheckReport RunCheck(StandardConfig config)
        {
            return _engine.CheckTemplate(config);
        }
    }
}
