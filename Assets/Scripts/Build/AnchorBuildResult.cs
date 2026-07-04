using System.Collections.Generic;
using BrokenAnchor.Pieces;

namespace BrokenAnchor.Build
{
    public class AnchorBuildResult
    {
        public readonly List<AnchorPiece> pieces = new List<AnchorPiece>();
        public readonly List<AttachJoint> joints = new List<AttachJoint>();
        public readonly List<string> risks = new List<string>();
        public AnchorPiece ropeTiePiece;
        public float totalWeight;
        public float gripScore;
        public float dragScore;
        public bool isConnected;
    }
}
