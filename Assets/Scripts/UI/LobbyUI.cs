using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    

    void Awake()
    {
        mainMenuButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        createLobbyButton.onClick.AddListener(() => {
            lobbyCreateUI.Show();
        });

        quickJoinButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.QuickJoin();
        });

        joinCodeButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });
    }
}
