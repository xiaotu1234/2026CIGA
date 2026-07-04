using System.Collections.Generic;
using UnityEngine;

namespace BrokenAnchor.Config
{
    public static class PrototypeCatalog
    {
        private const int RoundMaterialCount = 10;

        public static LevelConfig CreateLevel()
        {
            var level = new LevelConfig();
            ItemConfigLoader.ApplyGlobalsToLevel(level);
            return level;
        }

        public static List<MaterialConfig> CreateMaterials()
        {
            var configuredItems = ItemConfigLoader.LoadFromProjectFile(ItemConfigLoader.DefaultProjectRelativePath);
            var roundItems = ItemConfigLoader.SelectRoundItems(configuredItems, RoundMaterialCount);
            var configuredMaterials = ItemConfigLoader.ToMaterialConfigs(ItemConfigLoader.CloneWithItems(configuredItems, roundItems));
            if (configuredMaterials.Count > 0)
            {
                return configuredMaterials;
            }

            var prefabMaterials = PiecePrefabCatalog.LoadMaterialConfigs();
            if (prefabMaterials.Count > 0)
            {
                return prefabMaterials;
            }

            return CreateFallbackMaterials();
        }

        private static List<MaterialConfig> CreateFallbackMaterials()
        {
            return new List<MaterialConfig>
            {
                new MaterialConfig("iron_block", "IronBlock\n铁块", new Vector2(110, 72), 85, 7.8f, 0.55f, 80, 55, 0.45f, 0.35f, 0.25f, 0.2f, new Color(0.36f, 0.43f, 0.49f)),
                new MaterialConfig("wood_plank", "WoodPlank\n木板", new Vector2(170, 46), 28, 0.7f, 0.42f, 38, 30, 0.75f, 0.45f, 0.35f, 0.15f, new Color(0.68f, 0.47f, 0.25f)),
                new MaterialConfig("hook", "Hook\n铁钩", new Vector2(96, 96), 54, 7.6f, 0.38f, 60, 42, 0.55f, 0.5f, 0.3f, 1.2f, new Color(0.74f, 0.78f, 0.78f), true),
                new MaterialConfig("stone", "Stone\n石块", new Vector2(92, 82), 70, 2.5f, 0.28f, 48, 34, 0.65f, 0.7f, 0.7f, 0.55f, new Color(0.43f, 0.42f, 0.37f)),
                new MaterialConfig("tire", "Tire\n轮胎", new Vector2(118, 86), 32, 1.1f, 0.32f, 36, 28, 1.35f, 0.65f, 0.6f, 0.35f, new Color(0.08f, 0.08f, 0.09f))
            };
        }
    }
}
