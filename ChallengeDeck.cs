using ChallengeDeck.Modules;
using MelonLoader;
using System;

namespace ChallengeDeck
{
    public class ChallengeDeck : MelonMod
    {
        internal static Game Game { get; private set; }
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        private static bool _criticalErrorOccurred  = false;
        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            Harmony = new HarmonyLib.Harmony("nz.DerelictJade.ChallengeDeck");

            Settings.Register();

            CheckToggleAnticheat();

            ApplyAllPatches();
            AlwaysGifts.Activate();
            MikeyMode.Activate();
            CustomGhosts.ValidateGhostName();
        }
        private void ApplyAllPatches()
        {
            ApplyPatch(CustomGhosts.Patch, Settings.UseCustomGhosts.Value, false);
            ApplyPatch(AlwaysGifts.Patch, Settings.AlwaysGifts.Value, false);
            ApplyPatch(DisplayDemonCount.Patch, Settings.DisplayDemonCount.Value, false);
            ApplyPatch(BoofMode.Patch, Settings.BoofMode.Value);
            ApplyPatch(MikeyMode.Patch, Settings.MikeyMode.Value);
            ApplyPatch(UnlockLevelGate.Patch, Settings.UnlockLevelGate.Value);
        }
        private void ApplyPatch(Action<bool> patch, bool apply, bool serious=true)
        {
            try
            {
                patch(apply);
            }
            catch (Exception ex)
            {
                _criticalErrorOccurred  = _criticalErrorOccurred  | serious;
                CheckToggleAnticheat();
                MelonLogger.Error($"[{patch.Method.DeclaringType.FullName}] failed with value `{apply}`.\nException: {ex}");
            }
        }
        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            if (sceneName.Equals("HUB_HEAVEN"))
                CheckToggleAnticheat(true);
        }
        public override void OnPreferencesSaved()
        {
            CheckToggleAnticheat();
            ApplyAllPatches();
            CustomGhosts.ValidateGhostName();
        }
        private void CheckToggleAnticheat(bool canDisable = false)
        {
            bool triggerAnticheat = Settings.BoofMode.Value
                || Settings.MikeyMode.Value
                || Settings.UnlockLevelGate.Value;

            if (triggerAnticheat | _criticalErrorOccurred )
                NeonLite.Modules.Anticheat.Register(MelonAssembly);
            else if (canDisable)
                NeonLite.Modules.Anticheat.Unregister(MelonAssembly);
        }
        public static class Settings
        {
            public static MelonPreferences_Category Category;
            public static MelonPreferences_Entry<bool> UseCustomGhosts;
            public static MelonPreferences_Entry<string> CustomGhostName;
            public static MelonPreferences_Entry<bool> AlwaysGifts;
            public static MelonPreferences_Entry<bool> DisplayDemonCount;
            public static MelonPreferences_Entry<bool> BoofMode;
            public static MelonPreferences_Entry<bool> MikeyMode;
            public static MelonPreferences_Entry<bool> UnlockLevelGate;

            public static void Register()
            {
                Category = MelonPreferences.CreateCategory("Challenge Deck");
                UseCustomGhosts = Category.CreateEntry("Use Custom Ghosts", false, description: "Enable to use and update a different ghost, identified by the name below!\nUseful for separating your IL ghost from your challenge run ghosts.\nDISABLE NeonLite/Optimizations/Cache Ghosts to work as expected.");
                CustomGhostName = Category.CreateEntry("Ghost Name", "mikeyghost", description: "Name of the custom ghost to race against and update!\nOnly use letters, numbers, spaces, hyphens, or underscores. Do not use symbols like / \\ : * ? \" < > |.\nMay have to exit and re-enter level to take effect.");
                AlwaysGifts = Category.CreateEntry("Gifts Always Spawn", false, description: "Makes level gifts spawn even if they've already been collected. Also displays gift collection time.");
                DisplayDemonCount = Category.CreateEntry("Demon Count Everywhere", false, description: "Displays the demon count on every level, including Chapter 11 and sidequest levels.");
                BoofMode = Category.CreateEntry("Boof Mode", false, description: "Start every level with a book of life card!\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.");
                MikeyMode = Category.CreateEntry("Mikey Mode", false, description: "Replaces all card pickups with Dominion cards as though you were playing Mikey Rush.\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.");
                UnlockLevelGate = Category.CreateEntry("Unlock Level Gate Early", false, description: "End level gates always unlocked, regardless of demon count.\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.");
            }
        }
    }
}