using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.EventNetworking.Plugins.UnitySingleton.Scripts.Samples
{

    public class GameManager : PersistentMonoSingleton<GameManager>
    {

        [SerializeField]
        protected string m_PlayerName;

        protected virtual void Start()
        {
            SceneManager.LoadScene("Main Menu");
        }

        public string GetPlayerName()
        {
            return m_PlayerName;
        }

    }

}