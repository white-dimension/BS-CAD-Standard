global BSMS_Modules
global BSMS_ModuleDef
global BSMS_RegisterModule
global BSMS_FindModule
global BSMS_GetFirstModule

struct BSMS_ModuleDef
(
    id,
    title,
    tools
)

BSMS_Modules = #()

fn BSMS_RegisterModule id title tools =
(
    append BSMS_Modules (BSMS_ModuleDef id:id title:title tools:tools)
)

fn BSMS_FindModule moduleId =
(
    for moduleDef in BSMS_Modules where moduleDef.id == moduleId do return moduleDef
    undefined
)

fn BSMS_GetFirstModule =
(
    if BSMS_Modules.count > 0 then BSMS_Modules[1] else undefined
)
