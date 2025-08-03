using HarmonyLib;

namespace AncestralMod.Patches;

class KnockoutPatch
{
	[HarmonyPatch(typeof(Item), "Awake")]
	[HarmonyPostfix]
	static void Postfix(Item __instance)
	{
		__instance.gameObject.AddComponent<Knocker>();
		Debug.Log("Knocker component added to Item: " + __instance.name);
	}
}