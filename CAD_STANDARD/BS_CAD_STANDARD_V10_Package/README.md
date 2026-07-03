# BS CAD Standard V10 标准包

这是 BS CAD Standard V10 的落地标准包，用于生成和维护 AutoCAD 标准模板。

正式模板文件名：

```text
BS_CAD_STANDARD_V10.dwt
```

## 目录内容

```text
data/BS_Layer_Standard.xlsx
config/BS_CAD_Standard_V10.json
lisp/BS_INIT_STANDARD_V10.lsp
lisp/BS_CREATE_LAYERS_V10.lsp
lisp/BS_CREATE_TEXT_STYLES_V10.lsp
lisp/BS_CREATE_DIM_STYLES_V10.lsp
plot_styles/BS_CAD_STANDARD_V10.ctb
templates/BS_CAD_STANDARD_V10.dwt
```

## 标准数据

- DWT 默认核心图层：88 个
- 扩展图层：91 个，暂不默认塞进 DWT，后续由插件或脚本按需加载
- JSON 按钮/命令预设：46 个
- 打印样式表：`BS_CAD_STANDARD_V10.ctb`

## 生成 DWT 模板

1. 打开 AutoCAD，新建一个空白图纸。
2. 执行：

```text
APPLOAD
```

3. 加载：

```text
D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\lisp\BS_INIT_STANDARD_V10.lsp
```

4. 执行：

```text
BS_INIT_STANDARD_V10
```

初始化会自动完成：

- 创建/更新核心图层
- 创建/更新文字样式
- 创建/更新标注样式
- 设置图形单位为毫米
- 设置长度精度为 `0.00`
- 设置 A3 横向 PDF 打印基础参数
- 设置当前图层、文字样式、标注样式

5. 确认打印样式表选择：

```text
BS_CAD_STANDARD_V10.ctb
```

6. 手动检查或设置多重引线样式 `BS_MLEADER_NOTE`。
7. 另存为：

```text
D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\templates\BS_CAD_STANDARD_V10.dwt
```

## 文字样式

模板包含以下文字样式：

```text
BS_TEXT_CN
BS_TEXT_EN
BS_TEXT_TITLE
BS_TEXT_TABLE
BS_TEXT_NOTE
```

当前字体规则：

- `BS_TEXT_CN`：黑体
- `BS_TEXT_EN`：Arial
- `BS_TEXT_TITLE`：黑体
- `BS_TEXT_TABLE`：黑体
- `BS_TEXT_NOTE`：黑体

## 标注样式

模板包含以下标注样式：

```text
BS_DIM_100
BS_DIM_50
BS_DIM_DETAIL
```

当前规则：

- 文字样式：`BS_TEXT_CN`
- 文字高度：`2.5`
- 箭头：建筑标记
- 箭头大小：`2`
- 颜色、线型、线宽：ByLayer
- 主单位：小数
- 精度：`0.00`
- 注释性：关
- `BS_DIM_100` 全局比例：`100`
- `BS_DIM_50` 全局比例：`50`
- `BS_DIM_DETAIL` 全局比例：`20`

## 多重引线样式

V10 正式只保留一个多重引线样式：

```text
BS_MLEADER_NOTE
```

它用于：

- 普通说明
- 材料说明
- 节点说明
- 索引说明

推荐手动设置如下：

### 内容

- 多重引线类型：多行文字
- 默认文字：默认文字
- 文字样式：`BS_TEXT_CN`
- 文字角度：始终正向读取
- 文字颜色：ByLayer
- 文字高度：`2.5`
- 始终左对正：关
- 文字边框：关
- 引线连接：水平连接
- 连接位置左：第一行中间
- 连接位置右：第一行中间
- 基线间隙：`1`
- 将引线延伸至文字：开

### 引线结构

- 最大引线点数：开，数值 `2`
- 第一段角度：关
- 第二段角度：关
- 自动包含基线：开
- 设置基线距离：开，数值 `10`
- 注释性：关
- 指定比例：`100`

### 引线格式

- 类型：直线
- 颜色：ByLayer
- 线型：ByLayer
- 线宽：ByLayer
- 箭头符号：小点
- 箭头大小：`5`
- 打断大小：`0`

## 打印设置

推荐打印设置：

- 打印机：`AutoCAD PDF (High Quality Print).pc3`
- 图纸尺寸：`ISO A3 (297.00 x 420.00 毫米)`
- 打印范围：窗口
- 打印比例：布满图纸
- 居中打印：开
- 打印样式表：`BS_CAD_STANDARD_V10.ctb`
- 质量：最高
- 打印对象线宽：开
- 按样式打印：开
- 图形方向：横向

## 使用原则

- 模型空间 1:1 绘图。
- 指向模型对象的尺寸、文字、多重引线建议放在模型空间。
- 图框、标题栏、图号、项目名称等版面信息放在布局空间。
- 正式出图图层应优先使用标准图层。
- 扩展图层不默认进入 DWT，后续由插件按需加载。

## 说明

- DWT 必须在 AutoCAD 内生成和保存，不建议外部程序直接写入 DWG/DWT 二进制文件。
- Ribbon 面板暂不包含在 V10 标准包内。
- 当前阶段优先保证命令行、LISP、CTB、DWT 稳定。

## 下一步工作清单

### 1. 完成当前 DWT 底图

- 在 AutoCAD 中重新执行 `BS_INIT_STANDARD_V10`。
- 打开 `MLEADERSTYLE`，按本文档设置 `BS_MLEADER_NOTE`。
- 打开 `UNITS`，确认单位为毫米，长度精度为 `0.00`。
- 打开标注样式管理器，确认 `BS_DIM_100 / BS_DIM_50 / BS_DIM_DETAIL` 参数正确。
- 打开文字样式管理器，确认 5 个 BS 文字样式存在且字体正确。
- 打开图层管理器，确认核心图层已创建。
- 打开打印窗口，确认 CTB 为 `BS_CAD_STANDARD_V10.ctb`。

### 2. 保存正式模板

确认以上内容无误后，另存为：

```text
D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\templates\BS_CAD_STANDARD_V10.dwt
```

保存后不要只看当前图纸，要用这个 DWT 再新建一张图测试。

### 3. 用 DWT 新建图纸验证

用 `BS_CAD_STANDARD_V10.dwt` 新建图纸后，检查：

- 核心图层是否保留。
- 文字样式是否保留。
- 标注样式是否保留。
- `BS_MLEADER_NOTE` 是否保留。
- 打印样式表是否能选择 `BS_CAD_STANDARD_V10.ctb`。
- 当前绘图默认值是否为 ByLayer。
- 模型空间标注、文字、多重引线是否显示正常。

### 4. 做一张测试图

建议画一张小型测试图，至少包含：

- 墙体线、门窗线、家具线、填充线。
- 普通文字和小注释。
- 线性标注、连续标注、基线标注、半径/直径标注。
- 一个 `BS_MLEADER_NOTE` 多重引线说明。
- A3 PDF 打印预览。

重点看出图线宽层级、文字大小、标注间距、多重引线比例是否舒服。

### 5. 固化 CTB 和模板路径

- 确认 `BS_CAD_STANDARD_V10.ctb` 已复制到 AutoCAD Plot Styles 目录。
- 确认包内也保留一份 CTB：

```text
D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\plot_styles\BS_CAD_STANDARD_V10.ctb
```

- 确认正式 DWT 保存在：

```text
D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package\templates\BS_CAD_STANDARD_V10.dwt
```

### 6. 后续插件接入

插件后续优先做命令行稳定版，暂不做 Ribbon。

插件应识别和调用：

- 图层标准：来自 `BS_CAD_Standard_V10.json`
- 默认文字样式：`BS_TEXT_CN`
- 默认标注样式：`BS_DIM_100`
- 默认多重引线样式：`BS_MLEADER_NOTE`
- 打印样式：`BS_CAD_STANDARD_V10.ctb`

插件逻辑建议：

- 如果当前图纸已有标准样式，则直接使用。
- 如果缺少图层或样式，则提示用户运行初始化或使用标准 DWT。
- 扩展图层由插件按需加载，不默认全部放进 DWT。

### 7. 暂缓事项

以下内容等 DWT 和命令行插件稳定后再做：

- Ribbon 面板。
- 图框和标题栏块库。
- 材料编号系统。
- Keynote 编号系统。
- 自动页面设置批量应用。
- 项目级标准检查器。

