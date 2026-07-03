global BSMS_Config

struct BSMS_ConfigStruct
(
    appName = "BS Max Studio",
    settingsDir = ((getDir #userScripts) + "\\BSMaxStudio"),
    settingsFile = "",
    defaultWidth = 420,
    defaultHeight = 38,
    defaultX = 160,
    defaultY = 120,

    fn init =
    (
        BSMS_EnsureDirectory settingsDir
        settingsFile = settingsDir + "\\BSMaxStudio.ini"
    ),

    fn readInt section key defaultValue =
    (
        local raw = getINISetting settingsFile section key
        if raw == undefined or raw == "" then defaultValue else raw as integer
    ),

    fn writeInt section key value =
    (
        setINISetting settingsFile section key (value as string)
    ),

    fn getWindowState =
    (
        #(readInt "Window" "X" defaultX, readInt "Window" "Y" defaultY, readInt "Window" "Width" defaultWidth, readInt "Window" "Height" defaultHeight)
    ),

    fn saveWindowState posValue sizeValue =
    (
        writeInt "Window" "X" posValue.x
        writeInt "Window" "Y" posValue.y
        writeInt "Window" "Width" sizeValue.x
        writeInt "Window" "Height" sizeValue.y
    )
)

BSMS_Config = BSMS_ConfigStruct()
BSMS_Config.init()
