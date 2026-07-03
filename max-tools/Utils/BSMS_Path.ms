global BSMS_NormalizePath
global BSMS_EnsureDirectory

fn BSMS_NormalizePath pathValue =
(
    substituteString pathValue "/" "\\"
)

fn BSMS_EnsureDirectory directoryPath =
(
    if not (doesFileExist directoryPath) do makeDir directoryPath all:true
    directoryPath
)
