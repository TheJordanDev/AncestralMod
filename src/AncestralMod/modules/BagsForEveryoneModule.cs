using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AncestralMod.Modules;

class BagsForEveryoneModule : Module
{
	public override string ModuleName => "BagsForEveryone";

	public static BagsForEveryoneModule? Instance { get; private set; }

	private readonly string bagItemName = "BACKPACK";
	private List<int> playersWithBags = new();


	public override void Initialize()
	{
		if (Instance != null) return;
		Instance = this;
		playersWithBags.Clear();
		SceneManager.sceneLoaded += OnSceneLoaded;
		PhotonNetworkEventListener.Instance?.RegisterOnPlayerEnteredRoom(OnPlayerEnteredRoom);
		base.Initialize();
	}

	private static void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		if (Helper.IsMasterClient() && Helper.IsOnIsland())
		{
			if (Instance?.playersWithBags.Contains(newPlayer.ActorNumber) == false)
			{
				Plugin.Instance.StartCoroutine(Instance?.GrantBagToLatePlayer(newPlayer));
			}
		}
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (Helper.IsMasterClient() && Helper.IsOnIsland())
		{
			Instance?.playersWithBags.Clear();
			Plugin.Instance.StartCoroutine(Instance?.GrantBagsToAllPlayers());
		}
	}

	private IEnumerator GrantBagsToAllPlayers()
	{
		yield return new WaitForSeconds(2f);

		foreach (Photon.Realtime.Player photonPlayer in PhotonNetwork.PlayerList)
		{
			if (!playersWithBags.Contains(photonPlayer.ActorNumber))
			{
				Character character = PlayerHandler.GetPlayerCharacter(photonPlayer);
				Player player = PlayerHandler.GetPlayer(photonPlayer);
				if (character == null || player == null) continue;
				GiveBagToPlayer(player, character);
			}
		}
		Debug.Log("Bags granted to all players");
	}

	private IEnumerator GrantBagToLatePlayer(Photon.Realtime.Player newPlayer)
	{
		Character? newCharacter = null;
		yield return Helper.WaitUntilPlayerHasCharacter(newPlayer, (foundCharacter) => newCharacter = foundCharacter);
		if (newCharacter == null) yield break;
		Player player = PlayerHandler.GetPlayer(newPlayer);
		if (newCharacter == null || player == null) yield break;
		if (Instance?.GiveBagToPlayer(player, newCharacter) == true)
		{
			Instance.playersWithBags.Add(newPlayer.ActorNumber);
			Debug.Log($"Granted bag to {newPlayer.NickName}");
		}
	}

	private bool GiveBagToPlayer(Player player, Character character)
	{
		if (player == null || character == null) return false;
		bool hasBag = player.backpackSlot.hasBackpack;
		if (hasBag) return false;
		ItemHelper.FindItemByName(bagItemName, out Item? backpackItem);
		if (backpackItem == null) return false;
		bool success = player.AddItem(backpackItem.itemID, null, out ItemSlot slot);
		return success && slot is BackpackSlot;
	}

}