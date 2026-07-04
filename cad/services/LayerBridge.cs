using Autodesk.AutoCAD.DatabaseServices;
using BS_CAD_STANDARD_V10_Plugin.Core;
using BS_CAD_STANDARD_V10_Plugin.Engine.Layer;
using System.Collections.Generic;

namespace BS_CAD_STANDARD_V10_Plugin.Cad.Services
{
    /// <summary>
    /// 图层桥接层 — cad → engine 的唯一切入点。
    /// 命令层通过此桥调用引擎，不直接访问旧 Service。
    /// </summary>
    public static class LayerBridge
    {
        private static readonly LayerEngine _engine = new();

        // ===== 查询 =====

        public static List<CategoryInfo> GetCategories(StandardConfig config)
        {
            return _engine.GetCategories(config);
        }

        public static List<LayerConfig> GetLayersByCategory(StandardConfig config, string category)
        {
            return _engine.GetLayersByCategory(config, category);
        }

        public static bool LayerExists(string layerName)
        {
            return _engine.LayerExists(layerName);
        }

        public static ObjectId GetLayerId(string layerName)
        {
            return _engine.GetLayerId(layerName);
        }

        // ===== 操作 =====

        public static bool SwitchToLayer(ObjectId layerId)
        {
            return _engine.SwitchToLayer(layerId);
        }

        public static ObjectId CreateLayerFromConfig(LayerConfig cfg)
        {
            return _engine.CreateLayerFromConfig(cfg);
        }

        // ===== 引擎级入口 =====

        public static void RunCheck()
        {
            _engine.Check();
        }

        public static void RunDiff()
        {
            _engine.Diff();
        }

        public static void RunModeSwitch()
        {
            _engine.ModeSwitch();
        }
    }
}
