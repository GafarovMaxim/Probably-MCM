using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ProbablyMCM.Core
{
    public class McmModData
    {
        [Serializable]
        private class ConfigFile
        {
            public List<ConfigFileEntry> entries = new List<ConfigFileEntry>();
        }

        [Serializable]
        private class ConfigFileEntry
        {
            public string key;
            public string value;
        }

        public string ModId { get; set; }
        public string DisplayName { get; set; }
        public string Author { get; set; } = "Unknown";
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = "";

        public List<McmConfigEntry> Entries { get; } = new List<McmConfigEntry>();

        public IEnumerable<string> Sections => Entries
            .Select(e => string.IsNullOrEmpty(e.Section) ? "Общие" : e.Section)
            .Distinct();

        public string ConfigFilePath => Path.Combine(Application.persistentDataPath, "ModConfigs", ModId + ".json");

        public McmConfigEntry Get(string key) => Entries.FirstOrDefault(e => e.Key == key);

        public void AddEntry(McmConfigEntry entry)
        {
            int idx = Entries.FindIndex(e => e.Key == entry.Key);
            if (idx >= 0)
            {
                Entries[idx] = entry;
            }
            else
            {
                Entries.Add(entry);
            }
        }

        public void Load()
        {
            string path = ConfigFilePath;
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                ConfigFile file = JsonUtility.FromJson<ConfigFile>(json);
                if (file?.entries == null) return;

                foreach (var item in file.entries)
                {
                    var entry = Get(item.key);
                    if (entry != null)
                    {
                        entry.DeserializeValue(item.value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error loading config for '{ModId}': {ex}");
            }
        }

        public void Save()
        {
            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "ModConfigs");
                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }

                ConfigFile file = new ConfigFile();
                foreach (var entry in Entries)
                {
                    if (entry.EntryType == McmEntryType.ButtonAction) continue;

                    file.entries.Add(new ConfigFileEntry
                    {
                        key = entry.Key,
                        value = entry.SerializeValue()
                    });
                }

                string json = JsonUtility.ToJson(file, prettyPrint: true);
                File.WriteAllText(ConfigFilePath, json);

                // Notify vanilla game system if registered
                NotifyModConfigApplied(ModId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error saving config for '{ModId}': {ex}");
            }
        }

        public void ResetToDefaults()
        {
            foreach (var entry in Entries)
            {
                entry.ResetToDefault();
            }
            Save();
        }

        public static void NotifyModConfigApplied(string modId)
        {
            try
            {
                var modHookType = typeof(ModConfig).Assembly.GetType("ModHook");
                if (modHookType != null)
                {
                    AccessTools.Method(modHookType, "FireOnModConfigApplied")?.Invoke(null, new object[] { modId });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProbablyMCM] Could not fire OnModConfigApplied: {ex.Message}");
            }
        }
    }
}
