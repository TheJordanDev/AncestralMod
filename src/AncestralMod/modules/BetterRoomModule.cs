using System.Collections;
using AncestralMod.UI;
using Photon.Pun;
using UnityEngine;

namespace AncestralMod.Modules;

class BetterRoomModule : Module
{
	public override string ModuleName => "BetterRoom";

	public static BetterRoomModule? Instance { get; private set; }

	public override void Initialize()
	{
		if (Instance != null) return;
		Instance = this;
		base.Initialize();

		new GameObject("BetterRoomUI").AddComponent<BetterRoomUI>();
	}

}