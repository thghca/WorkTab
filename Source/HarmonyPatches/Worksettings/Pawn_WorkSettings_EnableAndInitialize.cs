// Karel Kroeze
// Pawn_WorkSettings_SetPriority.cs
// 2017-05-22

using Harmony;
using RimWorld;
using Verse;

namespace WorkTab
{
    [HarmonyPatch( typeof( Pawn_WorkSettings), "EnableAndInitialize")]
    public class Pawn_WorkSettings_EnableAndInitialize
    {
        static void Prefix(Pawn_WorkSettings __instance)
        {
            //clean favourite in case of pawn reccurect to avoid overriding
            var pawn = __instance.Pawn();
            FavouriteManager.Get[pawn] = null;
        }
    }
}