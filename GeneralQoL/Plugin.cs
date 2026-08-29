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
        public static ConfigEntry<bool> HideScreenBorder;
        public static ConfigEntry<bool> ImproveDiagonalMovement;
        public static ConfigEntry<float> DoubleTapInterval;
        public static ConfigEntry<float> DiagonalReleaseBufferTime;

        private static int originalMaxStack = 99;
        private static FieldInfo maxStackField;

        private void Awake()
        {
            Log = base.Logger;

            timeTickIntervalMultiplier = Config.Bind("Time.Multipliers", "TimeTickIntervalMultiplier", 1.0f, "Time tick interval multiplier (Higher value = slower time. e.g. 2.0 doubles interval, time flows 2x slower)\nTick 間隔倍率 (數字越大，時間流逝越慢。例如: 2.0 代表間隔加倍、時間變慢 2 倍)");
            EnableMaxStack999 = Config.Bind("General.Toggles", "EnableMaxStack999", true, "Enable max stack size limit of 999\n是否啟用 999 最大堆疊數量");
            HideScreenBorder = Config.Bind("General.Toggles", "HideScreenBorder", true, "Hide screen edge white border\n隱藏螢幕四周的白邊");
            ImproveDiagonalMovement = Config.Bind("Movement.Toggles", "ImproveDiagonalMovement", true, "Improve diagonal movement (Prevent drifting / Lock to farm grid)\n改善斜走效果 (防飄移/鎖定農場網格線)");
            DoubleTapInterval = Config.Bind("Movement.Settings", "DoubleTapInterval", 0.2f, "Double-tap interval to toggle movement mode (seconds)\n雙擊 Alt 切換移動模式的判定時間 (秒)");
            DiagonalReleaseBufferTime = Config.Bind("Movement.Settings", "DiagonalReleaseBufferTime", 0.05f, "Diagonal movement release tolerance time (seconds, prevents unwanted directional turning when releasing keys asynchronously)\n斜走鬆開按鍵容錯時間 (秒，避免一先一後放開時角色突兀轉向)");

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
                Logger.LogInfo($"Set max stack size to 999 / 已設定最大堆疊數為 999");
            }
            else
            {
                maxStackField.SetValue(null, originalMaxStack);
                Logger.LogInfo($"Restored max stack size to default ({originalMaxStack}) / 已還原最大堆疊數為原版預設值 {originalMaxStack}");
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
