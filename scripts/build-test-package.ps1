param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# 鈹€鈹€ 璺緞 鈹€鈹€
$RepoRoot     = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$PluginRoot   = $RepoRoot
$PackageRoot  = Join-Path $RepoRoot "standard"
$TestPkgRoot  = Join-Path $RepoRoot "test-package"
$DllSource    = Join-Path $PluginRoot "src\bin\$Configuration\net10.0\BS_CAD_STANDARD_1_0_Plugin.dll"

# 鈹€鈹€ Step 1: Build 鈹€鈹€
Write-Host "=== Step 1: dotnet build ===" -ForegroundColor Cyan
Set-Location $PluginRoot
dotnet build "src\BS_CAD_STANDARD_1_0_Plugin.csproj" --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit code $LASTEXITCODE)" }
Write-Host "Build OK" -ForegroundColor Green

# 鈹€鈹€ Step 2: Clean & create 鈹€鈹€
Write-Host "`n=== Step 2: Create test package ===" -ForegroundColor Cyan
if (Test-Path $TestPkgRoot) { Remove-Item -Path $TestPkgRoot -Recurse -Force }
New-Item -ItemType Directory -Path $TestPkgRoot | Out-Null

# 鈹€鈹€ Step 3: Subdirectories 鈹€鈹€
$SubDirs = @("plugin", "config", "templates", "plot_styles")
foreach ($d in $SubDirs) { New-Item -ItemType Directory -Path (Join-Path $TestPkgRoot $d) | Out-Null }

# 鈹€鈹€ Step 4: Copy files 鈹€鈹€
Write-Host "`n=== Step 3: Copy files ===" -ForegroundColor Cyan

Copy-Item -Path $DllSource -Destination (Join-Path $TestPkgRoot "plugin\BS_CAD_STANDARD_1_0_Plugin.dll") -Force
Write-Host "  [OK] plugin\BS_CAD_STANDARD_1_0_Plugin.dll"

Copy-Item -Path (Join-Path $PackageRoot "config\BS_CAD_Standard_1.0.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_CAD_Standard_1.0.json") -Force
Write-Host "  [OK] config\BS_CAD_Standard_1.0.json"


Copy-Item -Path (Join-Path $PackageRoot "config\BS_DimStyle_Standard_1.0.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_DimStyle_Standard_1.0.json") -Force
Write-Host "  [OK] config\BS_DimStyle_Standard_1.0.json"

Copy-Item -Path (Join-Path $PackageRoot "config\BS_Layer_Migration_Rules_1.0.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_Layer_Migration_Rules_1.0.json") -Force
Write-Host "  [OK] config\BS_Layer_Migration_Rules_1.0.json"

Copy-Item -Path (Join-Path $PackageRoot "templates\BS_CAD_STANDARD_1.0.dwt") `
          -Destination (Join-Path $TestPkgRoot "templates\BS_CAD_STANDARD_1.0.dwt") -Force
Write-Host "  [OK] templates\BS_CAD_STANDARD_1.0.dwt"

Copy-Item -Path (Join-Path $PackageRoot "plot_styles\BS_CAD_STANDARD_1.0.ctb") `
          -Destination (Join-Path $TestPkgRoot "plot_styles\BS_CAD_STANDARD_1.0.ctb") -Force
Write-Host "  [OK] plot_styles\BS_CAD_STANDARD_1.0.ctb"


# 鈹€鈹€ Summary 鈹€鈹€
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test package created!" -ForegroundColor Green
Write-Host "  Path: $TestPkgRoot"
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n鈹€鈹€ File list 鈹€鈹€" -ForegroundColor Yellow
Get-ChildItem -Path $TestPkgRoot -Recurse -File | ForEach-Object {
    Write-Host "  $($_.Directory.Name)/$($_.Name)  ($(($_.Length / 1KB).ToString('0.0')) KB)"
}

Write-Host "`n鈹€鈹€ Next steps 鈹€鈹€" -ForegroundColor Yellow
Write-Host "  1. Copy BS_CAD_STANDARD_1.0_TestPackage to target computer"
Write-Host "  2. Open AutoCAD 2027 and run NETLOAD"
Write-Host "  3. Select plugin\BS_CAD_STANDARD_1_0_Plugin.dll"
Write-Host "  4. Run BS_CHECK, then BS_INIT"
Write-Host "  5. Test BS_LAYER, BS_TEXT, BS_DIM, BS_MLEADER"
Write-Host "  6. Copy plot_styles\*.ctb to AutoCAD Plot Styles directory"
