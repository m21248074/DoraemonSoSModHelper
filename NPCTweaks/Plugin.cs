using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace NPCTweaks
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, "1.0.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dev.cavey.plugins.npctweaks";

        public const string PLUGIN_NAME = "NPC Tweaks";

        internal static new ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;

            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(NpcModel))]
        public static class NpcModel_Patch
        {
            [HarmonyPatch(nameof(NpcModel.ReceivePresent))]
            [HarmonyPostfix]
            public static void ReceivePresent_Postfix(NpcModel __instance)
            {
                Traverse.Create(__instance).Field("mIsReceivedPresent").SetValue(false);
            }
        }

        [HarmonyPatch(typeof(FloorController))]
        public static class FloorController_Patch
        {
            [HarmonyPatch("SetFurnitureCommands")]
            [HarmonyPostfix]
            public static void SetFurnitureCommands_Postfix(FurnitureController furniture_controller, FurnitureModel furniture_model, BeehiveController beehive_controller)
            {
                FurnitureMasterModel furnitureMasterModel = (furniture_model == null) ? null : furniture_model.Master;
                if (furnitureMasterModel != null && furnitureMasterModel.Id == 66000)
                {
                    List<ICommand> list = new List<ICommand>();
                    foreach (ICommand item in furniture_controller.Commands)
                    {
                        list.Add(item);
                    }
                    list.Add(new HarvestCommand());
                    furniture_controller.SetCommand(list.ToArray());
                }
            }
        }

        [HarmonyPatch(typeof(AnimalModel))]
        public static class AnimalModel_Patch
        {
            [HarmonyPatch("Snack")]
            [HarmonyPrefix]
            public static bool Snack_Prefix(AnimalModel __instance)
            {
                __instance.HasSnack = false;
                return true;
            }

            [HarmonyPatch("Brush")]
            [HarmonyPrefix]
            public static bool Brush_Prefix(AnimalModel __instance)
            {
                __instance.IsBrushed = false;
                return true;
            }
        }
    }
}
