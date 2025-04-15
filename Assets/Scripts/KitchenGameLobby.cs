using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

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
            initializationOption.SetProfile(Random.Range(0, 100000).ToString());

            await UnityServices.InitializeAsync();

            if (this == null)
                return;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        return;
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
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
            Debug.LogError(e);
        }

        return;
    }

    public async void QuickJoin() {
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            if (this == null)
                return; 

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            Debug.LogError(e);
        }
    }

    public async void JoinWithCode(string lobbyCode) 
    {
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            if (this == null)
                return;

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            Debug.LogError(e);
        }
    }

    private bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
}
