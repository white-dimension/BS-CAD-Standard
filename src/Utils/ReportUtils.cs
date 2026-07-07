using System;
using Autodesk.AutoCAD.EditorInput;

namespace BS_CAD_STANDARD_1_0_Plugin.Utils
{
    public static class ReportUtils
    {
        public static void Info(Editor ed, string message)
        {
            ed.WriteMessage($"\n[信息] {message}");
        }

        public static void Warning(Editor ed, string message)
        {
            ed.WriteMessage($"\n[警告] {message}");
        }

        public static void Error(Editor ed, string message)
        {
            ed.WriteMessage($"\n[错误] {message}");
        }

        public static void Exception(Editor ed, string message, Exception ex)
        {
            ed.WriteMessage($"\n[异常] {message}: {ex.Message}");
        }
    }
}
