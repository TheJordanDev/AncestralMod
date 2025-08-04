using HarmonyLib;

namespace AncestralMod.Patches;

public class ThrowablePassportPatch
{
	[HarmonyPatch(typeof(Item), "Start")]
    [HarmonyPostfix]
    static void PostStartFix(Item __instance)
    {
		if (__instance.UIData.itemName.ToLower() == "passport")
		{
			__instance.UIData.canDrop = true;
			__instance.UIData.canThrow = true;
		}
    }
}