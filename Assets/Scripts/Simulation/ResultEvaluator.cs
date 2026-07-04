using BrokenAnchor.Build;
using BrokenAnchor.Config;

namespace BrokenAnchor.Simulation
{
    public static class ResultEvaluator
    {
        public static SimulationResult Evaluate(AnchorBuildResult build, LevelConfig level, float remainingDistance, float anchorDamage, int brokenJoints)
        {
            var result = new SimulationResult();
            result.remainingDistance = remainingDistance;
            result.anchorDamage = anchorDamage;
            result.shipEnteredDangerZone = remainingDistance <= 0f;

            if (result.shipEnteredDangerZone)
            {
                result.reasons.Add("船进入危险区。");
            }

            if (!build.isConnected)
            {
                result.reasons.Add("船锚主体未连通。");
            }

            if (brokenJoints > 0)
            {
                result.reasons.Add("有连接在入水或下沉阶段断开。");
            }

            if (build.gripScore < 1.5f)
            {
                result.reasons.Add("触底抓地能力不足。");
            }

            if (build.totalWeight < 150f)
            {
                result.reasons.Add("船锚总重量偏轻。");
            }

            result.success = !result.shipEnteredDangerZone && build.isConnected && build.ropeTiePiece != null;
            result.narrowSuccess = result.success && (remainingDistance < level.dangerZoneDistance * 0.22f || anchorDamage > 0.62f);

            if (result.success && result.reasons.Count == 0)
            {
                result.reasons.Add(result.narrowSuccess ? "勉强稳住，结构损伤较高。" : "船锚成功拖住船，未进入危险区。");
            }

            while (result.reasons.Count > 3)
            {
                result.reasons.RemoveAt(result.reasons.Count - 1);
            }

            return result;
        }
    }
}
