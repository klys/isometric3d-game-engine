using System.Collections.Generic;
using UnityEngine;

namespace IsoSurvival
{
    public class BuildingsSystem : MonoBehaviour
    {
        private readonly List<BuildingEntity> buildings = new List<BuildingEntity>();
        private GameController controller;
        private Transform buildingsRoot;

        public BuildingType? PendingBuildingType { get; private set; }
        public bool HasPendingPlacement => PendingBuildingType.HasValue;

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

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                QueuePlacement(BuildingType.House);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                QueuePlacement(BuildingType.Wall);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                QueuePlacement(BuildingType.Tower);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                CancelPlacement();
            }
        }

        public void BeginSession()
        {
            buildingsRoot = new GameObject("Buildings").transform;
            buildings.Clear();
            PendingBuildingType = null;
        }

        public void ClearSession()
        {
            if (buildingsRoot != null)
            {
                Destroy(buildingsRoot.gameObject);
            }

            buildings.Clear();
            PendingBuildingType = null;
        }

        public void QueuePlacement(BuildingType type)
        {
            PendingBuildingType = type;
        }

        public void CancelPlacement()
        {
            PendingBuildingType = null;
        }

        public bool TryPlaceBuilding(Vector2Int tile)
        {
            if (!PendingBuildingType.HasValue)
            {
                return false;
            }

            var definition = Definitions.GetBuilding(PendingBuildingType.Value);
            if (!controller.Inventory.Spend(definition))
            {
                return false;
            }

            if (!controller.World.ReserveBuilding(tile))
            {
                Refund(definition);
                return false;
            }

            var buildingObject = new GameObject(definition.Type.ToString());
            buildingObject.transform.SetParent(buildingsRoot, false);
            buildingObject.transform.position = controller.World.TileToWorld(tile);

            var building = buildingObject.AddComponent<BuildingEntity>();
            building.Initialize(controller, tile, definition);
            buildings.Add(building);
            return true;
        }

        public void NotifyBuildingDestroyed(BuildingEntity building)
        {
            buildings.Remove(building);
            controller.World.ReleaseBuilding(building.Tile);
        }

        public IDamageable FindNearestLiving(Vector3 position, float maxDistance)
        {
            BuildingEntity best = null;
            var bestDistance = maxDistance;
            for (var i = 0; i < buildings.Count; i++)
            {
                var building = buildings[i];
                if (building == null || !building.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(position, building.AimPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = building;
                }
            }

            return best;
        }

        private void Refund(BuildingDefinition definition)
        {
            controller.Inventory.Add(CollectibleType.Plant, definition.Plants);
            controller.Inventory.Add(CollectibleType.Flower, definition.Flowers);
            controller.Inventory.Add(CollectibleType.Rock, definition.Rocks);
            controller.Inventory.Add(CollectibleType.Mineral, definition.Minerals);
        }
    }

    public class BuildingEntity : MonoBehaviour, IDamageable
    {
        private GameController controller;
        private BuildingDefinition definition;
        private float currentHealth;
        private float attackCooldown;

        public Vector2Int Tile { get; private set; }
        public bool IsAlive { get; private set; }
        public Vector3 AimPoint => transform.position + Vector3.up * 1.4f;

        public void Initialize(GameController gameController, Vector2Int tile, BuildingDefinition buildingDefinition)
        {
            controller = gameController;
            Tile = tile;
            definition = buildingDefinition;
            currentHealth = definition.MaxHealth;
            IsAlive = true;

            BuildVisuals();
        }

        private void Update()
        {
            if (!IsAlive || !controller.IsSimulationRunning || definition.Type != BuildingType.Tower)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;
            if (attackCooldown > 0f)
            {
                return;
            }

            var enemy = controller.Waves.FindNearestEnemy(transform.position, definition.Range);
            if (enemy == null)
            {
                return;
            }

            enemy.TakeDamage(definition.Damage);
            attackCooldown = definition.Cooldown;
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

        private void BuildVisuals()
        {
            switch (definition.Type)
            {
                case BuildingType.House:
                    CreateHouse();
                    break;
                case BuildingType.Wall:
                    CreateWall();
                    break;
                case BuildingType.Tower:
                    CreateTower();
                    break;
            }
        }

        private void CreateHouse()
        {
            var baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCube.transform.SetParent(transform, false);
            baseCube.transform.localScale = new Vector3(1.1f, 1f, 1.1f);
            baseCube.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            baseCube.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.78f, 0.56f, 0.38f));

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.transform.SetParent(transform, false);
            roof.transform.localScale = new Vector3(0.8f, 0.25f, 0.8f);
            roof.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            roof.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.58f, 0.19f, 0.15f));
        }

        private void CreateWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(transform, false);
            wall.transform.localScale = new Vector3(1.2f, 1.1f, 0.45f);
            wall.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            wall.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.53f, 0.54f, 0.6f));
        }

        private void CreateTower()
        {
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.transform.SetParent(transform, false);
            shaft.transform.localScale = new Vector3(0.75f, 1.4f, 0.75f);
            shaft.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            shaft.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.56f, 0.58f, 0.68f));

            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(transform, false);
            top.transform.localScale = new Vector3(1f, 0.3f, 1f);
            top.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            top.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.22f, 0.36f, 0.43f));
        }

        private void Die()
        {
            IsAlive = false;
            controller.Buildings.NotifyBuildingDestroyed(this);
            Destroy(gameObject);
        }
    }
}
