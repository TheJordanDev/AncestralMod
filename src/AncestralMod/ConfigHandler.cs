using BepInEx.Configuration;
using UnityEngine;

namespace AncestralMod;

public static class ConfigHandler
{
    public static ConfigFile config { get; private set; } = null!;

    public static ConfigEntry<KeyCode> OpenConfigEditor { get; private set; } = null!;

    // Better Bugle settings
    public static ConfigEntry<float> BugleVolume { get; private set; } = null!;
    public static ConfigEntry<string> BugleSoundGitRepository { get; private set; } = null!;

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

        BugleSoundGitRepository = config.Bind(
            "Better Bugle",
            "BugleSoundGitRepository",
            "https://gitlab.com/thejordan.dev/peak-ancestralmod-audiobank.git",
            "Sounds git repository URL"
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
