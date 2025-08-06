using UnityEngine;
using System;
using System.Reflection;
using BepInEx.Configuration;
using AncestralMod.UI;

namespace AncestralMod.Modules;

public class ConfigEditorModule : Module
{
    public override string ModuleName => "ConfigEditor";

    private ModSettingsUI? _settingsUI;

    public override void Initialize()
    {
        base.Initialize();
        InitializeSettingsUI();
    }

    public override void Update()
    {
        // Toggle settings UI with configured key (with F3 fallback)
        KeyCode toggleKey = GetToggleKey();

        if (Input.GetKeyDown(toggleKey))
        {
            Plugin.Log.LogInfo($"Toggle key {toggleKey} pressed, current UI state: {_settingsUI?.IsVisible ?? false}");
            ToggleSettingsUI();
        }

        // Handle key input for the UI
        _settingsUI?.HandleKeyInput();
    }

    private void InitializeSettingsUI()
    {
        try
        {
            _settingsUI = new ModSettingsUI();
            _settingsUI.OnCloseRequested += () => SetSettingsUIVisible(false);
            _settingsUI.OnSaveRequested += SaveConfiguration;
            _settingsUI.Initialize();
            
            Plugin.Log.LogInfo("Settings UI initialized successfully");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to initialize settings UI: {e.Message}");
        }
    }

    private KeyCode GetToggleKey()
    {
        try
        {
            return ConfigHandler.OpenConfigEditor?.Value ?? KeyCode.F3;
        }
        catch
        {
            return KeyCode.F3;
        }
    }

    private void ToggleSettingsUI()
    {
        if (_settingsUI == null)
        {
            Plugin.Log.LogWarning("Cannot toggle settings UI - not initialized");
            return;
        }

        SetSettingsUIVisible(!_settingsUI.IsVisible);
    }

    private void SetSettingsUIVisible(bool visible)
    {
        _settingsUI?.SetVisible(visible);
    }

    private void SaveConfiguration()
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

    public override void Destroy()
    {
        _settingsUI?.Destroy();
        _settingsUI = null;
        base.Destroy();
    }
}
