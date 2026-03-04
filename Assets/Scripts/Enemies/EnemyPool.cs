using UnityEngine;
using UnityEngine.Pool;

namespace SeagullStorm
{
    /// <summary>
    /// Object pool for all enemy types using UnityEngine.Pool.ObjectPool.
    /// </summary>
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool Instance { get; private set; }

        [SerializeField] private EnemyBase crabPrefab;
        [SerializeField] private EnemyBase jellyfishPrefab;
        [SerializeField] private EnemyBase piratePrefab;
        [SerializeField] private EnemyBase bossPrefab;

        private ObjectPool<EnemyBase> _crabPool;
        private ObjectPool<EnemyBase> _jellyfishPool;
        private ObjectPool<EnemyBase> _piratePool;
        private ObjectPool<EnemyBase> _bossPool;

        private void Awake()
        {
            Instance = this;
            _crabPool = CreatePool(crabPrefab, 20);
            _jellyfishPool = CreatePool(jellyfishPrefab, 10);
            _piratePool = CreatePool(piratePrefab, 10);
            _bossPool = CreatePool(bossPrefab, 2);
        }

        private ObjectPool<EnemyBase> CreatePool(EnemyBase prefab, int defaultCapacity)
        {
            return new ObjectPool<EnemyBase>(
                createFunc: () =>
                {
                    if (prefab == null) return null;
                    var obj = Instantiate(prefab, transform);
                    obj.gameObject.SetActive(false);
                    return obj;
                },
                actionOnGet: e => { },
                actionOnRelease: e => { if (e != null) e.gameObject.SetActive(false); },
                actionOnDestroy: e => { if (e != null) Destroy(e.gameObject); },
                defaultCapacity: defaultCapacity,
                maxSize: defaultCapacity * 2
            );
        }

        public EnemyBase SpawnEnemy(string type, Vector3 position, Transform target)
        {
            var config = GameManager.Instance?.Config;
            if (config == null) return null;

            EnemyBase enemy = null;

            switch (type)
            {
                case "crab":
                    enemy = _crabPool?.Get();
                    enemy?.Initialize(config.EnemyCrabSpeed, config.EnemyCrabHp, config.EnemyCrabDamage, config.EnemyCrabXp, target);
                    break;
                case "jellyfish":
                    enemy = _jellyfishPool?.Get();
                    enemy?.Initialize(config.EnemyJellyfishSpeed, config.EnemyJellyfishHp, config.EnemyJellyfishDamage, config.EnemyJellyfishXp, target);
                    break;
                case "pirate":
                    enemy = _piratePool?.Get();
                    enemy?.Initialize(config.EnemyPirateSpeed, config.EnemyPirateHp, config.EnemyPirateDamage, config.EnemyPirateXp, target);
                    break;
                case "boss":
                    enemy = _bossPool?.Get();
                    enemy?.Initialize(30f, config.WaveBossHp, 30, 100, target);
                    break;
            }

            if (enemy != null)
            {
                enemy.transform.position = position;
                enemy.gameObject.SetActive(true);
            }

            return enemy;
        }

        public void ReturnEnemy(EnemyBase enemy)
        {
            if (enemy == null) return;

            if (enemy is EnemyCrab) _crabPool?.Release(enemy);
            else if (enemy is EnemyJellyfish) _jellyfishPool?.Release(enemy);
            else if (enemy is EnemyPirate) _piratePool?.Release(enemy);
            else if (enemy is EnemyBoss) _bossPool?.Release(enemy);
            else enemy.gameObject.SetActive(false);
        }

        public void ReturnAll()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
