using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;



    private Lobby joinedLobby;
    private float heartbeatTimer;
    public Lobby GetLobby() => joinedLobby;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUnityAuthentication();
    }

    private void Update()
    {
        HandleHeartbeat();
    }

    private void HandleHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f) 
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void InitializeUnityAuthentication()
    {
        if(UnityServices.State != ServicesInitializationState.Initialized)
        {        
            InitializationOptions initializationOption = new InitializationOptions();
            initializationOption.SetProfile(UnityEngine.Random.Range(0, 100000).ToString());

            await UnityServices.InitializeAsync();

            if (this == null)
                return;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        return;
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.MAX_PLAYER_COUNT, new CreateLobbyOptions{
                IsPrivate = isPrivate,
            });

            if (this == null)
                return;

            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        } 
        catch (LobbyServiceException e) {
            Debug.LogWarning(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }

        return;
    }

    public async void QuickJoin() {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (this == null)
                return; 
            
            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogWarning(e);
        }
    }

    public async void JoinWithCode(string lobbyCode) 
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            if (this == null)
                return;

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogWarning(e);
        }
    }

    private bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void DeleteLobby() {
        if (joinedLobby != null)
        {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            }
            catch (LobbyServiceException e) {
                Debug.LogWarning(e);
            }
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null)
        {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
            }
            catch (LobbyServiceException e) {
                Debug.LogWarning(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost())
        {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e) {
                Debug.LogWarning(e);
            }
        }
    }
}
