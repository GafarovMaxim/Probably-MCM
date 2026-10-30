using System;
using HarmonyLib;
using ProbablyMCM.API;
using ProbablyMCM.Core;
using UnityEngine;

namespace ProbablyMCM
{
    public class ProbablyMCMMod : IMod
    {
        public static ProbablyMCMMod Instance { get; private set; }
        public ModManifest Manifest { get; private set; }

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;

            Debug.Log($"[ProbablyMCM] Initializing ProbablyMCM v{manifest?.ModVersion ?? "1.0.0"}...");

            try
            {
                // Register settings for ProbablyMCM itself with dynamic localization
                var settings = ModSettings.For("probably_mcm", "Probably MCM");

                settings.Section(McmLoc.Get("mcm_section_ui", "Интерфейс"));
                settings.AddToggle(
                    "show_descriptions",
                    () => McmLoc.Get("mcm_opt_show_descriptions", "Показывать описание модов"),
                    true,
                    tooltipGetter: () => McmLoc.Get("mcm_opt_show_descriptions_tip", "Отображать подробное описание мода в шапке панели настроек")
                );
                settings.AddToggle(
                    "auto_save_notice",
                    () => McmLoc.Get("mcm_opt_auto_save_notice", "Уведомлять об автосохранении"),
                    false,
                    tooltipGetter: () => McmLoc.Get("mcm_opt_auto_save_notice_tip", "Показывать всплывающее сообщение при сохранении настроек")
                );

                settings.Section(McmLoc.Get("mcm_section_hotkeys", "Быстрый доступ"));
                settings.AddKeyBind(
                    "quick_open_key",
                    () => McmLoc.Get("mcm_opt_quick_open_key", "Дополнительная клавиша открытия"),
                    KeyCode.F10,
                    tooltipGetter: () => McmLoc.Get("mcm_opt_quick_open_key_tip", "Необязательная горячая клавиша для открытия меню настроек модов без меню Esc. Нажмите Backspace для отключения.")
                );

                // Create runner GameObject for global hotkeys & lifetime hooks
                GameObject runner = new GameObject("ProbablyMCM_Runner");
                UnityEngine.Object.DontDestroyOnLoad(runner);
                runner.AddComponent<McmRunner>();

                Debug.Log("[ProbablyMCM] Self-configuration registered.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error during Init: {ex}");
            }
        }

        public void OnEnable()
        {
            Debug.Log("[ProbablyMCM] Enabled.");
        }

        public void OnDisable()
        {
            Debug.Log("[ProbablyMCM] Disabled.");
        }
    }

    public class McmRunner : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                // If KeyBindDialog is open, let it handle Escape
                if (UI.KeyBindDialog.IsOpen)
                {
                    return;
                }

                // Handle Escape key to close MCM window
                if (UI.ModSettingsWindow.IsOpen)
                {
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        if (!StoreUIManager.IsTextFieldFocused())
                        {
                            UI.ModSettingsWindow.Close();
                            return;
                        }
                    }
                }

                // Optional hotkey support if user configured a key
                var mcm = McmRegistry.Find("probably_mcm");
                if (mcm != null)
                {
                    var keyEntry = mcm.Get("quick_open_key");
                    if (keyEntry != null)
                    {
                        KeyCode hotkey = keyEntry.GetKeyCode();
                        if (hotkey != KeyCode.None && Input.GetKeyDown(hotkey))
                        {
                            if (!UI.ModSettingsWindow.IsOpen)
                            {
                                UI.ModSettingsWindow.Open();
                            }
                            else
                            {
                                UI.ModSettingsWindow.Close();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Prevent unhandled exception in Unity frame loop
            }
        }
    }
}
