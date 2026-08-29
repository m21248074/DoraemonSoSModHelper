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
            ___mAlphaBlendIntensity = 0f;
        }
    }
}
