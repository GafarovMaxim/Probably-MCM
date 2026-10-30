using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ProbablyMCM.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyMCM.UI
{
    public static class ModSettingsWindow
    {
        private const float LEFT_COLUMN_WIDTH = 340f;

        private static CustomUIWindow activeWindow;
        private static RectTransform listContainer;
        private static RectTransform detailContainer;
        private static string selectedModId;
        private static string searchFilter = "";

        public static bool IsOpen => activeWindow != null && activeWindow.IsAlive;

        public static void Close()
        {
            if (IsOpen)
            {
                activeWindow?.Close();
            }
        }

        public static void Open(string initialModId = null)
        {
            if (IsOpen)
            {
                if (!string.IsNullOrEmpty(initialModId))
                {
                    SelectMod(initialModId);
                }
                return;
            }

            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null)
            {
                Debug.LogError("[ProbablyMCM] No CustomUIManager in scene!");
                return;
            }

            // Sync mods before opening
            McmRegistry.SyncWithVanillaMods();
            List<McmModData> allMods = McmRegistry.GetAllMods();

            if (!string.IsNullOrEmpty(initialModId))
            {
                selectedModId = initialModId;
            }
            else if (string.IsNullOrEmpty(selectedModId) && allMods.Count > 0)
            {
                selectedModId = allMods[0].ModId;
            }

            bool inStore = StoreUIManager.Instance != null;
            string layerId = inStore ? "meta" : "menu";
            string windowTitle = McmLoc.Get("mcm_window_title", "Настройки модов");

            CustomUIBuilder builder = manager.CreateWindow(
                "probably_mcm_hub",
                windowTitle,
                layerId,
                manager.optionUIPrefab
            );

            // Explicitly set title to strip any localized text overrides
            builder.Handle.SetTitle(windowTitle);
            builder.SetCloseOnEscape(true);
            builder.SetSize(1000f, 660f);
            builder.Center();
            builder.OnClose(() =>
            {
                activeWindow = null;
                listContainer = null;
                detailContainer = null;
            });

            // Add KeyBindCapture component
            if (builder.Handle != null && builder.Handle.root != null)
            {
                if (builder.Handle.root.GetComponent<KeyBindCapture>() == null)
                {
                    builder.Handle.root.AddComponent<KeyBindCapture>();
                }
            }

            // 1. Search Bar & Status Row (Top Header)
            builder.BeginRow(8f, TextAnchor.MiddleLeft);
            SetHeight(builder.CurrentContainer, 36f);

            string searchPlaceholder = McmLoc.Get("mcm_search_placeholder", "🔍 Поиск мода или параметра...");
            builder.AddInput(searchPlaceholder, searchFilter, (val) =>
            {
                searchFilter = val;
                RefreshModList();
            });
            builder.Last.SetFixedWidth(LEFT_COLUMN_WIDTH);
            builder.AddFlexibleSpace();

            string countLabelText = McmLoc.Get("mcm_mod_count", "Всего модов: {0}", allMods.Count);
            builder.AddLabel(countLabelText, "mcm_mod_count");
            builder.End(); // end top header row

            builder.AddSeparator(null, 2);

            // 2. Main 2-Column Split View (Middle Area)
            builder.BeginRow(12f, TextAnchor.UpperLeft);
            RectTransform mainRow = builder.CurrentContainer;
            SetFlexible(mainRow, 1f, 1f);

            // Left Column: Mod List (Fixed Width: 340px, Flexible Height)
            builder.BeginColumn(4f);
            RectTransform leftCol = builder.CurrentContainer;
            SetFixedWidth(leftCol, LEFT_COLUMN_WIDTH, 1f);

            builder.BeginScrollFlexible();
            listContainer = builder.CurrentContainer;
            EnsureVerticalLayout(listContainer, 4f);
            builder.End(); // end left scroll

            builder.End(); // end left column

            // Right Column: Settings Detail (Flexible Width & Height)
            builder.BeginColumn(6f);
            RectTransform rightCol = builder.CurrentContainer;
            SetFlexible(rightCol, 1f, 1f);

            builder.BeginScrollFlexible();
            detailContainer = builder.CurrentContainer;
            EnsureVerticalLayout(detailContainer, 4f);
            builder.End(); // end right scroll

            builder.End(); // end right column
            builder.End(); // end main 2-column row

            // 3. Bottom Footer (Reset and Done Buttons)
            builder.AddSeparator(null, 2);
            builder.BeginRow(8f, TextAnchor.MiddleRight);
            SetHeight(builder.CurrentContainer, 36f);

            builder.AddFlexibleSpace();
            string resetBtnText = McmLoc.Get("mcm_btn_reset", "↺ Сбросить настройки");
            builder.AddButton(resetBtnText, OnResetClicked, "mcm_btn_reset");
            builder.Last.SetFixedWidth(200f);

            string doneBtnText = McmLoc.Get("mcm_btn_done", "Готово");
            builder.AddButton(doneBtnText, () => activeWindow?.Close(), "mcm_btn_done");
            builder.Last.SetFixedWidth(140f);
            builder.End(); // end bottom footer row

            activeWindow = builder.Show();

            // Populate both columns
            RefreshModList();
            RefreshDetailView();
        }

        public static void SelectMod(string modId)
        {
            selectedModId = modId;
            RefreshModList();
            RefreshDetailView();
        }

        private static void RefreshModList()
        {
            if (listContainer == null || CustomUIManager.Instance == null || activeWindow == null) return;

            CustomUIBuilder.ClearContainer(listContainer);
            EnsureVerticalLayout(listContainer, 4f);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, activeWindow, listContainer);

            List<McmModData> allMods = McmRegistry.GetAllMods();
            string filter = (searchFilter ?? "").Trim().ToLowerInvariant();

            foreach (McmModData mod in allMods)
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    bool matchName = mod.DisplayName.ToLowerInvariant().Contains(filter);
                    bool matchId = mod.ModId.ToLowerInvariant().Contains(filter);
                    bool matchAuthor = mod.Author.ToLowerInvariant().Contains(filter);
                    bool matchSetting = mod.Entries.Any(e => e.GetLabel().ToLowerInvariant().Contains(filter));
                    if (!matchName && !matchId && !matchAuthor && !matchSetting) continue;
                }

                string rawName = string.IsNullOrEmpty(mod.DisplayName) ? mod.ModId : mod.DisplayName;
                bool isSelected = string.Equals(mod.ModId, selectedModId, StringComparison.OrdinalIgnoreCase);
                string modIdCapture = mod.ModId;

                b.AddButton("", () => SelectMod(modIdCapture));

                if (b.Last != null)
                {
                    if (b.Last.Rect != null)
                    {
                        SetHeight(b.Last.Rect, 42f);
                    }

                    // Отступ текста от краев рамки кнопки
                    if (b.Last.text != null)
                    {
                        b.Last.text.margin = new Vector4(12f, 0f, 12f, 0f);
                    }

                    string buttonText = FormatModButtonText(rawName, mod.Entries.Count, isSelected, b.Last.text);
                    b.Last.SetText(buttonText);

                    // Подсказка с полным оригинальным названием мода
                    b.Last.SetTooltip(rawName);
                }
            }

            // Update mod count label in header
            var countLabel = activeWindow.Get("mcm_mod_count");
            if (countLabel != null)
            {
                countLabel.SetText(McmLoc.Get("mcm_mod_count", "Всего модов: {0}", allMods.Count));
            }

            CustomUIBuilder.RebuildLayout(listContainer);
        }

        private static float GetCharWeight(char c)
        {
            // CJK иероглифы (китайские, японские, корейские)
            if ((c >= 0x2E80 && c <= 0x9FFF) || 
                (c >= 0xAC00 && c <= 0xD7AF) || 
                (c >= 0xF900 && c <= 0xFAFF) ||
                (c >= 0x3000 && c <= 0x303F))
            {
                return 2.0f;
            }
            // Широкие символы
            if (c == 'W' || c == 'M' || c == 'Ж' || c == 'Ш' || c == 'Щ' || c == 'Ю' || c == '@' || c == '%')
            {
                return 1.3f;
            }
            // Узкие символы
            if (c == 'i' || c == 'l' || c == 'j' || c == 't' || c == 'r' || c == 'f' || 
                c == ' ' || c == '.' || c == ',' || c == ':' || c == ';' || c == '!' || c == '|' || c == '\'')
            {
                return 0.55f;
            }
            return 1.0f;
        }

        private static float GetStringWeight(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            float sum = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                sum += GetCharWeight(s[i]);
            }
            return sum;
        }

        private static string FormatModButtonText(string rawName, int entriesCount, bool isSelected, TextMeshProUGUI tmpText)
        {
            string badge = $" <color=#94a3b8>({entriesCount})</color>";
            string highlight = isSelected ? "<b><color=#38bdf8>▶ " : "<color=#cbd5e1>";
            string fullRaw = $"{highlight}{rawName}</color>{badge}";

            // Максимальный визуальный вес строки с учетом отступов (12px с обеих сторон)
            const float MAX_TOTAL_WEIGHT = 21.0f;
            const float ELLIPSIS_WEIGHT = 1.65f; // "..." (3 * 0.55f)
            const float MAX_PIXEL_WIDTH = 235f;

            // Проверяем точную ширину через TMP GetPreferredValues если доступно
            bool canUseTMP = tmpText != null && tmpText.font != null;
            if (canUseTMP)
            {
                float fullWidth = tmpText.GetPreferredValues(fullRaw).x;
                if (fullWidth > 0f)
                {
                    if (fullWidth <= MAX_PIXEL_WIDTH)
                    {
                        return fullRaw;
                    }

                    for (int len = rawName.Length - 1; len >= 1; len--)
                    {
                        string candidateName = rawName.Substring(0, len).TrimEnd() + "...";
                        string candidateFull = $"{highlight}{candidateName}</color>{badge}";
                        if (tmpText.GetPreferredValues(candidateFull).x <= MAX_PIXEL_WIDTH)
                        {
                            return candidateFull;
                        }
                    }

                    return $"{highlight}...{badge}";
                }
            }

            // Fallback: взвешенный расчет
            float badgeWeight = GetStringWeight($" ({entriesCount})");
            float prefixWeight = isSelected ? 2.0f : 0f;
            float rawWeight = GetStringWeight(rawName);

            if (prefixWeight + rawWeight + badgeWeight <= MAX_TOTAL_WEIGHT)
            {
                return fullRaw;
            }

            float allowedNameWeight = MAX_TOTAL_WEIGHT - prefixWeight - badgeWeight - ELLIPSIS_WEIGHT;
            if (allowedNameWeight <= 0f) allowedNameWeight = 1.0f;

            float currentWeight = 0f;
            int truncateLen = 0;
            for (int i = 0; i < rawName.Length; i++)
            {
                float w = GetCharWeight(rawName[i]);
                if (currentWeight + w > allowedNameWeight) break;
                currentWeight += w;
                truncateLen = i + 1;
            }

            string truncated = (truncateLen > 0 ? rawName.Substring(0, truncateLen).TrimEnd() : "") + "...";
            return $"{highlight}{truncated}</color>{badge}";
        }

        private static void RefreshDetailView()
        {
            if (detailContainer == null || CustomUIManager.Instance == null || activeWindow == null) return;

            CustomUIBuilder.ClearContainer(detailContainer);
            EnsureVerticalLayout(detailContainer, 4f);
            CustomUIBuilder b = new CustomUIBuilder(CustomUIManager.Instance, activeWindow, detailContainer);

            McmModData mod = McmRegistry.Find(selectedModId);
            if (mod == null)
            {
                b.AddSpace(20f);
                b.AddLabel($"<color=#94a3b8>{McmLoc.Get("mcm_select_mod_hint", "Выберите мод из списка слева для настройки параметров.")}</color>");
                CustomUIBuilder.RebuildLayout(detailContainer);
                return;
            }

            // Header Banner
            b.BeginRow(4f, TextAnchor.MiddleLeft);
            b.AddHeader($"<b><size=18>{mod.DisplayName}</size></b> <size=12><color=#64748b>v{mod.Version}</color></size>");
            b.AddFlexibleSpace();
            string authorText = McmLoc.Get("mcm_author", "Автор: {0}", mod.Author);
            b.AddLabel($"<color=#94a3b8>{authorText}</color>");
            b.End();

            if (!string.IsNullOrEmpty(mod.Description))
            {
                b.AddLabel($"<size=12><color=#cbd5e1>{mod.Description}</color></size>");
                b.AddSpace(6f);
            }

            b.AddSeparator(null, 2);

            // Group settings by section
            string filter = (searchFilter ?? "").Trim().ToLowerInvariant();
            var sections = mod.Sections.ToList();

            if (mod.Entries.Count == 0)
            {
                b.AddSpace(20f);
                b.AddLabel($"<color=#94a3b8>{McmLoc.Get("mcm_no_settings", "Этот мод не содержит настраиваемых параметров.")}</color>");
            }

            string defaultSectionName = McmLoc.Get("mcm_default_section", "Общие");
            foreach (string section in sections)
            {
                var sectionEntries = mod.Entries
                    .Where(e => (string.IsNullOrEmpty(e.Section) ? defaultSectionName : e.Section) == section)
                    .Where(e => string.IsNullOrEmpty(filter) || 
                                e.GetLabel().ToLowerInvariant().Contains(filter) || 
                                (e.GetTooltip() != null && e.GetTooltip().ToLowerInvariant().Contains(filter)))
                    .ToList();

                if (sectionEntries.Count == 0) continue;

                b.AddSpace(8f);
                b.BeginRow(4f, TextAnchor.MiddleLeft);
                b.AddHeader($"<color=#38bdf8><b>{section}</b></color>");
                b.End();
                b.AddSeparator(null, 1);

                foreach (McmConfigEntry entry in sectionEntries)
                {
                    BuildWidgetRow(b, mod, entry);
                }
            }

            CustomUIBuilder.RebuildLayout(detailContainer);
        }

        private static void BuildWidgetRow(CustomUIBuilder b, McmModData mod, McmConfigEntry entry)
        {
            string labelText = entry.GetLabel();
            string tooltip = entry.GetTooltip();
            if (!string.IsNullOrEmpty(tooltip))
            {
                string linkId = "mcm_tip:" + mod.ModId + ":" + entry.Key;
                TMPIconHover.CustomLinks[linkId] = () => tooltip;
                labelText += " <link=\"" + linkId + "\"><sprite name=\"info\" tint=1></link>";
            }

            b.BeginRow(6f, TextAnchor.MiddleLeft);
            b.AddLabel(labelText, "label_" + entry.Key);
            b.AddFlexibleSpace();

            switch (entry.EntryType)
            {
                case McmEntryType.Toggle:
                    b.AddToggle("", entry.GetBool(), (val) =>
                    {
                        entry.SetValue(val);
                        mod.Save();
                    }, "toggle_" + entry.Key);
                    break;

                case McmEntryType.Slider:
                    float curFloat = entry.GetFloat();
                    string fmt = entry.WholeNumbers 
                        ? Mathf.RoundToInt(curFloat).ToString() 
                        : curFloat.ToString("F2", CultureInfo.InvariantCulture);

                    b.AddSlider(entry.Min, entry.Max, curFloat, (val) =>
                    {
                        object applied = entry.WholeNumbers ? (object)Mathf.RoundToInt(val) : val;
                        entry.SetValue(applied);
                        mod.Save();
                        // Update text label next to slider
                        var valLbl = activeWindow?.Get("val_" + entry.Key);
                        if (valLbl != null)
                        {
                            string newFmt = entry.WholeNumbers 
                                ? Mathf.RoundToInt(val).ToString() 
                                : val.ToString("F2", CultureInfo.InvariantCulture);
                            valLbl.SetText(newFmt);
                        }
                    }, entry.WholeNumbers, "slider_" + entry.Key);
                    b.Last.SetFixedWidth(200f);

                    b.AddLabel(fmt, "val_" + entry.Key);
                    b.Last.SetFixedWidth(50f);
                    break;

                case McmEntryType.IntNumber:
                    b.AddInput("", entry.GetInt().ToString(), (val) =>
                    {
                        if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        {
                            if (entry.Min < entry.Max) parsed = Mathf.Clamp(parsed, (int)entry.Min, (int)entry.Max);
                            entry.SetValue(parsed);
                            mod.Save();
                        }
                    }, "input_" + entry.Key);
                    b.Last.SetFixedWidth(110f);
                    break;

                case McmEntryType.FloatNumber:
                    b.AddInput("", entry.GetFloat().ToString("F2", CultureInfo.InvariantCulture), (val) =>
                    {
                        if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                        {
                            if (entry.Min < entry.Max) parsed = Mathf.Clamp(parsed, entry.Min, entry.Max);
                            entry.SetValue(parsed);
                            mod.Save();
                        }
                    }, "input_" + entry.Key);
                    b.Last.SetFixedWidth(110f);
                    break;

                case McmEntryType.Dropdown:
                    int initialIdx = 0;
                    string curStr = entry.GetString();
                    if (entry.Choices != null && entry.Choices.Length > 0)
                    {
                        initialIdx = Mathf.Max(0, Array.IndexOf(entry.Choices, curStr));
                    }
                    b.AddDropdown(entry.Choices ?? Array.Empty<string>(), initialIdx, (idx) =>
                    {
                        if (entry.Choices != null && idx >= 0 && idx < entry.Choices.Length)
                        {
                            entry.SetValue(entry.Choices[idx]);
                            mod.Save();
                        }
                    }, "drop_" + entry.Key);
                    b.Last.SetFixedWidth(200f);
                    break;

                case McmEntryType.KeyBind:
                    string currentCombo = entry.GetStringValue();
                    string displayCombo = string.IsNullOrEmpty(currentCombo) || currentCombo.Equals("None", StringComparison.OrdinalIgnoreCase)
                        ? McmLoc.Get("mcm_key_not_bound", "Не назначена") 
                        : currentCombo;

                    b.AddButton($"[ {displayCombo} ]", null, "bind_" + entry.Key);
                    b.Last.SetFixedWidth(180f);
                    var capturedBtn = b.Last;
                    capturedBtn.OnClick(() =>
                    {
                        KeyBindDialog.Open(mod, entry, () =>
                        {
                            RefreshDetailView();
                        });
                    });
                    break;

                case McmEntryType.InputText:
                    b.AddInput("", entry.GetString(), (val) =>
                    {
                        entry.SetValue(val);
                        mod.Save();
                    }, "input_" + entry.Key);
                    b.Last.SetFixedWidth(220f);
                    break;

                case McmEntryType.ButtonAction:
                    b.AddButton(entry.GetLabel(), () =>
                    {
                        entry.ButtonAction?.Invoke();
                    }, "btn_" + entry.Key);
                    b.Last.SetFixedWidth(180f);
                    break;
            }

            b.End();
        }

        private static void OnResetClicked()
        {
            McmModData mod = McmRegistry.Find(selectedModId);
            if (mod != null)
            {
                mod.ResetToDefaults();
                RefreshDetailView();
            }
        }

        private static void SetFixedWidth(RectTransform rect, float width, float flexibleHeight = -1f)
        {
            if (rect == null) return;
            LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.minWidth = width;
            layoutElement.preferredWidth = width;
            layoutElement.flexibleWidth = 0f;
            if (flexibleHeight >= 0f)
            {
                layoutElement.flexibleHeight = flexibleHeight;
            }
        }

        private static void SetFlexible(RectTransform rect, float flexibleWidth = 1f, float flexibleHeight = 1f)
        {
            if (rect == null) return;
            LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }
            if (flexibleWidth >= 0f) layoutElement.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) layoutElement.flexibleHeight = flexibleHeight;
        }

        private static void SetHeight(RectTransform rect, float height)
        {
            if (rect == null) return;
            LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
        }

        private static void EnsureVerticalLayout(RectTransform container, float spacing = 4f)
        {
            if (container == null) return;

            VerticalLayoutGroup group = container.GetComponent<VerticalLayoutGroup>();
            if (group == null)
            {
                group = container.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            group.spacing = spacing;
            group.childAlignment = TextAnchor.UpperLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.padding = new RectOffset(6, 6, 6, 6);

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
