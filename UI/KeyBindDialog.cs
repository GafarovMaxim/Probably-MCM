using System;
using System.Collections.Generic;
using ProbablyMCM.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyMCM.UI
{
    public static class KeyBindDialog
    {
        private static CustomUIWindow activeDialog;
        private static McmModData currentMod;
        private static McmConfigEntry currentEntry;
        private static Action onCompleteCallback;

        private static string pendingComboString = "None";
        private static KeyCode pendingKeyCode = KeyCode.None;

        public static bool IsOpen => activeDialog != null && activeDialog.IsAlive;

        public static void Open(McmModData mod, McmConfigEntry entry, Action onComplete)
        {
            if (IsOpen)
            {
                Close();
            }

            currentMod = mod;
            currentEntry = entry;
            onCompleteCallback = onComplete;

            // Load initial state
            pendingComboString = entry.GetStringValue();
            pendingKeyCode = entry.GetKeyCode();

            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null) return;

            string title = McmLoc.Get("mcm_bind_dialog_title", "Настройка горячей клавиши");

            CustomUIBuilder builder = manager.CreateWindow(
                "mcm_keybind_dialog",
                title,
                "overlay"
            );

            builder.Handle.SetTitle(title);
            builder.SetCloseOnEscape(true);
            builder.SetSize(460f, 240f);
            builder.Center();
            builder.OnClose(() =>
            {
                activeDialog = null;
                currentMod = null;
                currentEntry = null;
                onCompleteCallback = null;
            });

            // Action Header
            builder.BeginRow(4f, TextAnchor.MiddleLeft);
            builder.AddHeader($"<b><size=15>{entry.GetLabel()}</size></b>", "bind_dlg_action");
            builder.AddFlexibleSpace();
            builder.AddLabel($"<color=#94a3b8>{mod.DisplayName}</color>", "bind_dlg_mod");
            builder.End();

            builder.AddSeparator(null, 1);

            // Preview Panel (Large box showing current combo)
            builder.BeginPanel(-1f, 44f, TextAnchor.MiddleCenter, 4f, 0f, "bind_preview_panel");
            builder.AddHeader(FormatComboDisplay(pendingComboString), "bind_combo_text");
            builder.End();

            // Instructions
            builder.AddSpace(3f);
            builder.AddLabel($"<color=#fbbf24>{McmLoc.Get("mcm_bind_hint", "Нажмите клавишу или комбинацию (Ctrl, Alt, Shift)...")}</color>", "bind_hint");
            builder.AddLabel($"<size=11><color=#94a3b8>{McmLoc.Get("mcm_bind_delete_hint", "Для очистки нажмите Delete. Для отмены нажмите Esc.")}</color></size>", "bind_del_hint");

            builder.AddSeparator(null, 1);

            // Footer buttons
            builder.BeginRow(8f, TextAnchor.MiddleRight);
            builder.AddButton(McmLoc.Get("mcm_bind_btn_clear", "Очистить"), OnClearClicked, "bind_btn_clear");
            builder.Last.SetFixedWidth(100f);
            builder.AddFlexibleSpace();

            builder.AddButton(McmLoc.Get("mcm_bind_btn_cancel", "Отмена"), OnCancelClicked, "bind_btn_cancel");
            builder.Last.SetFixedWidth(90f);

            builder.AddButton(McmLoc.Get("mcm_bind_btn_save", "Сохранить"), OnSaveClicked, "bind_btn_save");
            builder.Last.SetFixedWidth(100f);
            builder.End();

            activeDialog = builder.Show();

            // Attach interactive listener
            if (activeDialog?.root != null)
            {
                var capture = activeDialog.root.GetComponent<KeyBindDialogCapture>();
                if (capture == null)
                {
                    capture = activeDialog.root.AddComponent<KeyBindDialogCapture>();
                }
            }
        }

        public static void SetCombo(string comboStr, KeyCode primaryKey)
        {
            pendingComboString = comboStr;
            pendingKeyCode = primaryKey;

            var comboElement = activeDialog?.Get("bind_combo_text");
            if (comboElement != null)
            {
                comboElement.SetText(FormatComboDisplay(comboStr));
            }
        }

        public static void SaveAndClose()
        {
            if (currentEntry != null && currentMod != null)
            {
                if (currentEntry.ValueType == typeof(KeyCode) || currentEntry.Value is KeyCode)
                {
                    currentEntry.SetValue(pendingKeyCode);
                }
                else
                {
                    currentEntry.SetValue(pendingComboString);
                }

                currentMod.Save();
            }

            var callback = onCompleteCallback;
            Close();
            callback?.Invoke();
        }

        public static void Close()
        {
            if (activeDialog != null && activeDialog.IsAlive)
            {
                activeDialog.Close();
            }
            activeDialog = null;
        }

        private static void OnClearClicked()
        {
            SetCombo("None", KeyCode.None);
        }

        private static void OnCancelClicked()
        {
            Close();
        }

        private static void OnSaveClicked()
        {
            SaveAndClose();
        }

        private static string FormatComboDisplay(string combo)
        {
            if (string.IsNullOrEmpty(combo) || combo.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                string notBound = McmLoc.Get("mcm_key_not_bound", "Не назначена");
                return $"<size=20><color=#94a3b8>[ {notBound} ]</color></size>";
            }
            return $"<size=22><b><color=#38bdf8>[ {combo} ]</color></b></size>";
        }

        public static string FormatKeyName(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.Equals:
                case KeyCode.KeypadEquals:
                    return "Equals";
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    return "Minus";
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                    return "Plus";
                case KeyCode.Alpha0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                default:
                    return code.ToString();
            }
        }
    }

    public class KeyBindDialogCapture : MonoBehaviour
    {
        private void OnGUI()
        {
            if (!KeyBindDialog.IsOpen) return;

            Event e = Event.current;
            if (e == null) return;

            if (e.isKey && e.type == EventType.KeyDown)
            {
                e.Use();

                if (e.keyCode == KeyCode.Escape)
                {
                    KeyBindDialog.Close();
                    return;
                }

                // Delete clears the binding
                if (e.keyCode == KeyCode.Delete)
                {
                    KeyBindDialog.SetCombo("None", KeyCode.None);
                    return;
                }

                // Enter saves the binding
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    KeyBindDialog.SaveAndClose();
                    return;
                }

                // Ignore modifier keys by themselves
                if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl ||
                    e.keyCode == KeyCode.LeftAlt || e.keyCode == KeyCode.RightAlt ||
                    e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift ||
                    e.keyCode == KeyCode.None)
                {
                    return;
                }

                // Construct combination with held modifiers
                List<string> parts = new List<string>();
                if (e.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    parts.Add("Ctrl");
                }
                if (e.alt || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    parts.Add("Alt");
                }
                if (e.shift || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    parts.Add("Shift");
                }

                parts.Add(KeyBindDialog.FormatKeyName(e.keyCode));
                string combo = string.Join("+", parts);

                KeyBindDialog.SetCombo(combo, e.keyCode);
            }
        }
    }
}
