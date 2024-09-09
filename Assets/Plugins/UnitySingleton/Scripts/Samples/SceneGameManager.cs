using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace Plugins.UnitySingleton.Scripts.Samples
{

    public class SceneGameManager : MonoSingleton<SceneGameManager>
    {

        [SerializeField]
        protected string m_PlayerName = "NoSLoofah";

        public string GetPlayerName()
        {
            return m_PlayerName;
        }

    }

}