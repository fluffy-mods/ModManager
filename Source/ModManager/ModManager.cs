using System.Reflection;
using Harmony;
using Verse;

namespace ModManager
{
    public class ModManager: Mod
    {
        private static ModManager _instance;
        public ModManager( ModContentPack content ) : base( content )
        {
            _instance = this;
#if DEBUG
            HarmonyInstance.DEBUG = true;
#endif
            var harmonyInstance = HarmonyInstance.Create( "fluffy.modmanager" );
            harmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
        }

        public static void WriteAttributes()
        {
           Attributes.Write();
        }

        public static ModManagerSettings Attributes => _instance.GetSettings<ModManagerSettings>();
    }
}