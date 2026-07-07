using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.EditorInput;
using BS_CAD_STANDARD_1_0_Plugin.Core;
using BS_CAD_STANDARD_1_0_Plugin.Utils;

namespace BS_CAD_STANDARD_1_0_Plugin.Services
{
    public class CategoryInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryNo { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int LayerCount { get; set; }
        public int OrderIndex { get; set; }
    }

    public class LayerService
    {
        public static List<CategoryInfo> GetCategories(StandardConfig config)
        {
            // Priority 1: Use JSON categories array with actual layer counts
            if (config.ConfigCategories.Count > 0)
            {
                return config.ConfigCategories
                    .Select(cat => new CategoryInfo
                    {
                        CategoryNo = cat.CategoryNo,
                        CategoryName = cat.CategoryName,
                        Code = cat.CategoryNo,
                        Prefix = cat.CategoryNo,
                        Description = cat.CategoryName,
                        LayerCount = config.Layers.Count(l =>
                            string.Equals(l.CategoryNo, cat.CategoryNo, StringComparison.OrdinalIgnoreCase)),
                        OrderIndex = int.TryParse(cat.CategoryNo, out int n) ? n : 0
                    })
                    .OrderBy(c => c.OrderIndex)
                    .ToList();
            }

            // Priority 2: Fallback — group by layer name prefix
            return config.Layers
                .Select((layer, index) => new { layer, index })
                .GroupBy(l => GetCategoryPrefix(l.layer))
                .Select(g => new CategoryInfo
                {
                    Code = g.Key,
                    Prefix = g.Key,
                    Description = GetCategoryDescriptionFallback(g.Key),
                    CategoryNo = g.Key,
                    CategoryName = GetCategoryDescriptionFallback(g.Key),
                    LayerCount = g.Count(),
                    OrderIndex = g.Min(x => GetLayerOrder(x.layer, x.index))
                })
                .OrderBy(c => c.OrderIndex)
                .ToList();
        }

        public static List<LayerConfig> GetLayersByCategory(StandardConfig config, string category)
        {
            // Priority 1: Match by CategoryNo
            string normalized = NormalizeCategoryNo(category);
            var byCatNo = config.Layers
                .Where(l => string.Equals(l.CategoryNo, normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Order > 0 ? l.Order : int.MaxValue)
                .ThenBy(l => l.OrderIndex > 0 ? l.OrderIndex : int.MaxValue)
                .ToList();
            if (byCatNo.Count > 0) return byCatNo;

            // Priority 2: Match by layer Category field
            var byCategory = config.Layers
                .Where(l => string.Equals(l.Category, normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => GetLayerOrder(l, 0))
                .ToList();
            if (byCategory.Count > 0) return byCategory;

            // Priority 3: Fallback - match by name prefix
            return config.Layers
                .Select((layer, index) => new { layer, index })
                .Where(l => string.Equals(GetCategoryPrefix(l.layer), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => GetLayerOrder(l.layer, l.index))
                .Select(l => l.layer)
                .ToList();
        }

        private static string GetCategoryPrefix(LayerConfig layer)
        {
            if (!string.IsNullOrWhiteSpace(layer.CategoryNo))
                return layer.CategoryNo;

            if (!string.IsNullOrWhiteSpace(layer.Name) &&
                layer.Name.Length >= 2 &&
                char.IsDigit(layer.Name[0]) &&
                char.IsDigit(layer.Name[1]))
            {
                return layer.Name.Substring(0, 2);
            }

            return layer.Category;
        }

        private static string NormalizeCategoryNo(string value)
        {
            if (int.TryParse(value, out int number) && number >= 0 && number <= 99)
                return number.ToString("D2");
            return value;
        }

        private static int GetLayerOrder(LayerConfig layer, int fallbackIndex)
        {
            if (layer.Order > 0) return layer.Order;
            return layer.OrderIndex > 0 ? layer.OrderIndex : fallbackIndex + 1;
        }

        private static string GetCategoryDescriptionFallback(string code)
        {
            return code switch
            {
                "00" => "原始条件",
                "01" => "建筑基础",
                "02" => "室内完成面",
                "03" => "地面",
                "04" => "天花",
                "05" => "家具陈设",
                "06" => "展陈专项",
                "07" => "灯光照明",
                "08" => "强电",
                "09" => "弱电智能",
                "10" => "设备机电",
                "11" => "消防",
                "12" => "给排水",
                "13" => "文字说明",
                "14" => "标注符号",
                "15" => "图框布局",
                "16" => "参照底图",
                "17" => "辅助检查",
                _ => code switch
                {
                    "AR" => "建筑",
                    "IN" => "室内",
                    "FL" => "地面",
                    "CE" => "天花",
                    "FU" => "家具",
                    "EX" => "展厅",
                    "LI" => "灯具",
                    "EL" => "强电",
                    "WK" => "弱电",
                    "EQ" => "设备",
                    "FI" => "消防",
                    "PL" => "给排水",
                    "TX" => "文字",
                    "DM" => "标注",
                    "FR" => "图框",
                    "RF" => "参考底图",
                    "AN" => "辅助",
                    _ => "其它"
                }
            };
        }

        public static bool LayerExists(string layerName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                return lt.Has(layerName);
            }
        }

        public static ObjectId GetLayerId(string layerName)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName)) return lt[layerName];
                return ObjectId.Null;
            }
        }

        public static bool SwitchToLayer(ObjectId layerId)
        {
            if (layerId == ObjectId.Null) return false;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    db.Clayer = layerId;
                    tr.Commit();
                    return true;
                }
                catch (System.Exception ex)
                {
                    doc.Editor.WriteMessage($"\n[错误] 切换图层失败: {ex.Message}");
                    return false;
                }
            }
        }

        public static ObjectId CreateLayerFromConfig(LayerConfig cfg)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // 1. 文档锁定
            using (DocumentLock dl = doc.LockDocument())
            {
                // 2. 开启事务
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        // 3. 打开图层表
                        LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);

                        // 4. 如果已存在则直接返回
                        if (lt.Has(cfg.Name))
                        {
                            return lt[cfg.Name];
                        }

                        // 5. 准备写入
                        lt.UpgradeOpen();
                        LayerTableRecord ltr = new LayerTableRecord();
                        ltr.Name = cfg.Name;

                        // 设置基础属性（不需要数据库上下文）
                        ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, (short)cfg.Color);
                        ltr.LineWeight = AcadUtils.LineWeightFromMm(cfg.Lineweight);
                        ltr.IsPlottable = cfg.Plot;

                        // 线型处理
                        ObjectId resolvedLinetypeId = db.ContinuousLinetype;
                        if (!string.IsNullOrEmpty(cfg.Linetype) && !string.Equals(cfg.Linetype, "Continuous", StringComparison.OrdinalIgnoreCase))
                        {
                            if (AcadUtils.EnsureLinetypeLoaded(cfg.Linetype))
                            {
                                LinetypeTable ltt = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                                if (ltt.Has(cfg.Linetype))
                                {
                                    resolvedLinetypeId = ltt[cfg.Linetype];
                                }
                            }
                        }
                        ltr.LinetypeObjectId = resolvedLinetypeId;

                        // 6. 加入数据库 (必须先加入才能设置 Description 和 Transparency)
                        ObjectId layerId = lt.Add(ltr);
                        tr.AddNewlyCreatedDBObject(ltr, true);

                        // 7. 加入数据库后设置描述
                        try
                        {
                            if (!string.IsNullOrEmpty(cfg.Description))
                            {
                                ltr.Description = cfg.Description;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n[警告] 设置图层 [{cfg.Name}] 描述失败: {ex.Message}");
                        }

                        // 8. 加入数据库后设置透明度
                        try
                        {
                            if (cfg.Transparency > 0 && cfg.Transparency <= 90)
                            {
                                ltr.Transparency = new Transparency((byte)(255 * (100 - cfg.Transparency) / 100));
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\n[警告] 设置图层 [{cfg.Name}] 透明度失败: {ex.Message}");
                        }

                        tr.Commit();
                        return layerId;
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[错误] 创建图层 {cfg.Name} 失败: {ex.Message}");
                        return ObjectId.Null;
                    }
                }
            }
        }
    }
}
