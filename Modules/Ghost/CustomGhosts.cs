using HarmonyLib;
using MelonLoader;
using System.IO;
using System.Linq;
using System.Reflection;


namespace ChallengeDeck.Modules.Ghost
{
    internal class CustomGhosts
    {
        private static bool _patched = false;

        // Original ghost save
        static readonly MethodInfo ogsc = AccessTools.Method(typeof(GhostRecorder), "SaveCompressed");
        static readonly MethodInfo scprefixmi = typeof(CustomGhosts).GetMethod(nameof(PreSaveCompressed));
        static readonly HarmonyMethod scprefix = new HarmonyMethod(scprefixmi);

        // SaveLevelData postfix method
        static readonly MethodInfo ogsld = AccessTools.Method(typeof(GhostRecorder), "SaveLevelData");
        static readonly MethodInfo sldpostfixmi = typeof(CustomGhosts).GetMethod(nameof(PostSaveLevelData));
        static readonly HarmonyMethod sldpostfix = new HarmonyMethod(sldpostfixmi);

        // LoadLevelDataCompressed prefix, to overwrite ghost loading
        static readonly MethodInfo oglldc = AccessTools.Method(typeof(GhostUtils), "LoadLevelDataCompressed");
        static readonly MethodInfo lldcprefixmi = typeof(GhostCore).GetMethod(nameof(GhostCore.LoadCustomGhost));
        static readonly HarmonyMethod lldcprefix = new HarmonyMethod(lldcprefixmi);

        public static void Patch(bool apply)
        {
            if (_patched == apply)
                return;
            if (apply)
                DoPatch();
            else
                UndoPatch();
            ValidateGhostName();
            _patched = apply;
        }
        public static void GhostNameChanged(string newGhostName)
        {
            ValidateGhostName();
        }
        private static void DoPatch()
        {
            NeonLite.Settings.Find<bool>("NeonLite", "Optimization", "cacheGhosts").Value = false;
            ChallengeDeck.Harmony.Patch(ogsc, prefix: scprefix);
            ChallengeDeck.Harmony.Patch(ogsld, postfix: sldpostfix);
            ChallengeDeck.Harmony.Patch(oglldc, prefix: lldcprefix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogsc, scprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogsld, sldpostfixmi);
            ChallengeDeck.Harmony.Unpatch(oglldc, lldcprefixmi);
        }
        public static bool PreSaveCompressed() => false; // Prevents regular ghost saving
        public static void PostSaveLevelData()
        {
            GhostCore.SaveCustomGhost();
        }
        private static void ValidateGhostName()
        {
            string name = ChallengeDeck.Settings.CustomGhostName.Value;
            if (string.IsNullOrEmpty(name))
                name = "default";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(name
                .Trim()
                .Select(c => invalidChars.Contains(c) ? '_' : c)
                .ToArray());
            name = string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
            ChallengeDeck.Settings.CustomGhostName.Value = name;
        }
    }
}
