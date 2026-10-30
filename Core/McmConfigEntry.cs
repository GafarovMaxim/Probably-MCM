using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ProbablyMCM.Core
{
    public enum McmEntryType
    {
        Toggle,
        Slider,
        IntNumber,
        FloatNumber,
        Dropdown,
        KeyBind,
        InputText,
        ButtonAction
    }

    public class McmConfigEntry
    {
        public string Key { get; set; }
        public string Section { get; set; } = "Общие";
        public Func<string> LabelGetter { get; set; }
        public Func<string> TooltipGetter { get; set; }
        public McmEntryType EntryType { get; set; }
        public Type ValueType { get; set; }
        public bool RequiresRestart { get; set; }

        public object DefaultValue { get; set; }
        public object Value { get; set; }

        // Slider & Number ranges
        public float Min { get; set; }
        public float Max { get; set; } = 1f;
        public float Step { get; set; } = 0.1f;
        public bool WholeNumbers { get; set; }
        public Func<float, string> ValueFormatter { get; set; }

        // Dropdown choices
        public string[] Choices { get; set; }

        // Button action callback
        public Action ButtonAction { get; set; }

        // Callbacks
        public event Action<object> OnValueChanged;

        public string GetLabel()
        {
            try
            {
                if (LabelGetter != null) return LabelGetter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error getting label for '{Key}': {ex.Message}");
            }
            return Key;
        }

        public string GetTooltip()
        {
            try
            {
                if (TooltipGetter != null) return TooltipGetter();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error getting tooltip for '{Key}': {ex.Message}");
            }
            return null;
        }

        public void SetValue(object newValue, bool notify = true)
        {
            if (Equals(Value, newValue)) return;

            Value = newValue;
            if (notify)
            {
                try
                {
                    OnValueChanged?.Invoke(newValue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ProbablyMCM] Exception in OnValueChanged for '{Key}': {ex}");
                }
            }
        }

        public void ResetToDefault()
        {
            SetValue(DefaultValue, notify: true);
        }

        public bool GetBool() => Value is bool b && b;
        public float GetFloat() => Convert.ToSingle(Value, CultureInfo.InvariantCulture);
        public int GetInt() => Convert.ToInt32(Value, CultureInfo.InvariantCulture);
        public string GetString() => Value?.ToString() ?? "";
        public string GetStringValue()
        {
            if (Value == null) return "None";
            if (Value is KeyCode kc) return kc == KeyCode.None ? "None" : kc.ToString();
            string s = Value.ToString();
            return string.IsNullOrEmpty(s) ? "None" : s;
        }
        public KeyCode GetKeyCode()
        {
            if (Value is KeyCode kc) return kc;
            if (Value is string s && Enum.TryParse<KeyCode>(s, true, out var parsed)) return parsed;
            return KeyCode.None;
        }

        public string SerializeValue()
        {
            if (Value == null) return "";
            if (Value is KeyCode kc) return kc.ToString();
            if (Value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return Value.ToString();
        }

        public void DeserializeValue(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;

            try
            {
                if (ValueType == typeof(bool))
                {
                    if (bool.TryParse(raw, out var b)) Value = b;
                }
                else if (ValueType == typeof(int))
                {
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        if (Min < Max) i = Mathf.Clamp(i, (int)Min, (int)Max);
                        Value = i;
                    }
                }
                else if (ValueType == typeof(float))
                {
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        if (Min < Max) f = Mathf.Clamp(f, Min, Max);
                        Value = f;
                    }
                }
                else if (ValueType == typeof(KeyCode))
                {
                    if (Enum.TryParse<KeyCode>(raw, true, out var kc)) Value = kc;
                }
                else
                {
                    Value = raw;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProbablyMCM] Failed to parse value '{raw}' for '{Key}': {ex.Message}");
            }
        }
    }
}
