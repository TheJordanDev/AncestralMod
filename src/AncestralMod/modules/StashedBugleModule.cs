using UnityEngine;
using Zorro.Core;

namespace AncestralMod.Modules;

class StashedBugleModule : Module
{
	public override string ModuleName => "StashedBugle";

	private readonly string _bugleItemName = "Bugle";

	public override void Initialize()
	{
		base.Initialize();
	}

	public override void Update()
	{
		if (!Input.GetKeyDown(ConfigHandler.ToggleBugle.Value)) return;

		Character localCharacter = Character.localCharacter;
		if (localCharacter == null) return;

		Item heldItem = localCharacter.data.currentItem;
		if (heldItem != null && heldItem.UIData.itemName == _bugleItemName)
		{
			localCharacter.refs.items.DestroyHeldItemRpc();
			localCharacter.player.EmptySlot(localCharacter.refs.items.currentSelectedSlot);
			localCharacter.player.RPCRemoveItemFromSlot(localCharacter.refs.items.currentSelectedSlot.Value);
		}
		else if (heldItem == null)
		{
			ItemSlot? withBugleSlot = null;
			for (int i = 0; i < CharacterItems.MAX_SLOT; i++)
			{
				ItemSlot itemSlot = localCharacter.player.GetItemSlot((byte)i);
				if (itemSlot == null || itemSlot.prefab == null || itemSlot.prefab.UIData == null || itemSlot.prefab.UIData.itemName != _bugleItemName) continue;
				withBugleSlot = itemSlot;
				break;
			}

			if (withBugleSlot == null) localCharacter.refs.items.SpawnItemInHand(_bugleItemName);
			else localCharacter.refs.items.EquipSlot(Optionable<byte>.Some(withBugleSlot.itemSlotID));
		}
	}
}