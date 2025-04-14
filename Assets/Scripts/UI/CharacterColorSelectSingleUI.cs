using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour
{
    [SerializeField] private int colorId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => 
        {
            KitchenGameMultiplayer.Instance.ChangePlayerColor(colorId);
        });

        KitchenGameMultiplayer.Instance.OnPlayerDataListChanged += KitchenGameMultiplayer_OnPlayerDataListChanged;
    }

    private void KitchenGameMultiplayer_OnPlayerDataListChanged(object sender, EventArgs e)
    {
        UpdateIsSelected();
    }

    void Start()
    {
        image.color = KitchenGameMultiplayer.Instance.GetPlayerColor(colorId);
        UpdateIsSelected();
    }

    private void UpdateIsSelected()
    {
        if (KitchenGameMultiplayer.Instance.GetPlayerData().colorId == colorId)
        {
            selectedGameObject.SetActive(true);
        }
        else
        {
            selectedGameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataListChanged -= KitchenGameMultiplayer_OnPlayerDataListChanged;        
    }
}
