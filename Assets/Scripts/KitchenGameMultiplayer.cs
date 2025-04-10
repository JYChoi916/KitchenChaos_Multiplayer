using Unity.Netcode;
using UnityEngine;

public class KitchenGameMultiplayer : NetworkBehaviour
{
    public static KitchenGameMultiplayer Instance { get; private set; }

    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference parentNOReference)
    {
        KitchenObjectSO kitchenObjectSO = kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);
        NetworkObject kitchenNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenNetworkObject.Spawn(true); // Spawn the object on the network

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        parentNOReference.TryGet(out NetworkObject kitchenObjectParentNO);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNO.GetComponent<IKitchenObjectParent>();        
        
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }


    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNOReference) {
        kitchenObjectNOReference.TryGet(out NetworkObject kitchenObjectNO);
        KitchenObject kitchenObject = kitchenObjectNO.GetComponent<KitchenObject>();

        ClearKitchenObjectClientRpc(kitchenObjectNOReference);

        kitchenObject.DestroySelf();
    }

    [ClientRpc]
    private void ClearKitchenObjectClientRpc(NetworkObjectReference kitchenObjectNOReference)
    {
        kitchenObjectNOReference.TryGet(out NetworkObject kitchenObjectNO);
        KitchenObject kitchenObject = kitchenObjectNO.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }
}
