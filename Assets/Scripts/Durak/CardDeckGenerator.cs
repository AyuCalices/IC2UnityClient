using UnityEngine;
using Random = UnityEngine.Random;

namespace Durak
{
    //done
    public class CardDeckGenerator
    {
        private readonly GameData _gameData;
        private readonly GameBalancing _gameBalancing;

        private int _cardCountDelta;

        public CardDeckGenerator(GameData gameData, GameBalancing gameBalancing)
        {
            _gameData = gameData;
            _gameBalancing = gameBalancing;
        }

        public void InitializeDeck(int seed)
        {
            foreach (var cardGenerator in _gameBalancing.CardGenerators)
            {
                _gameData.DeckCards.Add(cardGenerator.Generate());
                
                _cardCountDelta++;
                if (_cardCountDelta >= _gameBalancing.MaxCardCount)
                {
                    break;
                }
            }
            
            Random.InitState(seed);
            Shuffle();

            _gameData.TrumpType = _gameData.DeckCards[0].CardType;
            Debug.Log($"Trump is {_gameData.DeckCards[0].CardType}");
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
