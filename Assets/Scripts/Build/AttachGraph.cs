using System.Collections.Generic;
using BrokenAnchor.Pieces;

namespace BrokenAnchor.Build
{
    public static class AttachGraph
    {
        public static bool IsFullyConnected(IReadOnlyList<AnchorPiece> pieces, IReadOnlyList<AttachJoint> joints)
        {
            if (pieces.Count <= 1)
            {
                return pieces.Count == 1;
            }

            var visited = new HashSet<AnchorPiece>();
            var queue = new Queue<AnchorPiece>();
            queue.Enqueue(pieces[0]);
            visited.Add(pieces[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var i = 0; i < joints.Count; i++)
                {
                    var joint = joints[i];
                    if (joint.isBroken)
                    {
                        continue;
                    }

                    AnchorPiece next = null;
                    if (joint.pieceA == current)
                    {
                        next = joint.pieceB;
                    }
                    else if (joint.pieceB == current)
                    {
                        next = joint.pieceA;
                    }

                    if (next != null && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count == pieces.Count;
        }
    }
}
