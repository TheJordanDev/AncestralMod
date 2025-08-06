using BepInEx.Configuration;
using UnityEngine;

namespace AncestralMod;

public static class ConfigHandler
{
    private static ConfigFile config = null!;

    public static ConfigEntry<KeyCode> OpenConfigEditor { get; private set; } = null!;

    // Better Bugle settings
    public static ConfigEntry<float> BugleVolume { get; private set; } = null!;

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
            "Keyboard key used to open the configuration editor"
        );

        // Better Bugle settings
        BugleVolume = config.Bind(
            "Better Bugle",
            "BugleVolume",
            0.5f,
            new ConfigDescription(
                "Volume of the bugle sound",
                new AcceptableValueRange<float>(0f, 1f)
            )
        );

        // Stashed Bugle settings

        ToggleBugle = config.Bind(
            "Control",
            "ToggleBugle",
            KeyCode.V,
            "Keyboard key used to spawn or destroy held bugle"
        );

        // Seed Override settings

        EnableSeedOverride = config.Bind(
            "Seed Override",
            "EnableSeedOverride",
            false,
            "Enable or disable the seed override feature"
        );

        SeedOverride = config.Bind(
            "Seed Override",
            "SeedOverride",
            3673,
            "The seed to use when the seed override is enabled"
        );

    }
}
