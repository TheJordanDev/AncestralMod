using System;
using System.Collections.Generic;
using System.Linq;

namespace AncestralMod.Modules;

public class ModuleManager
{
    private readonly Dictionary<string, Module> _modules = new();
    private readonly List<Module> _updateModules = new();
    private readonly List<Module> _fixedUpdateModules = new();

    // Define which module types to automatically load
    private static readonly Type[] ModuleTypes = [
        typeof(StashedBugleModule),
    ];

    public void Initialize()
    {
        LoadModules();
        Plugin.Log.LogInfo($"ModuleManager initialized with {_modules.Count} modules");
    }

    private void LoadModules()
    {
        foreach (Type moduleType in ModuleTypes)
        {
            try
            {
                Module module = (Module)Activator.CreateInstance(moduleType)!;                
                module.Initialize();
                _modules[module.ModuleName] = module;
                
                if (HasOverriddenMethod(moduleType, nameof(Module.Update)))
                {
                    _updateModules.Add(module);
                }
                
                if (HasOverriddenMethod(moduleType, nameof(Module.FixedUpdate)))
                {
                    _fixedUpdateModules.Add(module);
                }
                
                Plugin.Log.LogInfo($"Loaded module: {module.ModuleName}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to load module {moduleType.Name}: {e}");
            }
        }
    }

    private static bool HasOverriddenMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName);
        return method != null && method.DeclaringType != typeof(Module);
    }

    public void Update()
    {
        foreach (Module module in _updateModules)
        {
            if (module.IsEnabled)
            {
                try
                {
                    module.Update();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error in module '{module.ModuleName}' Update: {e}");
                }
            }
        }
    }

    public void FixedUpdate()
    {
        foreach (Module module in _fixedUpdateModules)
        {
            if (module.IsEnabled)
            {
                try
                {
                    module.FixedUpdate();
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Error in module '{module.ModuleName}' FixedUpdate: {e}");
                }
            }
        }
    }

    public void Destroy()
    {
        foreach (Module module in _modules.Values)
        {
            try
            {
                module.Destroy();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error destroying module '{module.ModuleName}': {e}");
            }
        }
        
        _modules.Clear();
        _updateModules.Clear();
        _fixedUpdateModules.Clear();
    }

    // Module access methods
    public T? GetModule<T>() where T : Module
    {
        return _modules.Values.OfType<T>().FirstOrDefault();
    }

    public Module? GetModule(string moduleName)
    {
        _modules.TryGetValue(moduleName, out Module? module);
        return module;
    }

    public void EnableModule(string moduleName)
    {
        GetModule(moduleName)?.Enable();
    }

    public void DisableModule(string moduleName)
    {
        GetModule(moduleName)?.Disable();
    }

    public void EnableModule<T>() where T : Module
    {
        GetModule<T>()?.Enable();
    }

    public void DisableModule<T>() where T : Module
    {
        GetModule<T>()?.Disable();
    }

    public IEnumerable<Module> GetAllModules()
    {
        return _modules.Values;
    }
}
