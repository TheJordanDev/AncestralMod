using BepInEx.Configuration;
using UnityEngine;

namespace AncestralMod;

public static class ConfigHandler
{
    public static ConfigFile config { get; private set; } = null!;

    public static ConfigEntry<KeyCode> OpenConfigEditor { get; private set; } = null!;

    // Easy Backpack
    public static ConfigEntry<KeyCode> OpenBackpack { get; private set; } = null!;

    // Better Bugle settings
    public static ConfigEntry<KeyCode> SyncAudioRepository { get; private set; } = null!;
    public static ConfigEntry<float> BugleVolume { get; private set; } = null!;
    public static ConfigEntry<string> BugleSoundAPIURL { get; private set; } = null!;
    public static ConfigEntry<bool> AutoSyncAudioRepository { get; private set; } = null!;
    public static ConfigEntry<string> AudioRepositorySubdirectory { get; private set; } = null!;

    // Stashed Bugle settings
    public static ConfigEntry<KeyCode> ToggleBugle { get; private set; } = null!;

    // Seed Override settings
    public static ConfigEntry<bool> EnableSeedOverride { get; private set; } = null!;
    public static ConfigEntry<int> SeedOverride { get; private set; } = null!;

    public static void Initialize(ConfigFile configFile)
    {
        config = configFile;

        OpenConfigEditor = config.Bind(
            "Key Bindings",
            "OpenConfigEditor",
            KeyCode.F3,
            "Open Configuration Editor UI"
        );

        // Easy Backpack settings
        OpenBackpack = config.Bind(
            "Key Bindings",
            "OpenBackpack",
            KeyCode.B,
            "Open Easy Backpack UI"
        );

        // Better Bugle settings
        BugleVolume = config.Bind(
            "Better Bugle",
            "BugleVolume",
            0.5f,
            new ConfigDescription(
                "Bugle Sound Volume",
                new AcceptableValueRange<float>(0f, 1f)
            )
        );

        BugleSoundAPIURL = config.Bind(
            "Better Bugle",
            "BugleSoundAPIURL",
            "https://audiobank.thejordan.dev/api",
            "Sounds API"
        );

        SyncAudioRepository = config.Bind(
            "Better Bugle",
            "SyncAudioRepository",
            KeyCode.L,
            "Manually sync audio repository from git"
        );

        // Stashed Bugle settings
        ToggleBugle = config.Bind(
            "Control",
            "ToggleBugle",
            KeyCode.V,
            "Give / destroy Bugle"
        );

    }
}
