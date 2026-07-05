using System;
using System.IO;
using UnityEngine;

namespace BrokenAnchor.Config
{
    public class RuntimeAssetCatalog : ScriptableObject
    {
        private const string ResourcePath = "RuntimeAssetCatalog";

        [SerializeField] private TextAsset itemConfigJson;
        [SerializeField] private TextAsset levelConfigJson;
        [SerializeField] private TextAsset globalConfigJson;
        [SerializeField] private GameObject[] piecePrefabs = Array.Empty<GameObject>();

        private static RuntimeAssetCatalog cached;

        public static RuntimeAssetCatalog Instance
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<RuntimeAssetCatalog>(ResourcePath);
                }

                return cached;
            }
        }

        public static GameObject[] PiecePrefabs => Instance == null || Instance.piecePrefabs == null
            ? Array.Empty<GameObject>()
            : Instance.piecePrefabs;

        public static bool TryGetJsonText(string fileName, out string json)
        {
            json = null;
            var catalog = Instance;
            if (catalog == null)
            {
                return false;
            }

            var asset = catalog.GetJsonAsset(fileName);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                return false;
            }

            json = asset.text;
            return true;
        }

        public static GameObject LoadPiecePrefab(string assetPath)
        {
            var catalog = Instance;
            return catalog == null ? null : catalog.FindPiecePrefab(assetPath);
        }

        public static string GetPiecePrefabAssetPath(GameObject prefab)
        {
            return prefab == null ? string.Empty : $"Assets/Prefabs/Pieces/{prefab.name}.prefab";
        }

        private TextAsset GetJsonAsset(string fileName)
        {
            switch (fileName)
            {
                case "item_config.json":
                    return itemConfigJson;
                case "level_config.json":
                    return levelConfigJson;
                case "global_config.json":
                    return globalConfigJson;
                default:
                    return null;
            }
        }

        private GameObject FindPiecePrefab(string assetPath)
        {
            if (piecePrefabs == null || piecePrefabs.Length == 0)
            {
                return null;
            }

            var requestedName = Path.GetFileNameWithoutExtension(assetPath);
            for (var i = 0; i < piecePrefabs.Length; i++)
            {
                var prefab = piecePrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                if (string.Equals(prefab.name, requestedName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetPiecePrefabAssetPath(prefab), assetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return prefab;
                }
            }

            return null;
        }
    }
}
