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
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class KitchenGameLobby : MonoBehaviour
{
    public static KitchenGameLobby Instance { get; private set; }

    private const string KEY_RELAY_JOIN_CODE = "RealyJoinCode";

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

        if (SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString())
        {
            HandlePeriodicListLobbies();
        }
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
            string profile = UnityEngine.Random.Range(0, 100000).ToString();

            InitializationOptions initializationOption = new InitializationOptions();
            initializationOption.SetProfile(profile);

            await UnityServices.InitializeAsync();

            if (this == null)
                return;

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                AuthenticationService.Instance.SwitchProfile(profile);
            }
            
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
        yield return new WaitUntil(() => task.IsCompleted || joinedLobby == null);
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

    private async Task<Allocation> AllocateRelay() 
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(KitchenGameMultiplayer.MAX_PLAYER_COUNT - 1);

            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning(e);
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            return relayJoinCode;
        }
        catch (RelayServiceException e) {
            Debug.LogWarning(e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning(e);
            return default;
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {

            // 일단 로비를 생성
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, KitchenGameMultiplayer.MAX_PLAYER_COUNT, new CreateLobbyOptions{
                IsPrivate = isPrivate,
            });

            // 릴레이 서버에 할당 요청
            Allocation allocation = await AllocateRelay();

            // 해당 릴레이 서버에 조인하기 위한 코드를 요청
            string relayJoinCode = await GetRelayJoinCode(allocation);

            // 만들어진 로비에 릴레이 서버 정보를 추가가
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(allocation.ToRelayServerData("dtls"));

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

            // 로비 데이터로부터 릴레이 서버 조인 코드를 가져옴
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            // 릴레이 서버에 조인하기 위한 할당 요청
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // 릴레이 서버 정보를 네트워크 매니저에 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

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

            // 로비 데이터로부터 릴레이 서버 조인 코드를 가져옴
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            // 릴레이 서버에 조인하기 위한 할당 요청
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // 릴레이 서버 정보를 네트워크 매니저에 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

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

            // 로비 데이터로부터 릴레이 서버 조인 코드를 가져옴
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            // 릴레이 서버에 조인하기 위한 할당 요청
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            // 릴레이 서버 정보를 네트워크 매니저에 설정
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));

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
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to cleanup lobby during quit: {e.Message}");
        }

        joinedLobby = null;
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
