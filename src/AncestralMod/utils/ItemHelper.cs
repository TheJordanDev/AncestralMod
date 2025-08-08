using UnityEngine;
using Zorro.Core;

namespace AncestralMod;

class ItemHelper
{
	private static ItemDatabase Database => SingletonAsset<ItemDatabase>.Instance;

	public static Item FindItemByName(string itemName, out Item? item)
	{
		item = Database.Objects.Find(item => item.name.Equals(itemName, System.StringComparison.OrdinalIgnoreCase));
		return item;
	}
}