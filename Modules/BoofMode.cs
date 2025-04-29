using HarmonyLib;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace ChallengeDeck.Modules
{
    internal class BoofMode
    {
        private static bool _patched = false;
        static readonly MethodInfo ogosv = AccessTools.Method(typeof(MenuScreenLevelRushComplete), "OnSetVisible");
        static readonly MethodInfo osvpostfixmi = typeof(BoofMode).GetMethod(nameof(PostOnSetVisible));
        static readonly HarmonyMethod osvpostfix = new HarmonyMethod(osvpostfixmi);

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
            ChallengeDeck.Harmony.Patch(ogosv, postfix: osvpostfix);
            ChallengeDeck.Game.OnLevelLoadComplete += OnLevelLoadComplete;
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogosv, osvpostfixmi);
            ChallengeDeck.Game.OnLevelLoadComplete -= OnLevelLoadComplete;
        }
        private static void OnLevelLoadComplete()
        {
            if (SceneManager.GetActiveScene().name.Equals("Heaven_Environment")) {
                return;
            }

            GS.AddCard("RAPTURE");
            LevelData currentLevel = ChallengeDeck.Game.GetCurrentLevel();
            foreach (DiscardLockData discardLockData in currentLevel.discardLockData)
                for (int i = 0; i < discardLockData.cards.Count; i++)
                    if (discardLockData.cards[i].discardAbility == PlayerCardData.DiscardAbility.Telefrag)
                        discardLockData.cards.RemoveAt(i);
        }
        public static void PostOnSetVisible(ref MenuScreenLevelRushComplete __instance)
        {
            LevelRush.LevelRushType currentRushType = LevelRush.GetCurrentLevelRushType();
            string text;

            if (currentRushType == LevelRush.LevelRushType.WhiteRush) text = "White's";
            else if (currentRushType == LevelRush.LevelRushType.MikeyRush) text = "Mikey's";
            else if (currentRushType == LevelRush.LevelRushType.RedRush) text = "Red's";
            else if (currentRushType == LevelRush.LevelRushType.VioletRush) text = "Violet's";
            else if (currentRushType == LevelRush.LevelRushType.YellowRush) text = "Yellow's";
            else return; // Error, abort mission

            text += (LevelRush.IsHellRush() ? " Hell " : " Heaven ") + "Boof Rush";
            __instance._rushName.textMeshProUGUI.text = text;
        }
    }
}
