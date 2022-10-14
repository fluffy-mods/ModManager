using System;
using HarmonyLib;
using Verse;

namespace ModManager {
    [HarmonyPatch(typeof(ModMetaData), nameof(ModMetaData.SamePackageId))]
    public class Patch_ModMetaData_SamePackageId {
        public static bool Prefix(ModMetaData __instance, ref bool __result, string ___packageIdLowerCase,
                                   bool ignorePostfix, string otherPackageId) {
            __result = ___packageIdLowerCase != null
&& (ignorePostfix
                    ? ___packageIdLowerCase.StripPostfixes().Equals(otherPackageId, StringComparison.CurrentCultureIgnoreCase)
                    : __instance.PackageId.Equals(otherPackageId, StringComparison.CurrentCultureIgnoreCase));
            return false;
        }
    }
}
