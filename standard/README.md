# BS CAD Standard Package

Current status: 1.0

This package contains the AutoCAD standard data and runtime assets used by the BS-CAD-Standard plugin.

## Source Of Truth

```text
data/BS_CAD_Layer_Standard_Working.xlsx
```

The workbook is the layer-standard source of truth. The generated runtime configuration is:

```text
config/BS_CAD_Standard_1.0.json
```

## Contents

```text
data/BS_CAD_Layer_Standard_Working.xlsx
config/BS_CAD_Standard_1.0.json
config/BS_DimStyle_Standard_1.0.json
config/BS_Layer_Migration_Rules_1.0.json
plot_styles/BS_CAD_STANDARD_1.0.ctb
templates/BS_CAD_STANDARD_1.0.dwt
```

## Current Standard Counts

```text
layers: 121
ctbRules: 9
loadModes: 12
categories: 18
```

`config/BS_CAD_Standard_1.0.json` should be regenerated whenever the workbook changes.

