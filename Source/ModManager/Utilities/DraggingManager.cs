// DraggingManager.cs
// Copyright Karel Kroeze, 2018-2018

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ModManager.Constants;

namespace ModManager
{
    public static class DraggingManager
    {
        public static ModButton Dragged;
        public static bool Dragging => Dragged != null;
        public static bool Dropped;

        public static void Update()
        {
            if ( Dropped )
            {
                Dragged = null;
                Dropped = false;
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            if ( Dragging && Event.current.type == EventType.MouseUp )
            {
                Dropped = true;
            }
        }

        public static bool ContainerUpdate<T>( IEnumerable<T> mods, Rect rect ) where T: ModButton
        {
            return ContainerUpdate( mods, rect, out _ );
        }

        public static bool ContainerUpdate<T>( IEnumerable<T> mods, Rect rect, out int index ) where T: ModButton
        {
            index = -1;
            if ( !Mouse.IsOver( rect ) )
                return false;

            // get index of mousePosition
            var position = ( Event.current.mousePosition.y - rect.yMin ) / ModButtonHeight;

            // start drag
            var dragIndex = Mathf.FloorToInt( position );
            var clampedDragIndex = Mathf.Clamp( dragIndex, 0, mods.Count() - 1 );
            if ( !Dragging && Event.current.type == EventType.MouseDrag && dragIndex == clampedDragIndex )
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Dragged = mods.ElementAt( clampedDragIndex );
            }

            if ( Dragging )
            {
                var dropIndex = Mathf.RoundToInt( position );
                var clampedDropIndex = Mathf.Clamp( dropIndex, 0, mods.Count() );
                index = clampedDropIndex;

                // drop element into place
                if ( Dropped )
                    return true;
            }

            return false;
        }

        public static void OnGUI()
        {
            if ( !Dragging )
                return;

            // draw as mouse attachment                
            var rect = new Rect( 0, 0, ModButtonWidth, ModButtonHeight );
            var pos = Event.current.mousePosition;
            rect.position = pos + new Vector2( 6f, 6f );

            // because it's my favourite number.
            Find.WindowStack.ImmediateWindow(24, rect, WindowLayer.Super, () =>
            {
                Dragged?.DoModButton( rect.AtZero(), true );
            }, false );
        }
    }
}