using HarmonyLib;
using System.Reflection;

namespace ChallengeDeck.Modules
{
    internal class DisplayDemonCount
    {
        private static bool _patched = false;
        private static int _lastDemonCount = -1;
        static readonly MethodInfo ogudc = AccessTools.Method(typeof(PlayerUI), "UpdateDemonCounter");
        static readonly MethodInfo udcprefixmi = typeof(DisplayDemonCount).GetMethod(nameof(PreUpdateDemonCounter));
        static readonly HarmonyMethod udcprefix = new HarmonyMethod(udcprefixmi);
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
            ChallengeDeck.Harmony.Patch(ogudc, prefix: udcprefix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogudc, udcprefixmi);
        }
        public static bool PreUpdateDemonCounter(PlayerUI __instance, int count)
        {
            __instance.demonCounterHolder.SetActive(count > 0);

            if (__instance.demonCounterHolder.activeInHierarchy)
            {
                if (_lastDemonCount != count)
                {
                    __instance.demonCounterSpring.CurrentPos += __instance.demonCounterSpringForce;
                }

                __instance.demonCounterNumberText.text = count.ToString();
            }
            _lastDemonCount = count;
            return false;
        }
    }
}
