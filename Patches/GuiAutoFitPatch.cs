using System;
using HarmonyLib;
using UnityEngine;

namespace ProbablyMCM.Patches
{
    /// <summary>
    /// Автоматическое масштабирование шрифта для IMGUI-кнопок модов (ShopAssist, SmartInventoryArrange и др.).
    /// Предотвращает вылезание длинного русского текста за границы кнопок.
    /// </summary>
    [HarmonyPatch(typeof(GUI), nameof(GUI.Button), new Type[] { typeof(Rect), typeof(GUIContent) })]
    public static class GuiButtonAutoFitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Rect position, GUIContent content, ref int __state)
        {
            __state = -1;
            try
            {
                if (content == null || string.IsNullOrEmpty(content.text)) return;
                if (position.width <= 24f) return;

                GUIStyle style = GUI.skin.button;
                if (style == null) return;

                Vector2 neededSize = style.CalcSize(content);
                if (neededSize.x > position.width)
                {
                    int baseSize = style.fontSize;
                    if (baseSize <= 0) baseSize = 13; // default IMGUI font size

                    __state = style.fontSize;
                    float ratio = (position.width - 6f) / neededSize.x;
                    int scaledSize = Mathf.Clamp(Mathf.RoundToInt(baseSize * ratio), 8, baseSize);
                    style.fontSize = scaledSize;
                }
            }
            catch
            {
                // Safety: never break UI rendering
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ref int __state)
        {
            try
            {
                if (__state != -1 && GUI.skin != null && GUI.skin.button != null)
                {
                    GUI.skin.button.fontSize = __state;
                }
            }
            catch
            {
                // Safety
            }
        }
    }
}
