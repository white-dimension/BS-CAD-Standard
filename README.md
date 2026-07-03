# BS-CAD-Standard

AutoCAD 图纸标准化工具集，用于室内设计 / 展厅 / 工装项目的 CAD 图层标准管理。

---

## 1. 项目定位

BS-CAD-Standard 是一个基于 AutoCAD .NET 的 CAD 标准工具包，当前 **v0.6-core** 版本重点解决 CAD 图层标准化问题。

当前核心能力：

- 生成标准图层
- 检查图层是否符合标准
- 修复图层属性偏差
- 补齐缺失标准图层
- 按工作模式切换图层显示
- 提供统一命令入口

---

## 2. 当前版本

**v0.6-core / layer workflow core**

当前已形成图层标准核心闭环：

```
生成 → 检查 → 修复 → 补齐 → 模式切换 → 恢复显示
```

---

## 3. 项目结构

```
src/        AutoCAD .NET 插件源码
config/     CAD 标准 JSON 配置
docs/       项目文档
scripts/    辅助脚本
standard/   标准文件
max-tools/  3ds Max 辅助工具
```

---

## 4. 环境要求

- Windows
- AutoCAD 2027
- .NET 10 SDK
- Visual Studio / Rider / VS Code 任一

AutoCAD .NET API 引用：

- `AcMgd.dll`
- `AcDbMgd.dll`
- `AcCoreMgd.dll`

`.csproj` 中引用路径为 `C:\Program Files\Autodesk\AutoCAD 2027\`。如果本机 AutoCAD 安装路径不同，需修改 `src/BS_CAD_STANDARD_V10_Plugin.csproj` 中的 HintPath。

---

## 5. 编译方式

```bash
dotnet build
```

预期输出：

```text
0 errors
```

---

## 6. AutoCAD 加载方式

1. 打开 AutoCAD
2. 输入 `NETLOAD`
3. 选择编译后的 DLL（通常位于 `src/bin/Debug/net10.0/` 或 `src/bin/Release/net10.0/`）
4. 输入 `BS`，进入 BS Toolkit 菜单

---

## 7. 命令列表

| 命令 | 功能 |
| --- | --- |
| `BS` | 打开统一菜单入口 |
| `BS_HELP` | 显示命令说明 |
| `BS_LAYER` | 生成标准图层 |
| `BS_CHECK` | 检查当前图纸图层标准 |
| `BS_FIX_LAYER` | 修复已有标准图层属性 |
| `BS_FIX_MISSING` | 补齐缺失标准图层 |
| `BS_LAYER_MODE` | 按加载模式切换图层显示 |
| `BS_LAYER_ALL` | 恢复所有图层显示 |

---

## 8. 推荐使用流程

```
新项目：
BS → 1 → BS_LAYER

接手旧图：
BS_CHECK → BS_FIX_LAYER → BS_FIX_MISSING

日常绘图：
BS_LAYER_MODE → 选择模式

恢复显示：
BS_LAYER_ALL
```

---

## 9. BS 统一菜单

执行 `BS` 后显示：

```
===== BS Toolkit =====

[1] 生成标准图层        BS_LAYER
[2] 检查图层标准        BS_CHECK
[3] 修复图层属性        BS_FIX_LAYER
[4] 补齐缺失图层        BS_FIX_MISSING
[5] 图层模式切换        BS_LAYER_MODE
[6] 恢复全部图层        BS_LAYER_ALL
[0] 退出
```

---

## 10. 图层模式系统

`BS_LAYER_MODE` 读取 `config/BS_CAD_Standard_v0.6.json` 中的 `loadModes`，不是硬编码模式。

当前支持约 12 种工作模式（以 JSON 配置为准）：

- 原始条件模式
- 平面布置模式
- 墙体拆改模式
- 地面铺装模式
- 天花布置模式
- 灯具定位模式
- 灯具连线开关模式
- 插座电源模式
- 给排水模式
- 空调设备模式
- 弱电智能模式
- 立面剖面节点模式

模式切换行为：

- 只显示当前模式需要的图层
- 其他图层关闭
- 0 和 Defpoints 永远保持显示
- 不删除图层
- 不创建图层
- 不修改颜色 / 线型 / 透明度 / 打印 / 锁定
- 只修改图层 `IsOff` 状态

---

## 11. 当前标准配置

主配置文件：`config/BS_CAD_Standard_v0.6.json`

当前标准包括：

- 121 个标准图层
- 18 个图层分类
- CTB 规则字段
- loadModes 加载模式规则

---

## 12. 当前版本边界

当前尚未实现：

- 自动生成 DWT 模板
- 自动生成 CTB 文件
- 图层差异导出
- Ribbon / WPF 可视化面板
- 自动清理非标准图层

`BS_AUTO_CLEAN` 暂不实现，自动删除 / 合并图层风险较高。

---

## 13. 开发路线

下一阶段方向：

1. `BS_CTB`：生成或辅助落地打印样式
2. `BS_TEMPLATE`：生成标准 DWT 模板
3. `BS_LAYER_DIFF`：输出当前图纸与标准的差异报告
4. Ribbon / 面板：后期产品化入口

---

## 14. 版本记录

### v0.6-core

已完成：

- `BS_LAYER`
- `BS_CHECK`
- `BS_FIX_LAYER`
- `BS_FIX_MISSING`
- `BS_LAYER_MODE`
- `BS_LAYER_ALL`
- `BS`
- `BS_HELP`

状态：图层标准核心闭环完成，编译通过 0 errors。
