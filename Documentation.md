# ProbablyMCM - Modder's Documentation & Integration Guide
# ProbablyMCM - 模组开发者文档与集成指南

---

## 🌐 Language Navigation / 语言导航
- [English Guide](#-english-guide)
  - [1. Overview](#1-overview)
  - [2. Approach A: Zero-Dependency Integration (Vanilla ModConfig)](#2-approach-a-zero-dependency-integration-vanilla-modconfig)
  - [3. Approach B: Fluent API (ProbablyMCM.API)](#3-approach-b-fluent-api-probablymcm-api)
  - [4. Reading Settings in Your Mod Logic](#4-reading-settings-in-your-mod-logic)
  - [5. Best Practices & Tips](#5-best-practices--tips)
- [中文文档 (简体中文)](#-中文文档-简体中文)
  - [1. 概述与工作原理](#1-概述与工作原理)
  - [2. 方案 A：零依赖无缝集成（原生 ModConfig）](#2-方案-a零依赖无缝集成原生-modconfig)
  - [3. 方案 B：高级 Fluent API（引用 ProbablyMCM.API）](#3-方案-b高级-fluent-api引用-probablymcm-api)
  - [4. 在模组逻辑中读取配置](#4-在模组逻辑中读取配置)
  - [5. 最佳实践与建议](#5-最佳实践与建议)

---

# 📖 English Guide

## 1. Overview
**ProbablyMCM** is a unified Mod Configuration Menu for the game *Probably Stolen*. It provides a clean, responsive two-column interface in both the Pause Menu (`Esc` -> "MCM Menu") and the Main Menu (under "Credits"):
- **Left Column:** Mod list with search bar, active settings count badge, and clean text truncation (`...`).
- **Right Column:** Section-based controls including toggles, sliders, number inputs, dropdowns, buttons, and an interactive **KeyBind dialog popup**.

### Two Ways to Integrate:
1. **Approach A (Zero-Dependency):** Use the game's built-in `ModConfig`. ProbablyMCM automatically discovers and renders your settings without requiring any reference to `ProbablyMCM.dll`.
2. **Approach B (Fluent API):** Add a reference to `ProbablyMCM.dll` to unlock advanced features like sections, localized label getters, tooltips with info icons, action buttons, and direct callbacks.

---

## 2. Approach A: Zero-Dependency Integration (Vanilla ModConfig)

If you don't want your mod to require `ProbablyMCM.dll`, simply use the game's native configuration system in your `Init(ModManifest manifest)`:

```csharp
using UnityEngine;

public class MyMod : IMod
{
    private ModConfig config;

    public void Init(ModManifest manifest)
    {
        config = ModConfig.For(manifest);

        // 1. Checkbox / Toggle
        config.Register("General", "EnableFastLoot", true, "Quickly pickup items without holding button");

        // 2. Slider (HasRange automatically detected)
        config.RegisterRange("Balance", "LootMultiplier", 1.5f, 0.5f, 5.0f, "Item drop multiplier");

        // 3. Exact Integers
        config.Register("Storage", "ExtraSlots", 10, "Number of bonus inventory slots");

        // 4. Hotkey / KeyBind (Auto-detected!)
        // Name the key or section containing "hotkey", "keybind", or "bind", 
        // or set the value to a KeyCode or valid key name (e.g., "F", "G", "Alpha1", "None").
        config.Register("Controls", "ToggleHudHotkey", KeyCode.H, "Press to hide or show HUD");

        config.Save();
    }
}
```

### Automatic UI Mapping Rules in MCM:
- **`bool`** ➔ Toggle switch.
- **`HasRange == true`** ➔ Slider with real-time value label.
- **`int` / `float`** ➔ Number input field with min/max clamp validation.
- **`Choices != null`** ➔ Dropdown select menu.
- **`KeyCode` or Key naming** (`keybind`, `hotkey`, `bind`, `shortcut`) ➔ Interactive **KeyBind Button** that opens the pop-up modal on click.
- **`string`** ➔ Text input field.

---

## 3. Approach B: Fluent API (`ProbablyMCM.API`)

For full control over visual layout, sections, live callbacks, and localized labels, reference `ProbablyMCM.dll` in your `.csproj`:

```xml
<ItemGroup>
    <Reference Include="ProbablyMCM">
        <HintPath>$(GamePath)\Mods\ProbablyMCM\ProbablyMCM.dll</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

### Complete Example:
```csharp
using ProbablyMCM.API;
using UnityEngine;

public class MyAdvancedMod : IMod
{
    public void Init(ModManifest manifest)
    {
        var settings = ModSettings.For("my_mod_id", "My Awesome Mod");

        // ==================== SECTION 1 ====================
        settings.Section("Gameplay");

        // 1. Toggle
        settings.AddToggle(
            key: "auto_sort",
            label: "Auto Sort Inventory",
            defaultValue: true,
            tooltip: "Automatically arrange items when opening chests",
            onChanged: (bool enabled) => Debug.Log($"Auto-sort changed: {enabled}")
        );

        // 2. Slider
        settings.AddSlider(
            key: "speed_mult",
            label: "Movement Speed Multiplier",
            defaultValue: 1.0f,
            min: 0.5f,
            max: 3.0f,
            step: 0.1f,
            wholeNumbers: false,
            tooltip: "Adjust character movement speed in the store"
        );

        // 3. Number Input (Int / Float)
        settings.AddInt(
            key: "max_customers",
            label: "Max Customers in Store",
            defaultValue: 8,
            min: 1,
            max: 30,
            tooltip: "Limit maximum simultaneous shoppers"
        );

        // ==================== SECTION 2 ====================
        settings.Section("Controls");

        // 4. Interactive KeyBind Dialog
        // Clicking this in MCM opens a popup modal with support for Ctrl/Alt/Shift + Key,
        // Delete to clear, and Esc to cancel.
        settings.AddKeyBind(
            key: "teleport_key",
            label: "Quick Teleport Key",
            defaultKey: KeyCode.T,
            tooltip: "Press in MCM to assign any key or key combo",
            onChanged: (KeyCode newKey) => Debug.Log($"New bind: {newKey}")
        );

        // ==================== SECTION 3 ====================
        settings.Section("Appearance");

        // 5. Dropdown
        settings.AddDropdown(
            key: "ui_theme",
            label: "Color Theme",
            defaultValue: "Dark",
            options: new[] { "Dark", "Neon", "Cyberpunk", "Minimal" },
            onChanged: (string theme) => ApplyTheme(theme)
        );

        // 6. Action Button
        settings.AddButton(
            key: "reset_cache",
            label: "Clear Mod Cache",
            onClick: () =>
            {
                Debug.Log("Cache successfully wiped!");
            },
            tooltip: "Click to wipe temporary textures and reload data"
        );
    }

    private void ApplyTheme(string theme) { /* ... */ }
}
```

### Dynamic Localization with `Func<string>`:
Every widget method has an overload accepting `Func<string>` instead of static `string`. This allows real-time language switching:

```csharp
settings.AddToggle(
    key: "feature_x",
    labelGetter: () => Loc.Get("mod_feature_x_title"),
    defaultValue: true,
    tooltipGetter: () => Loc.Get("mod_feature_x_tooltip")
);
```

---

## 4. Reading Settings in Your Mod Logic

To retrieve values anywhere in your code:

```csharp
var settings = ModSettings.For("my_mod_id");

bool autoSort = settings.Get<bool>("auto_sort");
float speed = settings.Get<float>("speed_mult");
int maxCustomers = settings.Get<int>("max_customers");
KeyCode teleportKey = settings.Get<KeyCode>("teleport_key");
string theme = settings.Get<string>("ui_theme");
```

---

## 5. Best Practices & Tips

1. **Hotkeys:** When using KeyBinds, players can press modifier keys (`Ctrl`, `Alt`, `Shift`) directly on the keyboard in the popup window. Pressing `Delete` clears the binding (`None`).
2. **Persistence:** Settings are automatically saved to:
   `%AppData%/../LocalLow/Salty Panda Studios/Probably Stolen/ModConfigs/<ModId>.json`.
3. **Tooltips:** Always provide descriptive tooltips. MCM renders an interactive info icon `[i]` next to the setting title with hover popovers.
4. **Mod Name Length:** In the left list, names are cleanly truncated with `...` while reserving space for the count badge `(XX)`. Full names are always displayed on hover.

---
---

# 📖 中文文档 (简体中文)

## 1. 概述与工作原理
**ProbablyMCM** 是为《Probably Stolen》游戏开发的通用模组配置菜单（Mod Configuration Menu）。它在游戏暂停菜单（按 `Esc` 弹出菜单中的「MCM-菜单」）以及主菜单（「制作人员/演职员表」正下方）中提供了统一、美观的双栏配置界面：
- **左侧模组列表：** 包含即时搜索栏、模组配置数量角标 `(XX)`、超出宽度智能 `...` 截断与悬停完整标题气泡。
- **右侧配置详情：** 按分区（Section）展示模组设置，支持开关、滑动条、数值输入框、下拉菜单、动作按钮以及**交互式按键绑定弹窗**。

### 两种接入方式：
1. **方案 A（零依赖集成）：** 直接使用游戏原生的 `ModConfig`。MCM 会自动读取并渲染您的设置，您的模组无需引用任何第三方 DLL。
2. **方案 B（Fluent API 高级接入）：** 引用 `ProbablyMCM.dll`，解锁分区排版、动态多语言函数、信息气泡提示、动作按钮及值变更实时回调。

---

## 2. 方案 A：零依赖无缝集成（原生 ModConfig）

如果您不想让自己的模组依赖 `ProbablyMCM.dll`，只需在模组的 `Init(ModManifest manifest)` 中使用游戏自带的配置系统：

```csharp
using UnityEngine;

public class MyMod : IMod
{
    private ModConfig config;

    public void Init(ModManifest manifest)
    {
        config = ModConfig.For(manifest);

        // 1. 开关勾选框 (Toggle)
        config.Register("常规", "EnableFastLoot", true, "无需长按，快速拾取物品");

        // 2. 范围滑动条 (Slider，自动检测 HasRange)
        config.RegisterRange("数值平衡", "LootMultiplier", 1.5f, 0.5f, 5.0f, "物品掉落倍率");

        // 3. 整数数值输入框
        config.Register("背包容量", "ExtraSlots", 10, "额外背包格子数量");

        // 4. 快捷键设置 (KeyBind，自动检测！)
        // 只要键名或分区包含 "hotkey"、"keybind"、"bind"、"shortcut" 或 "клавиш"，
        // 或者类型为 KeyCode，MCM 就会自动将其渲染为交互式快捷键设置按钮！
        config.Register("快捷键", "ToggleHudHotkey", KeyCode.H, "按下此键切换界面显示");

        config.Save();
    }
}
```

### MCM 自动控件映射规则：
- **`bool`** ➔ 复选开关（Toggle）。
- **`HasRange == true`** ➔ 带数值显示的平滑滑动条（Slider）。
- **`int` / `float`** ➔ 数值输入框，自动限制在最大/最小值之间。
- **`Choices != null`** ➔ 下拉选择框（Dropdown）。
- **`KeyCode` 或包含 hotkey/keybind/bind 关键词** ➔ **快捷键配置按钮**，点击即可弹出专属改键小窗口。
- **普通 `string`** ➔ 单行文本输入框。

---

## 3. 方案 B：高级 Fluent API（引用 `ProbablyMCM.API`）

如需使用分区标题、悬停提示图标 `[i]`、改键监听和实时生效回调，请在模组的 `.csproj` 中添加引用：

```xml
<ItemGroup>
    <Reference Include="ProbablyMCM">
        <HintPath>$(GamePath)\Mods\ProbablyMCM\ProbablyMCM.dll</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

### 完整示例代码：
```csharp
using ProbablyMCM.API;
using UnityEngine;

public class MyAdvancedMod : IMod
{
    public void Init(ModManifest manifest)
    {
        // 绑定模组 ID 和显示名称
        var settings = ModSettings.For("my_mod_id", "我的高级模组");

        // ==================== 第一分区 ====================
        settings.Section("玩法机制");

        // 1. 开关 (Toggle)
        settings.AddToggle(
            key: "auto_organize",
            label: "自动整理背包",
            defaultValue: true,
            tooltip: "打开容器时自动按照品类和价值整理物品",
            onChanged: (bool val) => Debug.Log($"整理开关改变: {val}")
        );

        // 2. 滑动条 (Slider)
        settings.AddSlider(
            key: "move_speed",
            label: "角色移动速度倍率",
            defaultValue: 1.0f,
            min: 0.5f,
            max: 3.0f,
            step: 0.1f,
            wholeNumbers: false,
            tooltip: "调整主角在店内移动的基础速度"
        );

        // 3. 限制范围的数值输入框 (Int / Float)
        settings.AddInt(
            key: "max_shoppers",
            label: "店内最大顾客数",
            defaultValue: 8,
            min: 1,
            max: 30,
            tooltip: "限制同屏进店选购的最大顾客数量"
        );

        // ==================== 第二分区 ====================
        settings.Section("按键操作");

        // 4. 交互式快捷键设置弹窗 (KeyBind)
        // 玩家点击按钮后，会弹出居中微型窗口，支持 Ctrl/Alt/Shift 组合键、Delete 清空、Esc 取消
        settings.AddKeyBind(
            key: "teleport_home_key",
            label: "快速传送回店按键",
            defaultKey: KeyCode.T,
            tooltip: "在菜单中点击即可直接按下键盘任意键重新绑定",
            onChanged: (KeyCode newKey) => Debug.Log($"新绑定按键: {newKey}")
        );

        // ==================== 第三分区 ====================
        settings.Section("界面与外观");

        // 5. 下拉菜单 (Dropdown)
        settings.AddDropdown(
            key: "hud_style",
            label: "界面样式风格",
            defaultValue: "经典暗黑",
            options: new[] { "经典暗黑", "赛博霓虹", "复古终端", "极简清爽" },
            onChanged: (string style) => ApplyHudStyle(style)
        );

        // 6. 动作按钮 (Action Button)
        settings.AddButton(
            key: "clean_mod_cache",
            label: "清理模组临时缓存",
            onClick: () =>
            {
                Debug.Log("临时缓存清理完毕！");
            },
            tooltip: "重置纹理缓存并释放多余内存"
        );
    }

    private void ApplyHudStyle(string style) { /* ... */ }
}
```

### 动态多语言支持（`Func<string>`）：
所有控件方法均提供重载，支持传入返回字符串的委托，在玩家切换语言时自动刷新文本：

```csharp
settings.AddToggle(
    key: "feature_toggle",
    labelGetter: () => LocHelper.GetLocalizedUI("my_mod_toggle_title"),
    defaultValue: true,
    tooltipGetter: () => LocHelper.GetLocalizedUI("my_mod_toggle_desc")
);
```

---

## 4. 在模组逻辑中读取配置

在游戏运行的任何地方，通过 `ModSettings.For` 即可快速读取当前已保存的值：

```csharp
var settings = ModSettings.For("my_mod_id");

bool autoSort = settings.Get<bool>("auto_organize");
float speed = settings.Get<float>("move_speed");
int maxShoppers = settings.Get<int>("max_shoppers");
KeyCode teleportKey = settings.Get<KeyCode>("teleport_home_key");
string hudStyle = settings.Get<string>("hud_style");
```

---

## 5. 最佳实践与建议

1. **快捷键体验：** 快捷键录入窗口采用非全屏轻量设计，支持键盘组合键（如 `Ctrl+Shift+F`）、`Delete` 清空（变为 `None` / `未绑定`）以及 `Esc` 安全取消，建议使用 `AddKeyBind` 代替传统的纯文本输入。
2. **存档路径：** 所有配置会在修改时实时自动持久化保存至：
   `%AppData%/../LocalLow/Salty Panda Studios/Probably Stolen/ModConfigs/<ModId>.json`。
3. **提示信息：** 为参数填写 `tooltip` 会在标题右侧自动生成信息图标 `[i]`，玩家将鼠标悬停在图标上即可查看详细说明。
4. **列表标题：** 左侧列表栏宽度已做智能适配，会自动计算全角/半角字符并在保留 `(XX)` 配置计数的前提下美观显示 `...`，悬停列表按钮即可查看完整名称。
