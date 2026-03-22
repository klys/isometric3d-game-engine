using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IsoSurvival
{
    public class UnitsSystem : MonoBehaviour
    {
        private readonly List<HumanoidUnit> humanoids = new List<HumanoidUnit>();
        private readonly List<HumanoidUnit> selectedHumanoids = new List<HumanoidUnit>();

        private static readonly Vector2Int[] FormationOffsets =
        {
            Vector2Int.zero,
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1)
        };

        private GameController controller;
        private Transform unitRoot;

        public int AliveCount
        {
            get
            {
                var alive = 0;
                for (var i = 0; i < humanoids.Count; i++)
                {
                    if (humanoids[i] != null && humanoids[i].IsAlive)
                    {
                        alive++;
                    }
                }

                return alive;
            }
        }

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

            HandleInput();
        }

        public void BeginSession(int startingHumanoids)
        {
            unitRoot = new GameObject("Humanoids").transform;
            humanoids.Clear();
            selectedHumanoids.Clear();

            for (var i = 0; i < startingHumanoids; i++)
            {
                var spawnTile = controller.World.FindNearestWalkable(new Vector2Int(i - startingHumanoids / 2, i % 2), 8);
                SpawnHumanoid(spawnTile, i);
            }
        }

        public void ClearSession()
        {
            if (unitRoot != null)
            {
                Destroy(unitRoot.gameObject);
            }

            humanoids.Clear();
            selectedHumanoids.Clear();
        }

        public Vector2Int GetFocusTile()
        {
            var aliveCount = 0;
            var sum = Vector2.zero;
            for (var i = 0; i < humanoids.Count; i++)
            {
                var humanoid = humanoids[i];
                if (humanoid == null || !humanoid.IsAlive)
                {
                    continue;
                }

                aliveCount++;
                sum += new Vector2(humanoid.CurrentTile.x, humanoid.CurrentTile.y);
            }

            if (aliveCount == 0)
            {
                return Vector2Int.zero;
            }

            return new Vector2Int(Mathf.RoundToInt(sum.x / aliveCount), Mathf.RoundToInt(sum.y / aliveCount));
        }

        public void RegisterDeath(HumanoidUnit humanoid)
        {
            selectedHumanoids.Remove(humanoid);
            controller.NotifyHumanoidDied();
        }

        public IDamageable FindNearestLiving(Vector3 position, float maxDistance)
        {
            HumanoidUnit best = null;
            var bestDistance = maxDistance;
            for (var i = 0; i < humanoids.Count; i++)
            {
                var humanoid = humanoids[i];
                if (humanoid == null || !humanoid.IsAlive)
                {
                    continue;
                }

                var distance = Vector3.Distance(position, humanoid.AimPoint);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = humanoid;
                }
            }

            return best;
        }

        public int SelectedCount()
        {
            return selectedHumanoids.Count;
        }

        private void HandleInput()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var camera = controller.MainCamera;
            if (camera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleLeftClick(camera);
            }

            if (Input.GetMouseButtonDown(1))
            {
                HandleRightClick(camera);
            }
        }

        private void HandleLeftClick(Camera camera)
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 200f))
            {
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    ClearSelection();
                }

                return;
            }

            if (controller.Buildings.HasPendingPlacement)
            {
                Vector2Int tile;
                if (controller.World.TryGetTileFromHit(hit, out tile))
                {
                    controller.Buildings.TryPlaceBuilding(tile);
                }

                return;
            }

            var humanoid = hit.collider.GetComponentInParent<HumanoidUnit>();
            if (humanoid != null)
            {
                var additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (!additive)
                {
                    ClearSelection();
                }

                AddSelection(humanoid);
                return;
            }

            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                ClearSelection();
            }
        }

        private void HandleRightClick(Camera camera)
        {
            if (controller.Buildings.HasPendingPlacement)
            {
                controller.Buildings.CancelPlacement();
                return;
            }

            if (selectedHumanoids.Count == 0)
            {
                return;
            }

            var ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 250f))
            {
                return;
            }

            Vector2Int baseTile;
            if (!controller.World.TryGetTileFromHit(hit, out baseTile))
            {
                return;
            }

            for (var i = 0; i < selectedHumanoids.Count; i++)
            {
                if (selectedHumanoids[i] == null || !selectedHumanoids[i].IsAlive)
                {
                    continue;
                }

                var offset = FormationOffsets[i % FormationOffsets.Length];
                var desired = baseTile + offset;
                var actual = controller.World.FindNearestWalkable(desired, 3);
                selectedHumanoids[i].SetDestination(actual);
            }
        }

        private void SpawnHumanoid(Vector2Int tile, int index)
        {
            var unitObject = new GameObject("Humanoid " + (index + 1));
            unitObject.transform.SetParent(unitRoot, false);
            unitObject.transform.position = controller.World.TileToWorld(tile);

            var humanoid = unitObject.AddComponent<HumanoidUnit>();
            humanoid.Initialize(controller, tile);
            humanoids.Add(humanoid);
        }

        private void AddSelection(HumanoidUnit humanoid)
        {
            if (selectedHumanoids.Contains(humanoid))
            {
                return;
            }

            selectedHumanoids.Add(humanoid);
            humanoid.SetSelected(true);
        }

        private void ClearSelection()
        {
            for (var i = 0; i < selectedHumanoids.Count; i++)
            {
                if (selectedHumanoids[i] != null)
                {
                    selectedHumanoids[i].SetSelected(false);
                }
            }

            selectedHumanoids.Clear();
        }
    }

    public class HumanoidUnit : MonoBehaviour, IDamageable
    {
        private GameController controller;
        private Transform modelRoot;
        private Transform selectionRing;
        private float maxHealth = 55f;
        private float currentHealth = 55f;
        private float moveSpeed = 3.6f;
        private float attackCooldown;

        private Vector2Int destinationTile;
        private bool hasDestination;

        public Vector2Int CurrentTile { get; private set; }
        public bool IsAlive { get; private set; }
        public Vector3 AimPoint => transform.position + Vector3.up * 1.2f;

        public void Initialize(GameController gameController, Vector2Int spawnTile)
        {
            controller = gameController;
            CurrentTile = spawnTile;
            destinationTile = spawnTile;
            IsAlive = true;

            BuildVisuals();
            SyncToCurrentTile();
        }

        private void Update()
        {
            if (!IsAlive || !controller.IsSimulationRunning)
            {
                return;
            }

            attackCooldown -= Time.deltaTime;

            var nearbyEnemy = controller.Waves.FindNearestEnemy(transform.position, 1.6f);
            if (nearbyEnemy != null && attackCooldown <= 0f)
            {
                nearbyEnemy.TakeDamage(10f);
                attackCooldown = 0.65f;
            }

            if (!hasDestination)
            {
                return;
            }

            var targetPosition = controller.World.TileToWorld(destinationTile);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) <= 0.02f)
            {
                CurrentTile = destinationTile;
                hasDestination = false;
                TryHarvest();
            }
        }

        public void SetDestination(Vector2Int tile)
        {
            if (!controller.World.IsHumanoidWalkable(tile))
            {
                return;
            }

            destinationTile = tile;
            hasDestination = true;
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing != null)
            {
                selectionRing.gameObject.SetActive(selected);
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

        private void BuildVisuals()
        {
            modelRoot = new GameObject("Model").transform;
            modelRoot.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(modelRoot, false);
            body.transform.localScale = new Vector3(0.55f, 0.8f, 0.55f);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.82f, 0.74f, 0.55f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(modelRoot, false);
            head.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            head.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            head.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(0.95f, 0.84f, 0.68f));

            selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            selectionRing.SetParent(transform, false);
            selectionRing.localScale = new Vector3(0.65f, 0.03f, 0.65f);
            selectionRing.localPosition = new Vector3(0f, 0.05f, 0f);
            selectionRing.GetComponent<MeshRenderer>().material = Definitions.CreateMaterial(new Color(1f, 0.93f, 0.2f));
            Destroy(selectionRing.GetComponent<Collider>());
            selectionRing.gameObject.SetActive(false);
        }

        private void SyncToCurrentTile()
        {
            transform.position = controller.World.TileToWorld(CurrentTile);
        }

        private void TryHarvest()
        {
            CollectiblePickup pickup;
            if (controller.World.TryHarvest(CurrentTile, out pickup))
            {
                controller.Inventory.Add(pickup.Type, pickup.Amount);
            }
        }

        private void Die()
        {
            IsAlive = false;
            controller.Units.RegisterDeath(this);
            Destroy(gameObject);
        }
    }
}
