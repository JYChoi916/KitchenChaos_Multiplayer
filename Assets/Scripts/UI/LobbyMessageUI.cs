using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        closeButton.onClick.AddListener(() => {
            Hide();
        });
    }

    void Start()
    {
        KitchenGameMultiplayer.Instance.OnFailedToJoinGame += KitchenGameMultiplayer_OnFailedToJoinGame;
        KitchenGameLobby.Instance.OnCreateLobbyStarted += KitchenGameLobby_OnCreateLobbyStarted;
        KitchenGameLobby.Instance.OnCreateLobbyFailed += KitchenGameLobby_OnCreateLobbyFailed;
        KitchenGameLobby.Instance.OnJoinStarted += KitchenGameLobby_OnJoinStarted;
        KitchenGameLobby.Instance.OnQuickJoinFailed += KitchenGameLobby_OnQuickJoinFailed;
        KitchenGameLobby.Instance.OnJoinFailed += KitchenGameLobby_OnJoinFailed;
        Hide();
    }

    private void KitchenGameLobby_OnJoinStarted(object sender, EventArgs e)
    {
        ShowMessage("Joining Lobby...");
    }

    private void KitchenGameLobby_OnQuickJoinFailed(object sender, EventArgs e)
    {
        ShowMessage("Could not find a lobby to join!");
    }

    private void KitchenGameLobby_OnJoinFailed(object sender, EventArgs e)
    {
        ShowMessage("Failed to join lobby!");
    }

    private void KitchenGameLobby_OnCreateLobbyStarted(object sender, EventArgs e)
    {
        ShowMessage("Creating Lobby...");
    }

    private void KitchenGameLobby_OnCreateLobbyFailed(object sender, EventArgs e)
    {
        ShowMessage("Failed to create lobby!");
    }

    private void KitchenGameMultiplayer_OnFailedToJoinGame(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
        {
            ShowMessage("Failed to connect");
        }
        else
        {
            ShowMessage(NetworkManager.Singleton.DisconnectReason);
        }
    }

    public void Show() 
    {
        gameObject.SetActive(true);
    }

    public void Hide() 
    {
        gameObject.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        messageText.text = message;
        Show();
    }

    private void OnDestroy()
    {
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.OnFailedToJoinGame -= KitchenGameMultiplayer_OnFailedToJoinGame;
        }
    }
}
