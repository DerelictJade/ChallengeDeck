using HarmonyLib;
using System.Reflection;

namespace ChallengeDeck.Modules
{
    internal class MikeyMode
    {
        private static bool _patched = false;
        private static bool _active = false;

        // CardPickup SetCard
        static readonly MethodInfo ogsetcard = AccessTools.Method(typeof(CardPickup), "SetCard");
        static readonly MethodInfo setcardprefixmi = typeof(MikeyMode).GetMethod(nameof(PreSetCard));
        static readonly HarmonyMethod setcardprefix = new HarmonyMethod(setcardprefixmi);
        static readonly MethodInfo setcardpostfixmi = typeof(MikeyMode).GetMethod(nameof(PostSetCard));
        static readonly HarmonyMethod setcardpostfix = new HarmonyMethod(setcardpostfixmi);

        // CardPickup Spawn
        static readonly MethodInfo ogspawn = AccessTools.Method(typeof(CardPickup), "Spawn");
        static readonly MethodInfo spawnprefixmi = typeof(MikeyMode).GetMethod(nameof(PreSpawn));
        static readonly HarmonyMethod spawnprefix = new HarmonyMethod(spawnprefixmi);
        static readonly MethodInfo spawnpostfixmi = typeof(MikeyMode).GetMethod(nameof(PostSpawn));
        static readonly HarmonyMethod spawnpostfix = new HarmonyMethod(spawnpostfixmi);

        // CardPickup SpawnPickupVendor
        static readonly MethodInfo ogspawnpickupvendor = AccessTools.Method(typeof(CardPickup), "SpawnPickupVendor");
        static readonly MethodInfo spawnpickupvendorprefixmi = typeof(MikeyMode).GetMethod(nameof(PreSpawnPickupVendor));
        static readonly HarmonyMethod spawnpickupvendorprefix = new HarmonyMethod(spawnpickupvendorprefixmi);
        static readonly MethodInfo spawnpickupvendorpostfixmi = typeof(MikeyMode).GetMethod(nameof(PostSpawnPickupVendor));
        static readonly HarmonyMethod spawnpickupvendorpostfix = new HarmonyMethod(spawnpickupvendorpostfixmi);

        public static void PreSetCard() { _active = true; }
        public static void PostSetCard() { _active = false; }
        public static void PreSpawn() { _active = true; }
        public static void PostSpawn() { _active = false; }
        public static void PreSpawnPickupVendor() { _active = true; }
        public static void PostSpawnPickupVendor() { _active = false; }

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
            ChallengeDeck.Harmony.Patch(ogsetcard, prefix: setcardprefix);
            ChallengeDeck.Harmony.Patch(ogsetcard, postfix: setcardpostfix);
            ChallengeDeck.Harmony.Patch(ogspawn, prefix: spawnprefix);
            ChallengeDeck.Harmony.Patch(ogspawn, postfix: spawnpostfix);
            ChallengeDeck.Harmony.Patch(ogspawnpickupvendor, prefix: spawnpickupvendorprefix);
            ChallengeDeck.Harmony.Patch(ogspawnpickupvendor, postfix: spawnpickupvendorpostfix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogsetcard, setcardprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogsetcard, setcardpostfixmi);
            ChallengeDeck.Harmony.Unpatch(ogspawn, spawnprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogspawn, spawnpostfixmi);
            ChallengeDeck.Harmony.Unpatch(ogspawnpickupvendor, spawnpickupvendorprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogspawnpickupvendor, spawnpickupvendorpostfixmi);
        }
        public static void Activate()
        {
            ChallengeDeck.Harmony.PatchAll(typeof(PatchAtLaunch));
        }
        public static class PatchAtLaunch
        {
            [HarmonyPatch(typeof(LevelRush), "GetCurrentLevelRushType")]
            [HarmonyPostfix]
            static void ItsMikeyTime(ref LevelRush.LevelRushType __result)
            {
                if (_active)
                {
                    __result = LevelRush.LevelRushType.MikeyRush;
                }
            }
            [HarmonyPatch(typeof(LevelRush), "IsLevelRush")]
            [HarmonyPostfix]
            public static void ItsMikeyTimeBaby(ref bool __result)
            {
                if (_active)
                {
                    __result = true;
                }
            }
        }
    }
}
