# BS CAD Standard V10 Plugin - 当前状态

## 版本

**v0.8 Portable Test Package**

## 核心命令

| 命令 | 状态 | 说明 |
| --- | --- | --- |
| `BS_CHECK` | 已完成 | 读取标准 JSON，检查图层、文字样式、标注样式、BS_MLEADER_NOTE、INSUNITS、CTB |
| `BS_LAYER` | 已完成 | 分类选择 → 图层选择 → 切换/创建 |
| `BS_TEXT` | 已完成 | 5 种标准文字样式，BS_TEXT_EN=Arial，其他=SimHei |
| `BS_DIM` | 已完成 | 读取 BS_DimStyle_Standard_V10.json，3 种标注样式 |
| `BS_MLEADER` | 已完成 | 创建 BS_MLEADER_NOTE，箭头使用 BS_ARROW_DOT |
| `BS_INIT` | 已完成 | 一键初始化图层/文字/标注/引线/单位/默认项 |

## 测试包

| 项目 | 路径 |
| --- | --- |
| 生成脚本 | `scripts/build-test-package.ps1` |
| 测试包根目录 | `D:\01_DesignProjects\BS_CAD_STANDARD_V10_TestPackage` |
| 已迁移验证 | E 盘测试包正常读取 JSON |

## 配置路径搜索（已投产）

| 优先级 | 路径 | 适用场景 |
| --- | --- | --- |
| ① | DLL 上一级 `config/` | 测试包：`E:\...TestPackage\config\` |
| ② | DLL 所在目录 `config/` | 其他部署方式 |
| ③ | `BS_CAD_STANDARD_ROOT\config/` | 环境变量指定 |
| ④ | 插件项目 `config/` 硬编码 | 开发环境后备 |
| ⑤ | 标准包 `config/` 硬编码 | 开发环境最后兜底 |

## 已修复问题

- PromptKeywordOptions 中文关键字异常 → 改用 PromptStringOptions + 手动解析
- CreateLayerFromConfig 在空白图纸中的 eNoDatabase
- BS_MLEADER_NOTE 箭头无法设为小点 → 自定义 BS_ARROW_DOT
- BS_TEXT_EN 被错误改为黑体 → 已修复为 Arial
- BS_CHECK 增加文字样式字体偏差检查
- 配置路径搜索优先级：DLL 相对路径优先于硬编码路径

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
