param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

# ── 路径 ──
$PluginRoot   = "D:\01_DesignProjects\BS_CAD_STANDARD_V10_Plugin"
$PackageRoot  = "D:\01_DesignProjects\BS_CAD_STANDARD_V10_Package"
$TestPkgRoot  = "D:\01_DesignProjects\BS_CAD_STANDARD_V10_TestPackage"
$DllSource    = Join-Path $PluginRoot "src\BS_CAD_STANDARD_V10_Plugin\bin\$Configuration\net10.0\BS_CAD_STANDARD_V10_Plugin.dll"

# ── Step 1: Build ──
Write-Host "=== Step 1: dotnet build ===" -ForegroundColor Cyan
Set-Location $PluginRoot
dotnet build "src\BS_CAD_STANDARD_V10_Plugin\BS_CAD_STANDARD_V10_Plugin.csproj" --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit code $LASTEXITCODE)" }
Write-Host "Build OK" -ForegroundColor Green

# ── Step 2: Clean & create ──
Write-Host "`n=== Step 2: Create test package ===" -ForegroundColor Cyan
if (Test-Path $TestPkgRoot) { Remove-Item -Path $TestPkgRoot -Recurse -Force }
New-Item -ItemType Directory -Path $TestPkgRoot | Out-Null

# ── Step 3: Subdirectories ──
$SubDirs = @("plugin", "config", "templates", "plot_styles", "lisp")
foreach ($d in $SubDirs) { New-Item -ItemType Directory -Path (Join-Path $TestPkgRoot $d) | Out-Null }

# ── Step 4: Copy files ──
Write-Host "`n=== Step 3: Copy files ===" -ForegroundColor Cyan

Copy-Item -Path $DllSource -Destination (Join-Path $TestPkgRoot "plugin\BS_CAD_STANDARD_V10_Plugin.dll") -Force
Write-Host "  [OK] plugin\BS_CAD_STANDARD_V10_Plugin.dll"

Copy-Item -Path (Join-Path $PackageRoot "config\BS_CAD_Standard_V10.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_CAD_Standard_V10.json") -Force
Write-Host "  [OK] config\BS_CAD_Standard_V10.json"

Copy-Item -Path (Join-Path $PackageRoot "config\BS_DimStyle_Standard_V10.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_DimStyle_Standard_V10.json") -Force
Write-Host "  [OK] config\BS_DimStyle_Standard_V10.json"

Copy-Item -Path (Join-Path $PackageRoot "config\BS_Layer_Migration_Rules_V10.json") `
          -Destination (Join-Path $TestPkgRoot "config\BS_Layer_Migration_Rules_V10.json") -Force
Write-Host "  [OK] config\BS_Layer_Migration_Rules_V10.json"

Copy-Item -Path (Join-Path $PackageRoot "templates\BS_CAD_STANDARD_V10.dwt") `
          -Destination (Join-Path $TestPkgRoot "templates\BS_CAD_STANDARD_V10.dwt") -Force
Write-Host "  [OK] templates\BS_CAD_STANDARD_V10.dwt"

Copy-Item -Path (Join-Path $PackageRoot "plot_styles\BS_CAD_STANDARD_V10.ctb") `
          -Destination (Join-Path $TestPkgRoot "plot_styles\BS_CAD_STANDARD_V10.ctb") -Force
Write-Host "  [OK] plot_styles\BS_CAD_STANDARD_V10.ctb"

Get-ChildItem (Join-Path $PackageRoot "lisp\*.lsp") | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination (Join-Path $TestPkgRoot "lisp\$($_.Name)") -Force
    Write-Host "  [OK] lisp\$($_.Name)"
}

# ── Summary ──
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Test package created!" -ForegroundColor Green
Write-Host "  Path: $TestPkgRoot"
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n── File list ──" -ForegroundColor Yellow
Get-ChildItem -Path $TestPkgRoot -Recurse -File | ForEach-Object {
    Write-Host "  $($_.Directory.Name)/$($_.Name)  ($(($_.Length / 1KB).ToString('0.0')) KB)"
}

Write-Host "`n── Next steps ──" -ForegroundColor Yellow
Write-Host "  1. Copy BS_CAD_STANDARD_V10_TestPackage to target computer"
Write-Host "  2. Open AutoCAD 2027 and run NETLOAD"
Write-Host "  3. Select plugin\BS_CAD_STANDARD_V10_Plugin.dll"
Write-Host "  4. Run BS_CHECK, then BS_INIT"
Write-Host "  5. Test BS_LAYER, BS_TEXT, BS_DIM, BS_MLEADER"
Write-Host "  6. Copy plot_styles\*.ctb to AutoCAD Plot Styles directory"
