using BrokenAnchor.Pieces;

namespace BrokenAnchor.Build
{
    public enum JointState
    {
        Stable,
        Stretching,
        Sliding,
        Broken
    }

    [System.Serializable]
    public class AttachJoint
    {
        public AnchorPiece pieceA;
        public AnchorPiece pieceB;
        public float initialAttachLength;
        public float currentAttachLength;
        public float currentStrength;
        public float relativeDisplacement;
        public float damage;
        public bool isBroken;
        public JointState jointState;

        public AttachJoint(AnchorPiece pieceA, AnchorPiece pieceB, float attachLength)
        {
            this.pieceA = pieceA;
            this.pieceB = pieceB;
            initialAttachLength = attachLength;
            currentAttachLength = attachLength;
            currentStrength = (pieceA.Config.adhesive + pieceB.Config.adhesive) * attachLength;
            jointState = JointState.Stable;
        }
    }
}
