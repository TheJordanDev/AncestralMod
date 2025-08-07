using UnityEngine;
using System;
using System.Reflection;
using BepInEx.Configuration;
using AncestralMod.UI;

namespace AncestralMod.Modules;

public class ConfigEditorModule : Module
{
    public static ConfigEditorModule Instance { get; private set; } = null!;
    public override string ModuleName => "ConfigEditor";

    public override void Initialize()
    {
        if (Instance != null) return;
        Instance = this;
        base.Initialize();
    }

    public override void Update()
    {
        // Toggle settings UI with configured key (with F3 fallback)
        KeyCode toggleKey = GetToggleKey();

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSettingsUI();
        }
    }

    private KeyCode GetToggleKey()
    {
        try { return ConfigHandler.OpenConfigEditor?.Value ?? KeyCode.F3; }
        catch { return KeyCode.F3; }
    }

    private void ToggleSettingsUI()
    {
        ConfigEditorUI.Instance?.Toggle();
    }

    public void SaveConfiguration()
    {
        try
        {
            var configField = typeof(ConfigHandler).GetField("config", BindingFlags.NonPublic | BindingFlags.Static);
            if (configField?.GetValue(null) is ConfigFile configFile)
            {
                configFile.Save();
                Plugin.Log.LogInfo("Configuration saved successfully");
            }
            else
            {
                Plugin.Log.LogError("Could not access config file for saving");
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to save configuration: {e.Message}");
        }
    }
}
