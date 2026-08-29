using HarmonyLib;

namespace GeneralQoL
{
    [HarmonyPatch(typeof(TopPostEffectController))]
    public static class TopPostEffectController_Patch
    {
        [HarmonyPatch("OnRenderImage")]
        [HarmonyPrefix]
        public static void OnRenderImage_Prefix(ref float ___mAlphaBlendIntensity)
        {
            if (!Plugin.HideScreenBorder.Value)
                return;
            ___mAlphaBlendIntensity = 0f;
        }
    }
}
