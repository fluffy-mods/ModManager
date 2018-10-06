using System.Reflection;
using Harmony;
using Verse;

namespace ModManager
{
    public class ModManager: Mod
    {
        public ModManager( ModContentPack content ) : base( content )
        {
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            var harmonyInstance = HarmonyInstance.Create( "fluffy.modmanager" );
            harmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
        }
    }
}