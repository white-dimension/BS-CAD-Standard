global BSMS_Theme

struct BSMS_ThemeStruct
(
    colorClass = dotNetClass "System.Drawing.Color",
    fontClass = dotNetClass "System.Drawing.Font",
    fontStyleClass = dotNetClass "System.Drawing.FontStyle",
    flatStyleClass = dotNetClass "System.Windows.Forms.FlatStyle",

    background = undefined,
    topBar = undefined,
    hover = undefined,
    pressed = undefined,
    panel = undefined,
    text = undefined,
    muted = undefined,
    accent = undefined,
    border = undefined,

    fn init =
    (
        background = colorClass.FromArgb 37 37 38
        topBar = colorClass.FromArgb 37 37 38
        hover = colorClass.FromArgb 50 50 51
        pressed = colorClass.FromArgb 74 74 74
        panel = colorClass.FromArgb 50 50 51
        text = colorClass.FromArgb 230 230 230
        muted = colorClass.FromArgb 150 150 150
        accent = colorClass.FromArgb 78 161 255
        border = colorClass.FromArgb 62 62 62
    ),

    fn font size style:#regular =
    (
        local fontStyle = case style of
        (
            #bold: fontStyleClass.Bold
            default: fontStyleClass.Regular
        )
        dotNetObject "System.Drawing.Font" "Segoe UI" size fontStyle
    ),

    fn applyButtonStyle btn isActive:false =
    (
        btn.FlatStyle = flatStyleClass.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.FlatAppearance.MouseOverBackColor = hover
        btn.FlatAppearance.MouseDownBackColor = pressed
        btn.BackColor = if isActive then pressed else topBar
        btn.ForeColor = if isActive then accent else text
        btn.Font = font 9.5 style:(if isActive then #bold else #regular)
        btn.Cursor = (dotNetClass "System.Windows.Forms.Cursors").Hand
        btn.UseVisualStyleBackColor = false
    )
)

BSMS_Theme = BSMS_ThemeStruct()
BSMS_Theme.init()
