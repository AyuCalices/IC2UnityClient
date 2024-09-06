using LockstepNetworking;
using SaveLoadSystem.Utility;
using UnityEngine;

public class IdentificationTest : MonoBehaviour
{
    public string text;

    [ContextMenu("Perform Reference Event")]
    private void PerformReferenceEvent()
    {
        UnityUtility.FindObjectOfTypeInScene<WebSocketClient>(gameObject.scene, true).RaiseLockstepEvent(new EventData(GetComponent<NetworkObject>()));
    }
}
