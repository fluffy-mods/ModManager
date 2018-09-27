using System.Reflection;
using Harmony;
using Verse;

namespace ModManager
{
    public class ModManager: Mod
    {
        public ModManager( ModContentPack content ) : base( content )
        {
            HarmonyInstance.DEBUG = true;
            var harmonyInstance = HarmonyInstance.Create( "fluffy.modmanager" );
            harmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
        }
    }
}