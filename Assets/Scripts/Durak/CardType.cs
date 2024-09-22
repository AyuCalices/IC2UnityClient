using UnityEngine;

namespace Durak
{
    [CreateAssetMenu]
    public class CardType : ScriptableObject
    {
        [SerializeField] private string type;

        public string Type => type;
    }
}
