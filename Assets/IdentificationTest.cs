using Component;
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
        UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true).RequestRaiseEvent(new EventData(GetComponent<NetworkObject>()));
    }

    [ContextMenu("Duplicate Instantiate")]
    private void DuplicateInstantiate()
    {
        var webSocketClient = UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true);
        var networkObject = GetComponent<NetworkObject>();
        
        var newNetworkObject = Instantiate(networkObject);
        var newID = newNetworkObject.SceneGuid;
        var instantiationEvent = new InstantiateEvent(networkObject, newID, webSocketClient.LocalConnection);
        
        webSocketClient.RequestRaiseEvent(instantiationEvent);
    }
    
    [ContextMenu("Network Instantiate")]
    private void NetworkInstantiate()
    {
        var webSocketClient = UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true);
        
        var newNetworkObject = Instantiate(prefab);
        var newID = newNetworkObject.SceneGuid;
        //TODO: Callback
        var instantiationEvent = new InstantiateEvent(prefab, newID, webSocketClient.LocalConnection);
        
        webSocketClient.RequestRaiseEvent(instantiationEvent);
    }
    
    [ContextMenu("Network Destroy")]
    private void NetworkDestroy()
    {
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject.IsUnityNull() || string.IsNullOrEmpty(networkObject.SceneGuid)) return;
        
        //TODO: Callback
        Destroy(networkObject.gameObject);
        
        var instantiationEvent = new DestroyEvent(networkObject);
        var webSocketClient = UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true);
        webSocketClient.RequestRaiseEvent(instantiationEvent);
    }

    [ContextMenu("Secure Request Ownership")]
    private void SecureRequestOwnership()
    {
        GetComponent<NetworkObject>().SecureRequestOwnership();
    }
    
    [ContextMenu("Secure Release Ownership")]
    private void SecureReleaseOwnership()
    {
        GetComponent<NetworkObject>().SecureReleaseOwnership();
    }
}

public readonly struct InstantiateEvent : INetworkEvent
{
    public readonly NetworkObject OriginObj;
    public readonly string OriginID;
    public readonly string NewID;
    public readonly NetworkConnection NetworkConnection;

    public InstantiateEvent(NetworkObject originObj, string newID, NetworkConnection networkConnection)
    {
        OriginObj = originObj;
        OriginID = originObj.SceneGuid;
        NewID = newID;
        NetworkConnection = networkConnection;
    }

    public bool ValidateRequest()
    {
        return true;
    }

    public void PerformEvent()
    {
        var webSocketClient = UnityUtility.FindObjectsOfTypeInAllScenes<WebSocketClient>(true);
        if (NetworkConnection.Equals(webSocketClient[0].LocalConnection)) return;
        
        OriginObj.SetSceneGuidGroup(NewID);
        Object.Instantiate(OriginObj);
        OriginObj.SetSceneGuidGroup(OriginID);
        
        //TODO: Callback
    }
}

public readonly struct DestroyEvent : INetworkEvent
{
    public readonly NetworkObject OriginObj;

    public DestroyEvent(NetworkObject originObj)
    {
        OriginObj = originObj;
    }

    public bool ValidateRequest()
    {
        var webSocketClient = UnityUtility.FindObjectsOfTypeInAllScenes<WebSocketClient>(true);
        return !OriginObj.HasOwner || OriginObj.Owner.Equals(webSocketClient[0].LocalConnection);
    }

    public void PerformEvent()
    {
        if (OriginObj.IsUnityNull()) return;
        
        //TODO: Callback
        Object.Destroy(OriginObj.gameObject);
    }
}
