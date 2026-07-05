using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrokenAnchor.Config
{
    public static class PrototypeJsonConfigCatalog
    {
        private const string TestDataFolder = "Resources_Prototype/TestData";
        private const string ItemConfigFileName = "item_config.json";
        private const string LevelConfigFileName = "level_config.json";
        private const string GlobalConfigFileName = "global_config.json";
        private const int DefaultLevelId = 1;
        private const int MaxRandomDrawAttempts = 1000;

        public static bool TryCreateLevel(out LevelConfig level)
        {
            return TryCreateLevel(DefaultLevelId, out level);
        }

        public static bool TryCreateLevel(int levelId, out LevelConfig level)
        {
            level = null;
            if (!TryLoadJson(LevelConfigFileName, out var levelJson))
            {
                return false;
            }

            var levelFile = JsonUtility.FromJson<LevelConfigFile>(levelJson);
            if (levelFile == null || levelFile.levels == null || levelFile.levels.Length == 0)
            {
                Debug.LogWarning("level_config.json has no usable level rows. Falling back to default LevelConfig.");
                return false;
            }

            var row = FindLevel(levelFile.levels, levelId) ?? levelFile.levels[0];
            level = CreateLevelFromRow(row);
            ApplyGlobalParameters(level);
            return true;
        }

        public static bool TryCreateLevels(out List<LevelConfig> levels)
        {
            levels = null;
            if (!TryLoadJson(LevelConfigFileName, out var levelJson))
            {
                return false;
            }

            var levelFile = JsonUtility.FromJson<LevelConfigFile>(levelJson);
            if (levelFile == null || levelFile.levels == null || levelFile.levels.Length == 0)
            {
                Debug.LogWarning("level_config.json has no usable level rows. Falling back to default LevelConfig.");
                return false;
            }

            levels = new List<LevelConfig>();
            for (var i = 0; i < levelFile.levels.Length; i++)
            {
                var row = levelFile.levels[i];
                if (row == null)
                {
                    continue;
                }

                var level = CreateLevelFromRow(row);
                ApplyGlobalParameters(level);
                levels.Add(level);
            }

            return levels.Count > 0;
        }

        private static LevelConfig CreateLevelFromRow(LevelConfigRow row)
        {
            var defaults = new LevelConfig();
            return new LevelConfig
            {
                levelId = row.levelId <= 0 ? DefaultLevelId : row.levelId,
                shipWeight = row.shipWeightDisplay,
                seaState = string.IsNullOrEmpty(row.stormLevel) ? defaults.seaState : row.stormLevel,
                waterDepth = defaults.waterDepth,
                seabedType = defaults.seabedType,
                dangerZoneDistance = row.initialDistance > 0f ? row.initialDistance : defaults.dangerZoneDistance,
                currentForceBase = row.basePullForce > 0f ? row.basePullForce : defaults.currentForceBase,
                stableDuration = row.buildTimeSeconds > 0f ? row.buildTimeSeconds : defaults.stableDuration,
                waterEntryAttack = defaults.waterEntryAttack,
                sinkWaterAttack = defaults.sinkWaterAttack,
                seabedWaterAttack = defaults.seabedWaterAttack,
                materialCount = Mathf.Max(0, row.minRandomItemCount),
                minRandomItemCount = Mathf.Max(0, row.minRandomItemCount),
                minTotalItemWeight = Mathf.Max(0f, row.minTotalItemWeight),
                recommendedWeightKg = Mathf.Max(0f, row.recommendedWeightKg),
                itemWeightCoefficient = row.itemWeightCoefficient,
                stormLevel = row.stormLevel,
                buildTimeSeconds = row.buildTimeSeconds,
                maxItemSpeed = Mathf.Max(0f, row.shipSpeedDisplay),
                underwaterRightForce = Mathf.Max(0f, row.baseItemCount),
                waterSurfaceTensionForce = row.waterSurfaceTensionForce > 0f ? row.waterSurfaceTensionForce : defaults.waterSurfaceTensionForce
            };
        }

        public static bool TryCreateMaterials(LevelConfig level, out List<MaterialConfig> materials)
        {
            materials = null;
            if (level == null || !TryLoadJson(ItemConfigFileName, out var itemJson))
            {
                return false;
            }

            var itemFile = JsonUtility.FromJson<ItemConfigFile>(itemJson);
            if (itemFile == null || itemFile.items == null || itemFile.items.Length == 0)
            {
                Debug.LogWarning("item_config.json has no usable item rows. Falling back to legacy materials.");
                return false;
            }

            var prefabMap = BuildPrefabMap(PiecePrefabCatalog.LoadMaterialConfigs());
            var availableItems = GetUnlockedItems(itemFile.items, level.levelId);
            var randomPool = new List<ItemConfigRow>();
            materials = new List<MaterialConfig>();
            var totalItemWeight = 0f;

            for (var i = 0; i < availableItems.Count; i++)
            {
                var row = availableItems[i];
                if (row.randomWeight > 0f && CanResolvePrefab(row, prefabMap))
                {
                    randomPool.Add(row);
                }

                var count = Mathf.Max(0, row.guaranteedSpawnCount);
                for (var spawn = 0; spawn < count; spawn++)
                {
                    if (TryCreateMaterial(row, prefabMap, out var material))
                    {
                        materials.Add(material);
                        totalItemWeight += Mathf.Max(0f, row.weightKg);
                    }
                }
            }

            if (IsRandomTargetReached(level, materials.Count, totalItemWeight))
            {
                return true;
            }

            if (randomPool.Count == 0)
            {
                Debug.LogWarning($"Random item pool is empty. Generated {materials.Count} items / {totalItemWeight:0.##}kg, below level {level.levelId} target.");
                return materials.Count > 0;
            }

            var totalRandomWeight = CalculateTotalRandomWeight(randomPool);
            if (totalRandomWeight <= 0f)
            {
                Debug.LogWarning($"Random item total weight is 0. Generated {materials.Count} items / {totalItemWeight:0.##}kg, below level {level.levelId} target.");
                return materials.Count > 0;
            }

            var attempts = 0;
            while (!IsRandomTargetReached(level, materials.Count, totalItemWeight) && attempts < MaxRandomDrawAttempts)
            {
                attempts++;
                var row = DrawWeightedItem(randomPool, totalRandomWeight);
                if (row == null)
                {
                    break;
                }

                if (TryCreateMaterial(row, prefabMap, out var material))
                {
                    materials.Add(material);
                    totalItemWeight += Mathf.Max(0f, row.weightKg);
                }
            }

            if (!IsRandomTargetReached(level, materials.Count, totalItemWeight))
            {
                Debug.LogWarning($"Random item draw stopped after {attempts} attempts without reaching target: count {materials.Count}/{level.minRandomItemCount}, weight {totalItemWeight:0.##}/{level.minTotalItemWeight:0.##}kg. Check config.");
            }

            return materials.Count > 0;
        }

        private static Dictionary<string, MaterialConfig> BuildPrefabMap(IReadOnlyList<MaterialConfig> prefabMaterials)
        {
            var map = new Dictionary<string, MaterialConfig>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < prefabMaterials.Count; i++)
            {
                var material = prefabMaterials[i];
                if (material == null)
                {
                    continue;
                }

                AddPrefabKey(map, material.id, material);
                AddPrefabKey(map, Path.GetFileNameWithoutExtension(material.prefabAssetPath), material);
            }

            return map;
        }

        private static void AddPrefabKey(Dictionary<string, MaterialConfig> map, string key, MaterialConfig material)
        {
            key = NormalizeKey(key);
            if (string.IsNullOrEmpty(key) || map.ContainsKey(key))
            {
                return;
            }

            map.Add(key, material);
        }

        private static bool CanResolvePrefab(ItemConfigRow row, Dictionary<string, MaterialConfig> prefabMap)
        {
            return prefabMap.ContainsKey(NormalizeKey(row.prefab));
        }

        private static bool TryCreateMaterial(ItemConfigRow row, Dictionary<string, MaterialConfig> prefabMap, out MaterialConfig material)
        {
            material = null;
            if (!prefabMap.TryGetValue(NormalizeKey(row.prefab), out var prefabMaterial))
            {
                Debug.LogWarning($"Item {row.itemId}({row.itemName}) prefab mapping not found: {row.prefab}. Skipped.");
                return false;
            }

            material = prefabMaterial.Clone(prefabMaterial.prefabAssetPath);
            material.id = $"item_{row.itemId}_{NormalizeKey(row.prefab)}";
            material.displayName = string.IsNullOrEmpty(row.itemName) ? prefabMaterial.displayName : row.itemName;
            material.materialName = string.IsNullOrEmpty(row.materialName) ? prefabMaterial.materialName : row.materialName;
            material.weight = Mathf.Max(0f, row.weightKg);
            material.adhesive = Mathf.Max(0f, row.health);
            material.tensileStrength = Mathf.Max(0f, row.defense);
            return true;
        }

        private static List<ItemConfigRow> GetUnlockedItems(IReadOnlyList<ItemConfigRow> items, int levelId)
        {
            var availableItems = new List<ItemConfigRow>();
            for (var i = 0; i < items.Count; i++)
            {
                var row = items[i];
                if (row != null && row.unlockLevel <= Mathf.Max(1, levelId))
                {
                    availableItems.Add(row);
                }
            }

            return availableItems;
        }

        private static bool IsRandomTargetReached(LevelConfig level, int count, float totalItemWeight)
        {
            return count >= level.minRandomItemCount && totalItemWeight >= level.minTotalItemWeight;
        }

        private static float CalculateTotalRandomWeight(IReadOnlyList<ItemConfigRow> randomPool)
        {
            var total = 0f;
            for (var i = 0; i < randomPool.Count; i++)
            {
                total += Mathf.Max(0f, randomPool[i].randomWeight);
            }

            return total;
        }

        private static ItemConfigRow DrawWeightedItem(IReadOnlyList<ItemConfigRow> randomPool, float totalRandomWeight)
        {
            var roll = UnityEngine.Random.Range(0f, totalRandomWeight);
            var cursor = 0f;
            for (var i = 0; i < randomPool.Count; i++)
            {
                cursor += Mathf.Max(0f, randomPool[i].randomWeight);
                if (roll <= cursor)
                {
                    return randomPool[i];
                }
            }

            return randomPool.Count > 0 ? randomPool[randomPool.Count - 1] : null;
        }

        private static LevelConfigRow FindLevel(IReadOnlyList<LevelConfigRow> levels, int levelId)
        {
            for (var i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null && levels[i].levelId == levelId)
                {
                    return levels[i];
                }
            }

            return null;
        }

        private static void ApplyGlobalParameters(LevelConfig level)
        {
            if (!TryLoadJson(GlobalConfigFileName, out var globalJson))
            {
                return;
            }

            var globalFile = JsonUtility.FromJson<GlobalConfigFile>(globalJson);
            if (globalFile == null || globalFile.parameters == null)
            {
                return;
            }

            var parameters = globalFile.parameters;
            level.stableDuration = parameters.successDurationSeconds > 0f ? parameters.successDurationSeconds : level.stableDuration;
            level.fallSpeedSoftLimitMetersPerSecond = parameters.fallSpeedSoftLimitMetersPerSecond;
            level.waterEntryInitialSpeed = parameters.waterEntryInitialSpeed > 0f ? parameters.waterEntryInitialSpeed : level.waterEntryInitialSpeed;
            level.forceCoefficient = parameters.forceCoefficient;
            level.weightDownForceCoefficient = parameters.weightDownForceCoefficient;
            level.waterDragCoefficient = parameters.waterDragCoefficient;
            level.thresholdCoefficient = parameters.thresholdCoefficient;
            level.damageCoefficient = parameters.damageCoefficient;
            level.healthCoefficient = parameters.healthCoefficient;
            level.defenseToJointFrequencyCoefficient = parameters.defenseToJointFrequencyCoefficient;
            level.seabedPhysicsFrictionCoefficient = parameters.seabedPhysicsFrictionCoefficient;
            level.seabedPhysicsBouncinessCoefficient = parameters.seabedPhysicsBouncinessCoefficient;
            level.showConnectionHealthDebug = parameters.showConnectionHealthDebug > 0.5f;
            level.rigidbodyMassCoefficient = parameters.rigidbodyMassCoefficient;
        }

        private static bool TryLoadJson(string fileName, out string json)
        {
            json = null;
            if (RuntimeAssetCatalog.TryGetJsonText(fileName, out json))
            {
                return true;
            }

            var path = Path.Combine(Application.dataPath, TestDataFolder, fileName);
            if (!File.Exists(path))
            {
                return false;
            }

            json = File.ReadAllText(path);
            return !string.IsNullOrEmpty(json);
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrEmpty(key) ? string.Empty : key.Trim();
        }

        [Serializable]
        private class ItemConfigFile
        {
            public ItemConfigRow[] items;
        }

        [Serializable]
        private class ItemConfigRow
        {
            public int itemId;
            public string itemName;
            public string materialName;
            public string prefab;
            public float weightKg;
            public float defense;
            public float health;
            public float sizeFactorReference;
            public int guaranteedSpawnCount;
            public float randomWeight;
            public int unlockLevel;
        }

        [Serializable]
        private class LevelConfigFile
        {
            public LevelConfigRow[] levels;
        }

        [Serializable]
        private class LevelConfigRow
        {
            public int levelId;
            public float shipWeightDisplay;
            public float initialDistance;
            public float shipSpeedDisplay;
            public float basePullForce;
            public float baseItemCount;
            public int minRandomItemCount;
            public float minTotalItemWeight;
            public float recommendedWeightKg;
            public float itemWeightCoefficient;
            public string stormLevel;
            public float buildTimeSeconds;
            public float waterSurfaceTensionForce;
        }

        [Serializable]
        private class GlobalConfigFile
        {
            public GlobalParameters parameters;
        }

        [Serializable]
        private class GlobalParameters
        {
            public float successDurationSeconds;
            public float fallSpeedSoftLimitMetersPerSecond;
            public float waterEntryInitialSpeed;
            public float forceCoefficient;
            public float weightDownForceCoefficient;
            public float waterDragCoefficient;
            public float thresholdCoefficient;
            public float damageCoefficient;
            public float healthCoefficient;
            public float defenseToJointFrequencyCoefficient;
            public float seabedPhysicsFrictionCoefficient;
            public float seabedPhysicsBouncinessCoefficient;
            public float showConnectionHealthDebug;
            public float rigidbodyMassCoefficient;
        }
    }
}
