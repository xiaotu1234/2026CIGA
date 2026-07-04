using System.Text;
using UnityEngine;

namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class MaterialConfig
    {
        [Tooltip("材料唯一 ID。用于生成按钮名、对象名和默认 Prefab 路径；建议使用小写英文和下划线，例如 iron_block。")]
        public string id;

        [Tooltip("材料显示名称。会显示在材料栏按钮、材料本体标签和模拟画面标签中；可用换行区分英文名和中文名。")]
        public string displayName;

        [Tooltip("材料在拼装界面的 2D 显示尺寸，单位为 UI 像素。主要影响显示和模拟缩放；真实贴合形状优先由 Collider2D 决定，缺少 Collider 时才用该尺寸兜底。")]
        public Vector2 size;

        [Tooltip("材料重量。越重，下沉更快、触底前更不容易被水流拖走，触底后对拉住船也有帮助；过轻会触发风险提示。")]
        public float weight;

        [Tooltip("材料密度。用于表达材质轻重特性，当前原型主要作为调参参考字段，后续可接入浮沉和物理计算。")]
        public float density;

        [Tooltip("材料粘合/贴合能力。用于表达材料边缘连接的可靠性，当前原型保留为连接强度扩展参数。")]
        public float adhesive;

        [Tooltip("抗拉强度。表示连接或材料承受绳索拉扯的能力，当前原型保留为后续断裂计算参数。")]
        public float tensileStrength;

        [Tooltip("抗剪强度。表示连接或材料承受横向错位/剪切冲击的能力，当前原型保留为后续断裂计算参数。")]
        public float shearStrength;

        [Tooltip("水阻系数。越高，下沉阶段越能拖慢锚体和船的漂移，但也会让部件在水流动画中晃动更明显。")]
        public float dragCoeff;

        [Tooltip("摩擦系数。表示触底后与海底的摩擦能力，当前原型主要作为抓地/支撑调参参考字段。")]
        public float frictionCoeff;

        [Tooltip("支撑系数。表示材料触底后提供稳定支撑的能力，当前原型保留为海底支撑扩展参数。")]
        public float supportCoeff;

        [Tooltip("抓地系数。越高，触底后越能拉住船；总抓地过低会触发风险提示并影响结算原因。")]
        public float gripCoeff;

        [Tooltip("材料颜色。当 Prefab 没有 Sprite 时使用该颜色绘制材料；模拟视图中也会作为兜底颜色。")]
        public Color color;

        [Tooltip("是否具有钩形结构。用于标记铁钩等特殊形状材料，当前原型保留为后续抓地和碰撞规则扩展参数。")]
        public bool hasHookShape;

        [Tooltip("材料 Prefab 资源路径。通常由系统按 id 自动写入；如需指定特殊 Prefab，可填写 Assets/Prefabs/Pieces/xxx.prefab。")]
        public string prefabAssetPath;

        public MaterialConfig()
        {
            id = "material";
            displayName = "Material";
            size = new Vector2(100, 80);
            weight = 50f;
            density = 1f;
            adhesive = 0.4f;
            tensileStrength = 40f;
            shearStrength = 30f;
            dragCoeff = 0.5f;
            frictionCoeff = 0.5f;
            supportCoeff = 0.4f;
            gripCoeff = 0.3f;
            color = Color.white;
            prefabAssetPath = GetDefaultPrefabAssetPath(id);
        }

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

        public MaterialConfig Clone(string overridePrefabAssetPath = null)
        {
            return new MaterialConfig(
                id,
                displayName,
                size,
                weight,
                density,
                adhesive,
                tensileStrength,
                shearStrength,
                dragCoeff,
                frictionCoeff,
                supportCoeff,
                gripCoeff,
                color,
                hasHookShape,
                string.IsNullOrEmpty(overridePrefabAssetPath) ? prefabAssetPath : overridePrefabAssetPath);
        }

        private static string GetDefaultPrefabAssetPath(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

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
