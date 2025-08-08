using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace AncestralMod.Modules;

class ReconnectCatchupModule : Module
{
	public override string ModuleName => "ReconnectCatchup";

	public static ReconnectCatchupModule? Instance { get; private set; }

	public override void Initialize()
	{
		if (Instance != null) return;
		Instance = this;
		base.Initialize();
		Debug.Log(PhotonNetworkEventListener.Instance != null ? "PhotonNetworkEventListener is available." : "PhotonNetworkEventListener is not available.");
		PhotonNetworkEventListener.Instance?.RegisterOnPlayerEnteredRoom(OnPlayerEnteredRoom);
	}

	private void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		Debug.Log($"Player {newPlayer.NickName} entered the room.");
		if (Helper.IsMasterClient())
		{
			Debug.Log($"Master client detected: {PhotonNetwork.LocalPlayer.NickName}");
			Plugin.Instance.StartCoroutine(WarpPlayerCoroutine(newPlayer));
		}
	}

	private IEnumerator WarpPlayerCoroutine(Photon.Realtime.Player joiningPlayer)
	{
		yield return new WaitForSeconds(2f);
		const int maxTries = 30;
		int tries = 0;
		Character? joiningCharacter = null;
		while (joiningCharacter == null && tries < maxTries)
		{
			joiningCharacter = PlayerHandler.GetPlayerCharacter(joiningPlayer);
			tries++;
			if (joiningCharacter == null) yield return new WaitForSeconds(1f);
		}

		if (joiningCharacter == null) yield break;

		Character? lowestCharacter = GetLowestCharacterFor(joiningCharacter);
		Debug.Log($"Lowest character found: {lowestCharacter?.name ?? "None"}");
		if (lowestCharacter == null) yield break;
		joiningCharacter.photonView.RPC("WarpPlayerRPC", RpcTarget.All, lowestCharacter.Head + Vector3.up, false);
		Debug.Log($"Warped {joiningCharacter.name} to {lowestCharacter.name}'s position after delay.");
	}

	private Character? GetLowestCharacterFor(Character joiningCharacter)
	{
		float lowest = float.MaxValue;
		Character? lowestCharacter = null;
		foreach (Character character in Character.AllCharacters)
		{
			if (character == null || character.transform == null || character.data.dead) continue;
			if (character == joiningCharacter) continue;
			if (character.transform.position.y >= lowest) continue;
			lowest = character.transform.position.y;
			lowestCharacter = character;
		}
		return lowestCharacter;
	}

}