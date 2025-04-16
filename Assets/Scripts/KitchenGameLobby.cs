using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }



    private Lobby joinedLobby;
    public Lobby GetLobby() => joinedLobby;
    private float heartbeatTimer;

    private float listLobbiesTimer;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeUnityAuthentication();
    }

    private void Update()
    {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies()
    {
        if (joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
        {
            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f)
            {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
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
    
    private bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void InitializeUnityAuthentication()
    {
        Debug.Log("Initializing Unity Authentication...");
        Debug.Log("UnityServices.State : " + UnityServices.State);
        if(UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOption = new InitializationOptions();
            initializationOption.SetProfile(UnityEngine.Random.Range(0, 100000).ToString());

            await UnityServices.InitializeAsync();

            if (this == null)
                return;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously to Unity Authentication : " + AuthenticationService.Instance.PlayerId);
            Application.wantsToQuit += Application_wantsToQuit;
        }

        return;
    }

    private bool Application_wantsToQuit()
    {
        bool canQuit = joinedLobby == null;
        if (joinedLobby != null)
        {
            StartCoroutine(LeaveLobbyBeforeQuit());
        }
        return canQuit;
    }

    IEnumerator LeaveLobbyBeforeQuit()
    {
        var task = ForceCleanupLobbyAsync();
        yield return new WaitUntil(() => task.IsCompleted);
        Application.Quit();
    }

    private async void ListLobbies() {
        try {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions {
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs {
                lobbyList = queryResponse.Results
            });
        }
        catch (LobbyServiceException e) {
            Debug.LogWarning(e);
        }
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

    public async void JoinWithId(string lobbyId) 
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            if (this == null)
                return;

            KitchenGameMultiplayer.Instance.StartClient();
        }
        catch (LobbyServiceException e) {
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
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

    private async Task ForceCleanupLobbyAsync()
    {
        if (joinedLobby == null) return;

        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                Debug.Log("Successfully deleted lobby during quit");
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("Successfully left lobby during quit");
            }
            joinedLobby = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to cleanup lobby during quit: {e.Message}");
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

    void OnDestroy()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            Application.wantsToQuit -= Application_wantsToQuit;
        }
    }
}
