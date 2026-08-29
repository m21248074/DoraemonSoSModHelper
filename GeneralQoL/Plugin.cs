using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Define;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GeneralQoL
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, "1.0.0.0")]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dev.cavey.plugins.generalqol";

        public const string PLUGIN_NAME = "General QoL";

        internal static new ManualLogSource Log;

        public static ConfigEntry<float> timeTickIntervalMultiplier;
        public static ConfigEntry<bool> EnableMaxStack999;
        private static int originalMaxStack = 99;
        private static FieldInfo maxStackField;

        private void Awake()
        {
            Log = base.Logger;

            timeTickIntervalMultiplier = Config.Bind("General.Multipliers", "TimeTickIntervalMultiplier", 1.0f, "Tick 間隔倍率 (數字越大，時間流逝越慢。例如: 2.0 代表間隔加倍、時間變慢 2 倍)");
            EnableMaxStack999 = Config.Bind("General", "EnableMaxStack999", true, "是否啟用 999 最大堆疊數量 (可隨時開關)");

            maxStackField = typeof(Item).GetField("MAX_STACK", BindingFlags.Static | BindingFlags.Public);

            if (maxStackField != null)
            {
                EnableMaxStack999.SettingChanged += (sender, args) => UpdateMaxStack();
                UpdateMaxStack();
            }
            else
            {
                Logger.LogError("找不到 Item.MAX_STACK 欄位！");
            }

            var field = typeof(Item).GetField("MAX_STACK", BindingFlags.Static | BindingFlags.Public);
            if (field != null)
                field.SetValue(null, 999);

            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void UpdateMaxStack()
        {
            if (maxStackField == null) return;

            if (EnableMaxStack999.Value)
            {
                maxStackField.SetValue(null, 999);
                Logger.LogInfo($"已設定最大堆疊數為 999");
            }
            else
            {
                maxStackField.SetValue(null, originalMaxStack);
                Logger.LogInfo($"已還原最大堆疊數為原版預設值 {originalMaxStack}");
            }
        }

        [HarmonyPatch(typeof(InputManager))]
        public static class InputManager_Patch
        {
            private const float DOUBLE_CLICK_INTERVAL = 0.2f;
            private static float lastTime = 0f;
            private static bool lastIndexIsShowInventory = false;

            [HarmonyPatch("Update")]
            [HarmonyPostfix]
            public static void Update_Postfix()
            {
                return;
                var chestController = SingletonMonoBehaviour<ChestUIController>.Instance;

                if (chestController == null || !chestController.gameObject.activeInHierarchy)
                    return;

                if (!UnityEngine.Input.GetMouseButtonDown(0)) return;

                var traverse = Traverse.Create(chestController);
                var chestWindow = traverse.Field<ChestUIPartController>("mChestWindow").Value;
                var inventoryWindow = traverse.Field<InventoryUIPartController>("mInventoryWindow").Value;
                bool isShowInventory = traverse.Field<bool>("mIsShowInventory").Value;

                if (chestWindow == null || inventoryWindow == null) return;

                bool isDoubleClick = (UnityEngine.Time.time - lastTime) < DOUBLE_CLICK_INTERVAL;
                bool isSameWindow = (lastIndexIsShowInventory == isShowInventory);

                if (isDoubleClick && isSameWindow)
                {
                    lastTime = 0f;

                    if (isShowInventory)
                    {
                        if (inventoryWindow.CanSendItem && chestWindow.CanReceiveItem)
                        {
                            chestWindow.ReceiveItem(inventoryWindow.CurrentItem);
                            inventoryWindow.SendItem();
                            PlaySendSE(inventoryWindow.SendItemSE);
                        }
                    }
                    else
                    {
                        if (chestWindow.CanSendItem && inventoryWindow.CanReceiveItem)
                        {
                            inventoryWindow.ReceiveItem(chestWindow.CurrentItem);
                            chestWindow.SendItem();
                            PlaySendSE(inventoryWindow.SendItemSE);
                        }
                    }
                }
                else
                {
                    lastTime = UnityEngine.Time.time;
                    lastIndexIsShowInventory = isShowInventory;
                }
            }

            private static void PlaySendSE(object seEnum)
            {
                if (seEnum == null) return;
                int seId = (int)seEnum;
                SingletonMonoBehaviour<SoundManager>.Instance?.PlayOneShotSE(seId);
            }
        }
    }
}
