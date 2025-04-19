using HarmonyLib;

namespace ChallengeDeck.Modules
{
    internal class AlwaysGifts
    {
        public static void Activate()
        {
            ChallengeDeck.Harmony.PatchAll(typeof(HasCollectibleBeenFoundPatch));
        }
        [HarmonyPatch(typeof(LevelStats), "HasCollectibleBeenFound")]
        public static class HasCollectibleBeenFoundPatch
        {
            static bool Prefix(LevelStats __instance, ref bool __result)
            {
                if (!ChallengeDeck.Settings.AlwaysGifts.Value)
                    return true;

                __result = false;
                return false;
            }
        }
    }
}
