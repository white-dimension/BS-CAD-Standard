global BSMS_OpenStudio
global BSMS_CloseStudio
global BSMS_Form
global BSMS_MainPanel
global BSMS_SubPanel
global BSMS_PrimaryButtons
global BSMS_PrimaryIndicators
global BSMS_SubButtons
global BSMS_ControlButtons
global BSMS_LogoButton
global BSMS_ActiveModuleId
global BSMS_IsMini
global BSMS_IsDragging
global BSMS_DragMouse
global BSMS_DragForm
global BSMS_AutoCollapseEnabled
global BSMS_AutoCollapseDelay
global BSMS_CollapseTimer

BSMS_Form = undefined
BSMS_MainPanel = undefined
BSMS_SubPanel = undefined
BSMS_PrimaryButtons = #()
BSMS_PrimaryIndicators = #()
BSMS_SubButtons = #()
BSMS_ControlButtons = #()
BSMS_LogoButton = undefined
BSMS_ActiveModuleId = ""
BSMS_IsMini = false
BSMS_IsDragging = false
BSMS_DragMouse = undefined
BSMS_DragForm = undefined
BSMS_AutoCollapseEnabled = false
BSMS_AutoCollapseDelay = 500
BSMS_CollapseTimer = undefined

fn BSMS_Size widthValue heightValue =
(
    dotNetObject "System.Drawing.Size" widthValue heightValue
)

fn BSMS_Point xValue yValue =
(
    dotNetObject "System.Drawing.Point" xValue yValue
)

fn BSMS_SetBounds control x y widthValue heightValue =
(
    control.Location = BSMS_Point x y
    control.Size = BSMS_Size widthValue heightValue
)

fn BSMS_RoundControl control radius =
(
    try
    (
        local path = dotNetObject "System.Drawing.Drawing2D.GraphicsPath"
        local rect = dotNetObject "System.Drawing.Rectangle" 0 0 control.Width control.Height
        local d = radius * 2
        path.AddArc rect.X rect.Y d d 180 90
        path.AddArc (rect.Right - d - 1) rect.Y d d 270 90
        path.AddArc (rect.Right - d - 1) (rect.Bottom - d - 1) d d 0 90
        path.AddArc rect.X (rect.Bottom - d - 1) d d 90 90
        path.CloseFigure()
        control.Region = dotNetObject "System.Drawing.Region" path
    )
    catch()
)

fn BSMS_TextWidth textValue =
(
    case textValue of
    (
        "Corona": 56
        default: 44
    )
)

fn BSMS_Max a b =
(
    if a > b then a else b
)

fn BSMS_ToolWidth textValue =
(
    case textValue of
    (
        "Corona Sun": 78
        "Corona Sky": 76
        default: (BSMS_Max 42 ((textValue.count * 10) + 18))
    )
)

fn BSMS_GetCollapsedWidth =
(
    local total = 10
    for moduleDef in BSMS_Modules do total += (BSMS_TextWidth moduleDef.title) + 7
    total + 48
)

fn BSMS_GetExpandedWidth moduleDef =
(
    local total = 10
    for toolName in moduleDef.tools do total += (BSMS_ToolWidth toolName) + 6
    total + 4
)

fn BSMS_TargetWidth =
(
    if BSMS_IsMini do return 38

    local baseWidth = BSMS_GetCollapsedWidth()
    if BSMS_ActiveModuleId != "" then
    (
        local moduleDef = BSMS_FindModule BSMS_ActiveModuleId
        if moduleDef != undefined do baseWidth = BSMS_Max baseWidth (BSMS_GetExpandedWidth moduleDef)
    )
    baseWidth
)

fn BSMS_TargetHeight =
(
    if BSMS_IsMini then 38 else (if BSMS_ActiveModuleId == "" then 38 else 74)
)

fn BSMS_ApplyTabStyle tab isActive:false =
(
    tab.BackColor = BSMS_Theme.topBar
    tab.ForeColor = if isActive then BSMS_Theme.text else BSMS_Theme.muted
    tab.Font = BSMS_Theme.font 9.5 style:(if isActive then #bold else #regular)
    tab.Cursor = (dotNetClass "System.Windows.Forms.Cursors").Hand
)

fn BSMS_ApplySubPillStyle btn =
(
    btn.FlatStyle = (dotNetClass "System.Windows.Forms.FlatStyle").Flat
    btn.FlatAppearance.BorderSize = 0
    btn.FlatAppearance.MouseOverBackColor = BSMS_Theme.hover
    btn.FlatAppearance.MouseDownBackColor = BSMS_Theme.pressed
    btn.BackColor = BSMS_Theme.panel
    btn.ForeColor = BSMS_Theme.text
    btn.Font = BSMS_Theme.font 8.5
    btn.Cursor = (dotNetClass "System.Windows.Forms.Cursors").Hand
    btn.UseVisualStyleBackColor = false
    BSMS_RoundControl btn 14
)

fn BSMS_ApplyControlStyle labelControl =
(
    labelControl.BackColor = BSMS_Theme.topBar
    labelControl.ForeColor = BSMS_Theme.muted
    labelControl.Font = BSMS_Theme.font 11
    labelControl.TextAlign = (dotNetClass "System.Drawing.ContentAlignment").MiddleCenter
    labelControl.Cursor = (dotNetClass "System.Windows.Forms.Cursors").Hand
)

fn BSMS_ControlMouseEnter sender args =
(
    sender.BackColor = BSMS_Theme.hover
    sender.ForeColor = BSMS_Theme.text
)

fn BSMS_ControlMouseLeave sender args =
(
    sender.BackColor = BSMS_Theme.topBar
    sender.ForeColor = BSMS_Theme.muted
)

fn BSMS_ApplyLogoStyle labelControl =
(
    labelControl.BackColor = BSMS_Theme.topBar
    labelControl.ForeColor = BSMS_Theme.accent
    labelControl.Font = BSMS_Theme.font 10.5 style:#bold
    labelControl.TextAlign = (dotNetClass "System.Drawing.ContentAlignment").MiddleCenter
    labelControl.Cursor = (dotNetClass "System.Windows.Forms.Cursors").Hand
)

fn BSMS_Message sender args =
(
    messageBox (sender.Text as string) title:BSMS_Config.appName
)

fn BSMS_FormMouseDown sender args =
(
    BSMS_IsDragging = true
    BSMS_DragMouse = (dotNetClass "System.Windows.Forms.Control").MousePosition
    BSMS_DragForm = BSMS_Form.Location
)

fn BSMS_FormMouseMove sender args =
(
    if BSMS_IsDragging do
    (
        local currentMouse = (dotNetClass "System.Windows.Forms.Control").MousePosition
        local dx = currentMouse.X - BSMS_DragMouse.X
        local dy = currentMouse.Y - BSMS_DragMouse.Y
        BSMS_Form.Location = BSMS_Point (BSMS_DragForm.X + dx) (BSMS_DragForm.Y + dy)
    )
)

fn BSMS_FormMouseUp sender args =
(
    BSMS_IsDragging = false
)

fn BSMS_StopCollapseTimer =
(
    if BSMS_CollapseTimer != undefined do BSMS_CollapseTimer.Stop()
)

fn BSMS_CollapseNow =
(
    if BSMS_ActiveModuleId != "" do
    (
        BSMS_ActiveModuleId = ""
        BSMS_ClearSubButtons()
        BSMS_UpdatePrimaryState()
        BSMS_Relayout()
    )
)

fn BSMS_MinimizeToolbar sender args =
(
    BSMS_ActiveModuleId = ""
    BSMS_IsMini = true
    BSMS_ClearSubButtons()
    BSMS_UpdatePrimaryState()
    BSMS_Relayout()
)

fn BSMS_RestoreToolbar sender args =
(
    BSMS_IsMini = false
    BSMS_Relayout()
)

fn BSMS_ExitToolbar sender args =
(
    BSMS_CloseStudio()
)

fn BSMS_BlankClick sender args =
(
    if not BSMS_IsMini do BSMS_CollapseNow()
)

fn BSMS_CollapseTick sender args =
(
    BSMS_StopCollapseTimer()
    BSMS_CollapseNow()
)

fn BSMS_StartCollapseTimer =
(
    if BSMS_AutoCollapseEnabled do
    (
        if BSMS_CollapseTimer != undefined do
        (
            BSMS_CollapseTimer.Interval = BSMS_AutoCollapseDelay
            BSMS_CollapseTimer.Stop()
            BSMS_CollapseTimer.Start()
        )
    )
)

fn BSMS_MouseEnterToolbar sender args =
(
    BSMS_StopCollapseTimer()
)

fn BSMS_MouseLeaveToolbar sender args =
(
    BSMS_StartCollapseTimer()
)

fn BSMS_TabMouseEnter sender args =
(
    if ((sender.Tag as string) != BSMS_ActiveModuleId) do sender.BackColor = BSMS_Theme.hover
    BSMS_StopCollapseTimer()
)

fn BSMS_TabMouseLeave sender args =
(
    sender.BackColor = BSMS_Theme.topBar
)

fn BSMS_SaveFormState sender args =
(
    try (BSMS_Config.saveWindowState BSMS_Form.Location BSMS_Form.Size) catch()
)

fn BSMS_UpdatePrimaryState =
(
    for i = 1 to BSMS_PrimaryButtons.count do
    (
        local tab = BSMS_PrimaryButtons[i]
        local indicator = BSMS_PrimaryIndicators[i]
        local isActive = ((tab.Tag as string) == BSMS_ActiveModuleId)
        BSMS_ApplyTabStyle tab isActive:isActive
        indicator.Visible = isActive
    )
)

fn BSMS_ClearSubButtons =
(
    if BSMS_SubPanel != undefined do BSMS_SubPanel.Controls.Clear()
    BSMS_SubButtons = #()
)

fn BSMS_BuildSubButtons moduleDef =
(
    BSMS_ClearSubButtons()
    if moduleDef == undefined do return false

    local x = 7
    for toolName in moduleDef.tools do
    (
        local widthValue = BSMS_ToolWidth toolName
        local btn = dotNetObject "System.Windows.Forms.Button"
        btn.Text = toolName
        btn.Tag = toolName
        BSMS_SetBounds btn x 3 widthValue 28
        BSMS_ApplySubPillStyle btn
        dotNet.addEventHandler btn "Click" BSMS_Message
        dotNet.addEventHandler btn "MouseEnter" BSMS_MouseEnterToolbar
        dotNet.addEventHandler btn "MouseLeave" BSMS_MouseLeaveToolbar
        BSMS_SubPanel.Controls.Add btn
        append BSMS_SubButtons btn
        x += widthValue + 6
    )
)

fn BSMS_Relayout =
(
    local targetWidth = BSMS_TargetWidth()
    local targetHeight = BSMS_TargetHeight()

    BSMS_Form.ClientSize = BSMS_Size targetWidth targetHeight
    BSMS_MainPanel.Size = BSMS_Size targetWidth targetHeight
    BSMS_RoundControl BSMS_MainPanel 14
    BSMS_RoundControl BSMS_Form 14

    if BSMS_LogoButton != undefined do
    (
        BSMS_LogoButton.Visible = BSMS_IsMini
        BSMS_SetBounds BSMS_LogoButton 0 0 targetWidth targetHeight
    )

    for btn in BSMS_PrimaryButtons do btn.Visible = not BSMS_IsMini
    for indicator in BSMS_PrimaryIndicators do indicator.Visible = false
    for btn in BSMS_ControlButtons do btn.Visible = not BSMS_IsMini

    if BSMS_IsMini do
    (
        if BSMS_SubPanel != undefined do BSMS_SubPanel.Visible = false
        return true
    )

    local x = 6
    for i = 1 to BSMS_PrimaryButtons.count do
    (
        local btn = BSMS_PrimaryButtons[i]
        local indicator = BSMS_PrimaryIndicators[i]
        local widthValue = BSMS_TextWidth btn.Text
        BSMS_SetBounds btn x 4 widthValue 30
        BSMS_SetBounds indicator (x + 8) 34 (widthValue - 16) 3
        indicator.Visible = ((btn.Tag as string) == BSMS_ActiveModuleId)
        x += widthValue + 6
    )

    if BSMS_ControlButtons.count >= 2 do
    (
        BSMS_SetBounds BSMS_ControlButtons[1] (targetWidth - 43) 4 18 30
        BSMS_SetBounds BSMS_ControlButtons[2] (targetWidth - 23) 4 18 30
    )

    if BSMS_SubPanel != undefined do
    (
        BSMS_SubPanel.Visible = (BSMS_ActiveModuleId != "")
        BSMS_SetBounds BSMS_SubPanel 0 38 targetWidth 34
    )
)

fn BSMS_PrimaryClick sender args =
(
    local moduleId = sender.Tag as string
    if BSMS_ActiveModuleId == moduleId then
    (
        BSMS_ActiveModuleId = ""
        BSMS_ClearSubButtons()
    )
    else
    (
        BSMS_ActiveModuleId = moduleId
        BSMS_BuildSubButtons (BSMS_FindModule moduleId)
    )

    BSMS_UpdatePrimaryState()
    BSMS_Relayout()
)

fn BSMS_BuildPrimaryButtons =
(
    BSMS_PrimaryButtons = #()
    BSMS_PrimaryIndicators = #()
    local x = 6
    for moduleDef in BSMS_Modules do
    (
        local btn = dotNetObject "System.Windows.Forms.Label"
        btn.Text = moduleDef.title
        btn.Tag = moduleDef.id
        btn.TextAlign = (dotNetClass "System.Drawing.ContentAlignment").MiddleCenter
        local widthValue = BSMS_TextWidth moduleDef.title
        BSMS_SetBounds btn x 4 widthValue 30
        BSMS_ApplyTabStyle btn
        dotNet.addEventHandler btn "Click" BSMS_PrimaryClick
        dotNet.addEventHandler btn "MouseEnter" BSMS_TabMouseEnter
        dotNet.addEventHandler btn "MouseLeave" BSMS_TabMouseLeave
        BSMS_MainPanel.Controls.Add btn
        append BSMS_PrimaryButtons btn

        local indicator = dotNetObject "System.Windows.Forms.Panel"
        indicator.BackColor = BSMS_Theme.accent
        indicator.Visible = false
        BSMS_SetBounds indicator (x + 8) 34 (widthValue - 16) 3
        BSMS_MainPanel.Controls.Add indicator
        append BSMS_PrimaryIndicators indicator

        x += widthValue + 6
    )
)

fn BSMS_BuildControlButtons =
(
    BSMS_ControlButtons = #()

    local minimizeBtn = dotNetObject "System.Windows.Forms.Label"
    minimizeBtn.Text = "-"
    minimizeBtn.Tag = "minimize"
    BSMS_ApplyControlStyle minimizeBtn
    dotNet.addEventHandler minimizeBtn "Click" BSMS_MinimizeToolbar
    dotNet.addEventHandler minimizeBtn "MouseEnter" BSMS_ControlMouseEnter
    dotNet.addEventHandler minimizeBtn "MouseLeave" BSMS_ControlMouseLeave
    BSMS_MainPanel.Controls.Add minimizeBtn
    append BSMS_ControlButtons minimizeBtn

    local closeBtn = dotNetObject "System.Windows.Forms.Label"
    closeBtn.Text = "x"
    closeBtn.Tag = "close"
    BSMS_ApplyControlStyle closeBtn
    dotNet.addEventHandler closeBtn "Click" BSMS_ExitToolbar
    dotNet.addEventHandler closeBtn "MouseEnter" BSMS_ControlMouseEnter
    dotNet.addEventHandler closeBtn "MouseLeave" BSMS_ControlMouseLeave
    BSMS_MainPanel.Controls.Add closeBtn
    append BSMS_ControlButtons closeBtn

    BSMS_LogoButton = dotNetObject "System.Windows.Forms.Label"
    BSMS_LogoButton.Text = "BS"
    BSMS_LogoButton.Visible = false
    BSMS_ApplyLogoStyle BSMS_LogoButton
    dotNet.addEventHandler BSMS_LogoButton "Click" BSMS_RestoreToolbar
    dotNet.addEventHandler BSMS_LogoButton "MouseDown" BSMS_FormMouseDown
    dotNet.addEventHandler BSMS_LogoButton "MouseMove" BSMS_FormMouseMove
    dotNet.addEventHandler BSMS_LogoButton "MouseUp" BSMS_FormMouseUp
    BSMS_MainPanel.Controls.Add BSMS_LogoButton
)

fn BSMS_CloseStudio =
(
    try
    (
        if BSMS_Form != undefined do BSMS_Form.Close()
    )
    catch()
)

fn BSMS_OpenStudio =
(
    BSMS_CloseStudio()

    local state = BSMS_Config.getWindowState()
    BSMS_ActiveModuleId = ""

    BSMS_Form = dotNetObject "System.Windows.Forms.Form"
    BSMS_Form.Text = BSMS_Config.appName
    BSMS_Form.FormBorderStyle = (dotNetClass "System.Windows.Forms.FormBorderStyle").None
    BSMS_Form.StartPosition = (dotNetClass "System.Windows.Forms.FormStartPosition").Manual
    BSMS_Form.Location = BSMS_Point state[1] state[2]
    BSMS_Form.ClientSize = BSMS_Size (BSMS_GetCollapsedWidth()) 38
    BSMS_Form.BackColor = BSMS_Theme.background
    BSMS_Form.TopMost = true
    BSMS_Form.ShowInTaskbar = false
    BSMS_Form.Opacity = 0.96

    BSMS_MainPanel = dotNetObject "System.Windows.Forms.Panel"
    BSMS_MainPanel.BackColor = BSMS_Theme.topBar
    BSMS_SetBounds BSMS_MainPanel 0 0 BSMS_Form.ClientSize.Width BSMS_Form.ClientSize.Height
    BSMS_Form.Controls.Add BSMS_MainPanel

    BSMS_SubPanel = dotNetObject "System.Windows.Forms.Panel"
    BSMS_SubPanel.BackColor = BSMS_Theme.topBar
    BSMS_SubPanel.Visible = false
    BSMS_SetBounds BSMS_SubPanel 0 38 BSMS_Form.ClientSize.Width 34
    BSMS_MainPanel.Controls.Add BSMS_SubPanel

    BSMS_CollapseTimer = dotNetObject "System.Windows.Forms.Timer"
    BSMS_CollapseTimer.Interval = BSMS_AutoCollapseDelay
    dotNet.addEventHandler BSMS_CollapseTimer "Tick" BSMS_CollapseTick

    BSMS_BuildPrimaryButtons()
    BSMS_BuildControlButtons()
    BSMS_Relayout()

    for dragTarget in #(BSMS_Form, BSMS_MainPanel, BSMS_SubPanel) do
    (
        dotNet.addEventHandler dragTarget "MouseDown" BSMS_FormMouseDown
        dotNet.addEventHandler dragTarget "MouseMove" BSMS_FormMouseMove
        dotNet.addEventHandler dragTarget "MouseUp" BSMS_FormMouseUp
        dotNet.addEventHandler dragTarget "MouseEnter" BSMS_MouseEnterToolbar
        dotNet.addEventHandler dragTarget "MouseLeave" BSMS_MouseLeaveToolbar
    )
    dotNet.addEventHandler BSMS_MainPanel "Click" BSMS_BlankClick
    dotNet.addEventHandler BSMS_SubPanel "Click" BSMS_BlankClick
    dotNet.addEventHandler BSMS_Form "FormClosing" BSMS_SaveFormState

    BSMS_Form.Show()
)
