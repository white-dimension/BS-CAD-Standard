/*
    BS Max Studio
    Phase 1 framework launcher.

    Run this file in 3ds Max:
    Scripting > Run Script... > startup.ms
*/

(
    local sourceFile = getSourceFileName()
    local rootPath = getFilenamePath sourceFile

    fileIn (rootPath + "Utils\\BSMS_Path.ms")
    fileIn (rootPath + "Core\\BSMS_Config.ms")
    fileIn (rootPath + "Core\\BSMS_ModuleRegistry.ms")
    fileIn (rootPath + "UI\\BSMS_Theme.ms")

    fileIn (rootPath + "Modules\\Layer\\LayerModule.ms")
    fileIn (rootPath + "Modules\\Model\\ModelModule.ms")
    fileIn (rootPath + "Modules\\Material\\MaterialModule.ms")
    fileIn (rootPath + "Modules\\Corona\\CoronaModule.ms")
    fileIn (rootPath + "Modules\\Camera\\CameraModule.ms")
    fileIn (rootPath + "Modules\\More\\MoreModule.ms")

    fileIn (rootPath + "UI\\BSMS_MainWindow.ms")

    if BSMS_OpenStudio != undefined then
    (
        BSMS_OpenStudio()
    )
    else
    (
        messageBox "BS Max Studio failed to load UI/BSMS_MainWindow.ms" title:"BS Max Studio"
    )
)
