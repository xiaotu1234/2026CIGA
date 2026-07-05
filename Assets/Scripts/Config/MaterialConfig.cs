using System.Text;
using UnityEngine;

namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class MaterialConfig
    {
        [Tooltip("【已生效】材料唯一 ID。用于生成按钮名、对象名和默认 Prefab 路径；建议使用小写英文和下划线，例如 iron_block。")]
        public string id;

        [Tooltip("【已生效】材料显示名称。会显示在材料栏按钮、材料本体标签和模拟画面标签中；可用换行区分英文名和中文名。")]
        public string displayName;
        public string materialName;

        [Tooltip("【已生效】材料在拼装界面的 2D 显示尺寸，单位为 UI 像素。影响显示、模拟缩放和无 Collider 时的贴合兜底判定。")]
        public Vector2 size;

        [Tooltip("【已生效】材料重量。影响入水受力、刚体质量、锚总重量、船只减速效果和风险提示；越重通常越能拉住船。")]
        public float weight;

        [Tooltip("【预留】材料密度。当前不参与玩法计算，后续可接入浮沉、体积重量或更完整的物理计算。")]
        public float density;

        [Tooltip("【已生效】材料粘合能力。两个连接零件的 Adhesive 相加作为连接点最大血量；血量归 0 后连接断裂。")]
        public float adhesive;

        [Tooltip("【已生效】抗拉强度。两个连接零件的 Tensile Strength 相加作为连接点防御力；连接每秒伤害 = Max(0, 当前水攻击力 - 防御力) * 位置伤害衰减系数。")]
        public float tensileStrength;

        [Tooltip("【预留】抗剪强度。当前不参与玩法计算，后续可用于横向错位、剪切冲击或碰撞断裂。")]
        public float shearStrength;

        [Tooltip("【已生效】水阻系数。影响下沉和触底阶段受到的水流横推、刚体阻尼，以及建造结果中的水阻评分。")]
        public float dragCoeff;

        [Tooltip("【已生效】摩擦系数。影响模拟刚体角阻尼，以及触底拖拽时的速度衰减。")]
        public float frictionCoeff;

        [Tooltip("【预留】支撑系数。当前不参与玩法计算，后续可用于触底支撑、抗翻滚或海底受力稳定性。")]
        public float supportCoeff;

        [Tooltip("【已生效】抓地系数。影响触底后的锚固减速效果、建造风险提示和结算原因；越高越能拉住船。")]
        public float gripCoeff;

        [Tooltip("【已生效】材料颜色。当 Prefab 没有 Sprite 时用于绘制材料；模拟视图中也会作为兜底颜色。")]
        public Color color;

        [Tooltip("【预留】是否具有钩形结构。当前不参与玩法计算，后续可用于抓地、碰撞或特殊形状规则。")]
        public bool hasHookShape;

        [Tooltip("【已生效】材料 Prefab 资源路径。用于从资源加载拼装和模拟用的材料 Prefab；为空或加载失败时会用基础矩形兜底。")]
        public string prefabAssetPath;

        public MaterialConfig()
        {
            id = "material";
            displayName = "Material";
            materialName = "";
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
            string prefabAssetPath = null,
            string materialName = "")
        {
            this.id = id;
            this.displayName = displayName;
            this.materialName = materialName;
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
                string.IsNullOrEmpty(overridePrefabAssetPath) ? prefabAssetPath : overridePrefabAssetPath,
                materialName);
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
