using System;
using System.Collections.Generic;
using AncestralMod.Events;
using AncestralMod.Modules;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AncestralMod;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{

    public static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private static Harmony? _harmony;
    private ModuleManager? _moduleManager;

    private static readonly List<Type> _globalPatches = [
        typeof(Patches.PassportPatch),
        typeof(Patches.KnockoutPatch),
    ];

    private void Awake()
    {
        Instance = this;

        ConfigHandler.Initialize(Config);
        Log = Logger;
        Debug.Log("AncestralMod is starting...");
        SetupEvents();
        SetupModules();
        SetupPatches();
    }

    private void Update()
    {
        _moduleManager?.Update();
    }

    private void FixedUpdate()
    {
        _moduleManager?.FixedUpdate();
    }

    private void OnDestroy()
    {
        RemovePatches();
        _moduleManager?.Destroy();
    }

    protected void SetupPatches()
    {
        _harmony ??= new Harmony(Info.Metadata.GUID);
        Type[] patches = [.. _globalPatches];
        if (_moduleManager != null)
        {
            foreach (Module module in _moduleManager.GetAllModules())
            {
                patches = [.. patches, .. module.GetPatches()];
            }
        }

        foreach (var patchType in patches)
        {
            try
            {
                _harmony.PatchAll(patchType);
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to patch {patchType.Name}: {e}");
            }
        }
    }

    protected void RemovePatches()
    {
        if (_harmony == null) return;
        try
        {
            _harmony.UnpatchSelf();
            _harmony = null;
        }
        catch (Exception e)
        {
            Log.LogError($"Failed to remove patches: {e}");
        }
    }

    protected void SetupEvents()
    {
        SceneChangeListener.Initialize();
    }

    protected void SetupModules()
    {
        _moduleManager = new ModuleManager();
        _moduleManager.Initialize();
    }

}
