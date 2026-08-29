using HarmonyLib;

namespace GeneralQoL
{
    [HarmonyPatch(typeof(TimeModel))]
    public static class TimeModel_Patch
    {
        [HarmonyPatch("Second", MethodType.Setter)]
        [HarmonyPrefix]
        public static void set_Second_Prefix(TimeModel __instance, ref float value)
        {
            float multiplier = Plugin.timeTickIntervalMultiplier.Value;

            if (multiplier <= 0f) return;

            float currentSecond = __instance.Second;

            float deltaSecond = value - currentSecond;

            if (deltaSecond > 0f)
                value = currentSecond + (deltaSecond / multiplier);
        }
    }
}
