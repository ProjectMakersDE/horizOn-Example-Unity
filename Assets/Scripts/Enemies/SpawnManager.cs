using UnityEngine;

namespace SeagullStorm
{
    /// <summary>
    /// Wave-based enemy spawner. Enemies spawn outside camera view.
    /// </summary>
    public class SpawnManager : MonoBehaviour
    {
        public static SpawnManager Instance { get; private set; }

        private float _waveTimer;
        private bool _bossSpawned;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            _waveTimer = 0f;
            _bossSpawned = false;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Run) return;

            var run = GameManager.Instance.RunState;
            var config = GameManager.Instance.Config;

            // Update timer
            run.timeRemaining -= Time.deltaTime;
            run.duration += Time.deltaTime;
            run.score = run.kills + run.xpCollected + Mathf.RoundToInt(run.duration);

            // Update crash keys
            HorizonManager.Instance?.SetCrashCustomKey("wave", run.wave.ToString());
            HorizonManager.Instance?.SetCrashCustomKey("level", run.level.ToString());

            // Boss check
            if (run.timeRemaining <= 0f && !_bossSpawned)
            {
                run.timeRemaining = 0f;
                if (config.BossWaveEnabled)
                {
                    _bossSpawned = true;
                    SpawnBoss();
                    HorizonManager.Instance?.RecordBreadcrumb("state", "boss_spawned");
                }
                else
                {
                    // End run without boss
                    PlayerController.Instance?.TakeDamage(99999);
                }
                return;
            }

            // Wave spawning
            _waveTimer -= Time.deltaTime;
            if (_waveTimer <= 0f && run.timeRemaining > 0f)
            {
                _waveTimer = config.WaveIntervalSeconds;
                run.wave++;
                SpawnWave(run.wave, config);
            }
        }

        private void SpawnWave(int waveNumber, GameConfig config)
        {
            int enemyCount = Mathf.RoundToInt(config.WaveEnemyCountBase * Mathf.Pow(config.WaveEnemyCountGrowth, waveNumber - 1));
            var playerTransform = PlayerController.Instance?.transform;

            for (int i = 0; i < enemyCount; i++)
            {
                string type = ChooseEnemyType(waveNumber);
                Vector3 spawnPos = GetSpawnPosition();
                EnemyPool.Instance?.SpawnEnemy(type, spawnPos, playerTransform);
            }
        }

        private string ChooseEnemyType(int wave)
        {
            float r = Random.value;
            if (wave >= 5 && r < 0.2f) return "pirate";
            if (wave >= 3 && r < 0.4f) return "jellyfish";
            return "crab";
        }

        private void SpawnBoss()
        {
            AudioManager.Instance?.PlayBossMusic();
            Vector3 pos = GetSpawnPosition();
            EnemyPool.Instance?.SpawnEnemy("boss", pos, PlayerController.Instance?.transform);
        }

        private Vector3 GetSpawnPosition()
        {
            Vector2 playerPos = GameManager.Instance?.RunState.playerPosition ?? Vector2.zero;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(300f, 400f);
            return new Vector3(playerPos.x + Mathf.Cos(angle) * dist, playerPos.y + Mathf.Sin(angle) * dist, 0);
        }
    }
}
