using System.IO;
using System.Reflection;

namespace AncestralMod;

public static class PathHelper
{
	// Assembly location functions

	/// <summary>
	/// Gets the full file path of the currently executing assembly (the mod DLL)
	/// </summary>
	/// <returns>Full path to the mod DLL file</returns>
	public static string GetModPath()
	{
		return Assembly.GetExecutingAssembly().Location;
	}

	/// <summary>
	/// Gets the directory where the mod DLL is located
	/// </summary>
	/// <returns>Directory path containing the mod DLL</returns>
	public static string GetModDirectory()
	{
		return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
	}

	/// <summary>
	/// Gets just the filename of the mod DLL (without path)
	/// </summary>
	/// <returns>Filename of the mod DLL</returns>
	public static string GetModFileName()
	{
		return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
	}

	/// <summary>
	/// Gets the mod name without the .dll extension
	/// </summary>
	/// <returns>Mod name without extension</returns>
	public static string GetModName()
	{
		return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
	}

	/// <summary>
	/// Gets a path relative to the mod's directory
	/// </summary>
	/// <param name="relativePath">Path relative to mod directory</param>
	/// <returns>Full path combining mod directory with relative path</returns>
	public static string GetModRelativePath(string relativePath)
	{
		return Path.Combine(GetModDirectory(), relativePath);
	}

	/// <summary>
	/// Gets the BepInEx plugins directory (parent of mod directory)
	/// </summary>
	/// <returns>BepInEx plugins directory path</returns>
	public static string GetPluginsDirectory()
	{
		return Path.GetDirectoryName(GetModDirectory()) ?? string.Empty;
	}

	/// <summary>
	/// Gets the BepInEx root directory
	/// </summary>
	/// <returns>BepInEx root directory path</returns>
	public static string GetBepInExDirectory()
	{
		var pluginsDir = GetPluginsDirectory();
		return Path.GetDirectoryName(pluginsDir) ?? string.Empty;
	}

	/// <summary>
	/// Gets the game's root directory
	/// </summary>
	/// <returns>Game root directory path</returns>
	public static string GetGameDirectory()
	{
		var bepInExDir = GetBepInExDirectory();
		return Path.GetDirectoryName(bepInExDir) ?? string.Empty;
	}

	/// <summary>
	/// Creates a directory if it doesn't exist, relative to the mod directory
	/// </summary>
	/// <param name="relativePath">Path relative to mod directory</param>
	/// <returns>Full path to the created directory</returns>
	public static string EnsureModDirectory(string relativePath)
	{
		string fullPath = GetModRelativePath(relativePath);
		Directory.CreateDirectory(fullPath);
		return fullPath;
	}

	/// <summary>
	/// Checks if a file exists relative to the mod directory
	/// </summary>
	/// <param name="relativePath">Path relative to mod directory</param>
	/// <returns>True if file exists</returns>
	public static bool ModFileExists(string relativePath)
	{
		return File.Exists(GetModRelativePath(relativePath));
	}

	/// <summary>
	/// Checks if a directory exists relative to the mod directory
	/// </summary>
	/// <param name="relativePath">Path relative to mod directory</param>
	/// <returns>True if directory exists</returns>
	public static bool ModDirectoryExists(string relativePath)
	{
		return Directory.Exists(GetModRelativePath(relativePath));
	}
}