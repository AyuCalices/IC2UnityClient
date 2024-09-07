using LockstepNetworking;
using SaveLoadSystem.Utility;
using UnityEngine;

public class IdentificationTest : MonoBehaviour
{
    [SerializeField] private NetworkObject prefab;
    
    public string text;

    [ContextMenu("Perform Reference Event")]
    private void PerformReferenceEvent()
    {
        UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true).RaiseLockstepEvent(new EventData(GetComponent<NetworkObject>()));
    }

    [ContextMenu("Test Instantiate")]
    private void DuplicateInstantiate()
    {
        Instantiate(gameObject);
    }
    
    [ContextMenu("Network Instantiate")]
    private void NetworkInstantiate()
    {
        var originID = prefab.SceneGuid;
        var networkObject = Instantiate(prefab);
        var newID = networkObject.SceneGuid;
        var instantiationEvent = new InstantiateEvent(prefab, originID, newID, WebSocketClient.LocalConnection);
        
        UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true).RaiseLockstepEvent(instantiationEvent);
    }
}

public readonly struct InstantiateEvent : INetworkEvent
{
    public readonly NetworkObject Prefab;
    public readonly string OriginID;
    public readonly string NewID;
    public readonly NetworkConnection NetworkConnection;

    public InstantiateEvent(NetworkObject prefab, string originID, string newID, NetworkConnection networkConnection)
    {
        Prefab = prefab;
        OriginID = originID;
        NewID = newID;
        NetworkConnection = networkConnection;
    }

    public void PerformEvent()
    {
        if (NetworkConnection.Equals(WebSocketClient.LocalConnection)) return;
        
        Prefab.SetSceneGuidGroup(NewID);
        Object.Instantiate(Prefab);
        Prefab.SetSceneGuidGroup(OriginID);
    }
}
