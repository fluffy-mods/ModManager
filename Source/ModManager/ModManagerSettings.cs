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
        public Dictionary<string, ModAttributes> ModAttributes = new Dictionary<string, ModAttributes>();

        public ModAttributes this[ ModMetaData mod ]
        {
            get
            {
                if ( ModAttributes.ContainsKey( mod.Identifier ) )
                    return ModAttributes[mod.Identifier];
                var attributes = new ModAttributes( mod );
                ModAttributes.Add( mod.Identifier, attributes );
                return attributes;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if ( Scribe.mode == LoadSaveMode.Saving )
                _saveableModAttributes = ModAttributes.Values
                    .Where( a => !a.IsDefault )
                    .ToList();

            Scribe_Collections.Look( ref _saveableModAttributes, "ModAttributes", LookMode.Deep );


            if ( Scribe.mode == LoadSaveMode.PostLoadInit )
            {
                ModAttributes = new Dictionary<string, ModAttributes>();
                foreach ( var modAttribute in _saveableModAttributes)
                    if ( modAttribute.Mod != null && !modAttribute.IsDefault )
                        ModAttributes.Add( modAttribute.Mod.Identifier, modAttribute );
            }
        }
    }
}