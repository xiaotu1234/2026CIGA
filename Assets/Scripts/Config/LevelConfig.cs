namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class LevelConfig
    {
        public float shipWeight = 4200f;
        public string seaState = "急流 / 侧风";
        public float waterDepth = 32f;
        public string seabedType = "碎石沙底";
        public float dangerZoneDistance = 1000f;        // 距离危险区的初始距离
        public float stableDuration = 24f;              // 需要坚持的时间
        public float currentForceBase = 7f;             // 水流的湍急程度，判定船锚能不能被推走，不参与攻击力计算
        public float waterEntryAttack = 160f;           // 入水时受到的伤害
        public float sinkWaterAttack = 130f;            // 下沉阶段受到的水流伤害
        public float seabedWaterAttack = 90f;           // 触底阶段受到的水流伤害
        public int materialCount = 5;
    }
}
