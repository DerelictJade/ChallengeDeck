using ChallengeDeck.Modules;
using ChallengeDeck.Modules.Ghost;
using MelonLoader;
using System;

namespace ChallengeDeck
{
    public class ChallengeDeck : MelonMod
    {
        internal static Game Game { get; private set; }
        internal static new HarmonyLib.Harmony Harmony { get; private set; }
        public override void OnLateInitializeMelon()
        {
            Game = Singleton<Game>.Instance;
            Harmony = new HarmonyLib.Harmony("nz.DerelictJade.ChallengeDeck");

            Settings.Register(this);

            AlwaysGifts.Activate();
            MikeyMode.Activate();

            Game.OnInitializationComplete += () =>
            {
                CheckToggleAnticheat();
                CustomGhosts.Patch(Settings.UseCustomGhosts.Value);
                RecordLastRun.Patch(Settings.RecordLastRunAsGhost.Value);
                AlwaysGifts.Patch(Settings.AlwaysGifts.Value);
                DisplayDemonCount.Patch(Settings.DisplayDemonCount.Value);
                BoofMode.Patch(Settings.BoofMode.Value);
                MikeyMode.Patch(Settings.MikeyMode.Value);
                UnlockLevelGate.Patch(Settings.UnlockLevelGate.Value);
            };
        }
        public override void OnSceneWasLoaded(int buildindex, string sceneName)
        {
            if (sceneName.Equals("HUB_HEAVEN"))
                CheckToggleAnticheat(true);
        }
        private void CheckToggleAnticheat(bool canDisable = false)
        {
            bool triggerAnticheat = Settings.BoofMode.Value
                || Settings.MikeyMode.Value
                || Settings.UnlockLevelGate.Value;

            if (triggerAnticheat)
                NeonLite.Modules.Anticheat.Register(MelonAssembly);
            else if (canDisable)
                NeonLite.Modules.Anticheat.Unregister(MelonAssembly);
        }
        public static class Settings
        {
            public static MelonPreferences_Category Category;
            public static MelonPreferences_Entry<bool> AlwaysGifts;
            public static MelonPreferences_Entry<bool> DisplayDemonCount;
            public static MelonPreferences_Entry<bool> BoofMode;
            public static MelonPreferences_Entry<bool> MikeyMode;
            public static MelonPreferences_Entry<bool> UnlockLevelGate;

            public static MelonPreferences_Category GhostCategory;
            public static MelonPreferences_Entry<bool> UseCustomGhosts;
            public static MelonPreferences_Entry<string> CustomGhostName;
            public static MelonPreferences_Entry<bool> RecordLastRunAsGhost;
            public static void Register(ChallengeDeck modInstance)
            {
                Category = MelonPreferences.CreateCategory("Challenge Deck");
                GhostCategory = MelonPreferences.CreateCategory("Challenge Deck/Ghosts");

                // Modded-runs Related Settings
                AlwaysGifts = CreateSettingEntry(Modules.AlwaysGifts.Patch, "Gifts Always Spawn", false,
                    description: "Makes level gifts spawn even if they've already been collected. Also displays gift collection time.",
                    triggersAnticheat: false);

                DisplayDemonCount = CreateSettingEntry(Modules.DisplayDemonCount.Patch, "Demon Count Everywhere", false,
                    description: "Displays the demon count on every level, including Chapter 11 and sidequest levels.",
                    triggersAnticheat: false);

                BoofMode = CreateSettingEntry(Modules.BoofMode.Patch, "Boof Mode", false,
                    description: "Start every level with a book of life card!\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.",
                    triggersAnticheat: true);

                MikeyMode = CreateSettingEntry(Modules.MikeyMode.Patch, "Mikey Mode", false,
                    description: "Replaces all card pickups with Dominion cards as though you were playing Mikey Rush.\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.",
                    triggersAnticheat: true);

                UnlockLevelGate = CreateSettingEntry(Modules.UnlockLevelGate.Patch, "Unlock Level Gate Early", false,
                    description: "End level gates always unlocked, regardless of demon count.\nEnabling triggers anticheat. To disable anticheat, turn off all anticheat-related settings and return to the hub.",
                    triggersAnticheat: true);

                // Ghost Related Settings
                UseCustomGhosts = CreateSettingEntry(CustomGhosts.Patch, "Use Custom Ghosts", false,
                    description: "Enable to use and update a different ghost, identified by the name below!\nUseful for separating your IL ghost from your challenge run ghosts.\nEnabling will DISABLE NeonLite/Optimizations/Cache Ghosts.",
                    triggersAnticheat: false,
                    GhostCategory);

                CustomGhostName = CreateSettingEntry(CustomGhosts.GhostNameChanged, "Ghost Name", "mikeyghost",
                    description: "Name of the custom ghost to race against and update!\nOnly use letters, numbers, spaces, hyphens, or underscores. Do not use symbols like / \\ : * ? \" < > |.\nMay have to exit and re-enter level to take effect.",
                    triggersAnticheat: false,
                    GhostCategory);

                RecordLastRunAsGhost = CreateSettingEntry(RecordLastRun.Patch, "Record Last Run Ghost", false,
                    description: "Enable to always record your last run's ghost, for DNFs, gift% runs, etc.\nWill save to regular level ghost location as \"last.phant\".",
                    triggersAnticheat: false,
                    GhostCategory);


                MelonPreferences_Entry<T> CreateSettingEntry<T>(Action<T> patchCallback, string title, T defaultValue, string description = "", bool triggersAnticheat = false, MelonPreferences_Category category = null)
                {
                    if (category == null)
                    {
                        category = Settings.Category;
                    }

                    var entry = category.CreateEntry(title, defaultValue, description: description);
                    entry.OnEntryValueChanged.Subscribe((before, after) =>
                    {
                        if (triggersAnticheat && after is bool boolValue && boolValue)
                        {
                            modInstance.CheckToggleAnticheat();
                        }
                        patchCallback.Invoke(after);
                    });
                    
                    return entry;
                }
            }
        }
    }
}