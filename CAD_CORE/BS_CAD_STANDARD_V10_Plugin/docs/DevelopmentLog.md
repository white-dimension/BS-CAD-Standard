# BS CAD Standard V10 Plugin - 开发进度日志

## 当前版本

**v0.8 Portable Test Package**

当前版本已完成全部 6 个命令行命令实现，验证可移植测试包可在 E 盘正常读取 JSON。

## 项目概况

- 目标环境：AutoCAD 2027
- 开发框架：.NET 10.0 C# Class Library DLL
- 加载方式：AutoCAD `NETLOAD`
- 插件项目路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_Plugin`
- 标准包路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package`
- 测试包路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_TestPackage`

## v0.7 已完成（Command MVP）

- `BS_CHECK`：标准检查（图层/文字样式/标注样式/引线样式/单位/CTB）
- `BS_LAYER`：图层切换与按 JSON 创建图层
- `BS_TEXT`：标准文字创建，BS_TEXT_EN=Arial，其他 BS_TEXT_*=SimHei/黑体
- `BS_DIM`：标准标注样式，读取 BS_DimStyle_Standard_V10.json
- `BS_MLEADER`：标准多重引线，使用 BS_MLEADER_NOTE 和 BS_ARROW_DOT
- `BS_INIT`：一键初始化当前图纸标准环境
- 配置驱动（`BS_CAD_Standard_V10.json` + `BS_DimStyle_Standard_V10.json`）
- 5 级可移植配置路径搜索
- 配置文件缺失时的优雅降级与警告

## v0.8 新增（Portable Test Package）

- `scripts/build-test-package.ps1`：一键构建 + 打包脚本
- 测试包结构：`plugin/` + `config/` + `templates/` + `plot_styles/` + `lisp/` + `README_TEST.md`
- 配置路径搜索优先级重构：DLL 相对路径优先于硬编码开发路径
- 已验证 E 盘测试包路径正常读取 JSON

## v0.7 已修复问题

- BS_LAYER 的 PromptKeywordOptions 中文关键字异常
- CreateLayerFromConfig 的 eNoDatabase
- BS_MLEADER_NOTE 箭头无法设为小点，改为稳定自定义箭头块 BS_ARROW_DOT
- BS_TEXT_EN 被错误改为黑体，已修复为 Arial
- BS_CHECK 增加文字样式字体偏差检查
- 配置路径搜索优先级：DLL 相对路径优先于硬编码开发路径

## 功能记录

### BS_CHECK

- 读取标准 JSON
- 检查核心图层及属性
- 检查额外非标准图层
- 检查文字样式字体偏差
- 检查标注样式
- 检查 BS_MLEADER_NOTE
- 检查 INSUNITS
- 检查 CTB

### BS_LAYER

- 支持分类选择
- 支持图层选择
- 支持已有图层切换
- 支持空白图纸中按 JSON 创建标准图层

### BS_TEXT

- BS_TEXT_EN = Arial / arial.ttf
- BS_TEXT_CN = SimHei / simhei.ttf
- BS_TEXT_TITLE = SimHei / simhei.ttf
- BS_TEXT_TABLE = SimHei / simhei.ttf
- BS_TEXT_NOTE = SimHei / simhei.ttf
- 支持空白图纸中创建缺失文字样式

### BS_DIM

- 读取 BS_DimStyle_Standard_V10.json
- 支持 BS_DIM_100、BS_DIM_50、BS_DIM_DETAIL
- 支持检查、创建、更新标注样式

### BS_MLEADER

- 创建 BS_MLEADER_NOTE 多重引线样式
- 箭头使用自定义 BS_ARROW_DOT（跨语言版本一致的实心小点）

### BS_INIT

- 交互式初始化：图层 → 文字样式 → 标注样式 → 多重引线 → 单位 → 默认项
- 每步先统计缺失量，0 缺失时不询问
- DimStyle JSON 缺失时仅跳过标注初始化，不影响其余流程

## 下一阶段

- 小范围真实项目图纸测试
- 记录实际反馈
- 修复稳定性问题
- Ribbon 面板暂缓

## 暂缓

- Ribbon
- 图框
- 标题栏
- Keynote
- 材料编号
- 批量打印

## 约束

- 当前版本不修改 DWT、CTB、DWG 文件
- 当前版本不创建 .NET Framework 4.8 项目
- 当前版本锁定 AutoCAD 2027 / .NET 10.0
