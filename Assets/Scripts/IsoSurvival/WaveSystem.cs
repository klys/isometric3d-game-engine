using System.Collections.Generic;
using UnityEngine;

namespace IsoSurvival
{
    public class WaveSystem : MonoBehaviour
    {
        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();

        private GameController controller;
        private Transform enemyRoot;
        private float spawnCountdown;
        private int waveNumber;

        public void Initialize(GameController gameController)
        {
            controller = gameController;
        }

        private void Update()
        {
            if (!controller.IsSimulationRunning)
            {
                return;
            }

            spawnCountdown -= Time.deltaTime;
            if (spawnCountdown <= 0f)
            {
                SpawnWave();
            }
        }

        public void BeginSession()
        {
            enemyRoot = new GameObject("Enemies").transform;
            enemies.Clear();
            waveNumber = 0;
            spawnCountdown = 8f;
        }

        public void ClearSession()
        {
            if (enemyRoot != null)
            {
                Destroy(enemyRoot.gameObject);
            }

            enemies.Clear();
            waveNumber = 0;
        }

        public EnemyUnit FindNearestEnemy(Vector3 position, float maxDistance)
        {
            EnemyUnit best = null;
            var bestDistance = maxDistance;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(position, enemy.AimPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }

            return best;
        }

        public void NotifyEnemyDeath(EnemyUnit enemy)
        {
            enemies.Remove(enemy);
        }

        private void SpawnWave()
        {
            waveNumber++;
            controller.RegisterWaveStarted(waveNumber);

            var amount = Mathf.CeilToInt((4 + waveNumber * 1.5f) * controller.ActiveSettings.EnemyIntensity);
            var focus = controller.Units.GetFocusTile();
            var radius = controller.ActiveSettings.ChunkSize * controller.ActiveSettings.ChunkViewRadius + 4;

            for (var i = 0; i < amount; i++)
            {
                var angle = i * Mathf.PI * 2f / Mathf.Max(1, amount);
                var ring = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var rawTile = focus + new Vector2Int(Mathf.RoundToInt(ring.x), Mathf.RoundToInt(ring.y));
                var spawnTile = controller.World.FindNearestWalkable(rawTile, 6);
                var type = PickEnemyType(controller.World.GetTileData(rawTile).Biome, i);
                SpawnEnemy(spawnTile, type, waveNumber);
            }

            spawnCountdown = controller.ActiveSettings.WaveIntervalSeconds;
        }

        private void SpawnEnemy(Vector2Int tile, EnemyType type, int difficulty)
        {
            var enemyObject = new GameObject(type + " Enemy");
            enemyObject.transform.SetParent(enemyRoot, false);
            enemyObject.transform.position = controller.World.TileToWorld(tile);

            var enemy = enemyObject.AddComponent<EnemyUnit>();
            enemy.Initialize(controller, tile, type, difficulty);
            enemies.Add(enemy);
        }

        private static EnemyType PickEnemyType(BiomeType biome, int index)
        {
            if (biome == BiomeType.Sea)
            {
                return EnemyType.Water;
            }

            if (index % 5 == 0)
            {
                return EnemyType.Flying;
            }

            if (index % 3 == 0)
            {
                return EnemyType.Underground;
            }

            return EnemyType.Ground;
        }
    }

    public class EnemyUnit : MonoBehaviour, IDamageable
    {
        private GameController controller;
        private EnemyDefinition definition;
        private float currentHealth;
        private float attackCooldown;
        private float hoverOffset;

        public Vector2Int CurrentTile { get; private set; }
        public EnemyType Type => definition.Type;
        public bool IsAlive { get; private set; }
        public Vector3 AimPoint => transform.position + Vector3.up * 0.9f;

        public void Initialize(GameController gameController, Vector2Int spawnTile, EnemyType type, int difficulty)
        {
            controller = gameController;
            CurrentTile = spawnTile;
            definition = Definitions.GetEnemy(type);
            currentHealth = definition.MaxHealth + difficulty * 4f;
            IsAlive = true;
            hoverOffset = type == EnemyType.Flying ? 1.7f : 0f;

            BuildVisuals(type);
            SyncPosition();
        }

        private void Update()
        {
            if (!IsAlive || !controller.IsSimulationRunning)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            var target = controller.FindNearestDamageable(transform.position, 50f);
            if (target == null)
            {
                return;
            }

            var targetPosition = target.AimPoint;
            var direction = targetPosition - transform.position;
            direction.y = 0f;
            var distance = direction.magnitude;

            if (distance > definition.AttackRange)
            {
                var move = direction.normalized * definition.Speed * Time.deltaTime;
                transform.position += move;
                SyncHeight();
            }
            else if (attackCooldown <= 0f)
            {
                target.TakeDamage(definition.Damage);
                attackCooldown = 0.95f;
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth -= damage;
            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void BuildVisuals(EnemyType type)
        {
            var primitive = PrimitiveType.Capsule;
            var color = new Color(0.7f, 0.25f, 0.22f);
            var scale = new Vector3(0.65f, 0.8f, 0.65f);

            switch (type)
            {
                case EnemyType.Underground:
                    primitive = PrimitiveType.Cylinder;
                    color = new Color(0.44f, 0.28f, 0.16f);
                    scale = new Vector3(0.7f, 0.5f, 0.7f);
                    break;
                case EnemyType.Flying:
                    primitive = PrimitiveType.Sphere;
                    color = new Color(0.66f, 0.25f, 0.74f);
                    scale = new Vector3(0.7f, 0.45f, 0.9f);
                    break;
                case EnemyType.Water:
                    primitive = PrimitiveType.Sphere;
                    color = new Color(0.18f, 0.65f, 0.86f);
                    scale = new Vector3(0.78f, 0.55f, 0.78f);
                    break;
            }

            var model = GameObject.CreatePrimitive(primitive);
            model.transform.SetParent(transform, false);
            model.transform.localScale = scale;
            model.transform.localPosition = new Vector3(0f, type == EnemyType.Flying ? 1.2f : 0.8f, 0f);
            model.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(color);
        }

        private void SyncPosition()
        {
            transform.position = controller.World.TileToWorld(CurrentTile) + Vector3.up * hoverOffset;
        }

        private void SyncHeight()
        {
            var tile = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / controller.ActiveSettings.TileWorldSize),
                Mathf.RoundToInt(transform.position.z / controller.ActiveSettings.TileWorldSize));
            var height = controller.World.TileToWorld(tile).y + hoverOffset;
            transform.position = new Vector3(transform.position.x, height, transform.position.z);
        }

        private void Die()
        {
            IsAlive = false;
            controller.Waves.NotifyEnemyDeath(this);
            Destroy(gameObject);
        }
    }
}
