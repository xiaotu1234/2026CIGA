namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class LevelConfig
    {
        public int levelId = 1;
        public float shipWeight = 4200f;
        public string seaState = "急流 / 侧风";
        public float waterDepth = 64f;
        public string seabedType = "碎石沙底";
        public float dangerZoneDistance = 1000f;        // 距离危险区的初始距离
        public float stableDuration = 24f;              // 需要坚持的时间
        public float currentForceBase = 7f;             // 水流的湍急程度，判定船锚能不能被推走，不参与攻击力计算
        public float waterEntryAttack = 160f;           // 入水时受到的伤害
        public float sinkWaterAttack = 130f;            // 下沉阶段受到的水流伤害
        public float seabedWaterAttack = 90f;           // 触底阶段受到的水流伤害
        public int materialCount = 5;
        public int minRandomItemCount = 0;
        public float minTotalItemWeight = 0f;
        public float recommendedWeightKg = 0f;
        public float itemWeightCoefficient = 1f;
        public string stormLevel = "";
        public float buildTimeSeconds = 0f;
        public float maxItemSpeed = 0f;
        public float underwaterRightForce = 0f;
        public float fallSpeedSoftLimitMetersPerSecond = 4f;
        public float waterEntryInitialSpeed = 10f;
        public float waterSurfaceTensionForce = 2f;
        public float forceCoefficient = 1f;
        public float weightDownForceCoefficient = 0.18f;
        public float waterDragCoefficient = 0.1f;
        public float thresholdCoefficient = 3f;
        public float damageCoefficient = 0.01f;
        public float healthCoefficient = 50f;
        public float defenseToJointFrequencyCoefficient = 0.025f;
        public float seabedPhysicsFrictionCoefficient = 0.7f;
        public float seabedPhysicsBouncinessCoefficient = 0.15f;
        public bool showConnectionHealthDebug = true;
        public float rigidbodyMassCoefficient = 1f;
    }
}
