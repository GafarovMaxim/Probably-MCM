using System;
using ProbablyMCM.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProbablyMCM.UI
{
    public class KeyBindCapture : MonoBehaviour
    {
        public static KeyBindCapture Instance { get; private set; }

        private McmConfigEntry activeEntry;
        private Action onComplete;
        private TMP_Text buttonText;
        private string originalText;

        public bool IsCapturing => activeEntry != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void StartCapture(McmConfigEntry entry, TMP_Text label, Action callback)
        {
            if (activeEntry != null)
            {
                StopCapture(cancelled: true);
            }

            activeEntry = entry;
            buttonText = label;
            onComplete = callback;

            if (buttonText != null)
            {
                originalText = buttonText.text;
                string prompt = McmLoc.Get("mcm_press_any_key", "Нажмите клавишу...");
                buttonText.text = $"<color=#fbbf24>[ {prompt} ]</color>";
            }
        }

        public void StopCapture(bool cancelled = false)
        {
            if (activeEntry == null) return;

            if (cancelled && buttonText != null && originalText != null)
            {
                buttonText.text = originalText;
            }

            activeEntry = null;
            buttonText = null;
            originalText = null;

            try
            {
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error in keybind completion callback: {ex}");
            }
            onComplete = null;
        }

        private void OnGUI()
        {
            if (activeEntry == null) return;

            Event e = Event.current;
            if (e == null) return;

            if (e.isKey && e.type == EventType.KeyDown)
            {
                e.Use();

                if (e.keyCode == KeyCode.Escape)
                {
                    // Escape cancels without changing
                    StopCapture(cancelled: true);
                }
                else if (e.keyCode == KeyCode.Backspace || e.keyCode == KeyCode.Delete)
                {
                    // Backspace/Delete clears the binding
                    activeEntry.SetValue(KeyCode.None);
                    StopCapture(cancelled: false);
                }
                else if (e.keyCode != KeyCode.None)
                {
                    activeEntry.SetValue(e.keyCode);
                    StopCapture(cancelled: false);
                }
            }
        }

        private void Update()
        {
            if (activeEntry == null) return;

            // Also check legacy Input for mouse cancel
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                // If clicked somewhere else, stop capturing
                // StopCapture(cancelled: true);
            }
        }

        private void OnDisable()
        {
            if (activeEntry != null)
            {
                StopCapture(cancelled: true);
            }
        }
    }
}
