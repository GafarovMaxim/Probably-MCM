using System;
using HarmonyLib;
using ProbablyMCM.Core;
using ProbablyMCM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace ProbablyMCM.Patches
{
    [HarmonyPatch(typeof(MainMenuUIController))]
    public static class MainMenuPatch
    {
        private const string BUTTON_NAME = "ProbablyMCM_MainMenuButton";

        [HarmonyPatch(nameof(MainMenuUIController.HandleLocaleDropdown))]
        [HarmonyPostfix]
        public static void PostfixHandleLocale(MainMenuUIController __instance)
        {
            EnsureButton(__instance);
        }

        [HarmonyPatch(nameof(MainMenuUIController.OnLocaleChanged))]
        [HarmonyPostfix]
        public static void PostfixLocaleChanged(MainMenuUIController __instance)
        {
            EnsureButton(__instance);
        }

        public static void EnsureButton(MainMenuUIController instance)
        {
            try
            {
                if (instance == null) return;

                GameObject creditBtn = instance.creditButton;
                GameObject sourceBtn = creditBtn != null ? creditBtn : instance.moddingButton;
                if (sourceBtn == null || sourceBtn.transform.parent == null) return;

                Transform parent = sourceBtn.transform.parent;
                string buttonText = McmLoc.Get("mcm_esc_button", "MCM-меню");

                // Check if button already exists
                Transform existing = parent.Find(BUTTON_NAME);
                if (existing != null)
                {
                    UpdateButton(existing.gameObject, buttonText);
                    existing.gameObject.SetActive(true);
                    return;
                }

                // Create clone
                GameObject newBtnObj = UnityEngine.Object.Instantiate(sourceBtn, parent);
                newBtnObj.name = BUTTON_NAME;

                // Position right below "Авторы" (creditButton)
                if (creditBtn != null)
                {
                    newBtnObj.transform.SetSiblingIndex(creditBtn.transform.GetSiblingIndex() + 1);
                }
                else
                {
                    newBtnObj.transform.SetSiblingIndex(sourceBtn.transform.GetSiblingIndex() + 1);
                }

                // Strip localization components so they don't overwrite text
                foreach (LocalizeStringEvent loc in newBtnObj.GetComponentsInChildren<LocalizeStringEvent>(true))
                {
                    UnityEngine.Object.Destroy(loc);
                }

                // Set text
                UpdateButton(newBtnObj, buttonText);

                // Setup click listener
                Button btn = newBtnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick = new Button.ButtonClickedEvent();
                    btn.onClick.AddListener(() =>
                    {
                        instance.OnAnyButtonClick();
                        ModSettingsWindow.Open();
                    });
                }

                newBtnObj.SetActive(true);
                Debug.Log($"[ProbablyMCM] Injected '{buttonText}' button into Main Menu under Authors.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Failed to inject MCM button into Main Menu: {ex}");
            }
        }

        private static void UpdateButton(GameObject btnObj, string text)
        {
            if (btnObj == null) return;

            foreach (LocalizeStringEvent loc in btnObj.GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                UnityEngine.Object.Destroy(loc);
            }

            TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = text;
            }
            else
            {
                Text oldText = btnObj.GetComponentInChildren<Text>(true);
                if (oldText != null) oldText.text = text;
            }
        }
    }
}
