using Component;
using UnityEngine;

public class IdentificationTest : MonoBehaviour
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

