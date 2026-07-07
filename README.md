# BS-CAD-Standard

BS-CAD-Standard is an AutoCAD .NET standardization plugin for CAD layer, CTB, template, text, dimension, and annotation checks.

Current status: 1.0

## Scope

This repository is CAD-only. The source of truth for layer standards is:

```text
standard/data/BS_CAD_Layer_Standard_Working.xlsx
```

The plugin runtime configuration is generated from that workbook:

```text
standard/config/BS_CAD_Standard_1.0.json
```

## Project Structure

```text
src/        AutoCAD .NET plugin commands and services
engine/     CAD business engines
cad/        AutoCAD bridge layer
standard/   CAD standard package: data / config / templates / plot_styles
scripts/    build and package scripts
```

## Main Commands

```text
BS_LAYER          Select/create/switch standard layers
BS_CHECK          Check current drawing against the standard
BS_FIX_LAYER      Fix existing standard layer properties
BS_FIX_MISSING    Create missing standard layers
BS_LAYER_MODE     Apply a layer visibility mode
BS_LAYER_ALL      Restore layer visibility snapshot, or show all layers
BS_CTB_CHECK      Check layer colors against CTB rules
BS_TEMPLATE_CHECK Check template environment
```

## Build

```text
dotnet build src/BS_CAD_STANDARD_1_0_Plugin.csproj --no-restore
```
