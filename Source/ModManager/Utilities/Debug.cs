// Debug.cs
// Copyright Karel Kroeze, 2018-2018

using System.Diagnostics;
using Verse;

namespace ModManager
{
    public static class Debug
    {
        [Conditional("DEBUG")]
        public static void Log( string message)
        {
            Verse.Log.Message( "Mod Manager :: " + message, true );
        }
    }
}