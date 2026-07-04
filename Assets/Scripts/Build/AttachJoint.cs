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
        public float adhesiveAbility;
        public float maxHealth;
        public float currentHealth;
        public float defense;
        public float forceThreshold;
        public float damageFalloff;
        public float relativeDisplacement;
        public float lastForce;
        public float damage;
        public bool isBroken;
        public JointState jointState;

        public AttachJoint(AnchorPiece pieceA, AnchorPiece pieceB, float attachLength)
        {
            this.pieceA = pieceA;
            this.pieceB = pieceB;
            initialAttachLength = attachLength;
            currentAttachLength = attachLength;
            adhesiveAbility = pieceA.Config.adhesive + pieceB.Config.adhesive;
            maxHealth = adhesiveAbility;
            currentHealth = maxHealth;
            defense = pieceA.Config.tensileStrength >= pieceB.Config.tensileStrength ? pieceA.Config.tensileStrength : pieceB.Config.tensileStrength;
            forceThreshold = 0f;
            damageFalloff = 1f;
            lastForce = 0f;
            jointState = JointState.Stable;
        }
    }
}
