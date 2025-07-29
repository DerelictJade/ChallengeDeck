using HarmonyLib;
using System.Reflection;
using MelonLoader;
using System;

namespace ChallengeDeck.Modules.Ghost
{
    internal class RecordLastRun
    {
        private static bool _patched = false;

        // PlayLevel(LevelData, bool, bool)
        static readonly MethodInfo ogpl_LevelData = AccessTools.Method(typeof(Game), "PlayLevel", new Type[] { typeof(LevelData), typeof(bool), typeof(bool) });

        // PlayLevel(string, bool, Action)
        static readonly MethodInfo ogpl_String = AccessTools.Method(typeof(Game), "PlayLevel", new Type[] { typeof(string), typeof(bool), typeof(System.Action) });

        static readonly MethodInfo plprefixmi = typeof(RecordLastRun).GetMethod(nameof(ForceSaveLastRun));
        static readonly HarmonyMethod plprefix = new HarmonyMethod(plprefixmi);

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
            ChallengeDeck.Harmony.Patch(ogpl_LevelData, prefix: plprefix);
            ChallengeDeck.Harmony.Patch(ogpl_String, prefix: plprefix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogpl_LevelData, plprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogpl_String, plprefixmi);
        }

        public static bool ForceSaveLastRun()
        {
            if (ChallengeDeck.Settings.RecordLastRunAsGhost.Value)
            {
                try
                {
                    GhostCore.SaveCustomGhost("last.phant", true);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Failed to save last run ghost: {ex}");
                }

            }
            return true;
        }
    }
}
