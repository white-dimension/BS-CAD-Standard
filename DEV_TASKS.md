# BS Toolkit 开发任务书

版本：v0.1  
项目：BS-CAD-Standard  
仓库：white-dimension/BS-CAD-Standard  
当前阶段：v0.8-template 后整理阶段

---

## 1. 项目现状

BS-CAD-Standard 是基于 AutoCAD .NET 的 CAD 标准化工具集，服务于室内设计、展厅、工装项目的 CAD 图层标准管理。

当前项目已经从单一图层生成工具，发展为一个较完整的 CAD 标准检查与修复闭环。

当前核心闭环：

```text
生成标准图层
↓
检查图层标准
↓
修复图层属性
↓
补齐缺失图层
↓
按工作模式切换图层显示
↓
恢复全部图层显示
↓
检查 CTB 颜色规则
↓
导出 CTB 制作规则
↓
检查模板基础环境
```

当前版本定位：

```text
v0.8-template
目标：模板基础环境诊断
状态：已可用，后续进入测试、稳定、导出报告阶段
```

---

## 2. 已完成模块

### 2.1 图层标准核心闭环

已完成：

```text
BS_LAYER          生成标准图层
BS_CHECK          检查当前图纸图层标准
BS_FIX_LAYER      修复已有标准图层属性
BS_FIX_MISSING    补齐缺失标准图层
BS_LAYER_MODE     按工作模式切换图层显示
BS_LAYER_ALL      恢复所有图层显示
```

说明：

- 图层标准核心闭环已完成。
- `BS_LAYER_MODE` 已支持从 JSON 配置读取 loadModes。
- `BS_LAYER_MODE` 只控制图层开关，不删除图层、不创建图层、不修改颜色/线型/透明度/打印/锁定。
- `0` 和 `Defpoints` 图层始终保持显示。

---

### 2.2 统一入口

已完成：

```text
BS        统一菜单入口
BS_HELP   命令帮助
```

当前 BS 菜单包含：

```text
[1] 生成标准图层        BS_LAYER
[2] 检查图层标准        BS_CHECK
[3] 修复图层属性        BS_FIX_LAYER
[4] 补齐缺失图层        BS_FIX_MISSING
[5] 图层模式切换        BS_LAYER_MODE
[6] 恢复全部图层        BS_LAYER_ALL
[7] 检查 CTB 颜色规则    BS_CTB_CHECK
[8] 导出 CTB 规则说明    BS_CTB_EXPORT
[9] 模板基础环境检查    BS_TEMPLATE_CHECK
[0] 退出
```

---

### 2.3 CTB 规则系统

已完成：

```text
BS_CTB_CHECK    检查当前图纸图层颜色是否符合 CTB 规则
BS_CTB_EXPORT   导出 CTB 规则说明文件
```

当前能力：

- 从 JSON 中读取 `ctbRules`
- 检查标准图层颜色是否符合规则
- 检查图层颜色是否存在于 CTB 规则中
- 报告标准图层颜色偏差
- 报告非标准图层和非 CTB 规则颜色
- 导出 Markdown / CSV 文件
- 输出到 `exports/`

当前边界：

- 不自动生成 `.ctb` 文件
- 不自动安装 `.ctb`
- 不修改 DWG
- 不修改图层
- 不修改 JSON

---

### 2.4 模板基础环境检查

已完成：

```text
BS_TEMPLATE_CHECK
```

当前检查内容：

```text
单位检查
图层检查
CTB / 打印样式检查
文字样式检查
标注样式检查
布局检查
图框 / 视口检查
默认图层检查
非标准图层检查
```

当前定位：

- 只做诊断
- 不自动生成 DWT
- 不自动修复模板
- 不自动修改当前 DWG

---

## 3. 当前标准配置

主配置文件：

```text
standard/config/BS_CAD_Standard_V10.json
```

当前标准内容：

```text
121 个标准图层
18 个图层分类
loadModes 加载模式规则
ctbRules 打印颜色规则
```

相关辅助配置：

```text
BS_TextStyle_Standard_V10.json
BS_DimStyle_Standard_V10.json
```

---

## 4. 当前版本边界

当前明确不做：

```text
自动生成 .ctb 文件
自动安装 .ctb 到 AutoCAD 打印样式目录
自动生成 DWT 模板
自动清理非标准图层
Ribbon / WPF 可视化面板
```

其中：

```text
BS_AUTO_CLEAN 暂不实现
```

原因：

- 自动删除 / 合并图层风险较高
- 旧图、外部图、设计院图纸中可能存在非标准但有用的图层
- 当前阶段应优先诊断和提示，而不是自动破坏图纸结构

---

## 5. 下一阶段开发顺序

### P0：稳定当前 v0.8-template

优先级最高。

任务：

```text
1. 测试 BS_TEMPLATE_CHECK 在空白图、旧图、标准图中的输出
2. 修正误报 / 漏报
3. 检查 WARN / ERROR 统计是否准确
4. 检查建议输出是否过度提示
5. 保证 dotnet build 继续 0 errors
```

验收标准：

```text
BS_TEMPLATE_CHECK 在空白图中能稳定运行
BS_TEMPLATE_CHECK 在已有项目图中不崩溃
输出内容清晰
建议内容不乱跳
编译 0 errors
```

---

### P1：开发 BS_TEMPLATE_REPORT

目标：

```text
把 BS_TEMPLATE_CHECK 的检查结果导出为文件
```

建议命令：

```text
BS_TEMPLATE_REPORT
```

建议输出：

```text
exports/BS_TEMPLATE_CHECK_REPORT.md
exports/BS_TEMPLATE_CHECK_REPORT.csv
```

功能范围：

- 复用 `TemplateCheckService`
- 不重复写检查逻辑
- 导出 Markdown 报告
- 可选导出 CSV
- 文件名可包含日期时间

不做：

```text
不自动修复
不生成 DWT
不修改 DWG
```

---

### P2：开发 BS_LAYER_DIFF

目标：

```text
输出当前 DWG 与标准图层之间的差异报告
```

建议命令：

```text
BS_LAYER_DIFF
```

检查内容：

```text
缺失标准图层
多余非标准图层
颜色偏差
线型偏差
透明度偏差
是否打印偏差
锁定状态偏差
```

建议输出：

```text
exports/BS_LAYER_DIFF_REPORT.md
exports/BS_LAYER_DIFF_REPORT.csv
```

用途：

- 接手旧图前检查
- 项目归档前检查
- 作为修复前后的对比依据

---

### P3：开发 BS_TEMPLATE_INIT

目标：

```text
一键把当前 DWG 初始化为接近标准模板的状态
```

建议命令：

```text
BS_TEMPLATE_INIT
```

建议功能：

```text
设置单位为毫米
生成缺失标准图层
修复标准图层属性
检查 CTB 名称
检查文字样式
检查标注样式
提示布局 / 图框 / 视口问题
```

注意：

- 只做低风险初始化
- 不删除非标准图层
- 不强制替换用户已有布局
- 不自动生成复杂 DWT

---

### P4：开发 BS_TEMPLATE

目标：

```text
生成标准 DWT 模板
```

建议放在后面开发。

原因：

- 自动生成 DWT 涉及布局、图框、视口、打印样式、文字样式、标注样式
- 风险高于诊断和报告
- 需要先稳定 BS_TEMPLATE_CHECK 和 BS_TEMPLATE_INIT

---

### P5：后期产品化入口

后期再做：

```text
Ribbon 面板
WPF 可视化界面
悬浮工具窗
项目模板选择器
一键项目初始化
```

当前不做。

---

## 6. 近期任务清单

### 今天可完成

```text
[x] 新建 DEV_TASKS.md
[ ] 确认 README 与 DEV_TASKS.md 不冲突
[ ] 不开发新功能
```

---

### 下一次开发建议

```text
[ ] 新建 BS_TEMPLATE_REPORT
[ ] 复用 TemplateCheckService
[ ] 导出 Markdown 报告
[ ] 导出 CSV 报告
[ ] 更新 README
[ ] 更新 BS_HELP
[ ] dotnet build
```

---

## 7. 当前不应重复开发的功能

以下功能已经完成，不要重复开新坑：

```text
BS_LAYER
BS_CHECK
BS_FIX_LAYER
BS_FIX_MISSING
BS_LAYER_MODE
BS_LAYER_ALL
BS_CTB_CHECK
BS_CTB_EXPORT
BS_TEMPLATE_CHECK
BS
BS_HELP
```

这些功能后续只做：

```text
测试
修 bug
优化输出
补文档
增加导出报告
```

---

## 8. 开发原则

### 8.1 先诊断，后修复

所有高风险功能优先做检查报告，不直接修改 DWG。

推荐顺序：

```text
CHECK
REPORT
INIT
FIX
TEMPLATE
UI
```

---

### 8.2 不自动删除用户图层

非标准图层只提示，不自动删除。

原因：

- 可能是甲方图层
- 可能是设计院图层
- 可能是外部参照相关图层
- 可能是临时但有用的图层

---

### 8.3 所有规则从 JSON 读取

禁止硬编码标准。

应继续保持：

```text
图层规则来自 JSON
图层模式来自 JSON
CTB 规则来自 JSON
文字样式尽量来自配置
标注样式尽量来自配置
```

---

### 8.4 每次新增命令必须同步更新

每次新增命令后必须同步更新：

```text
README.md
BS_HELP
BS 菜单
DEV_TASKS.md
```

---

## 9. 推荐下一步命令

下一步最建议开发：

```text
BS_TEMPLATE_REPORT
```

原因：

- 复用已有 `BS_TEMPLATE_CHECK`
- 开发量小
- 风险低
- 适合快速闭环
- 对实际项目检查有用
- 可以作为后续模板初始化和 DWT 生成的依据

暂不建议马上开发：

```text
Ribbon / WPF
BS_AUTO_CLEAN
自动生成 CTB
自动生成完整 DWT
```
