using Plugins.EventNetworking.Component;
using UnityEngine;

public class IdentificationTest : NetworkObject
{
    [SerializeField] private NetworkObject prefab;

    [ContextMenu("Duplicate Instantiate")]
    private void DuplicateInstantiate()
    {
        NetworkManager.Instance.NetworkInstantiate(GetComponent<NetworkObject>());
    }
    
    [ContextMenu("Network Instantiate")]
    private void NetworkInstantiate()
    {
        NetworkManager.Instance.NetworkInstantiate(prefab);
    }
    
    [ContextMenu("Network Destroy")]
    private void NetworkDestroy()
    {
        NetworkManager.Instance.NetworkDestroy(GetComponent<NetworkObject>());
    }

    [ContextMenu("Request Ownership")]
    private void RequestOwnership()
    {
        SecureRequestOwnership();
    }
    
    [ContextMenu("Release Ownership")]
    private void ReleaseOwnership()
    {
        SecureReleaseOwnership();
    }
}

