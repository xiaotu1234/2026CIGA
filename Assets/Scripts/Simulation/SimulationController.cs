using System;
using System.Collections;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.Simulation
{
    public class SimulationController : MonoBehaviour
    {
        private RectTransform playArea;
        private RectTransform ship;
        private RectTransform anchor;
        private RectTransform rope;
        private Text stageText;
        private Text metricText;
        private Slider progressSlider;
        private Action<SimulationResult> onFinished;

        private AnchorBuildResult build;
        private LevelConfig level;
        private float remainingDistance;
        private float anchorDamage;
        private int brokenJoints;
        private Coroutine running;

        public void Initialize(
            RectTransform playArea,
            RectTransform ship,
            RectTransform anchor,
            RectTransform rope,
            Text stageText,
            Text metricText,
            Slider progressSlider,
            Action<SimulationResult> onFinished)
        {
            this.playArea = playArea;
            this.ship = ship;
            this.anchor = anchor;
            this.rope = rope;
            this.stageText = stageText;
            this.metricText = metricText;
            this.progressSlider = progressSlider;
            this.onFinished = onFinished;
        }

        public void StartSimulation(AnchorBuildResult build, LevelConfig level)
        {
            this.build = build;
            this.level = level;
            remainingDistance = level.dangerZoneDistance;
            anchorDamage = 0f;
            brokenJoints = 0;

            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            ResetVisuals();
            yield return RunStage("入水冲击", 3.5f, WaterEntryTick);
            yield return RunStage("下沉拉扯", 6.5f, SinkTick);
            yield return RunStage("触底稳船", level.stableDuration, SeabedTick);

            var result = ResultEvaluator.Evaluate(build, level, remainingDistance, anchorDamage, brokenJoints);
            onFinished?.Invoke(result);
        }

        private IEnumerator RunStage(string stageName, float duration, Action<float> tick)
        {
            stageText.text = stageName;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                tick(t);
                UpdateMetrics(duration - elapsed);
                yield return null;
            }
        }

        private void WaterEntryTick(float t)
        {
            anchor.anchoredPosition = Vector2.Lerp(new Vector2(-170f, 145f), new Vector2(-130f, 40f), t);
            ship.anchoredPosition = Vector2.Lerp(new Vector2(-250f, 150f), new Vector2(-232f, 150f), t);
            anchorDamage = Mathf.Max(anchorDamage, Mathf.Clamp01((1.25f - AverageJointStrength()) * 0.35f));
            brokenJoints = anchorDamage > 0.28f && build.joints.Count > 0 ? 1 : 0;
            DrawRope();
        }

        private void SinkTick(float t)
        {
            anchor.anchoredPosition = Vector2.Lerp(new Vector2(-130f, 40f), new Vector2(-55f, -110f), t);
            ship.anchoredPosition += new Vector2(Time.deltaTime * 8f, 0f);
            var stress = Mathf.Max(0f, level.currentForceBase - build.totalWeight * 0.025f);
            anchorDamage = Mathf.Clamp01(anchorDamage + stress * Time.deltaTime * 0.012f);
            if (anchorDamage > 0.55f && build.joints.Count > 1)
            {
                brokenJoints = Mathf.Max(brokenJoints, 2);
            }
            DrawRope();
        }

        private void SeabedTick(float t)
        {
            var holdPower = build.gripScore * 11f + build.totalWeight * 0.045f + build.dragScore * 2.5f;
            var drift = Mathf.Max(1.2f, level.currentForceBase * 2.25f - holdPower);
            remainingDistance = Mathf.Max(0f, remainingDistance - drift * Time.deltaTime);
            ship.anchoredPosition += new Vector2(drift * Time.deltaTime * 2.2f, 0f);
            anchor.anchoredPosition = Vector2.Lerp(anchor.anchoredPosition, new Vector2(-10f, -150f), Time.deltaTime * 1.5f);
            progressSlider.value = 1f - remainingDistance / level.dangerZoneDistance;
            DrawRope();
        }

        private float AverageJointStrength()
        {
            if (build.joints.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < build.joints.Count; i++)
            {
                total += build.joints[i].currentStrength / 40f;
            }

            return total / build.joints.Count;
        }

        private void ResetVisuals()
        {
            ship.anchoredPosition = new Vector2(-250f, 150f);
            anchor.anchoredPosition = new Vector2(-170f, 145f);
            progressSlider.value = 0f;
            DrawRope();
        }

        private void DrawRope()
        {
            var start = ship.anchoredPosition + new Vector2(52f, -30f);
            var end = anchor.anchoredPosition + new Vector2(0f, 35f);
            var delta = end - start;
            rope.anchoredPosition = (start + end) * 0.5f;
            rope.sizeDelta = new Vector2(delta.magnitude, 5f);
            rope.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void UpdateMetrics(float remainingStageTime)
        {
            metricText.text =
                $"危险区剩余距离：{remainingDistance:0.0} m\n" +
                $"船锚损伤：{anchorDamage * 100f:0}%\n" +
                $"断开连接：{brokenJoints}\n" +
                $"阶段剩余：{Mathf.Max(0f, remainingStageTime):0.0} s";
        }
    }
}
