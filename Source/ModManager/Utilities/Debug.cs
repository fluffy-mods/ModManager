// Debug.cs
// Copyright Karel Kroeze, 2018-2018
using System.Diagnostics;

namespace ModManager
{
    public static class Debug
    {
        [Conditional("DEBUG")]
        public static void Log( string message)
        {
            Verse.Log.Message( "Mod Manager :: " + message, true );
        }

        [Conditional( "TRACE_PROMOTIONS" )]
        public static void TracePromotions( string message ) =>
            Verse.Log.Message( "Mod Manager :: Promotions :: " + message, true );

        [Conditional( "TRACE_DEPENDENCIES" )]
        public static void TraceDependencies( string message ) =>
            Verse.Log.Message( "Mod Manager :: Dependencies :: " + message, true );

        public static void Error( string msg ) => Verse.Log.Error( "Mod Manager :: " + msg );

    }
}