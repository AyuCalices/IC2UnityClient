using UnityEngine;

namespace EventNetworking.Identification
{
    public interface IChangeGameObjectParent
    {
        public void OnChangeGameObjectParent(GameObject newParent, GameObject previousParent);
    }
}
