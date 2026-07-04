# LEGACY_ONLY — BS-CAD-Standard 遗留服务层

**状态: 🧊 已冻结 (Frozen)**

此目录 (`src/Services/`) 中的所有文件均为遗留代码，已在 Phase 4 中被 `engine/` 层替代。

## 规则

- ❌ 不允许在此目录新增或修改代码
- ❌ 不允许 `engine/`、`cad/`、`src/Commands/` 新增对此目录的引用
- ⚠️ 现有引用已标记 `DEPRECATED_CALL`，需在后续 Phase 清理
- ✅ 仅做参考阅读，业务逻辑以 `engine/` 层为准

## 替代映射

| 遗留文件 | 引擎替代 |
|----------|----------|
| `CheckEngine.cs` | `engine/core/CheckPipeline.cs` |
| `LayerService.cs` | `engine/layer/LayerEngine.cs` |
| `TemplateCheckService.cs` | `engine/template/TemplateEngine.cs` |
| `CtbCheckService.cs` | `engine/ctb/CtbEngine.cs` |
| `CtbExportService.cs` | `engine/ctb/CtbEngine.cs` |
| `LayerModeService.cs` | `engine/layer/LayerEngine.cs` (ModeSwitch TBD) |
| `LayerFixService.cs` | (pending engine migration) |
| `LayerMissingService.cs` | (pending engine migration) |
| `InitService.cs` | (pending engine migration) |
| `LayerAuditEngine.cs` | (pending engine migration) |
| `TextStyleService.cs` | (pending engine migration) |
| `DimStyleService.cs` | (pending engine migration) |
| `MLeaderStyleService.cs` | (pending engine migration) |
| `DimensionService.cs` | (pending engine migration) |
| `TextService.cs` | (pending engine migration) |
| `MLeaderEntityService.cs` | (pending engine migration) |
| `InitReport.cs` | (pending engine migration) |
