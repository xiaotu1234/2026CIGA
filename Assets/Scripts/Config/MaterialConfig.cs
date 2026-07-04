using System.Text;
using UnityEngine;

namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class MaterialConfig
    {
        public string id;
        public string displayName;
        public Vector2 size;
        public float weight;
        public float density;
        public float adhesive;
        public float tensileStrength;
        public float shearStrength;
        public float dragCoeff;
        public float frictionCoeff;
        public float supportCoeff;
        public float gripCoeff;
        public Color color;
        public bool hasHookShape;
        public string prefabAssetPath;

        public MaterialConfig(
            string id,
            string displayName,
            Vector2 size,
            float weight,
            float density,
            float adhesive,
            float tensileStrength,
            float shearStrength,
            float dragCoeff,
            float frictionCoeff,
            float supportCoeff,
            float gripCoeff,
            Color color,
            bool hasHookShape = false,
            string prefabAssetPath = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.size = size;
            this.weight = weight;
            this.density = density;
            this.adhesive = adhesive;
            this.tensileStrength = tensileStrength;
            this.shearStrength = shearStrength;
            this.dragCoeff = dragCoeff;
            this.frictionCoeff = frictionCoeff;
            this.supportCoeff = supportCoeff;
            this.gripCoeff = gripCoeff;
            this.color = color;
            this.hasHookShape = hasHookShape;
            this.prefabAssetPath = string.IsNullOrEmpty(prefabAssetPath) ? GetDefaultPrefabAssetPath(id) : prefabAssetPath;
        }
        private static string GetDefaultPrefabAssetPath(string id)
        {
            var builder = new StringBuilder();
            var parts = id.Split('_');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    continue;
                }

                builder.Append(char.ToUpperInvariant(parts[i][0]));
                if (parts[i].Length > 1)
                {
                    builder.Append(parts[i].Substring(1));
                }
            }

            return "Assets/Prefabs/Pieces/" + builder + ".prefab";
        }
    }
}
