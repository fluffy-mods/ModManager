// Utilities.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager
{
    public class Utilities
    {
        public static bool ButtonIcon( ref Rect rect, Texture2D icon, string tooltip = null, Texture2D iconAddon = null,
            Direction8Way addonLocation = Direction8Way.NorthEast, Color? mouseOverColor = null,
            Color? baseColor = null, int gapSize = SmallMargin, UIDirection direction = UIDirection.LeftThenDown )
        {
            if ( Mouse.IsOver( rect ) )
                GUI.color = mouseOverColor ?? GenUI.MouseoverColor;
            else
                GUI.color = baseColor ?? Color.white;

            GUI.DrawTexture( rect, icon );
            if ( iconAddon != null )
                GUI.DrawTexture( AddonRect( rect, addonLocation ), iconAddon );
            if ( !tooltip.NullOrEmpty() )
                TooltipHandler.TipRegion( rect, tooltip );

            var clicked = Widgets.ButtonInvisible( rect );

            switch ( direction )
            {
                    case UIDirection.LeftThenDown:
                    case UIDirection.LeftThenUp:
                        rect.x -= rect.width + gapSize;
                        break;
                    case UIDirection.RightThenDown:
                    case UIDirection.RightThenUp:
                        rect.x += rect.width + gapSize;
                        break;
            }
            return clicked;
        }

        private static Rect AddonRect( Rect canvas, Direction8Way loc )
        {
            var rect = new Rect( 0f, 0f, canvas.width / 2f, canvas.height / 2f );
            switch ( loc )
            {
                case Direction8Way.NorthWest:
                    rect.x = canvas.xMin;
                    rect.y = canvas.yMin;
                    break;
                case Direction8Way.NorthEast:
                    rect.x = canvas.xMin + canvas.width / 2f;
                    rect.y = canvas.yMin;
                    break;
                default:
                    Debug.Log( "invalid addon icon location: " + loc );
                    break;
            }
            return rect;
        }

        public static float MaxWidth( params string[] strings )
        {
            Text.WordWrap = false;
            var max = Mathf.Max( strings.Select( s => Text.CalcSize( s ).x ).ToArray() );
            Text.WordWrap = true;
            return max;
        }

        public static int Modulo( int x, int m )
        {
            var r = x % m;
            if ( r < 0 ) r += m;
            return r;
        }

        public static void DoLabel( ref Rect canvas, string label )
        {
            var labelRect = new Rect(
                canvas.xMin + SmallIconSize,
                canvas.yMin,
                canvas.width - SmallIconSize,
                LabelHeight );
            canvas.yMin += LabelHeight - LabelOffset;
            GUI.color = Color.grey;
            Text.Font = GameFont.Tiny;
            Widgets.Label( labelRect, label );
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        public static void ActionButton( Rect canvas, Action resolve )
        {
            Widgets.DrawHighlightIfMouseover( canvas );
            if ( Widgets.ButtonInvisible( canvas ) ) resolve?.Invoke();
        }

        public static void FloatMenu( List<FloatMenuOption> options )
        {
            Find.WindowStack.Add( new FloatMenu( options ) );
        }

        public static List<FloatMenuOption> NewOptions => new List<FloatMenuOption>();


        public static void OpenSettingsFor( ModMetaData mod )
        {
            if ( !mod.HasSettings() )
                return;
            OpenSettingsFor( mod.ModClassWithSettings() );
        }

        private static Regex authorTag = new Regex( @"^\[[A-Z]{1,3}\]", RegexOptions.IgnoreCase );
        private static Regex versionTag = new Regex( @"(\[|\()(\d\.\d+|[A-B]\d{2})(\]|\))", RegexOptions.IgnoreCase );
        private static Regex versionString = new Regex(@"(\[|\()?v?\d+(\.\d+)+|[A-B]\d{2}(\]|\))?", RegexOptions.IgnoreCase );
        public static string TrimModName( string name )
        {
            if ( ModManager.Settings.TrimTags )
            {
                name = authorTag.Replace( name, "" );
                name = versionTag.Replace( name, "" );
                if ( ModManager.Settings.TrimVersionStrings )
                    name = versionString.Replace( name, "" );
                name = name.Trim().Trim( '-' ).Trim();
            }

            return name;
        }

        public static void OpenSettingsFor( Mod mod )
        {
            if ( mod.SettingsCategory().NullOrEmpty() )
                return;
            var dialog = new Dialog_ModSettings();
            Traverse.Create( dialog ).Field<Mod>( "selMod" ).Value = mod;
            Find.WindowStack.Add( dialog );
        }
    }
}