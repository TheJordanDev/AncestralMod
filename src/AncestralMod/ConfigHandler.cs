using BepInEx.Configuration;
using UnityEngine;

namespace AncestralMod;

public static class ConfigHandler
{
    private static ConfigFile config = null!;

    public static ConfigEntry<KeyCode> ToggleBugle { get; private set; } = null!;

    public static void Initialize(ConfigFile configFile)
    {
        config = configFile;

        ToggleBugle = config.Bind(
            "Control",
            "ToggleBugle",
            KeyCode.V,
            "Keyboard key used to spawn or destroy held bugle"
        );
    }
}
