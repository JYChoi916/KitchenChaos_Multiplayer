using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

    private Lobby joinedLobby;
    public Lobby GetLobby() => joinedLobby;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication()
    {
        if(UnityServices.State != ServicesInitializationState.Initialized)
        {        
            InitializationOptions initializationOption = new InitializationOptions();
            initializationOption.SetProfile(Random.Range(0, 100000).ToString());

            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.MAX_PLAYER_COUNT, new CreateLobbyOptions{
                IsPrivate = isPrivate,
            });

            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        } 
        catch (LobbyServiceException e) {
            Debug.LogError(e);
        }
    }

    public async void QuickJoin() {
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
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
            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            Debug.LogError(e);
        }
    }
}
