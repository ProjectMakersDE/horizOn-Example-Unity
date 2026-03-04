using UnityEngine;
using SeagullStorm.Gameplay.Player;
using SeagullStorm.Managers;

namespace SeagullStorm.Gameplay.Spawning
{
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [SerializeField] private GameObject crabPrefab;
        [SerializeField] private GameObject jellyfishPrefab;
        [SerializeField] private GameObject piratePrefab;
        [SerializeField] private GameObject bossPrefab;

        private float _waveTimer;
        private float _spawnDistance = 300f;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _waveTimer += Time.deltaTime;
            var config = GameManager.Instance.Config;

            if (_waveTimer >= config.waveIntervalSeconds)
            {
                _waveTimer = 0f;
                SpawnWave();
            }
        }

        private void SpawnWave()
        {
            var run = GameManager.Instance.CurrentRun;
            var config = GameManager.Instance.Config;
            run.currentWave++;

            int enemyCount = Mathf.RoundToInt(config.waveEnemyCountBase *
                Mathf.Pow(config.waveEnemyCountGrowth, run.currentWave - 1));

            SDK.HorizonSDKIntegration.Instance?.SetCrashCustomKey("wave", run.currentWave.ToString());

            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy(run.currentWave);
            }
        }

        public void SpawnBossWave()
        {
            if (bossPrefab == null) return;

            var config = GameManager.Instance.Config;
            Vector2 spawnPos = GetRandomEdgePosition();
            var boss = Instantiate(bossPrefab, spawnPos, Quaternion.identity);

            var enemyBase = boss.GetComponent<Enemies.EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.maxHP = config.waveBossHp;
                enemyBase.moveSpeed = 30f;
                enemyBase.damage = 30;
                enemyBase.xpReward = 100;
                enemyBase.enemyType = "boss";
            }

            AudioManager.Instance?.PlayBossMusic();
        }

        private void SpawnEnemy(int wave)
        {
            var config = GameManager.Instance.Config;
            Vector2 spawnPos = GetRandomEdgePosition();

            GameObject prefab;
            float speed;
            int hp, damage, xp;
            string type;

            if (wave >= 5 && Random.value < 0.3f)
            {
                prefab = piratePrefab;
                speed = config.enemyPirateSpeed;
                hp = config.enemyPirateHp;
                damage = config.enemyPirateDamage;
                xp = config.enemyPirateXp;
                type = "pirate";
            }
            else if (wave >= 3 && Random.value < 0.3f)
            {
                prefab = jellyfishPrefab;
                speed = config.enemyJellyfishSpeed;
                hp = config.enemyJellyfishHp;
                damage = config.enemyJellyfishDamage;
                xp = config.enemyJellyfishXp;
                type = "jellyfish";
            }
            else
            {
                prefab = crabPrefab;
                speed = config.enemyCrabSpeed;
                hp = config.enemyCrabHp;
                damage = config.enemyCrabDamage;
                xp = config.enemyCrabXp;
                type = "crab";
            }

            if (prefab == null) return;

            var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            var enemyBase = enemy.GetComponent<Enemies.EnemyBase>();
            if (enemyBase != null)
            {
                enemyBase.moveSpeed = speed;
                enemyBase.maxHP = hp;
                enemyBase.damage = damage;
                enemyBase.xpReward = xp;
                enemyBase.enemyType = type;
            }
        }

        private Vector2 GetRandomEdgePosition()
        {
            if (PlayerController.Instance == null) return Vector2.zero;

            Vector2 playerPos = PlayerController.Instance.transform.position;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnDistance;
        }
    }
}
