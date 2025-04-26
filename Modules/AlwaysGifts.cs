using HarmonyLib;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ChallengeDeck.Modules
{
    internal class AlwaysGifts
    {
        private static bool _patched = false;
        static readonly MethodInfo ogdcp = AccessTools.Method(typeof(MechController), "DoCardPickup");
        static readonly MethodInfo dcpprefixmi = typeof(AlwaysGifts).GetMethod(nameof(PostDoCardPickup));
        static readonly HarmonyMethod dcpprefix = new HarmonyMethod(dcpprefixmi);

        private static bool _collectedGift = false;
        private static long _lastGiftTime = 0;
        static TextMeshProUGUI textMesh;
        static GameObject textObject;
        public static void Activate()
        {
            ChallengeDeck.Harmony.PatchAll(typeof(HasCollectibleBeenFoundPatch));
        }
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
            ChallengeDeck.Harmony.Patch(ogdcp, prefix: dcpprefix);
            ChallengeDeck.Game.OnLevelLoadComplete += OnLevelLoadComplete;
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogdcp, dcpprefixmi);
            ChallengeDeck.Game.OnLevelLoadComplete -= OnLevelLoadComplete;

            _collectedGift = false;
            if (textObject)
                textObject.SetActive(false);
        }
        private static void OnLevelLoadComplete()
        {
            _collectedGift = false;
            if (textObject)
                textObject.SetActive(false);
        }
        public static void PostDoCardPickup(PlayerCardData card)
        {
            if (_collectedGift) return; // Stops this from running twice
            if (card && card.consumableType == PlayerCardData.ConsumableType.GiftCollectible)
            {
                _collectedGift = true;
                ShowGiftTime();
            }
        }
        private static void ShowGiftTime()
        {
            if (!textObject)
                CreateTextObject();

            var playerUI = Object.FindObjectOfType<PlayerUI>();
            if (!playerUI) return;

            _lastGiftTime = ChallengeDeck.Game.GetCurrentLevelTimerMicroseconds();
            string timerText = NeonLite.Helpers.FormatTime(_lastGiftTime / 1000, null, '.');

            textMesh.text = timerText;
            textObject.SetActive(true);
        }
        private static void CreateTextObject()
        {
            // Create the text object and attach it to the canvas
            if (!MainMenu.Instance()) return;
            Canvas canvas = MainMenu.Instance().GetComponentInChildren<Canvas>();
            if (!canvas) return;
            textObject = new GameObject("GiftTimeText");
            textObject.transform.SetParent(canvas.transform, false);

            // Add the TextMeshProUGUI component and format the text
            textMesh = textObject.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = 28;
            textMesh.color = Color.cyan;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.outlineColor = new Color32(0, 0, 0, 255);
            textMesh.outlineWidth = 0.16f;

            // Set position and size
            RectTransform rectTransform = textMesh.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200f, 50f);

            textObject.SetActive(false);
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
