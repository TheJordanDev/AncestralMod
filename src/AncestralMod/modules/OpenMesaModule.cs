using System;

namespace AncestralMod.Modules;

class OpenMesaModule : Module
{
	public override string ModuleName => "OpenMesa";

	public static OpenMesaModule? Instance { get; private set; }

	public override void Initialize()
	{
		if (Instance != null) return;
		Instance = this;
		base.Initialize();
	}

	public override Type[] GetPatches()
	{
		return [typeof(Patches.OpenMesaPatch)];
	}


}