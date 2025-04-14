using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        readyButton.onClick.AddListener(() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }

    void Start()
    {
        Lobby lobby = KitchenGameLobby.Instance.GetLobby();

        lobbyNameText.text = "Lobby Name : " + lobby.Name;
        lobbyCodeText.text = "Lobby Code : " + lobby.LobbyCode;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
