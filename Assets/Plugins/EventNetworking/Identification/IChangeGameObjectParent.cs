using UnityEngine;

namespace Plugins.EventNetworking.Identification
{
    public interface IChangeGameObjectParent
    {
        public void OnChangeGameObjectParent(GameObject newParent, GameObject previousParent);
    }
}
