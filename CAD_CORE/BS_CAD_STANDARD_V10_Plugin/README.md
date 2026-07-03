# BS CAD Standard V10 Plugin

## 当前版本

**v0.8 Portable Test Package**

当前版本已完成全部 6 个命令行命令实现，并验证可移植测试包可在其他电脑运行。

## 目标环境

- AutoCAD 2027
- .NET 10.0
- C# Class Library DLL
- 通过 AutoCAD `NETLOAD` 加载
- 不兼容 AutoCAD 2024 及更早版本

## 已完成并测试通过

- `BS_CHECK`：标准检查（图层/文字样式/标注样式/引线样式/单位/CTB）
- `BS_LAYER`：图层切换与按 JSON 创建图层
- `BS_TEXT`：标准文字创建，BS_TEXT_EN=Arial，其他 BS_TEXT_*=SimHei/黑体
- `BS_DIM`：标准标注样式，读取 BS_DimStyle_Standard_V10.json
- `BS_MLEADER`：标准多重引线，使用 BS_MLEADER_NOTE 和 BS_ARROW_DOT
- `BS_INIT`：初始化当前图纸标准环境
- 可移植测试包（`scripts/build-test-package.ps1` 一键打包）
- 插件从测试包 `plugin/` 目录加载时，优先读取测试包 `config/` 目录
- 已验证 E 盘测试包路径可以正常读取 JSON

## 已修复问题

- BS_LAYER 的 PromptKeywordOptions 中文关键字异常
- CreateLayerFromConfig 的 eNoDatabase
- BS_MLEADER_NOTE 箭头无法设为小点，改为稳定自定义箭头块 BS_ARROW_DOT
- BS_TEXT_EN 被错误改为黑体，已修复为 Arial
- BS_CHECK 增加文字样式字体偏差检查
- 配置路径搜索优先级：DLL 相对路径优先于硬编码开发路径

## 当前配置

- 插件项目路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_Plugin`
- 标准包路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package`
- 测试包路径：`D:\01_DesignProjects\BS_CAD_STANDARD_V10_TestPackage`
- 主标准配置：`BS_CAD_Standard_V10.json`
- 标注样式配置：`BS_DimStyle_Standard_V10.json`

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

## 状态说明

`v0.8 Portable Test Package` 以跨机可移植的命令行插件为目标。当前不新增 Ribbon，不修改 DWT、CTB、DWG 文件。
