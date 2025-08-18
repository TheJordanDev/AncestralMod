using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace AncestralMod.Modules;

public class ModuleManager
{
    private readonly Dictionary<string, Module> _modules = new();
    private readonly List<Module> _updateModules = new();
    private readonly List<Module> _fixedUpdateModules = new();

    // Define which module types to automatically load
    private static readonly Type[] ModuleTypes = [
        typeof(BagsForEveryoneModule),
        typeof(ReconnectCatchupModule),
        typeof(StashedBugleModule),
        typeof(ConfigEditorModule),
        typeof(BetterBugleModule),
        typeof(EasyBackpackModule),
        // typeof(BetterRoomModule)
    ];

    public void Initialize()
    {
        AddNetworkEventListener();
        LoadModules();
        Plugin.Log.LogInfo($"ModuleManager initialized with {_modules.Count} modules");
    }

    public List<Module> GetAllModules()
    {
        return [.. _modules.Values];
    }

    private void AddNetworkEventListener()
    {
        GameObject listenerObject = new("PhotonNetworkEventListener");
        UnityEngine.Object.DontDestroyOnLoad(listenerObject);
        listenerObject.AddComponent<PhotonNetworkEventListener>();
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
}

class PhotonNetworkEventListener : MonoBehaviourPunCallbacks
{
	public static PhotonNetworkEventListener? Instance { get; private set; }

    public override void OnEnable()
    {
        if (Instance != null) return;
        Instance = this;
        base.OnEnable();
	}

    public event Action? Connected;
    public event Action? LeftRoom;
    public event Action<Photon.Realtime.Player>? MasterClientSwitched;
    public event Action<short, string>? CreateRoomFailed;
    public event Action<short, string>? JoinRoomFailed;
    public event Action? CreatedRoom;
    public event Action? JoinedLobby;
    public event Action? LeftLobby;
    public event Action<DisconnectCause>? Disconnected;
    public event Action<RegionHandler>? RegionListReceived;
    public event Action<List<RoomInfo>>? RoomListUpdate;
    public event Action? JoinedRoom;
    public event Action<Photon.Realtime.Player>? PlayerEnteredRoom;
    public event Action<Photon.Realtime.Player>? PlayerLeftRoom;
    public event Action<short, string>? JoinRandomFailed;
    public event Action? ConnectedToMaster;
    public event Action<ExitGames.Client.Photon.Hashtable>? RoomPropertiesUpdate;
    public event Action<Photon.Realtime.Player, ExitGames.Client.Photon.Hashtable>? PlayerPropertiesUpdate;
    public event Action<List<FriendInfo>>? FriendListUpdate;
    public event Action<Dictionary<string, object>>? CustomAuthenticationResponse;
    public event Action<string>? CustomAuthenticationFailed;
    public event Action<OperationResponse>? WebRpcResponse;
    public event Action<List<TypedLobbyInfo>>? LobbyStatisticsUpdate;
    public event Action<ErrorInfo>? ErrorInfoEvent;

    // Register methods
    public void RegisterOnConnected(Action handler) => Connected += handler;
    public void RegisterOnLeftRoom(Action handler) => LeftRoom += handler;
    public void RegisterOnMasterClientSwitched(Action<Photon.Realtime.Player> handler) => MasterClientSwitched += handler;
    public void RegisterOnCreateRoomFailed(Action<short, string> handler) => CreateRoomFailed += handler;
    public void RegisterOnJoinRoomFailed(Action<short, string> handler) => JoinRoomFailed += handler;
    public void RegisterOnCreatedRoom(Action handler) => CreatedRoom += handler;
    public void RegisterOnJoinedLobby(Action handler) => JoinedLobby += handler;
    public void RegisterOnLeftLobby(Action handler) => LeftLobby += handler;
    public void RegisterOnDisconnected(Action<DisconnectCause> handler) => Disconnected += handler;
    public void RegisterOnRegionListReceived(Action<RegionHandler> handler) => RegionListReceived += handler;
    public void RegisterOnRoomListUpdate(Action<List<RoomInfo>> handler) => RoomListUpdate += handler;
    public void RegisterOnJoinedRoom(Action handler) => JoinedRoom += handler;
    public void RegisterOnPlayerEnteredRoom(Action<Photon.Realtime.Player> handler) => PlayerEnteredRoom += handler;
    public void RegisterOnPlayerLeftRoom(Action<Photon.Realtime.Player> handler) => PlayerLeftRoom += handler;
    public void RegisterOnJoinRandomFailed(Action<short, string> handler) => JoinRandomFailed += handler;
    public void RegisterOnConnectedToMaster(Action handler) => ConnectedToMaster += handler;
    public void RegisterOnRoomPropertiesUpdate(Action<ExitGames.Client.Photon.Hashtable> handler) => RoomPropertiesUpdate += handler;
    public void RegisterOnPlayerPropertiesUpdate(Action<Photon.Realtime.Player, ExitGames.Client.Photon.Hashtable> handler) => PlayerPropertiesUpdate += handler;
    public void RegisterOnFriendListUpdate(Action<List<FriendInfo>> handler) => FriendListUpdate += handler;
    public void RegisterOnCustomAuthenticationResponse(Action<Dictionary<string, object>> handler) => CustomAuthenticationResponse += handler;
    public void RegisterOnCustomAuthenticationFailed(Action<string> handler) => CustomAuthenticationFailed += handler;
    public void RegisterOnWebRpcResponse(Action<OperationResponse> handler) => WebRpcResponse += handler;
    public void RegisterOnLobbyStatisticsUpdate(Action<List<TypedLobbyInfo>> handler) => LobbyStatisticsUpdate += handler;
    public void RegisterOnErrorInfo(Action<ErrorInfo> handler) => ErrorInfoEvent += handler;

    // Override and invoke actions
    public override void OnConnected()
    {
        Connected?.Invoke();
    }

    public override void OnLeftRoom()
    {
        LeftRoom?.Invoke();
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        MasterClientSwitched?.Invoke(newMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        CreateRoomFailed?.Invoke(returnCode, message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        JoinRoomFailed?.Invoke(returnCode, message);
    }

    public override void OnCreatedRoom()
    {
        CreatedRoom?.Invoke();
    }

    public override void OnJoinedLobby()
    {
        JoinedLobby?.Invoke();
    }

    public override void OnLeftLobby()
    {
        LeftLobby?.Invoke();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Disconnected?.Invoke(cause);
    }

    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        RegionListReceived?.Invoke(regionHandler);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        RoomListUpdate?.Invoke(roomList);
    }

    public override void OnJoinedRoom()
    {
        JoinedRoom?.Invoke();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        PlayerEnteredRoom?.Invoke(newPlayer);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        PlayerLeftRoom?.Invoke(otherPlayer);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        JoinRandomFailed?.Invoke(returnCode, message);
    }

    public override void OnConnectedToMaster()
    {
        ConnectedToMaster?.Invoke();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        RoomPropertiesUpdate?.Invoke(propertiesThatChanged);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        PlayerPropertiesUpdate?.Invoke(targetPlayer, changedProps);
    }

    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        FriendListUpdate?.Invoke(friendList);
    }

    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        CustomAuthenticationResponse?.Invoke(data);
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        CustomAuthenticationFailed?.Invoke(debugMessage);
    }

    public override void OnWebRpcResponse(OperationResponse response)
    {
        WebRpcResponse?.Invoke(response);
    }

    public override void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        LobbyStatisticsUpdate?.Invoke(lobbyStatistics);
    }

    public override void OnErrorInfo(ErrorInfo errorInfo)
    {
        ErrorInfoEvent?.Invoke(errorInfo);
    }

}