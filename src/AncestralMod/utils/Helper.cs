using UnityEngine;
using UnityEngine.SceneManagement;

namespace AncestralMod;

public static class Helper
{

	public static bool IsOnIsland()
	{
		return SceneManager.GetActiveScene().name.ToLower().StartsWith("level_") || SceneManager.GetActiveScene().name == "WilIsland";
	}

	public static float MouseScrollDelta()
	{
		return Input.mouseScrollDelta.y;
	}

}
