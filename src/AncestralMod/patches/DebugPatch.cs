using HarmonyLib;

namespace AncestralMod.Patches;

[HarmonyPatch(typeof(Character), "OnJump")]
public class JumpPassoutPatch
{
    [HarmonyPostfix]
    static void Postfix(Character __instance)
    {
		if (Helper.IsOnIsland() && __instance.CheckStand())
		{
			__instance.Fall(2f);
		}
    }
}