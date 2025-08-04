using BepInEx.Configuration;

namespace AncestralMod;

public static class ConfigHandler
{
    private static ConfigFile config = null!;

    public static ConfigEntry<float> KnockerMinVelocity { get; private set; } = null!;

    public static void Initialize(ConfigFile configFile)
    {
        config = configFile;
    }
}
