using System.Collections;
using UnityEngine;

namespace Managers
{
    public sealed class GameManager : Singleton<GameManager>
    {
        public enum GameState
        {
            Starting,
            Playing,
            GameOver
        }

        [Header("Player")]
        [SerializeField] private PlayerHealth playerPrefab;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField, Min(0f)] private float playerSpawnAnimationDuration = 1.3f;

        private PlayerHealth player;
        private EnemySpawner enemySpawner;
        private DifficultyController difficultyController;
        private Coroutine roundStartRoutine;

        public GameState State { get; private set; }
        public PlayerHealth Player => player;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            enemySpawner = FindObjectOfType<EnemySpawner>();
            difficultyController = GetComponent<DifficultyController>();
            player = FindObjectOfType<PlayerHealth>();

            if (player == null && playerPrefab != null)
                player = Instantiate(playerPrefab, SpawnPosition, SpawnRotation);

            if (player == null)
            {
                Debug.LogError("GameManager requires a player prefab.", this);
                enabled = false;
                return;
            }

            player.Died += OnPlayerDied;
            if (enemySpawner != null)
            {
                enemySpawner.SetPlayer(player.transform);
                enemySpawner.ResetAllEnemies();
            }
        }

        private void Start()
        {
            if (player != null) StartRound();
        }

        private void OnDestroy()
        {
            if (player != null) player.Died -= OnPlayerDied;
        }

        public void RestartGame()
        {
            if (State == GameState.Starting || player == null) return;
            StartRound();
        }

        private void StartRound()
        {
            State = GameState.Starting;
            if (roundStartRoutine != null) StopCoroutine(roundStartRoutine);

            if (enemySpawner != null) enemySpawner.ResetAllEnemies();

            if (UIManager.Instance != null)
                UIManager.Instance.ShowGame(player);

            if (difficultyController != null) difficultyController.ResetDifficulty();
            player.PrepareForRespawn(SpawnPosition, SpawnRotation);
            if (!player.PlaySpawnAnimation())
                Debug.LogWarning("Player Animator could not play the Spawn state.", player);
            roundStartRoutine = StartCoroutine(ActivateRoundAfterSpawn());
        }

        private IEnumerator ActivateRoundAfterSpawn()
        {
            if (playerSpawnAnimationDuration > 0f)
                yield return new WaitForSecondsRealtime(playerSpawnAnimationDuration);

            roundStartRoutine = null;
            player.ActivateAfterSpawn();
            if (difficultyController != null) difficultyController.Begin();
            if (enemySpawner != null) enemySpawner.BeginSpawning();
            State = GameState.Playing;
        }

        private void OnPlayerDied()
        {
            if (roundStartRoutine != null)
            {
                StopCoroutine(roundStartRoutine);
                roundStartRoutine = null;
            }

            if (enemySpawner != null) enemySpawner.ResetAllEnemies();
            if (difficultyController != null) difficultyController.Stop();
            State = GameState.GameOver;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowRestart();
        }

        private Vector3 SpawnPosition => playerSpawnPoint != null
            ? playerSpawnPoint.position
            : transform.position;

        private Quaternion SpawnRotation => playerSpawnPoint != null
            ? playerSpawnPoint.rotation
            : Quaternion.identity;
    }
}
