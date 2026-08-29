using Define;
using HarmonyLib;
using UnityEngine;

namespace GeneralQoL
{
    [HarmonyPatch(typeof(InputManager))]
    public static class InputManager_Patch
    {
        public static float lastTime = 0f;
        public static bool isChangeDirection = false;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix()
        {
            if (!UnityEngine.Input.anyKeyDown) return;

            var keySettings = SingletonMonoBehaviour<UserManager>.Instance?.Option?.KeySettings;
            if (keySettings == null || keySettings.Length <= 15) return;

            KeyCode targetKey = keySettings[15].KeyboardKeyCode;

            if (UnityEngine.Input.GetKeyDown(targetKey))
            {
                if (UnityEngine.Time.time - lastTime < 0.2f)
                {
                    isChangeDirection = !isChangeDirection;

                    var farmTopUI = SingletonMonoBehaviour<UIManager>.Instance?.GetUIController(UI.TypeEnum.FarmTop) as FarmTopUIController;
                    if (farmTopUI != null)
                    {
                        string msg = isChangeDirection ? "切換移動方向為斜向" : "切換移動方向為正向";
                        farmTopUI.AddLogRequest(msg, -1);
                    }

                    lastTime = 0f;
                }
                else
                {
                    lastTime = UnityEngine.Time.time;
                }
            }
        }

        public static bool isInclinedWalk = false;
        public static float lastTime2;
        public static Vector2 mLStickInputRecord = Vector2.zero;

        [HarmonyPatch("UpdateInputs")]
        [HarmonyPostfix]
        public static void UpdateInputs_Postfix(ref Vector2 ___mLStickInput)
        {
            if (isChangeDirection && ___mLStickInput != Vector2.zero)
            {
                if (___mLStickInput.x == 0f || ___mLStickInput.y == 0f)
                {
                    Vector2 newVector = Vector2.zero;

                    if (___mLStickInput.y > 0f) newVector += new Vector2(1f, 1f);
                    else if (___mLStickInput.y < 0f) newVector += new Vector2(-1f, -1f);

                    if (___mLStickInput.x < 0f) newVector += new Vector2(-1f, 1f);
                    else if (___mLStickInput.x > 0f) newVector += new Vector2(1f, -1f);

                    ___mLStickInput = newVector.normalized;
                }
            }

            bool isDiagonalInput = ___mLStickInput.x != 0f && ___mLStickInput.y != 0f;
            bool isAnyInputActive = ___mLStickInput != Vector2.zero;

            if (isDiagonalInput)
            {
                isInclinedWalk = true;
                lastTime2 = UnityEngine.Time.time;
                mLStickInputRecord = ___mLStickInput;
            }
            else if (isInclinedWalk)
            {
                if (!isAnyInputActive)
                {
                    isInclinedWalk = false;
                    mLStickInputRecord = Vector2.zero;
                }
                else if (UnityEngine.Time.time - lastTime2 < 2f)
                {
                    ___mLStickInput = mLStickInputRecord;
                }
                else
                {
                    isInclinedWalk = false;
                    mLStickInputRecord = Vector2.zero;
                }
            }

            ___mLStickInput.y *= 1.15f;
        }
    }
}
