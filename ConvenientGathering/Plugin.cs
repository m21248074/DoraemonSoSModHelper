using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace ConvenientGathering
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, "1.0.0.0")]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dev.cavey.plugins.convenientgathering";

        public const string PLUGIN_NAME = "Convenient Gathering";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> EnableFixedHoleIndex;

        private void Awake()
        {
            Log = base.Logger;

            EnableFixedHoleIndex = Config.Bind("Mine.Toggles", "EnableFixedHoleIndex", true, "Enable fixed stairs/pit hole locations\n是否啟用固定樓梯/落穴位置");

            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(MapModel))]
        public static class MapModel_Patch
        {
            [HarmonyPatch("DecideHoleIndexByRandom")]
            [HarmonyPrefix]
            public static bool DecideHoleIndexByRandom_Prefix(MapModel __instance, int upstairs_hole_index, ref int ___mHoleIndex)
            {
                if (!Plugin.EnableFixedHoleIndex.Value)
                    return true;

                if (__instance.IsArrivedBottomFloor && __instance.Master.LowerMapId == -1)
                {
                    ___mHoleIndex = -1;
                    return false;
                }
                int targetIndex = (upstairs_hole_index == 0) ? 1 : 0;
                ___mHoleIndex = targetIndex;
                return false;
            }
        }
    }
}
