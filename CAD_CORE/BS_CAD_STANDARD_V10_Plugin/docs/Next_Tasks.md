# BS CAD Standard V10 Plugin - 后续任务

## 下一步

- [ ] 小范围真实项目图纸测试
- [ ] 记录实际反馈
- [ ] 修复稳定性问题

## 回归测试清单

- [ ] NETLOAD 加载插件 DLL
- [ ] 在标准 DWT 中运行 BS_CHECK
- [ ] BS_LAYER 切换已有图层
- [ ] BS_LAYER 在空白图纸中按 JSON 创建图层
- [ ] BS_TEXT 在标准 DWT 中运行
- [ ] BS_TEXT 在空白图纸中运行
- [ ] BS_DIM 在标准 DWT 中运行
- [ ] BS_DIM 在空白图纸中运行
- [ ] BS_MLEADER 创建 BS_MLEADER_NOTE
- [ ] BS_INIT 一键初始化
- [ ] 从测试包 plugin/ 目录 NETLOAD 加载
- [ ] 测试包放在非 D 盘路径（如 E 盘）仍正常读取 JSON
- [ ] BS_DimStyle_Standard_V10.json 缺失时不影响 BS_CHECK / BS_LAYER / BS_TEXT / BS_MLEADER

## 暂缓任务

- [ ] Ribbon 面板
- [ ] 图框
- [ ] 标题栏
- [ ] Keynote
- [ ] 材料编号
- [ ] 批量打印

## 约束

- 暂不新增自动修复类批处理功能
- 暂不修改 DWT、CTB、DWG 文件
- 暂不创建 .NET Framework 4.8 项目
- 继续锁定 AutoCAD 2027 + .NET 10.0
