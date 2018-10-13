// ButtonAttributes.cs
// Copyright Karel Kroeze, 2018-2018

using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class ButtonAttributes: IExposable
    {
        private string _name;
        public ModButton_Installed Button { get; private set; }
        private Color _color = Color.white;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                ModManager.WriteAttributes();
            }
        }
        public ButtonAttributes() { 
            // scribe
        }

        public ButtonAttributes( ModButton_Installed button )
        {
            Button = button;
            _name = button.Name;
        }

        public bool IsDefault => _color == Color.white;

        public bool TryResolve()
        {
            Button = ModButtonManager.AllButtons
                .OfType<ModButton_Installed>()
                .FirstOrDefault( b => b.Name == _name );
            return Button != null;
        }
        public void ExposeData()
        {
            Scribe_Values.Look( ref _name, "Name" );
            Scribe_Values.Look( ref _color, "Color", Color.white );

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                TryResolve();
        }
    }
}