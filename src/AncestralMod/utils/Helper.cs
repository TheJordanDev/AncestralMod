using System.Collections;
using Photon.Pun;
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

	public static bool IsMasterClient()
	{
		return PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient;
	}

	public static void LogToPlayer(string _message)
	{
		string message;
		if (_message.Contains("<color")) message = _message;
		else message = $"<color=white>{_message}</color>";

		UnityEngine.Object.FindAnyObjectByType<PlayerConnectionLog>()?.SendMessage(message);
	}

	public static IEnumerator WaitUntilPlayerHasCharacter(Photon.Realtime.Player player, System.Action<Character?> onComplete, int maxTries = 30, float waitTime = 1f)
	{
		int tries = 0;
		Character? character = null;
		while (character == null && tries < maxTries)
		{
			character = PlayerHandler.GetPlayerCharacter(player);
			if (character == null)
			{
				tries++;
				yield return new WaitForSeconds(waitTime);
			}
		}
		onComplete?.Invoke(character);
	}

}
