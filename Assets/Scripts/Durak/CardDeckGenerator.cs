using UnityEngine;
using Random = UnityEngine.Random;

namespace Durak
{
    public class CardDeckGenerator
    {
        private readonly GameData _gameData;
        private readonly GameBalancing _gameBalancing;

        public CardDeckGenerator(GameData gameData, GameBalancing gameBalancing)
        {
            _gameData = gameData;
            _gameBalancing = gameBalancing;
        }

        public void InitializeDeck(int seed)
        {
            int cardCountDelta = 0;
            
            foreach (var cardGenerator in _gameBalancing.CardGenerators)
            {
                _gameData.DeckCards.Add(cardGenerator.Generate());
                
                cardCountDelta++;
                if (cardCountDelta >= _gameBalancing.MaxDeckCardCount)
                {
                    break;
                }
            }
            
            Random.InitState(seed);
            Shuffle();
        }

        private void Shuffle()
        {
            var n = _gameData.DeckCards.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (_gameData.DeckCards[k], _gameData.DeckCards[n]) = (_gameData.DeckCards[n], _gameData.DeckCards[k]);
            }
        }
    }
}
