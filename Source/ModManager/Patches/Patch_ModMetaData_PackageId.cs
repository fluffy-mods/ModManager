using System;
using HarmonyLib;
using Verse;

namespace ModManager
{
//     [HarmonyPatch( typeof( ModMetaData ), nameof( ModMetaData.PackageId ), MethodType.Getter )]
//     public class Patch_ModMetaData_PackageId
//     {
//         public static bool Prefix( ModMetaData __instance, ref string __result, string ___packageIdLowerCase )
//         {
//             if ()
//             __result = ___packageIdLowerCase;
// //            Debug.Log( $"in: {___packageIdLowerCase}, out: {__result}");
//             return false;
//         }
//     }

    [HarmonyPatch( typeof( ModMetaData ), nameof( ModMetaData.SamePackageId ) )]
    public class Patch_ModMetaData_SamePackageId
    {
        public static bool Prefix( ModMetaData __instance, ref bool __result, string ___packageIdLowerCase,
                                   bool ignorePostfix, string otherPackageId )
        {
            if ( ___packageIdLowerCase == null )
            {
                __result = false;
            } 
            else if ( ignorePostfix )
            {
                // we don't care about steam/local (basically search)
                __result = ___packageIdLowerCase.StripIdentifiers().Equals( otherPackageId, StringComparison.CurrentCultureIgnoreCase );
            }
            else
            {
                // we _do_ care about steam/local, which means that we need to translate to foldername syntax.
                __result = __instance.PackageId.Equals( otherPackageId, StringComparison.CurrentCultureIgnoreCase );
            }
            return false;
        }
    }
}