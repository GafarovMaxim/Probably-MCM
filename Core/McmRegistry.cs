using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ProbablyMCM.Core
{
    public static class McmRegistry
    {
        private static readonly Dictionary<string, McmModData> mods = new Dictionary<string, McmModData>(StringComparer.OrdinalIgnoreCase);

        public static event Action OnRegistryUpdated;

        public static McmModData GetOrCreate(string modId, string displayName = null)
        {
            if (string.IsNullOrEmpty(modId)) return null;

            if (!mods.TryGetValue(modId, out var data))
            {
                data = new McmModData
                {
                    ModId = modId,
                    DisplayName = displayName ?? modId
                };

                // Try to fill metadata from LoadedMod if available
                FillModMetadata(data);
                data.Load();
                mods[modId] = data;
                OnRegistryUpdated?.Invoke();
            }
            else if (!string.IsNullOrEmpty(displayName) && data.DisplayName == modId)
            {
                data.DisplayName = displayName;
            }

            return data;
        }

        public static McmModData Find(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return null;
            mods.TryGetValue(modId, out var data);
            return data;
        }

        public static List<McmModData> GetAllMods()
        {
            SyncWithVanillaMods();
            return mods.Values.OrderBy(m => m.DisplayName).ToList();
        }

        private static void FillModMetadata(McmModData data)
        {
            if (ModLoader.Mods == null) return;

            LoadedMod loaded = ModLoader.Mods.FirstOrDefault(m => m.Manifest != null && string.Equals(m.Manifest.ID, data.ModId, StringComparison.OrdinalIgnoreCase));
            if (loaded?.Manifest != null)
            {
                if (string.IsNullOrEmpty(data.DisplayName) || data.DisplayName == data.ModId)
                    data.DisplayName = loaded.Manifest.Name ?? data.ModId;

                data.Author = loaded.Manifest.Author ?? "Unknown";
                data.Version = loaded.Manifest.ModVersion ?? "1.0.0";
                data.Description = loaded.Manifest.Description ?? "";
            }
        }

        public static void SyncWithVanillaMods()
        {
            if (ModLoader.Mods == null) return;

            foreach (LoadedMod mod in ModLoader.Mods)
            {
                if (mod?.Manifest == null) continue;
                string modId = mod.Manifest.ID;
                if (string.IsNullOrEmpty(modId)) continue;

                // 1. Sync ModConfig entries
                ModConfig vanillaConfig = ModConfig.Find(modId);
                if (vanillaConfig != null && vanillaConfig.HasEntries)
                {
                    McmModData data = GetOrCreate(modId, mod.Manifest.Name);
                    FillModMetadata(data);

                    foreach (ConfigEntry vEntry in vanillaConfig.Entries)
                    {
                        if (data.Get(vEntry.Key) != null) continue; // Already added

                        McmConfigEntry entry = new McmConfigEntry
                        {
                            Key = vEntry.Key,
                            Section = string.IsNullOrEmpty(vEntry.Section) ? "Общие" : vEntry.Section,
                            LabelGetter = vEntry.Label,
                            TooltipGetter = vEntry.Tooltip,
                            ValueType = vEntry.ValueType,
                            DefaultValue = vEntry.BoxedDefault,
                            Value = vEntry.BoxedValue,
                            RequiresRestart = vEntry.RequiresRestart
                        };

                        if (vEntry.ValueType == typeof(bool))
                        {
                            entry.EntryType = McmEntryType.Toggle;
                        }
                        else if (vEntry.HasRange)
                        {
                            entry.EntryType = McmEntryType.Slider;
                            entry.Min = vEntry.Min;
                            entry.Max = vEntry.Max;
                            entry.WholeNumbers = vEntry.ValueType == typeof(int);
                        }
                        else if (vEntry.Choices != null && vEntry.Choices.Length > 0)
                        {
                            entry.EntryType = McmEntryType.Dropdown;
                            entry.Choices = vEntry.Choices;
                        }
                        else if (vEntry.ValueType == typeof(int))
                        {
                            entry.EntryType = McmEntryType.IntNumber;
                        }
                        else if (vEntry.ValueType == typeof(float) || vEntry.ValueType == typeof(double))
                        {
                            entry.EntryType = McmEntryType.FloatNumber;
                        }
                        else if (IsHotkeyEntry(vEntry))
                        {
                            entry.EntryType = McmEntryType.KeyBind;
                        }
                        else
                        {
                            entry.EntryType = McmEntryType.InputText;
                        }

                        // Sync changes back to vanilla ConfigEntry via reflection
                        entry.OnValueChanged += (newVal) =>
                        {
                            try
                            {
                                AccessTools.Method(vEntry.GetType(), "ApplyBoxed")?.Invoke(vEntry, new[] { newVal });
                                vanillaConfig.Save();
                                McmModData.NotifyModConfigApplied(modId);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[ProbablyMCM] Error applying value to vanilla ConfigEntry: {ex}");
                            }
                        };

                        data.AddEntry(entry);
                    }
                }
            }
        }

        private static bool IsHotkeyEntry(ConfigEntry vEntry)
        {
            if (vEntry == null) return false;
            if (vEntry.ValueType == typeof(KeyCode)) return true;
            if (vEntry.ValueType != typeof(string)) return false;

            string key = (vEntry.Key ?? "").ToLowerInvariant();
            string sec = (vEntry.Section ?? "").ToLowerInvariant();

            bool nameSuggestsHotkey = key.Contains("hotkey") || key.Contains("keybind") || key.Contains("bind") || key.Contains("shortcut")
                                   || sec.Contains("hotkey") || sec.Contains("keybind") || sec.Contains("bind") || sec.Contains("клавиш");

            string val = ConfigEntry.Format(vEntry.BoxedValue);
            if (!string.IsNullOrEmpty(val))
            {
                val = val.Trim();
                // Числа (целые или с плавающей точкой) никогда не считаем хоткеем
                if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    return false;

                if (val.Equals("None", StringComparison.OrdinalIgnoreCase)) return true;
                if (val.Contains("+")) return true;

                // Если строка соответствует названию клавиши (не числовому коду)
                if (Enum.TryParse<KeyCode>(val, true, out var code) && !int.TryParse(val, out _))
                {
                    return code != KeyCode.None;
                }
            }

            return nameSuggestsHotkey;
        }
    }
}
