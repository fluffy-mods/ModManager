using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;

namespace ModManager
{
    public class ModManagerSettings : ModSettings
    {
        private List<ModAttributes> _saveableModAttributes;
        private List<ButtonAttributes> _saveableButtonAttributes;
        public Dictionary<string, ModAttributes> ModAttributes = new Dictionary<string, ModAttributes>();
        public Dictionary<string, ButtonAttributes> ButtonAttributes = new Dictionary<string, ButtonAttributes>();

        public ModAttributes this[ModMetaData mod]
        {
            get
            {
                if (ModAttributes.ContainsKey(mod.Identifier))
                    return ModAttributes[mod.Identifier];
                var attributes = new ModAttributes(mod);
                ModAttributes.Add(mod.Identifier, attributes);
                return attributes;
            }
        }

        public ButtonAttributes this[ModButton_Installed button]
        {
            get
            {
                if (ButtonAttributes.ContainsKey(button.Name))
                    return ButtonAttributes[button.Name];
                var attributes = new ButtonAttributes(button);
                ButtonAttributes.Add(button.Name, attributes);
                return attributes;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if ( Scribe.mode == LoadSaveMode.Saving )
            {
                _saveableButtonAttributes = ButtonAttributes.Values
                    .Where( a => !a.IsDefault )
                    .ToList();
                _saveableModAttributes = ModAttributes.Values
                    .Where( a => !a.IsDefault )
                    .ToList();
            }


            Scribe_Collections.Look( ref _saveableModAttributes, "ModAttributes", LookMode.Deep );
            Scribe_Collections.Look( ref _saveableButtonAttributes, "ButtonAttributes", LookMode.Deep );


            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                ModAttributes = new Dictionary<string, ModAttributes>();
                if ( !_saveableModAttributes.NullOrEmpty() )
                    foreach ( var modAttribute in _saveableModAttributes )
                        if ( modAttribute.Mod != null && !modAttribute.IsDefault )
                            ModAttributes.Add( modAttribute.Mod.Identifier, modAttribute );

                ButtonAttributes = new Dictionary<string, ButtonAttributes>();
                if ( !_saveableButtonAttributes.NullOrEmpty() )
                    foreach ( var buttonAttribute in _saveableButtonAttributes )
                        if ( buttonAttribute.Button != null && !buttonAttribute.IsDefault )
                            ButtonAttributes.Add( buttonAttribute.Button.Name, buttonAttribute );
            }
        }
    }
}