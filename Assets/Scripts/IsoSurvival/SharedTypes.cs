using System;
using System.Collections.Generic;
using UnityEngine;

namespace IsoSurvival
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public enum BiomeType
    {
        Sea,
        Beach,
        Plains,
        Savanna,
        Forest,
        Mountains,
        Rift,
        Snow
    }

    public enum CollectibleType
    {
        None,
        Plant,
        Flower,
        Rock,
        Mineral
    }

    public enum BuildingType
    {
        House,
        Wall,
        Tower
    }

    public enum EnemyType
    {
        Ground,
        Underground,
        Flying,
        Water
    }

    [Serializable]
    public class GameSettings
    {
        public int Seed;
        public int StartingHumanoids = 4;
        public int ChunkSize = 10;
        public int ChunkViewRadius = 3;
        public float TileWorldSize = 1.6f;
        public float WaveIntervalSeconds = 28f;
        public float EnemyIntensity = 1f;

        public int StartingPlants = 14;
        public int StartingFlowers = 8;
        public int StartingRocks = 18;
        public int StartingMinerals = 6;

        public void Clamp()
        {
            StartingHumanoids = Mathf.Clamp(StartingHumanoids, 1, 12);
            ChunkSize = Mathf.Clamp(ChunkSize, 8, 16);
            ChunkViewRadius = Mathf.Clamp(ChunkViewRadius, 2, 5);
            WaveIntervalSeconds = Mathf.Clamp(WaveIntervalSeconds, 10f, 90f);
            EnemyIntensity = Mathf.Clamp(EnemyIntensity, 0.5f, 3f);
        }

        public GameSettings Clone()
        {
            return new GameSettings
            {
                Seed = Seed,
                StartingHumanoids = StartingHumanoids,
                ChunkSize = ChunkSize,
                ChunkViewRadius = ChunkViewRadius,
                TileWorldSize = TileWorldSize,
                WaveIntervalSeconds = WaveIntervalSeconds,
                EnemyIntensity = EnemyIntensity,
                StartingPlants = StartingPlants,
                StartingFlowers = StartingFlowers,
                StartingRocks = StartingRocks,
                StartingMinerals = StartingMinerals
            };
        }
    }

    public readonly struct TileData
    {
        public TileData(
            Vector2Int coordinate,
            BiomeType biome,
            float topHeight,
            CollectibleType collectibleType,
            int collectibleAmount)
        {
            Coordinate = coordinate;
            Biome = biome;
            TopHeight = topHeight;
            CollectibleType = collectibleType;
            CollectibleAmount = collectibleAmount;
        }

        public Vector2Int Coordinate { get; }
        public BiomeType Biome { get; }
        public float TopHeight { get; }
        public CollectibleType CollectibleType { get; }
        public int CollectibleAmount { get; }

        public bool IsWater => Biome == BiomeType.Sea;
        public bool IsBuildable => Biome != BiomeType.Sea && Biome != BiomeType.Rift;
        public bool SupportsHumanoids => Biome != BiomeType.Sea;
    }

    public readonly struct CollectiblePickup
    {
        public CollectiblePickup(CollectibleType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public CollectibleType Type { get; }
        public int Amount { get; }
        public bool IsValid => Type != CollectibleType.None && Amount > 0;
    }

    public readonly struct BuildingDefinition
    {
        public BuildingDefinition(
            BuildingType type,
            int plants,
            int flowers,
            int rocks,
            int minerals,
            float maxHealth,
            float range,
            float damage,
            float cooldown)
        {
            Type = type;
            Plants = plants;
            Flowers = flowers;
            Rocks = rocks;
            Minerals = minerals;
            MaxHealth = maxHealth;
            Range = range;
            Damage = damage;
            Cooldown = cooldown;
        }

        public BuildingType Type { get; }
        public int Plants { get; }
        public int Flowers { get; }
        public int Rocks { get; }
        public int Minerals { get; }
        public float MaxHealth { get; }
        public float Range { get; }
        public float Damage { get; }
        public float Cooldown { get; }
    }

    public readonly struct EnemyDefinition
    {
        public EnemyDefinition(EnemyType type, float maxHealth, float speed, float damage, float attackRange)
        {
            Type = type;
            MaxHealth = maxHealth;
            Speed = speed;
            Damage = damage;
            AttackRange = attackRange;
        }

        public EnemyType Type { get; }
        public float MaxHealth { get; }
        public float Speed { get; }
        public float Damage { get; }
        public float AttackRange { get; }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Vector3 AimPoint { get; }
        void TakeDamage(float damage);
    }

    public static class Definitions
    {
        private static readonly Dictionary<BuildingType, BuildingDefinition> BuildingDefinitions =
            new Dictionary<BuildingType, BuildingDefinition>
            {
                { BuildingType.House, new BuildingDefinition(BuildingType.House, 6, 2, 4, 0, 110f, 0f, 0f, 0f) },
                { BuildingType.Wall, new BuildingDefinition(BuildingType.Wall, 0, 0, 5, 0, 180f, 0f, 0f, 0f) },
                { BuildingType.Tower, new BuildingDefinition(BuildingType.Tower, 0, 0, 6, 3, 130f, 8f, 16f, 0.9f) }
            };

        private static readonly Dictionary<EnemyType, EnemyDefinition> EnemyDefinitions =
            new Dictionary<EnemyType, EnemyDefinition>
            {
                { EnemyType.Ground, new EnemyDefinition(EnemyType.Ground, 42f, 2.2f, 7f, 1.2f) },
                { EnemyType.Underground, new EnemyDefinition(EnemyType.Underground, 54f, 2.8f, 9f, 1.1f) },
                { EnemyType.Flying, new EnemyDefinition(EnemyType.Flying, 34f, 3.3f, 6f, 1.4f) },
                { EnemyType.Water, new EnemyDefinition(EnemyType.Water, 48f, 2.0f, 8f, 1.25f) }
            };

        public static BuildingDefinition GetBuilding(BuildingType type)
        {
            return BuildingDefinitions[type];
        }

        public static EnemyDefinition GetEnemy(EnemyType type)
        {
            return EnemyDefinitions[type];
        }

        public static Color GetBiomeColor(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Sea:
                    return new Color(0.17f, 0.45f, 0.75f);
                case BiomeType.Beach:
                    return new Color(0.87f, 0.8f, 0.56f);
                case BiomeType.Plains:
                    return new Color(0.42f, 0.7f, 0.34f);
                case BiomeType.Savanna:
                    return new Color(0.74f, 0.67f, 0.31f);
                case BiomeType.Forest:
                    return new Color(0.2f, 0.5f, 0.24f);
                case BiomeType.Mountains:
                    return new Color(0.49f, 0.49f, 0.52f);
                case BiomeType.Rift:
                    return new Color(0.35f, 0.23f, 0.2f);
                case BiomeType.Snow:
                    return new Color(0.92f, 0.95f, 0.98f);
                default:
                    return Color.magenta;
            }
        }

        public static Color GetCollectibleColor(CollectibleType collectible)
        {
            switch (collectible)
            {
                case CollectibleType.Plant:
                    return new Color(0.24f, 0.79f, 0.3f);
                case CollectibleType.Flower:
                    return new Color(1f, 0.58f, 0.75f);
                case CollectibleType.Rock:
                    return new Color(0.57f, 0.57f, 0.6f);
                case CollectibleType.Mineral:
                    return new Color(0.39f, 0.9f, 0.91f);
                default:
                    return Color.white;
            }
        }

        public static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }
    }
}
