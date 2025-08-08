using System;
using System.Collections.Generic;

namespace AncestralMod.Modules;

public abstract class Module
{
    public abstract string ModuleName { get; }
    public bool IsEnabled { get; set; } = true;

    public virtual void Initialize()
    {
        Plugin.Log.LogInfo($"Module '{ModuleName}' initialized");
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }
    
    public virtual Type[] GetPatches()
    {
        return [];
    }

    public virtual void Destroy()
    {
        Plugin.Log.LogInfo($"Module '{ModuleName}' destroyed");
    }

    public void Enable()
    {
        IsEnabled = true;
        Plugin.Log.LogInfo($"Module '{ModuleName}' enabled");
    }

    public void Disable()
    {
        IsEnabled = false;
        Plugin.Log.LogInfo($"Module '{ModuleName}' disabled");
    }

}
