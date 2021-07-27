// Copyright Karel Kroeze, 2020-2021.
// ModManager/ModManager/Debug.cs

using System.Diagnostics;

namespace ModManager
{
    public static class Debug
    {
        public static void Error(string msg)
        {
            Verse.Log.Error("Mod Manager :: " + msg);
        }

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Verse.Log.Message("Mod Manager :: " + message);
        }

        [Conditional("TRACE_DEPENDENCIES")]
        public static void TraceDependencies(string message)
        {
            Verse.Log.Message("Mod Manager :: Dependencies :: " + message);
        }

        [Conditional("TRACE_PROMOTIONS")]
        public static void TracePromotions(string message)
        {
            Verse.Log.Message("Mod Manager :: Promotions :: " + message);
        }
    }
}