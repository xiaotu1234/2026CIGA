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

        public static List<MaterialConfig> LoadMaterialConfigs()
        {
            var materials = new List<MaterialConfig>();
            var runtimePrefabs = RuntimeAssetCatalog.PiecePrefabs;
            for (var i = 0; i < runtimePrefabs.Length; i++)
            {
                var prefab = runtimePrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                var piece = prefab.GetComponent<AnchorPiece>();
                if (piece == null)
                {
                    continue;
                }

                materials.Add(piece.CreateConfigSnapshot(RuntimeAssetCatalog.GetPiecePrefabAssetPath(prefab)));
            }

            if (materials.Count > 0)
            {
                return materials;
            }

#if UNITY_EDITOR
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PiecePrefabFolder });
            Array.Sort(prefabGuids, (left, right) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));
            for (var i = 0; i < prefabGuids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
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

        public static GameObject LoadPrefab(string assetPath)
        {
            var runtimePrefab = RuntimeAssetCatalog.LoadPiecePrefab(assetPath);
            if (runtimePrefab != null)
            {
                return runtimePrefab;
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(assetPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif
            return null;
        }
    }
}
