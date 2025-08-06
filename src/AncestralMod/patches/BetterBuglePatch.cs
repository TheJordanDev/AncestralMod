using System;
using System.Collections;
using System.Collections.Generic;
using AncestralMod.Modules;
using AncestralMod.UI;
using ExitGames.Client.Photon.StructWrapping;
using HarmonyLib;
using UnityEngine;

namespace AncestralMod.Patches;

public class BetterBuglePatch
{

	[HarmonyPatch(typeof(Item), "Start")]
	[HarmonyPostfix]
	static void ItemStartPostfix(Item __instance)
	{
		if (__instance.itemState != ItemState.Held) return;
		if (__instance.UIData == null) return;
		
		if (__instance.TryGetComponent<BugleSFX>(out var bugleSFX))
		{
			Action secondaryAction = OnRightClick;
			Action<float> scrollAction = OnScroll;

			__instance.UIData.hasSecondInteract = true;
			__instance.UIData.hasScrollingInteract = true;

			__instance.OnSecondaryStarted += secondaryAction;
			__instance.OnScrolled += scrollAction;

			__instance.UIData.secondaryInteractPrompt = "SONG_LIST";
			__instance.UIData.scrollInteractPrompt = "CHANGE_SONG";

			__instance.gameObject.AddComponent<BetterBugleSFX>();
		}
	}

	private static void OnRightClick()
	{
		// if (Song.Songs.Count == 0)
		// {
		// 	BetterBugleUI.Instance?.ShowActionbar("No songs available.");
		// 	return;
		// }
		if (BetterBugleModule.IsLoading) return;
		if (!BetterBugleModule.HadConfirmation)
		{
			BetterBugleUI.Instance?.ShowActionbar("Are you sure you want to refresh songs ? Right-click again to reload.");
			BetterBugleModule.HadConfirmation = true;
			Plugin.Instance.StartCoroutine(ResetConfirmation());
			return;
		}
		else
		{
			BetterBugleModule.HadConfirmation = false; // Reset confirmation state
			BetterBugleUI.Instance?.ShowActionbar("Refreshing songs...");
			BetterBugleModule.Instance.GetAudioClips();
		}

	}

	private static IEnumerator ResetConfirmation()
	{
		yield return new WaitForSeconds(2f);
		BetterBugleUI.Instance?.ShowActionbar("No answer, not refreshing songs.");
		BetterBugleModule.HadConfirmation = false;
	}

	private static void OnScroll(float scrollDelta)
	{
		if (BetterBugleModule.IsLoading) return;
		bool isNext = scrollDelta > 0;
		if (Song.Songs.Count == 0)
		{
			BetterBugleUI.Instance?.ShowActionbar("No songs available.");
			return;
		}

		if (isNext && BetterBugleModule.CurrentSongIndex < Song.Songs.Count - 1) BetterBugleModule.CurrentSongIndex++;
		else if (isNext && BetterBugleModule.CurrentSongIndex == Song.Songs.Count - 1) BetterBugleModule.CurrentSongIndex = 0;
		else if (!isNext && BetterBugleModule.CurrentSongIndex > 0) BetterBugleModule.CurrentSongIndex--;
		else BetterBugleModule.CurrentSongIndex = Song.Songs.Count - 1;
		BetterBugleModule.CurrentSongName = Song.GetSongNames_Alphabetically()[BetterBugleModule.CurrentSongIndex];

		Song currentSong = Song.Songs[BetterBugleModule.CurrentSongName];
		BetterBugleUI.Instance?.ShowActionbar($"{BetterBugleModule.CurrentSongIndex + 1} | {currentSong.Name}");
	}

	[HarmonyPatch(typeof(CharacterItems), "Awake")]
	[HarmonyPostfix]
	static void CharacterItemsEquipPostfix(CharacterItems __instance)
	{
		__instance.onSlotEquipped += () =>
		{
			if (__instance.character == null || __instance.character != Character.localCharacter) return;
			Item? currentItem = __instance.character.data.currentItem;
			if (currentItem == null || currentItem.UIData == null) return;
			if (currentItem.itemState != ItemState.Held) return;
			if (currentItem.TryGetComponent<BugleSFX>(out var bugleSFX))
			{
				Song? song = Song.Songs.GetValueOrDefault(BetterBugleModule.CurrentSongName);
				if (song == null) return;
				BetterBugleUI.Instance?.ShowActionbar($"{BetterBugleModule.CurrentSongIndex + 1} | {song.Name}");
			}
		};
	}

	[HarmonyPatch(typeof(BugleSFX), "Update")]
	[HarmonyPostfix]
	static void BugleSFXUpdatePostfix(BugleSFX __instance)
	{
		if (__instance.volume > 0f) __instance.volume = 0;
	}

}

