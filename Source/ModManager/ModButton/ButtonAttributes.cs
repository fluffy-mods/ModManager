// ButtonAttributes.cs
// Copyright Karel Kroeze, 2018-2018

using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class ButtonAttributes: IUserData
    {
        public ModButton Button { get; set; }
        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Write();
            }
        }
        public ButtonAttributes() { 
            // scribe
        }

        public ButtonAttributes( ModButton button )
        {
            Button = button;
        }

        public void ExposeData()
        {
            Scribe_Values.Look( ref _color, "Color", Color.white );
        }

        public string FilePath => UserData.GetButtonAttributesPath( Button );

        public void Write()
        {
            UserData.Write( this );
        }
    }
}