using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None
        };
        private readonly Dictionary<string, string> pendingValues = new Dictionary<string, string>(StringComparer.Ordinal);
        internal ModConfig NativeConfig { get; set; }

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
            ApplyStoredValue(entry);
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

        private void ApplyStoredValue(McmConfigEntry entry)
        {
            if (entry.NativeEntry != null)
                entry.Value = entry.NativeEntry.BoxedValue;
            else if (entry.EntryType != McmEntryType.ButtonAction && pendingValues.TryGetValue(entry.Key, out var raw))
                entry.DeserializeValue(raw);
        }

        public void Load()
        {
            string path = ConfigFilePath;
            if (!File.Exists(path)) return;

            try
            {
                var values = ReadValues(path);
                pendingValues.Clear();
                MergeValues(pendingValues, values);
                foreach (var entry in Entries) ApplyStoredValue(entry);
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
                string path = ConfigFilePath;
                // Validate before even native Save: an unreadable file must remain recoverable.
                var values = new Dictionary<string, string>(pendingValues, StringComparer.Ordinal);
                if (File.Exists(path)) MergeValues(values, ReadValues(path));
                bool nativeOnly = NativeConfig != null && Entries.All(e =>
                    e.EntryType == McmEntryType.ButtonAction || e.NativeEntry != null);
                if (nativeOnly)
                {
                    NativeConfig.Save();
                    NotifyModConfigApplied(ModId);
                    return;
                }

                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path));
                IDictionary<string, string> nativeRaw = null;
                if (NativeConfig != null)
                {
                    nativeRaw = AccessTools.Field(typeof(ModConfig), "rawValues")?.GetValue(NativeConfig) as IDictionary<string, string>;
                    if (nativeRaw == null) throw new MissingFieldException(typeof(ModConfig).FullName, "rawValues");
                    // Native Save replaces its file, so preserve custom values before that write too.
                    if (File.Exists(path)) File.Copy(path, path + ".pre-native.bak", overwrite: true);
                    NativeConfig.Save();
                    if (!File.Exists(path)) throw new IOException("Native config Save did not produce a config file.");
                    MergeValues(values, ReadValues(path));
                }
                foreach (var entry in Entries)
                {
                    if (entry.EntryType == McmEntryType.ButtonAction) continue;
                    values[entry.Key] = entry.NativeEntry != null
                        ? ConfigEntry.Format(entry.NativeEntry.BoxedValue)
                        : entry.SerializeValue();
                }
                ConfigFile file = new ConfigFile();
                foreach (var value in values)
                    file.entries.Add(new ConfigFileEntry { key = value.Key, value = value.Value });
                string json;
                using (var output = new StringWriter())
                using (var writer = new JsonTextWriter(output) { Formatting = Formatting.Indented })
                {
                    JsonSerializer.Create(JsonSettings).Serialize(writer, file);
                    writer.Flush();
                    json = output.ToString();
                }
                WriteAtomic(path, json);
                // Later native saves must preserve the MCM-only values merged into this same file.
                if (nativeRaw != null) foreach (var value in values) nativeRaw[value.Key] = value.Value;
                pendingValues.Clear();
                MergeValues(pendingValues, values);
                NotifyModConfigApplied(ModId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProbablyMCM] Error saving config for '{ModId}': {ex}");
            }
        }

        private static Dictionary<string, string> ReadValues(string path)
        {
            JObject root;
            using (var input = File.OpenText(path))
            using (var reader = new JsonTextReader(input))
            {
                root = JsonSerializer.Create(JsonSettings).Deserialize<JObject>(reader);
                if (reader.Read()) throw new JsonSerializationException("Unexpected trailing config data.");
            }
            if (root == null) throw new JsonSerializationException("Config must be a JSON object.");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var entries = root["entries"];
            // Earlier Unity serialization produced {}; keep that known empty-file migration valid.
            if (entries == null && !root.HasValues) return values;
            if (!(entries is JArray items)) throw new JsonSerializationException("Config entries must be an array.");
            foreach (var item in items)
            {
                if (!(item is JObject entry) || entry["key"]?.Type != JTokenType.String ||
                    (entry["value"]?.Type != JTokenType.String && entry["value"]?.Type != JTokenType.Null))
                    throw new JsonSerializationException("Each config entry must contain a string key and value.");
                values[(string)entry["key"]] = (string)entry["value"] ?? "";
            }
            return values;
        }

        private static void MergeValues(Dictionary<string, string> target, Dictionary<string, string> source)
        {
            foreach (var value in source) target[value.Key] = value.Value;
        }

        private static string NewBackupPath(string path) => path + ".bak";

        private static void WriteAtomic(string path, string json)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                    writer.Write(json);
                if (File.Exists(path)) File.Replace(temporary, path, NewBackupPath(path));
                else File.Move(temporary, path);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
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
