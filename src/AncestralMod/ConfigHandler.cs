using BepInEx.Configuration;

namespace AncestralMod;

public static class ConfigHandler
{
    private static ConfigFile config = null!;

    // Audio Streaming Configuration
    public static ConfigEntry<float> KnockerMinVelocity { get; private set; } = null!;

    public static void Initialize(ConfigFile configFile)
    {
        config = configFile;
        
        KnockerMinVelocity = config.Bind(
			"Knockout",
			"MinVelocityToKO",
			17f,
			"Minimum velocity for an item to get knocked out."
		);
    }
}
