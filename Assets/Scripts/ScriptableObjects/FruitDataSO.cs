using UnityEngine;

namespace JYW.JellySuika.SOs
{
    [CreateAssetMenu(menuName = "FruitDataSO", fileName = "FruitDataSO")]
    public class FruitDataSO : ScriptableObject
    {
        public GameObject parentPrefab;
        public GameObject childPrefab;
        public Sprite fruitSprite;
    }
}