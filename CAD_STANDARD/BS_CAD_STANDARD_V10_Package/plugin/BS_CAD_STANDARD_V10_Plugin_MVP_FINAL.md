# BS CAD Standard V10 — AutoCAD 插件 MVP 开发文档

## 1. MVP 总目标

开发一个 AutoCAD .NET DLL（`NETLOAD` 加载），规划提供 **6 个命令行命令**，但第一阶段只实现并注册 **BS_CHECK** 和 **BS_LAYER**。其余命令仅保留在 P2 计划中，第一阶段不创建空桩命令。

核心原则：
- **只读优先**：BS_CHECK 只看不动
- **确认后写入**：BS_LAYER 创建图层前征求用户同意
- **配置驱动**：规则来自 `BS_CAD_Standard_V10.json`，不硬编码
- **纯命令行**：不做 Ribbon / Toolbar / Palette

> 重要约束：核心图层数量、分类数量、图层总数均从 JSON 动态读取，代码和测试不得硬编码 88、17、179 等当前数据量。

## 2. 项目文件结构

```
BS_CAD_STANDARD_V10_Plugin/
├── BS_CAD_STANDARD_V10_Plugin.sln
├── src/
│   └── BS_CAD_STANDARD_V10_Plugin/
│       ├── BS_CAD_STANDARD_V10_Plugin.csproj
│       ├── Commands/
│       │   ├── CheckCommands.cs          # BS_CHECK
│       │   └── LayerCommands.cs          # BS_LAYER
│       ├── Core/
│       │   ├── StandardConfig.cs         # JSON 数据模型
│       │   ├── ConfigLoader.cs           # JSON 加载与反序列化
│       │   ├── StandardDefaults.cs       # JSON 缺失时的默认值（CTB/样式路径等）
│       │   └── StandardPaths.cs          # 路径常量
│       ├── Services/
│       │   ├── LayerService.cs           # 图层查询/创建/切换
│       │   └── CheckEngine.cs            # BS_CHECK 各类检查
│       └── Utils/
│           ├── AcadUtils.cs              # AutoCAD .NET API 辅助（线型加载、颜色映射、线宽映射等）
│           └── PromptUtils.cs            # 命令行交互（列表选择、Y/N 确认）
└── config/
    └── BS_CAD_Standard_V10.json          # 构建时复制到输出目录
```

## 3. 目标框架

- **AutoCAD 版本**：AutoCAD 2027（不兼容 2024 及更早版本）
- **目标框架**：.NET 10.0（Class Library 项目）
- **程序集引用**：从 AutoCAD 2027 安装目录引用 `AcMgd.dll`、`AcDbMgd.dll`、`AcCoreMgd.dll`，设置 **Copy Local = false**
- **不要创建 .NET Framework 4.8 项目**，本 MVP 只面向 AutoCAD 2027。

## 4. 命令功能说明

| 命令 | 阶段 | 功能 |
|---|---|---|
| **BS_CHECK** | P1 | 扫描当前图纸，对照 JSON 标准报告偏差——仅检查，不修改 |
| **BS_LAYER** | P1 | 按分类列出标准图层，切换当前图层；图层不存在时提示创建 |
| **BS_TEXT** | P2 | 创建/更新 5 个标准文字样式并设为当前 |
| **BS_DIM** | P2 | 创建/更新 3 个标准标注样式并设为当前 |
| **BS_MLEADER** | P2 | 创建标准多重引线样式（BS_MLEADER_NOTE）并设为当前 |
| **BS_INIT** | P2 | 一键完成以上所有初始化 + 设单位/打印样式/默认图层 |

**第一阶段只实现并注册 BS_CHECK 和 BS_LAYER**。BS_TEXT、BS_DIM、BS_MLEADER、BS_INIT 仅保留在 P2 计划中，第一阶段不注册空桩命令。

## 5. BS_CHECK 检查规则

第一版检查以下 8 项内容，**只输出报告，不修改任何内容**。

### 5.1 检查项目

| 序号 | 检查项 | 检查方式 |
|---|---|---|
| ① | **核心图层是否存在** | 遍历 JSON 中所有 `core: true` 的图层，检查 DWG 中同名图层是否存在。当前数据量可在报告中显示，但代码不得硬编码数量 |
| ② | **核心图层属性一致性** | 对存在的图层，检查 `color` / `linetype` / `lineweight` / `plot` 是否与 JSON 一致 |
| ③ | **额外非标准图层** | 列出 DWG 中所有不在 JSON 图层列表内的图层 |
| ④ | **标准文字样式是否存在** | 检查 `BS_TEXT_CN` / `BS_TEXT_EN` / `BS_TEXT_TITLE` / `BS_TEXT_TABLE` / `BS_TEXT_NOTE` 是否注册 |
| ⑤ | **标准标注样式是否存在** | 检查 `BS_DIM_100` / `BS_DIM_50` / `BS_DIM_DETAIL` 是否注册 |
| ⑥ | **BS_MLEADER_NOTE 是否存在** | 检查多重引线样式字典中是否存在该样式 |
| ⑦ | **单位是否为毫米** | 检查 `INSUNITS` 系统变量是否等于 4（毫米）|
| ⑧ | **当前布局 CTB 是否正确** | 检查当前布局的打印样式表是否为 `BS_CAD_STANDARD_V10.ctb` |

### 5.2 报告输出格式

```
===== BS CHECK · BS CAD Standard V10 =====
[图层] 核心标准: 88 层
  缺失: 2
    - 01-AR-原有墙体
    - 17-AN-辅助线
  属性偏差: 1
    - 01-AR-墙体: Color 应为 7, 实际为 1; Lineweight 应为 0.35mm, 实际为 0.25mm
  额外非标准图层: 5
    - OLD_LAYER_1

[文字样式] 标准: 5 → 已就绪 4, 缺失 1
    - BS_TEXT_NOTE 不存在

[标注样式] 标准: 3 → 已就绪 3

[多重引线样式] BS_MLEADER_NOTE: 不存在

[单位] INSUNITS=4 (毫米) → 正确

[打印样式] 当前: acad.ctb → 应为 BS_CAD_STANDARD_V10.ctb

============================================
```

### 5.3 错误处理

- JSON 文件找不到 → 报错中断，提示标准包路径
- JSON 格式错误 → 报错中断，显示解析异常详情
- 图层/样式不存在 → 正常记录为缺失，不抛异常
- AutoCAD 内部异常 → `try/catch` 包裹单项检查，保证一项目失败不影响其余项目

## 6. BS_LAYER 图层切换规则

### 6.1 交互流程

```
命令: BS_LAYER

→ 从 JSON 动态读取并列出所有分类（编号 + 代码 + 图层数）:
  [01] AR — 建筑 (11层)
  [02] IN — 室内 (7层)
  [03] FL — 地面 (5层)
  ...
→ 用户输入分类编号（或分类代码，如 AR / IN）

→ 列出该分类下所有图层（编号 + 完整名称 + 颜色号）:
  [01] 01-AR-原有墙体 (253)
  [02] 01-AR-墙体 (7)
  ...

→ 用户输入图层编号

→ 逻辑:
  ├─ 图层已存在 → 切换当前图层 + 打印 "已切换到: 01-AR-墙体"
  └─ 图层不存在 → 提示 "图层 [01-AR-墙体] 不存在，是否创建？[Y/N]"
        ├─ Y → 按 JSON 属性创建并切换
        └─ N → 取消，回到分类/图层选择

→ 支持输入 X 返回上一级，输入 Q 退出命令
```

### 6.2 JSON → AutoCAD 属性映射

| JSON 字段 | AutoCAD 属性 | 映射 |
|---|---|---|
| `name` | `Layer.Name` | 直接使用 |
| `color` | `Layer.Color` | `Color.FromColorIndex(ColorMethod.ByAci, value)` |
| `linetype` | `Layer.Linetype` | 非 Continuous 时先 `Load` 再设置 |
| `lineweight` | `Layer.Lineweight` | mm → `LineWeight` 枚举映射 |
| `transparency` | `Layer.Transparency` | 0-90 转百分比透明度 |
| `plot` | `Layer.Plottable` | `true` / `false` |
| `description` | `Layer.Description` | 直接写入 |

### 6.3 线型加载兜底

`linetype` 非 `Continuous` 且当前 DWG 未加载时：
- 依次尝试从 `acad.lin` / `acadiso.lin` 加载
- 加载失败 → 打印警告，使用 Continuous 作为 fallback，不中断

### 6.4 事务安全

所有创建/修改图层的操作使用 `TransactionManager.StartTransaction()`：
- 成功 → `Transaction.Commit()`
- 异常 → `Transaction.Abort()`，回滚全部变更

## 7. JSON 配置与默认值

### 7.1 配置加载优先级

```
BS_CAD_Standard_V10.json（标准包路径下）
        ↓ 存在？→ 反序列化
        ↓ 不存在？→ 报错，提示用户检查标准包路径
```

### 7.2 JSON 数据模型（C# 定义要点）

```csharp
// RootConfig — 顶层
class RootConfig {
    string Version;
    string StandardName;
    string PackageName;
    string Ctb;           // "BS_CAD_STANDARD_V10.ctb"
    List<LayerConfig> Layers;     // 图层数量从 JSON 动态读取，不硬编码
    StylesConfig Styles;
}

class StylesConfig {
    string TextDefault;           // "BS_TEXT_CN"
    string DimDefault;            // "BS_DIM_100"
    List<string> TextStyles;      // ["BS_TEXT_CN", ...]
    List<string> DimStyles;       // ["BS_DIM_100", ...]
}

class LayerConfig {
    string Id;           // "AR_WALL"
    string Name;         // "01-AR-墙体"
    int Color;           // 7
    string Linetype;     // "Continuous"
    double Lineweight;   // 0.35
    int Transparency;    // 0
    bool Plot;           // true
    bool Core;           // true/false
    string Category;     // "AR"
    // ... 其余字段(description, displayName, englishKey...) 暂不映射
}
```

### 7.3 StandardDefaults.cs

JSON 中 **没有** 定义以下数据时，由 `StandardDefaults.cs` 提供硬编码默认值：

```
CTB 文件名:       BS_CAD_STANDARD_V10.ctb
多重引线样式:      BS_MLEADER_NOTE
标准文字样式列表:   BS_TEXT_CN, BS_TEXT_EN, BS_TEXT_TITLE, BS_TEXT_TABLE, BS_TEXT_NOTE
标准标注样式列表:   BS_DIM_100, BS_DIM_50, BS_DIM_DETAIL
默认文字样式:      BS_TEXT_CN
默认标注样式:      BS_DIM_100
单位:             毫米 (INSUNITS=4)
```

## 8. 错误处理原则

| 级别 | 行为 | 场景 |
|---|---|---|
| Info | 直接 `WriteMessage` | 切换成功、检查通过 |
| Warning | 前缀 `[WARNING]` | 图层属性偏差、线型加载失败 |
| Error | 前缀 `[ERROR]` + 命令终止 | JSON 找不到/格式错误 |
| Exception | `try/catch` + Error 报告 | AutoCAD .NET API 异常 |

- 全部 AutoCAD API 调用用 `try/catch` 包裹
- `Transaction.Abort()` 保护写入操作的原子性
- 用户按 Esc / 输入 Q → `PromptStatus.Cancel` 正常退出，不视为错误

## 9. 不做（MVP 边界）

| 不要做的功能 | 理由 |
|---|---|
| Ribbon / Toolbar / Palette | 第二版 |
| 批量修复（一键修复全部） | MVP 只检查 |
| 图框/标题栏处理 | 不属于样式范畴 |
| Keynote / 材料编号 | 超出 MVP |
| 批量打印 | 超出 MVP |
| 选择集遍历/对象级检查 | BS_CHECK 只看字典/系统变量 |
| 多语言界面 | 初期只做中文 |

## 10. 给 Codex 的开发任务清单

```
Phase 0 — 项目脚手架
├─ [ ] 创建 Class Library 项目（.NET 10.0，不要创建 .NET Framework 4.8 项目）
│   ├─ 引用 AutoCAD 2027 安装目录中的 AcMgd.dll / AcDbMgd.dll / AcCoreMgd.dll（Copy Local = false）
│   ├─ 构建后事件: copy DLL 到 AutoCAD 插件目录
│   └─ 实现 IExtensionApplication → Initialize() 打印加载信息
├─ [ ] 创建 StandardConfig.cs：JSON 反序列化数据模型
├─ [ ] 创建 ConfigLoader.cs：
│   ├─ 从标准包路径读取 BS_CAD_Standard_V10.json
│   ├─ 反序列化为 RootConfig 对象
│   └─ 文件不存在时抛明确错误
├─ [ ] 创建 StandardDefaults.cs：JSON 缺失时的兜底常量
├─ [ ] 创建 StandardPaths.cs：标准包路径 + CTB/DWT 路径常量
└─ [ ] 创建 CommandRegistration.cs：空桩注册 6 个命令

Phase 1 — 图层服务
├─ [ ] 创建 AcadUtils.cs：
│   ├─ LineweightFromMm(double mm) → LineWeight 枚举
│   ├─ EnsureLinetypeLoaded(string name) → bool
│   └─ ColorFromIndex(int aci) → Color
├─ [ ] 创建 PromptUtils.cs：
│   ├─ PromptForCategory(List<CategoryInfo>) → 分类选择
│   ├─ PromptForLayer(List<LayerConfig>) → 图层选择
│   └─ PromptYesNo(string msg) → bool
├─ [ ] 创建 LayerService.cs：
│   ├─ GetCoreLayerCount() → int（从 JSON 动态统计 core=true）
│   ├─ GetCategories() → List<CategoryInfo>
│   ├─ GetLayersByCategory(string cat) → List<LayerConfig>
│   ├─ LayerExists(string name) → bool
│   ├─ SwitchToLayer(string name) → void
│   └─ CreateLayerFromConfig(LayerConfig cfg) → void（严格映射属性）

Phase 2 — BS_CHECK 命令
├─ [ ] 创建 CheckEngine.cs：
│   ├─ CheckCoreLayers() → (missing[], mismatch[], extra[])
│   ├─ CheckTextStyles() → (ready[], missing[])
│   ├─ CheckDimStyles() → (ready[], missing[])
│   ├─ CheckMLeaderStyle() → bool
│   ├─ CheckUnits() → bool
│   └─ CheckPlotStyle() → (current, expected, match)
└─ [ ] 实现 CheckCommands.cs：[CommandMethod("BS_CHECK")] 调用 CheckEngine 输出报告

Phase 3 — BS_LAYER 命令
└─ [ ] 实现 LayerCommands.cs：[CommandMethod("BS_LAYER")]
    ├─ step 1: 列出分类 → 用户选择
    ├─ step 2: 列出图层 → 用户选择
    ├─ step 3: 存在则切换，不存在则确认创建
    └─ step 4: 按 JSON 创建图层属性并切换

Phase 4 — 集成测试
├─ [ ] 空图纸中 NETLOAD 加载 DLL
├─ [ ] BS_CHECK 空图纸运行，验证报告中的核心图层数量与 JSON 中 core=true 的数量一致
├─ [ ] 用现有 BS_CREATE_LAYERS_V10.lsp 创建图层后，再跑 BS_CHECK 验证属性一致
├─ [ ] BS_LAYER 测试：分类浏览 → 切换 → 不存在时创建 → 取消
├─ [ ] 错误场景测试：JSON 不存在、文件损坏、超出 CLI 输入范围
└─ [ ] 交付物打包：DLL + config/BS_CAD_Standard_V10.json + README.md
```


## 11. 给 Codex 的第一轮执行指令

第一轮不要一次性实现全部命令，只执行以下内容：

```text
请读取 BS_CAD_STANDARD_V10_Plugin_MVP_FINAL.md。

目标 AutoCAD 版本：AutoCAD 2027。
目标框架：.NET 10.0。
不要创建 .NET Framework 4.8 项目。

现在只执行：
1. Phase 0 项目脚手架
2. ConfigLoader / StandardConfig / StandardDefaults / StandardPaths
3. BS_CHECK

暂时不要实现 BS_LAYER。
暂时不要实现 BS_TEXT、BS_DIM、BS_MLEADER、BS_INIT。
不要做 Ribbon。
不要做批量修复。
所有图层数量、分类数量、总图层数量必须从 JSON 动态读取，不能硬编码。
```

## 附录 A：参考已有实现

现有 LSP 脚本位于标准包 `lisp/` 目录，供对照验证：

| LSP 文件 | 用途 |
|---|---|
| `BS_CREATE_LAYERS_V10.lsp` | 历史/对照用图层创建脚本，实际标准以 JSON 为准 |
| `BS_CREATE_TEXT_STYLES_V10.lsp` | 创建 5 种文字样式 |
| `BS_CREATE_DIM_STYLES_V10.lsp` | 创建 3 种标注样式 |
| `BS_INIT_STANDARD_V10.lsp` | 一键初始化 |

插件第一版以 `config/BS_CAD_Standard_V10.json` 为唯一图层标准来源，LSP 只作为人工对照参考，不作为代码逻辑依据。`BS_LAYER` 创建结果应与 JSON 标准一致。
