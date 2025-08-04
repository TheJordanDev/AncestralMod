using HarmonyLib;

namespace AncestralMod.Patches;

class KnockoutPatch
{
	[HarmonyPatch(typeof(Item), "Awake")]
	[HarmonyPostfix]
	static void Postfix(Item __instance)
	{
		if (__instance.itemState == ItemState.Held || __instance.itemState == ItemState.InBackpack)
		{
			return;
		}
		__instance.gameObject.AddComponent<Bonkable>();
	}
}