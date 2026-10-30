using System;
using ProbablyMCM.Core;
using UnityEngine;

namespace ProbablyMCM.API
{
    public class ModSettings
    {
        private readonly McmModData data;
        private string currentSection = "Общие";

        private ModSettings(McmModData data)
        {
            this.data = data;
        }

        public static ModSettings For(string modId, string displayName = null)
        {
            if (string.IsNullOrEmpty(modId))
            {
                throw new ArgumentNullException(nameof(modId));
            }
            McmModData modData = McmRegistry.GetOrCreate(modId, displayName);
            return new ModSettings(modData);
        }

        public ModSettings Section(string sectionName)
        {
            if (!string.IsNullOrEmpty(sectionName))
            {
                currentSection = sectionName;
            }
            return this;
        }

        public McmConfigEntry AddToggle(string key, string label, bool defaultValue, string section = null, string tooltip = null, Action<bool> onChanged = null)
        {
            return AddToggle(key, () => label, defaultValue, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddToggle(string key, Func<string> labelGetter, bool defaultValue, string section = null, Func<string> tooltipGetter = null, Action<bool> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(bool), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.Toggle;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged((bool)val);
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddSlider(string key, string label, float defaultValue, float min, float max, float step = 0.1f, bool wholeNumbers = false, string section = null, string tooltip = null, Action<float> onChanged = null)
        {
            return AddSlider(key, () => label, defaultValue, min, max, step, wholeNumbers, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddSlider(string key, Func<string> labelGetter, float defaultValue, float min, float max, float step = 0.1f, bool wholeNumbers = false, string section = null, Func<string> tooltipGetter = null, Action<float> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(float), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.Slider;
            entry.Min = min;
            entry.Max = max;
            entry.Step = step;
            entry.WholeNumbers = wholeNumbers;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged(Convert.ToSingle(val));
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddInt(string key, string label, int defaultValue, int min = 0, int max = 100, string section = null, string tooltip = null, Action<int> onChanged = null)
        {
            return AddInt(key, () => label, defaultValue, min, max, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddInt(string key, Func<string> labelGetter, int defaultValue, int min = 0, int max = 100, string section = null, Func<string> tooltipGetter = null, Action<int> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(int), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.IntNumber;
            entry.Min = min;
            entry.Max = max;
            entry.WholeNumbers = true;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged(Convert.ToInt32(val));
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddFloat(string key, string label, float defaultValue, float min = 0f, float max = 100f, string section = null, string tooltip = null, Action<float> onChanged = null)
        {
            return AddFloat(key, () => label, defaultValue, min, max, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddFloat(string key, Func<string> labelGetter, float defaultValue, float min = 0f, float max = 100f, string section = null, Func<string> tooltipGetter = null, Action<float> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(float), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.FloatNumber;
            entry.Min = min;
            entry.Max = max;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged(Convert.ToSingle(val));
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddDropdown(string key, string label, string defaultValue, string[] options, string section = null, string tooltip = null, Action<string> onChanged = null)
        {
            return AddDropdown(key, () => label, defaultValue, options, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddDropdown(string key, Func<string> labelGetter, string defaultValue, string[] options, string section = null, Func<string> tooltipGetter = null, Action<string> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(string), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.Dropdown;
            entry.Choices = options ?? Array.Empty<string>();
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged(val?.ToString());
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddKeyBind(string key, string label, KeyCode defaultKey, string section = null, string tooltip = null, Action<KeyCode> onChanged = null)
        {
            return AddKeyBind(key, () => label, defaultKey, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddKeyBind(string key, Func<string> labelGetter, KeyCode defaultKey, string section = null, Func<string> tooltipGetter = null, Action<KeyCode> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(KeyCode), defaultKey, section, tooltipGetter);
            entry.EntryType = McmEntryType.KeyBind;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) =>
                {
                    if (val is KeyCode kc) onChanged(kc);
                };
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddInput(string key, string label, string defaultValue, string section = null, string tooltip = null, Action<string> onChanged = null)
        {
            return AddInput(key, () => label, defaultValue, section, tooltip != null ? () => tooltip : (Func<string>)null, onChanged);
        }

        public McmConfigEntry AddInput(string key, Func<string> labelGetter, string defaultValue, string section = null, Func<string> tooltipGetter = null, Action<string> onChanged = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(string), defaultValue, section, tooltipGetter);
            entry.EntryType = McmEntryType.InputText;
            if (onChanged != null)
            {
                entry.OnValueChanged += (val) => onChanged(val?.ToString());
            }
            data.AddEntry(entry);
            return entry;
        }

        public McmConfigEntry AddButton(string key, string label, Action onClick, string section = null, string tooltip = null)
        {
            return AddButton(key, () => label, onClick, section, tooltip != null ? () => tooltip : (Func<string>)null);
        }

        public McmConfigEntry AddButton(string key, Func<string> labelGetter, Action onClick, string section = null, Func<string> tooltipGetter = null)
        {
            var entry = CreateEntry(key, labelGetter, typeof(object), null, section, tooltipGetter);
            entry.EntryType = McmEntryType.ButtonAction;
            entry.ButtonAction = onClick;
            data.AddEntry(entry);
            return entry;
        }

        public T Get<T>(string key)
        {
            var entry = data.Get(key);
            if (entry == null || entry.Value == null) return default;

            if (entry.Value is T direct) return direct;
            return (T)Convert.ChangeType(entry.Value, typeof(T));
        }

        private McmConfigEntry CreateEntry(string key, Func<string> labelGetter, Type valueType, object defaultValue, string section, Func<string> tooltipGetter)
        {
            var existing = data.Get(key);
            if (existing != null)
            {
                existing.LabelGetter = labelGetter;
                existing.TooltipGetter = tooltipGetter;
                existing.Section = section ?? currentSection;
                return existing;
            }

            return new McmConfigEntry
            {
                Key = key,
                LabelGetter = labelGetter,
                TooltipGetter = tooltipGetter,
                Section = section ?? currentSection,
                ValueType = valueType,
                DefaultValue = defaultValue,
                Value = defaultValue
            };
        }
    }
}
