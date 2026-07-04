using System.Collections.Generic;
using BrokenAnchor.Pieces;
using UnityEngine;

namespace BrokenAnchor.Build
{
    public class AnchorBuildResult
    {
        public class PieceSnapshot
        {
            public AnchorPiece source;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Quaternion localRotation;
            public Vector3 localScale;
        }

        public readonly List<AnchorPiece> pieces = new List<AnchorPiece>();
        public readonly List<AttachJoint> joints = new List<AttachJoint>();
        public readonly List<string> risks = new List<string>();
        public readonly List<PieceSnapshot> pieceSnapshots = new List<PieceSnapshot>();
        public AnchorPiece ropeTiePiece;
        public float totalWeight;
        public float gripScore;
        public float dragScore;
        public bool isConnected;

        public PieceSnapshot GetPieceSnapshot(AnchorPiece piece)
        {
            for (var i = 0; i < pieceSnapshots.Count; i++)
            {
                if (pieceSnapshots[i].source == piece)
                {
                    return pieceSnapshots[i];
                }
            }

            return null;
        }
    }
}
