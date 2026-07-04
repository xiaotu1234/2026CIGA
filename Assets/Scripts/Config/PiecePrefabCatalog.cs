using System;
using System.Collections.Generic;
using BrokenAnchor.Pieces;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrokenAnchor.Config
{
    public static class PiecePrefabCatalog
    {
        private const string PiecePrefabFolder = "Assets/Prefabs/Pieces";

        public static List<string> LoadPrefabAssetPaths()
        {
            var assetPaths = new List<string>();

#if UNITY_EDITOR
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PiecePrefabFolder });
            Array.Sort(prefabGuids, (left, right) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));
            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    assetPaths.Add(assetPath);
                }
            }
#endif

            return assetPaths;
        }

        public static List<MaterialConfig> LoadMaterialConfigs()
        {
            var materials = new List<MaterialConfig>();

#if UNITY_EDITOR
            var assetPaths = LoadPrefabAssetPaths();
            for (var i = 0; i < assetPaths.Count; i++)
            {
                var assetPath = assetPaths[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                {
                    continue;
                }

                var piece = prefab.GetComponent<AnchorPiece>();
                if (piece == null)
                {
                    continue;
                }

                materials.Add(piece.CreateConfigSnapshot(assetPath));
            }
#endif

            return materials;
        }
    }
}
