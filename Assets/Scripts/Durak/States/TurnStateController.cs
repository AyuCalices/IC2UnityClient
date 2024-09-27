using UnityEngine;

namespace Durak.States
{
    public class TurnStateController : MonoBehaviour
    {
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private CardSpawner cardSpawner;
    
        private void Awake()
        {
            StartGameState.OnStartGameCompleted += EnterTurnState;
        }

        private void OnDestroy()
        {
            StartGameState.OnStartGameCompleted -= EnterTurnState;
        }

        private void EnterTurnState()
        {
            gameStateManager.RequestState(new TurnState(cardSpawner));
        }
    }
}
