// Patch_WindowResizer_DoResizeControl.cs
// Copyright Karel Kroeze, 2019-2019

using Harmony;
using UnityEngine;
using Verse;

namespace ModManager
{
    [HarmonyPatch(typeof(WindowResizer), nameof(WindowResizer.DoResizeControl))]
    public static class Patch_WindowResizer_DoResizeControl
    {
        public static void Postfix( ref bool ___isResizing )
        {
            if ( ___isResizing && ( Input.GetMouseButtonUp( 0 ) || !Application.isFocused ) )
                ___isResizing = false;
        }
    }
}