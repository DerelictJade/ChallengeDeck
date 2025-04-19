using HarmonyLib;
using System.Reflection;

namespace ChallengeDeck
{
    internal class UnlockLevelGate
    {
        private static bool _patched = false;
        static readonly MethodInfo ogug = AccessTools.Method(typeof(LevelGate), "SetUnlocked");
        static readonly MethodInfo ugprefixmi= typeof(UnlockLevelGate).GetMethod(nameof(PreSetUnlocked));
        static readonly HarmonyMethod ugprefix = new HarmonyMethod(ugprefixmi);
        public static void Patch(bool apply)
        {
            if (_patched == apply)
                return;
            if (apply)
                DoPatch();
            else
                UndoPatch();
            _patched = apply;
        }
        private static void DoPatch()
        {
            ChallengeDeck.Harmony.Patch(ogug, prefix: ugprefix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogug, ugprefixmi);
        }
        public static bool PreSetUnlocked(LevelGate __instance, ref bool u)
        {
            if (__instance.Unlocked)
                return false;

            u = true;
            return true;
        }
    }
}
