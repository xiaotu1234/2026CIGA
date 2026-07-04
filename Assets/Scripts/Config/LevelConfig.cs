namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class LevelConfig
    {
        public float shipWeight = 4200f;
        public string seaState = "急流 / 侧风";
        public float waterDepth = 32f;
        public string seabedType = "碎石沙底";
        public float dangerZoneDistance = 100f;
        public float stableDuration = 24f;
        public float currentForceBase = 12f;
        public int materialCount = 5;
    }
}
