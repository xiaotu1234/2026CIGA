namespace BrokenAnchor.Config
{
    [System.Serializable]
    public class AttachConfig
    {
        public float snapTolerance = 20f;
        public float maxAttachAngle = 18f;
        public float minAttachLength = 24f;
        public float elasticDisplacementLimit = 8f;
        public float maxJointDisplacement = 28f;
        public float displacementGain = 0.12f;
        public float attachLengthDecay = 0.08f;
        public float sinkBreakDelay = 0.6f;
    }
}
