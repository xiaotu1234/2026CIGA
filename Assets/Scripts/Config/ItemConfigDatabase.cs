using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrokenAnchor.Config
{
    [Serializable]
    public class GlobalConfig
    {
        public float sustainDurationSeconds = 20f;
        public float initialAnchorDownVelocityMetersPerSecond = 5f;
        public float dropSpeedLimitMetersPerSecond = 4f;
        public float waterSurfaceTensionCoefficient = 1f;
        public float forceCoefficient = 1f;
        public float itemMassScale = 1f;
        public float weightForceScale = 0.008f;
        public float waterDragForceScale = 0.32f;
        public float thresholdCoefficient = 3f;
        public float damageCoefficient = 1f;
        public float healthCoefficient = 0.3f;
        public float jointFrequencyPerDefense = 0.025f;
        public float seabedFriction = 0.70f;
        public float seabedBounciness = 0.02f;
        public int showJointHealthDebug = 1;
    }

    [Serializable]
    public class ItemConfig
    {
        public int itemId;
        public string id;
        public string displayName;
        public string artResource;
        public float weightGrams;
        public float defense;
        public float health;
        public float friction;
        public float sizeCoefficient;
        public int guaranteedSpawnCount;
        public int randomWeight;
        public int unlockLevel;

        public MaterialConfig ToMaterialConfig(string fallbackPrefabAssetPath = null)
        {
            var safeId = string.IsNullOrEmpty(id) ? $"item_{Mathf.Max(0, itemId):000}" : id;
            var safeName = string.IsNullOrEmpty(displayName) ? safeId : displayName;
            var prefabPath = string.IsNullOrEmpty(artResource) ? fallbackPrefabAssetPath : artResource;

            return new MaterialConfig(
                safeId,
                safeName,
                Vector2.zero,
                Mathf.Max(0.1f, weightGrams),
                1f,
                Mathf.Max(0f, health),
                Mathf.Max(0f, defense),
                Mathf.Max(0f, defense),
                Mathf.Clamp01(friction),
                Mathf.Clamp01(friction),
                0.4f,
                Mathf.Clamp01(friction),
                GetFallbackColor(itemId),
                false,
                prefabPath);
        }

        private static Color GetFallbackColor(int seed)
        {
            var hue = Mathf.Repeat(seed * 0.173f, 1f);
            return Color.HSVToRGB(hue, 0.45f, 0.72f);
        }
    }

    [CreateAssetMenu(fileName = "ItemConfigDatabase", menuName = "Broken Anchor/Item Config Database")]
    public class ItemConfigDatabase : ScriptableObject
    {
        public GlobalConfig globals = new GlobalConfig();
        public List<ItemConfig> items = new List<ItemConfig>();
    }

    [Serializable]
    public class ItemConfigJsonData
    {
        public GlobalConfig globals = new GlobalConfig();
        public List<ItemConfig> items = new List<ItemConfig>();
    }

    public static class ItemConfigLoader
    {
        public const string DefaultProjectRelativePath = "Assets/Resources_Prototype/TestData/item_config.json";

        public static GlobalConfig LoadGlobals()
        {
            var data = LoadFromProjectFile(DefaultProjectRelativePath);
            return data?.globals ?? new GlobalConfig();
        }

        public static void ApplyGlobalsToLevel(LevelConfig level)
        {
            if (level == null)
            {
                return;
            }

            var globals = LoadGlobals();
            level.stableDuration = Mathf.Max(0.1f, globals.sustainDurationSeconds);
        }

        public static ItemConfigJsonData LoadFromProjectFile(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath))
            {
                return null;
            }

            var fullPath = Path.Combine(Application.dataPath, "..", projectRelativePath);
            fullPath = Path.GetFullPath(fullPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(fullPath);
                return JsonUtility.FromJson<ItemConfigJsonData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load item config json at {projectRelativePath}: {exception.Message}");
                return null;
            }
        }

        public static List<MaterialConfig> ToMaterialConfigs(ItemConfigJsonData data)
        {
            var materials = new List<MaterialConfig>();
            if (data == null || data.items == null)
            {
                return materials;
            }

            var prefabPaths = PiecePrefabCatalog.LoadPrefabAssetPaths();
            Shuffle(prefabPaths);
            for (var i = 0; i < data.items.Count; i++)
            {
                if (data.items[i] == null)
                {
                    continue;
                }

                var fallbackPrefabPath = prefabPaths.Count > 0 ? prefabPaths[i % prefabPaths.Count] : null;
                materials.Add(data.items[i].ToMaterialConfig(fallbackPrefabPath));
            }

            return materials;
        }

        public static ItemConfigJsonData CloneWithItems(ItemConfigJsonData source, List<ItemConfig> items)
        {
            if (source == null)
            {
                return null;
            }

            return new ItemConfigJsonData
            {
                globals = source.globals,
                items = items ?? new List<ItemConfig>()
            };
        }

        public static List<ItemConfig> SelectRoundItems(ItemConfigJsonData data, int targetCount)
        {
            var selected = new List<ItemConfig>();
            if (data == null || data.items == null || targetCount <= 0)
            {
                return selected;
            }

            var guaranteedItems = new List<ItemConfig>();
            var pool = new List<ItemConfig>();
            for (var i = 0; i < data.items.Count; i++)
            {
                var item = data.items[i];
                if (item == null)
                {
                    continue;
                }

                var guaranteedCount = Mathf.Max(0, item.guaranteedSpawnCount);
                for (var count = 0; count < guaranteedCount; count++)
                {
                    guaranteedItems.Add(item);
                }

                if (guaranteedCount <= 0)
                {
                    pool.Add(item);
                }
            }

            Shuffle(guaranteedItems);
            for (var i = 0; i < guaranteedItems.Count && selected.Count < targetCount; i++)
            {
                selected.Add(guaranteedItems[i]);
            }

            while (selected.Count < targetCount && pool.Count > 0)
            {
                var item = TakeWeightedRandomItem(pool);
                if (item == null)
                {
                    break;
                }

                selected.Add(item);
                pool.Remove(item);
            }

            return selected;
        }

        private static ItemConfig TakeWeightedRandomItem(List<ItemConfig> pool)
        {
            var totalWeight = 0;
            for (var i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(0, pool[i].randomWeight);
            }

            if (totalWeight <= 0)
            {
                return pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            var roll = UnityEngine.Random.Range(0, totalWeight);
            for (var i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(0, pool[i].randomWeight);
                if (roll < 0)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = UnityEngine.Random.Range(0, i + 1);
                var value = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = value;
            }
        }
    }
}
