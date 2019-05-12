// Resources.cs
// Copyright Karel Kroeze, 2018-2018

using System.Linq;
using UnityEngine;
using Verse;

namespace ModManager
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static Color SlightlyDarkBackground;

        public static Texture2D Close,
            EyeOpen,
            EyeClosed,
            Search,
            Steam,
            Ludeon,
            Folder,
            File,
            Warning,
            Question,
            Palette,
            Gear,
            Status_Cross,
            Status_Down,
            Status_Up,
            Status_Plus;

        public static Texture2D[] Spinner;

        static Resources()
        {
            SlightlyDarkBackground = new Color( 0f, 0f, 0f, .2f );
            Close = ContentFinder<Texture2D>.Get( "UI/Icons/Close" );
            EyeOpen = ContentFinder<Texture2D>.Get( "UI/Icons/EyeOpen" );
            EyeClosed = ContentFinder<Texture2D>.Get( "UI/Icons/EyeClosed" );
            Search = ContentFinder<Texture2D>.Get( "UI/Icons/Search" );
            Steam = ContentFinder<Texture2D>.Get( "UI/Icons/ContentSources/SteamWorkshop" );
            Ludeon = ContentFinder<Texture2D>.Get( "UI/Icons/Ludeon" );
            File = ContentFinder<Texture2D>.Get( "UI/Icons/File" );
            Folder = ContentFinder<Texture2D>.Get( "UI/Icons/ContentSources/LocalFolder" );
            Warning = ContentFinder<Texture2D>.Get( "UI/Icons/Warning" );
            Question = ContentFinder<Texture2D>.Get( "UI/Icons/Question" );
            // the joys of case-unaware file systems - I now don't know which version is out there...
            Palette = ContentFinder<Texture2D>.Get( "UI/Icons/Palette", false );
            if ( Palette == null ) Palette = ContentFinder<Texture2D>.Get( "UI/Icons/palette" );
            Gear = ContentFinder<Texture2D>.Get( "UI/Icons/Gear" );

            Status_Cross = ContentFinder<Texture2D>.Get("UI/Icons/Status/Cross");
            Status_Down = ContentFinder<Texture2D>.Get("UI/Icons/Status/Down");
            Status_Up = ContentFinder<Texture2D>.Get("UI/Icons/Status/Up");
            Status_Plus = ContentFinder<Texture2D>.Get("UI/Icons/Status/Plus");

            Spinner = ContentFinder<Texture2D>.GetAllInFolder( "UI/Icons/Spinner" ).ToArray();
        }
    }
}