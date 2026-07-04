using System.Collections.Generic;
using BrokenAnchor.Pieces;

namespace BrokenAnchor.Build
{
    public static class BuildRiskEvaluator
    {
        public static List<string> Evaluate(IReadOnlyList<AnchorPiece> pieces, IReadOnlyList<AttachJoint> joints, AnchorPiece ropeTiePiece)
        {
            var risks = new List<string>();
            if (pieces.Count == 0)
            {
                risks.Add("还没有放置材料。");
                return risks;
            }

            if (!AttachGraph.IsFullyConnected(pieces, joints))
            {
                risks.Add("主体未完全连通，入水后可能散架。");
            }

            if (ropeTiePiece == null)
            {
                risks.Add("尚未把材料覆盖到绳子挂点。");
            }
            else if (ropeTiePiece.Config.weight < 35f)
            {
                risks.Add("绳子挂在轻材料上，绳力可能先拉坏连接。");
            }

            var totalWeight = 0f;
            var grip = 0f;
            for (var i = 0; i < pieces.Count; i++)
            {
                totalWeight += pieces[i].Config.weight;
                grip += pieces[i].Config.gripCoeff;
            }

            if (totalWeight < 150f)
            {
                risks.Add("总重量偏轻，触底前可能被水流拖走。");
            }

            if (grip < 1.5f)
            {
                risks.Add("抓地能力偏弱，触底后抗拖拽不足。");
            }

            if (joints.Count < pieces.Count - 1)
            {
                risks.Add("连接数量不足，有断链风险。");
            }

            if (risks.Count == 0)
            {
                risks.Add("结构可下锚，但仍会经历入水冲击和下沉拉扯。");
            }

            return risks;
        }
    }
}
