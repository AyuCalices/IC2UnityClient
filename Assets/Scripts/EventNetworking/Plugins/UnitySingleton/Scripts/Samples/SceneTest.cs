using UnityEngine;

namespace EventNetworking.Plugins.UnitySingleton.Scripts.Samples
{

    public class SceneTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log(SceneGameManager.Instance.GetPlayerName());
        }
    }

}