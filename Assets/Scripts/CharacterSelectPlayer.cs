using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyGameObject;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshPro playerNameText;

    void Awake()
    {
        kickButton.onClick.AddListener(() => 
        {
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.playerId.ToString());
            KitchenGameMultiplayer.Instance.DisconnectPlayer(playerData.clientId);
        });

        KitchenGameMultiplayer.Instance.OnPlayerDataListChanged += KitchenGameMultiplayer_OnPlayerDataListChanged;
    }

    void Start()
    {
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;

        UpdatePlayer();
    }

    private void CharacterSelectReady_OnReadyChanged(object sender, EventArgs e)
    {
        UpdatePlayer();
    }

    private void KitchenGameMultiplayer_OnPlayerDataListChanged(object sender, EventArgs e)
    {
        Debug.Log("KitchenGameMultiplayer_OnPlayerDataListChanged (Sender is : " + sender + ")");
        UpdatePlayer();
    }

    private void UpdatePlayer()
    {
        if (KitchenGameMultiplayer.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Debug.Log($"Player {playerIndex} is connected.");
            Show();

            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));
            playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
            playerNameText.text = playerData.playerName.ToString();

            if (NetworkManager.Singleton.IsHost)
            {
                kickButton.gameObject.SetActive(playerData.clientId != NetworkManager.Singleton.LocalClientId);
            }
            else 
            {
                // this player is not the host
                kickButton.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log($"Player {playerIndex} is not connected.");
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChanged -= KitchenGameMultiplayer_OnPlayerDataListChanged;
        CharacterSelectReady.Instance.OnReadyChanged -= CharacterSelectReady_OnReadyChanged;
    }
}
