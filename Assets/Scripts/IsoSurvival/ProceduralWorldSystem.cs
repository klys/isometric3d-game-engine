using System.Collections.Generic;
using UnityEngine;

namespace IsoSurvival
{
    public class ProceduralWorldSystem : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, TileData> tileCache = new Dictionary<Vector2Int, TileData>();
        private readonly Dictionary<Vector2Int, TileRuntime> liveTiles = new Dictionary<Vector2Int, TileRuntime>();
        private readonly Dictionary<Vector2Int, GameObject> chunkRoots = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, CollectibleRuntime> liveCollectibles = new Dictionary<Vector2Int, CollectibleRuntime>();
        private readonly HashSet<Vector2Int> depletedCollectibles = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> occupiedByBuildings = new HashSet<Vector2Int>();

        private GameController controller;
        private Transform worldRoot;
        private GameSettings settings;
        private float refreshCountdown;

        public void Initialize(GameController gameController)
        {
            controller = gameController;
        }

        private void Update()
        {
            if (!controller.IsSimulationRunning || settings == null)
            {
                return;
            }

            refreshCountdown -= Time.deltaTime;
            if (refreshCountdown > 0f)
            {
                return;
            }

            refreshCountdown = 0.25f;
            var focusTile = controller.Units.GetFocusTile();
            RefreshVisibleChunks(focusTile);
        }

        public void BeginSession(GameSettings newSettings)
        {
            settings = newSettings;
            worldRoot = new GameObject("World").transform;
            tileCache.Clear();
            liveTiles.Clear();
            chunkRoots.Clear();
            liveCollectibles.Clear();
            depletedCollectibles.Clear();
            occupiedByBuildings.Clear();
            refreshCountdown = 0f;
            RefreshVisibleChunks(Vector2Int.zero);
        }

        public void ClearSession()
        {
            if (worldRoot != null)
            {
                Destroy(worldRoot.gameObject);
            }

            settings = null;
            tileCache.Clear();
            liveTiles.Clear();
            chunkRoots.Clear();
            liveCollectibles.Clear();
            depletedCollectibles.Clear();
            occupiedByBuildings.Clear();
        }

        public Vector3 TileToWorld(Vector2Int tile)
        {
            var data = GetTileData(tile);
            return new Vector3(tile.x * settings.TileWorldSize, data.TopHeight, tile.y * settings.TileWorldSize);
        }

        public TileData GetTileData(Vector2Int tile)
        {
            TileData data;
            if (tileCache.TryGetValue(tile, out data))
            {
                return data;
            }

            data = GenerateTileData(tile);
            tileCache[tile] = data;
            return data;
        }

        public bool TryGetTileFromHit(RaycastHit hit, out Vector2Int tile)
        {
            tile = Vector2Int.zero;
            var marker = hit.collider.GetComponentInParent<TileMarker>();
            if (marker == null)
            {
                return false;
            }

            tile = marker.Coordinate;
            return true;
        }

        public bool IsHumanoidWalkable(Vector2Int tile)
        {
            var data = GetTileData(tile);
            return data.SupportsHumanoids && !occupiedByBuildings.Contains(tile);
        }

        public bool IsOccupied(Vector2Int tile)
        {
            return occupiedByBuildings.Contains(tile);
        }

        public bool ReserveBuilding(Vector2Int tile)
        {
            if (occupiedByBuildings.Contains(tile) || !GetTileData(tile).IsBuildable)
            {
                return false;
            }

            occupiedByBuildings.Add(tile);
            return true;
        }

        public void ReleaseBuilding(Vector2Int tile)
        {
            occupiedByBuildings.Remove(tile);
        }

        public bool TryHarvest(Vector2Int tile, out CollectiblePickup pickup)
        {
            pickup = default(CollectiblePickup);

            if (depletedCollectibles.Contains(tile))
            {
                return false;
            }

            var data = GetTileData(tile);
            if (data.CollectibleType == CollectibleType.None || data.CollectibleAmount <= 0)
            {
                return false;
            }

            depletedCollectibles.Add(tile);
            CollectibleRuntime runtime;
            if (liveCollectibles.TryGetValue(tile, out runtime) && runtime != null)
            {
                Destroy(runtime.gameObject);
            }

            liveCollectibles.Remove(tile);
            pickup = new CollectiblePickup(data.CollectibleType, data.CollectibleAmount);
            return true;
        }

        public Vector2Int FindNearestWalkable(Vector2Int center, int maxRadius)
        {
            if (IsHumanoidWalkable(center))
            {
                return center;
            }

            for (var radius = 1; radius <= maxRadius; radius++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var top = new Vector2Int(center.x + x, center.y + radius);
                    if (IsHumanoidWalkable(top))
                    {
                        return top;
                    }

                    var bottom = new Vector2Int(center.x + x, center.y - radius);
                    if (IsHumanoidWalkable(bottom))
                    {
                        return bottom;
                    }
                }

                for (var y = -radius + 1; y <= radius - 1; y++)
                {
                    var left = new Vector2Int(center.x - radius, center.y + y);
                    if (IsHumanoidWalkable(left))
                    {
                        return left;
                    }

                    var right = new Vector2Int(center.x + radius, center.y + y);
                    if (IsHumanoidWalkable(right))
                    {
                        return right;
                    }
                }
            }

            return center;
        }

        private void RefreshVisibleChunks(Vector2Int focusTile)
        {
            var centerChunk = new Vector2Int(
                Mathf.FloorToInt((float)focusTile.x / settings.ChunkSize),
                Mathf.FloorToInt((float)focusTile.y / settings.ChunkSize));

            var keepChunks = new HashSet<Vector2Int>();
            for (var x = -settings.ChunkViewRadius; x <= settings.ChunkViewRadius; x++)
            {
                for (var y = -settings.ChunkViewRadius; y <= settings.ChunkViewRadius; y++)
                {
                    var chunk = new Vector2Int(centerChunk.x + x, centerChunk.y + y);
                    keepChunks.Add(chunk);
                    if (!chunkRoots.ContainsKey(chunk))
                    {
                        CreateChunk(chunk);
                    }
                }
            }

            var staleChunks = new List<Vector2Int>();
            foreach (var pair in chunkRoots)
            {
                if (!keepChunks.Contains(pair.Key))
                {
                    staleChunks.Add(pair.Key);
                }
            }

            for (var i = 0; i < staleChunks.Count; i++)
            {
                DestroyChunk(staleChunks[i]);
            }
        }

        private void CreateChunk(Vector2Int chunkCoordinate)
        {
            var chunkRoot = new GameObject("Chunk " + chunkCoordinate.x + "," + chunkCoordinate.y);
            chunkRoot.transform.SetParent(worldRoot, false);
            chunkRoots[chunkCoordinate] = chunkRoot;

            for (var x = 0; x < settings.ChunkSize; x++)
            {
                for (var y = 0; y < settings.ChunkSize; y++)
                {
                    var tileCoordinate = new Vector2Int(
                        chunkCoordinate.x * settings.ChunkSize + x,
                        chunkCoordinate.y * settings.ChunkSize + y);

                    CreateTile(tileCoordinate, chunkRoot.transform);
                }
            }
        }

        private void DestroyChunk(Vector2Int chunkCoordinate)
        {
            GameObject chunkRoot;
            if (!chunkRoots.TryGetValue(chunkCoordinate, out chunkRoot))
            {
                return;
            }

            var keysToRemove = new List<Vector2Int>();
            foreach (var tile in liveTiles)
            {
                var chunk = new Vector2Int(
                    Mathf.FloorToInt((float)tile.Key.x / settings.ChunkSize),
                    Mathf.FloorToInt((float)tile.Key.y / settings.ChunkSize));

                if (chunk == chunkCoordinate)
                {
                    keysToRemove.Add(tile.Key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                liveTiles.Remove(keysToRemove[i]);
                liveCollectibles.Remove(keysToRemove[i]);
            }

            Destroy(chunkRoot);
            chunkRoots.Remove(chunkCoordinate);
        }

        private void CreateTile(Vector2Int coordinate, Transform parent)
        {
            var data = GetTileData(coordinate);
            var tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileObject.name = "Tile " + coordinate.x + "," + coordinate.y;
            tileObject.transform.SetParent(parent, false);

            var footprint = settings.TileWorldSize;
            var height = Mathf.Max(0.2f, data.TopHeight + 0.25f);
            tileObject.transform.localScale = new Vector3(footprint, height, footprint);
            tileObject.transform.position = new Vector3(
                coordinate.x * footprint,
                data.TopHeight - height * 0.5f,
                coordinate.y * footprint);

            var renderer = tileObject.GetComponent<MeshRenderer>();
            renderer.material = Definitions.CreateMaterial(Definitions.GetBiomeColor(data.Biome));

            var marker = tileObject.AddComponent<TileMarker>();
            marker.Coordinate = coordinate;

            liveTiles[coordinate] = new TileRuntime
            {
                Coordinate = coordinate,
                Data = data,
                GameObject = tileObject
            };

            if (!depletedCollectibles.Contains(coordinate) && data.CollectibleType != CollectibleType.None)
            {
                CreateCollectible(coordinate, data, tileObject.transform);
            }
        }

        private void CreateCollectible(Vector2Int coordinate, TileData data, Transform parent)
        {
            var primitive = PrimitiveType.Cylinder;
            var scale = new Vector3(0.25f, 0.3f, 0.25f);
            var verticalOffset = 0.5f;

            switch (data.CollectibleType)
            {
                case CollectibleType.Plant:
                    primitive = PrimitiveType.Capsule;
                    scale = new Vector3(0.25f, 0.45f, 0.25f);
                    break;
                case CollectibleType.Flower:
                    primitive = PrimitiveType.Sphere;
                    scale = new Vector3(0.3f, 0.3f, 0.3f);
                    verticalOffset = 0.35f;
                    break;
                case CollectibleType.Rock:
                    primitive = PrimitiveType.Cube;
                    scale = new Vector3(0.35f, 0.28f, 0.35f);
                    verticalOffset = 0.22f;
                    break;
                case CollectibleType.Mineral:
                    primitive = PrimitiveType.Cylinder;
                    scale = new Vector3(0.2f, 0.55f, 0.2f);
                    break;
            }

            var collectibleObject = GameObject.CreatePrimitive(primitive);
            collectibleObject.name = data.CollectibleType.ToString();
            collectibleObject.transform.SetParent(parent, false);
            collectibleObject.transform.localScale = scale;
            collectibleObject.transform.position = TileToWorld(coordinate) + new Vector3(0f, verticalOffset, 0f);

            var renderer = collectibleObject.GetComponent<MeshRenderer>();
            renderer.material = Definitions.CreateMaterial(Definitions.GetCollectibleColor(data.CollectibleType));
            Destroy(collectibleObject.GetComponent<Collider>());

            var runtime = collectibleObject.AddComponent<CollectibleRuntime>();
            runtime.Coordinate = coordinate;
            liveCollectibles[coordinate] = runtime;
        }

        private TileData GenerateTileData(Vector2Int coordinate)
        {
            var heightNoise = FractalNoise(coordinate, 0.02f, 3, 0.5f, 0.55f, 17f);
            var temperature = FractalNoise(coordinate, 0.013f, 3, 0.55f, 0.6f, 113f);
            var moisture = FractalNoise(coordinate, 0.015f, 4, 0.52f, 0.55f, 239f);
            var riftNoise = FractalNoise(coordinate, 0.04f, 2, 0.55f, 0.55f, 401f);

            var biome = BiomeType.Plains;
            if (heightNoise < 0.27f)
            {
                biome = BiomeType.Sea;
            }
            else if (heightNoise < 0.32f)
            {
                biome = BiomeType.Beach;
            }
            else if (riftNoise > 0.82f && heightNoise > 0.45f)
            {
                biome = BiomeType.Rift;
            }
            else if (heightNoise > 0.8f || (temperature < 0.32f && heightNoise > 0.68f))
            {
                biome = BiomeType.Snow;
            }
            else if (heightNoise > 0.7f)
            {
                biome = BiomeType.Mountains;
            }
            else if (moisture > 0.7f)
            {
                biome = BiomeType.Forest;
            }
            else if (temperature > 0.65f && moisture < 0.4f)
            {
                biome = BiomeType.Savanna;
            }

            var topHeight = 0.15f + Mathf.Pow(heightNoise, 1.35f) * 2.7f;
            if (biome == BiomeType.Sea)
            {
                topHeight = 0.1f + heightNoise * 0.2f;
            }
            else if (biome == BiomeType.Beach)
            {
                topHeight = 0.22f + heightNoise * 0.25f;
            }
            else if (biome == BiomeType.Rift)
            {
                topHeight = 0.7f + heightNoise * 1.4f;
            }

            var resourceNoise = FractalNoise(coordinate, 0.08f, 2, 0.5f, 0.6f, 777f);
            var collectibleType = CollectibleType.None;
            var collectibleAmount = 0;

            if (biome != BiomeType.Sea && resourceNoise > 0.58f)
            {
                switch (biome)
                {
                    case BiomeType.Beach:
                        collectibleType = CollectibleType.Rock;
                        collectibleAmount = 2;
                        break;
                    case BiomeType.Plains:
                        collectibleType = resourceNoise > 0.8f ? CollectibleType.Flower : CollectibleType.Plant;
                        collectibleAmount = resourceNoise > 0.8f ? 2 : 3;
                        break;
                    case BiomeType.Savanna:
                        collectibleType = CollectibleType.Plant;
                        collectibleAmount = 3;
                        break;
                    case BiomeType.Forest:
                        collectibleType = resourceNoise > 0.78f ? CollectibleType.Flower : CollectibleType.Plant;
                        collectibleAmount = resourceNoise > 0.78f ? 2 : 4;
                        break;
                    case BiomeType.Mountains:
                    case BiomeType.Rift:
                        collectibleType = resourceNoise > 0.82f ? CollectibleType.Mineral : CollectibleType.Rock;
                        collectibleAmount = resourceNoise > 0.82f ? 2 : 3;
                        break;
                    case BiomeType.Snow:
                        collectibleType = resourceNoise > 0.77f ? CollectibleType.Mineral : CollectibleType.Rock;
                        collectibleAmount = 2;
                        break;
                }
            }

            return new TileData(coordinate, biome, topHeight, collectibleType, collectibleAmount);
        }

        private float FractalNoise(Vector2Int coordinate, float scale, int octaves, float persistence, float lacunarity, float offset)
        {
            var amplitude = 1f;
            var frequency = 1f;
            var sum = 0f;
            var weight = 0f;

            for (var i = 0; i < octaves; i++)
            {
                var x = (coordinate.x + settings.Seed * 0.37f + offset) * scale * frequency;
                var y = (coordinate.y - settings.Seed * 0.21f - offset) * scale * frequency;
                sum += Mathf.PerlinNoise(x, y) * amplitude;
                weight += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return sum / Mathf.Max(0.001f, weight);
        }
        private class TileRuntime
        {
            public Vector2Int Coordinate;
            public TileData Data;
            public GameObject GameObject;
        }
    }

    public class TileMarker : MonoBehaviour
    {
        public Vector2Int Coordinate;
    }

    public class CollectibleRuntime : MonoBehaviour
    {
        public Vector2Int Coordinate;
    }
}
