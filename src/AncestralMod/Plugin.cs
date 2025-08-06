using System;
using System.IO;
using System.Reflection;
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

    private static readonly Type[] PatchTypes = [
        typeof(Patches.PassportPatch),
        typeof(Patches.KnockoutPatch),
        typeof(Patches.BetterBuglePatch),
    ];

    private void Awake()
    {
        Instance = this;

        ConfigHandler.Initialize(Config);
        Log = Logger;
        Debug.Log("AncestralMod is starting...");

        SetupPatches();
        SetupEvents();
        SetupModules();
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
        foreach (var patchType in PatchTypes)
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
