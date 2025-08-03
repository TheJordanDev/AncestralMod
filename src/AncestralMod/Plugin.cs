using System;
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

    private static readonly Type[] PatchTypes = [
        typeof(Patches.JumpPassoutPatch),
        typeof(Patches.KnockoutPatch)
    ];

    private void Awake()
    {
        Instance = this;

		ConfigHandler.Initialize(Config);
        Log = Logger;
        Debug.Log("AncestralMod is starting...");

        SetupPatches();
    }

    private void Start()
    {

    }

    private void OnDestroy()
    {
        RemovePatches();
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
}
