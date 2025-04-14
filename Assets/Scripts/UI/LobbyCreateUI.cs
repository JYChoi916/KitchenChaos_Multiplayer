using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    [SerializeField] private Button closeUIButton;
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Button createPrivateLobbyButton;
    [SerializeField] private Button createPublicLobbyButton;

    void Awake()
    {
        closeUIButton.onClick.AddListener(() => {
            Hide();
        });

        createPublicLobbyButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, false);
        });

        createPrivateLobbyButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.CreateLobby(lobbyNameInputField.text, true);
        });
    }

    void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
