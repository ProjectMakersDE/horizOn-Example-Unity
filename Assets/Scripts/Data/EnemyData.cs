using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// ScriptableObject defining enemy stats template.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "SeagullStorm/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public string enemyId;
        public float moveSpeed = 40f;
        public int maxHP = 30;
        public int damage = 10;
        public int xpReward = 10;
        public int firstAppearWave = 1;
        public Sprite sprite;
    }
}
