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
    [HarmonyPatch(typeof(EscapeUIManager), nameof(EscapeUIManager.OpenEscapeMenu))]
    public static class EscapeMenuPatch
    {
        private const string BUTTON_NAME = "ProbablyMCM_EscapeButton";

        [HarmonyPostfix]
        public static void Postfix(EscapeUIManager __instance)
        {
            try
            {
                if (__instance == null || __instance.menuPanel == null) return;

                string buttonText = McmLoc.Get("mcm_esc_button", "MCM-меню");

                // Check if button already exists in menuPanel
                Transform existing = __instance.menuPanel.transform.Find(BUTTON_NAME);
                if (existing != null)
                {
                    UpdateButtonText(existing.gameObject, buttonText);
                    existing.gameObject.SetActive(true);
                    return;
                }

                // Also check recursive in case menu has sub-panels
                Button existingBtn = null;
                foreach (Button b in __instance.menuPanel.GetComponentsInChildren<Button>(true))
                {
                    if (b.gameObject.name == BUTTON_NAME)
                    {
                        existingBtn = b;
                        break;
                    }
                }

                if (existingBtn != null)
                {
                    UpdateButtonText(existingBtn.gameObject, buttonText);
                    existingBtn.gameObject.SetActive(true);
                    return;
                }

                // Find the Option button to clone its style, font, dimensions and sounds
                Button targetButton = null;
                Button[] allButtons = __instance.menuPanel.GetComponentsInChildren<Button>(true);

                foreach (Button btn in allButtons)
                {
                    string goName = btn.gameObject.name.ToLowerInvariant();
                    if (goName.Contains("option") || goName.Contains("setting"))
                    {
                        targetButton = btn;
                        break;
                    }

                    TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        string txt = tmp.text.ToLowerInvariant();
                        if (txt.Contains("option") || txt.Contains("setting") || txt.Contains("настройк") || txt.Contains("设置"))
                        {
                            targetButton = btn;
                            break;
                        }
                    }
                }

                // Fallback to any button if option button not found by name
                if (targetButton == null && allButtons.Length > 0)
                {
                    targetButton = allButtons[0];
                }

                if (targetButton == null)
                {
                    Debug.LogWarning("[ProbablyMCM] Could not find a button in menuPanel to clone for MCM.");
                    return;
                }

                // Instantiate clone
                GameObject newBtnObj = UnityEngine.Object.Instantiate(targetButton.gameObject, targetButton.transform.parent);
                newBtnObj.name = BUTTON_NAME;
                newBtnObj.transform.SetSiblingIndex(targetButton.transform.GetSiblingIndex() + 1);

                // Strip any Unity Localization components so they don't overwrite our custom text
                foreach (LocalizeStringEvent loc in newBtnObj.GetComponentsInChildren<LocalizeStringEvent>(true))
                {
                    UnityEngine.Object.Destroy(loc);
                }

                // Set localized label
                UpdateButtonText(newBtnObj, buttonText);

                // Setup click listener
                Button newBtn = newBtnObj.GetComponent<Button>();
                if (newBtn != null)
                {
                    newBtn.onClick = new Button.ButtonClickedEvent();
                    newBtn.onClick.AddListener(() =>
                    {
                        __instance.CloseEscapeMenu();
                        __instance.OnAnyClick();
                        ModSettingsWindow.Open();
                    });
                }

                newBtnObj.SetActive(true);
                Debug.Log($"[ProbablyMCM] Injected '{buttonText}' button into Escape menu successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Failed to inject button into Escape menu: {ex}");
            }
        }

        private static void UpdateButtonText(GameObject btnObj, string text)
        {
            if (btnObj == null) return;

            // Strip localization components if still present
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
