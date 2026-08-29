using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace StaminaTweaks
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, "1.0.0.0")]
    public class Plugin: BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "dev.cavey.plugins.staminatweaks";

        public const string PLUGIN_NAME = "Stamina Tweaks";

        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> isShowStamina;
        public static ConfigEntry<string> showStaminaPrefixText;
        public static ConfigEntry<float> staminaConsumeMult;
        public static ConfigEntry<bool> ignoreLateSleepStaminaPenalty;
        public static ConfigEntry<bool> enableBetterNap;

        private void Awake()
        {
            Log = base.Logger;

            isShowStamina = Config.Bind("General.Toggles", "IsShowStamina", true, "是否顯示體力");
            showStaminaPrefixText = Config.Bind("General.Strings", "ShowStaminaPrefixText", "Stamina: ", "顯示體力的前綴文字");
            staminaConsumeMult = Config.Bind("General.Multipliers", "StaminaConsumeMult", 1.0f, "體力消耗倍數");
            ignoreLateSleepStaminaPenalty = Config.Bind("General.Toggles", "IgnoreLateSleepStaminaPenalty", true, "是否忽略晚睡體力懲罰");
            enableBetterNap = Config.Bind("General.Toggles", "EnableBetterNap", true, "是否啟用更好的午睡 (每小時10體力+背包等級*20)");

            var harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(CurrentTimeUIPartController))]
    public static class CurrentTimeUIPartController_Patch
    {
        public static CurrentTimeUIPartController instance = null;
        public static Text StaminaText = null;

        [HarmonyPatch("UpdateText")]
        [HarmonyPostfix]
        public static void UpdateText_Postfix(CurrentTimeUIPartController __instance, Text ___mText)
        {
            //instance = __instance;

            Transform staminaTransform = ___mText.transform.parent.Find("StaminaText");

            if (!Plugin.isShowStamina.Value)
            {
                if (staminaTransform != null)
                    staminaTransform.gameObject.SetActive(false);
                return;
            }

            Text staminaText;
            if (staminaTransform == null) {
                staminaText = Object.Instantiate<Text>(___mText, ___mText.transform.parent);
                staminaText.name = "StaminaText";
                staminaText.transform.position = ___mText.transform.position;
                staminaText.transform.localScale = ___mText.transform.localScale;
                float offset = ___mText.preferredWidth * 1.0f + 20f;
                staminaText.transform.localPosition = ___mText.transform.localPosition + new Vector3(offset, 0, 0);
            }
            else
                staminaText = staminaTransform.GetComponent<Text>();

            var stamina = SingletonMonoBehaviour<UserManager>.Instance.User.Player.Stamina;
            int stamina_now = stamina.Now;
            int stamina_max = stamina.Max;
            staminaText.gameObject.SetActive(true);
            staminaText.text = $"{Plugin.showStaminaPrefixText.Value}{stamina_now}/{stamina_max}";
            staminaText.color = (stamina_now >= 10) ? Color.white : Color.red;
        }
    }

    [HarmonyPatch(typeof(StaminaModel))]
    public static class StaminaModel_Patch
    {
        [HarmonyPatch("Consume")]
        [HarmonyPrefix]
        public static void Consume_Prefix(ref int value)
        {
            value = Mathf.RoundToInt(value * Plugin.staminaConsumeMult.Value);
        }

        //[HarmonyPatch("Consume")]
        //[HarmonyPostfix]
        //public static void Consume_Postfix()
        //{
        //    if (CurrentTimeUIPartController_Patch.instance != null && Plugin.isShowStamina.Value)
        //    {
        //        CurrentTimeUIPartController_Patch.instance.UpdateText();
        //    }
        //}

        //[HarmonyPatch("Recover")]
        //[HarmonyPostfix]
        //public static void Recover_Postfix()
        //{
        //    if (CurrentTimeUIPartController_Patch.instance != null && Plugin.isShowStamina.Value)
        //    {
        //        CurrentTimeUIPartController_Patch.instance.UpdateText();
        //    }
        //}

        //[HarmonyPatch("RecoverFully")]
        //[HarmonyPostfix]
        //public static void RecoverFully_Postfix()
        //{
        //    if (CurrentTimeUIPartController_Patch.instance != null && Plugin.isShowStamina.Value)
        //    {
        //        CurrentTimeUIPartController_Patch.instance.UpdateText();
        //    }
        //}

        [HarmonyPatch("RecoverWithSleep")]
        [HarmonyPrefix]
        public static bool RecoverWithSleep_Prefix(StaminaModel __instance)
        {
            if (Plugin.ignoreLateSleepStaminaPenalty.Value)
            {
                __instance.RecoverFully();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FarmSleepState))]
    public static class FarmSleepState_Patch
    {
        [HarmonyPatch("AdvanceNapTime")]
        [HarmonyPrefix]
        public static bool AdvanceNapTime_Prefix()
        {
            if (Plugin.enableBetterNap.Value)
            {
                SingletonMonoBehaviour<UserManager>.Instance.User.Player.Stamina.Recover((SingletonMonoBehaviour<UserManager>.Instance.User.Inventory.Level - 1) * 20);
            }
            return true;
        }
    }
}
