using System;
using System.Collections;
using System.Collections.Generic;
using BrokenAnchor.Build;
using BrokenAnchor.Config;
using BrokenAnchor.Pieces;
using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.Simulation
{
    public class SimulationController : MonoBehaviour
    {
        private class SimulatedPiece
        {
            public AnchorPiece source;
            public RectTransform rect;
            public Vector2 baseLocalPosition;
            public Vector2 currentOffset;
            public float baseRotation;
            public float waterDriftSeed;
            public bool weaklyConnected;
        }

        private readonly List<SimulatedPiece> simulatedPieces = new List<SimulatedPiece>();
        private readonly List<RectTransform> seabedSegments = new List<RectTransform>();

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
        private float anchorScale = 0.55f;
        private float simulationElapsed;
        private float shipVelocity;
        private bool anchorOnSeabed;
        private Coroutine running;
        private float dangerBoundaryX;
        private float shipTravelPixelsPerMeter = 4f;

        private const float WaterEntryDuration = 0.8f;
        private const float DangerZoneLeftAnchor = 0.88f;

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
            BuildAnchorVisual();
            BuildUnevenSeabed();

            while (simulationElapsed < level.stableDuration && remainingDistance > 0f)
            {
                var deltaTime = Time.deltaTime;
                simulationElapsed += deltaTime;

                if (simulationElapsed <= WaterEntryDuration)
                {
                    stageText.text = "入水冲击";
                    WaterEntryTick(simulationElapsed / WaterEntryDuration, deltaTime);
                }
                else if (!anchorOnSeabed)
                {
                    stageText.text = "下沉中";
                    SinkTick(deltaTime);
                }
                else
                {
                    stageText.text = "触底拖拽";
                    SeabedTick(deltaTime);
                }

                UpdateMetrics(Mathf.Max(0f, level.stableDuration - simulationElapsed));
                yield return null;
            }

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

        private void WaterEntryTick(float t, float deltaTime)
        {
            anchor.anchoredPosition = Vector2.Lerp(new Vector2(-170f, 145f), new Vector2(-130f, 40f), t);
            anchorDamage = Mathf.Max(anchorDamage, Mathf.Clamp01((1.25f - AverageJointStrength()) * 0.35f));
            brokenJoints = anchorDamage > 0.28f && build.joints.Count > 0 ? 1 : 0;
            AnimateAnchorPieces(t, 0.35f + anchorDamage, false);
            ApplyShipCurrent(deltaTime, 0.2f);
            DrawRope();
        }

        private void SinkTick(float deltaTime)
        {
            var sinkSpeed = Mathf.Clamp(58f + build.totalWeight * 0.12f - build.dragScore * 8f, 46f, 92f);
            var currentPush = 14f + level.currentForceBase * 1.4f;
            anchor.anchoredPosition += new Vector2(currentPush * deltaTime * 0.35f, -sinkSpeed * deltaTime);

            var stress = Mathf.Max(0f, level.currentForceBase - build.totalWeight * 0.025f);
            anchorDamage = Mathf.Clamp01(anchorDamage + stress * deltaTime * 0.012f);
            if (anchorDamage > 0.55f && build.joints.Count > 1)
            {
                brokenJoints = Mathf.Max(brokenJoints, 2);
            }

            AnimateAnchorPieces(Mathf.Clamp01(simulationElapsed / level.stableDuration), 0.65f + anchorDamage * 1.2f, false);
            if (CountSeabedContacts() > 0)
            {
                anchorOnSeabed = true;
                AnimateAnchorPieces(1f, 0.45f + anchorDamage, true);
            }

            ApplyShipCurrent(deltaTime, 0.45f);
            DrawRope();
        }

        private void SeabedTick(float deltaTime)
        {
            anchor.anchoredPosition += new Vector2(Mathf.Max(0.3f, shipVelocity) * deltaTime * 0.35f, 0f);
            ApplyShipCurrent(deltaTime, 1f);
            AnimateAnchorPieces(Mathf.Clamp01(simulationElapsed / level.stableDuration), 0.45f + anchorDamage, true);
            DrawRope();
        }

        private void ApplyShipCurrent(float deltaTime, float anchorEffectiveness)
        {
            var currentAcceleration = 26f + level.currentForceBase * 2.4f;
            var preTouchHold = build.dragScore * 1.2f + build.totalWeight * 0.015f;
            var seabedHold = build.gripScore * 12f + build.totalWeight * 0.06f + build.dragScore * 2.4f;
            var holdPower = Mathf.Lerp(preTouchHold, seabedHold, anchorOnSeabed ? 1f : anchorEffectiveness);
            holdPower *= Mathf.Clamp01(1f - anchorDamage * 0.75f);

            var waterDrag = shipVelocity * (anchorOnSeabed ? 0.95f + holdPower * 0.035f : 0.12f);
            shipVelocity += (currentAcceleration - holdPower * (anchorOnSeabed ? 1.45f : 0.35f) - waterDrag) * deltaTime;
            shipVelocity = Mathf.Clamp(shipVelocity, 0f, 42f);

            ship.anchoredPosition += new Vector2(shipVelocity * deltaTime * shipTravelPixelsPerMeter, 0f);
            UpdateRemainingDistanceFromShipPosition();
            progressSlider.value = 1f - remainingDistance / level.dangerZoneDistance;
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
            anchor.localRotation = Quaternion.identity;
            progressSlider.value = 0f;
            simulationElapsed = 0f;
            shipVelocity = 0f;
            anchorOnSeabed = false;
            ConfigureDangerZoneMetrics();
            ClearAnchorVisual();
            DrawRope();
        }

        private void DrawRope()
        {
            var start = ship.anchoredPosition + new Vector2(-52f, -30f);
            var end = GetRopeEndPosition();
            var delta = end - start;
            rope.anchoredPosition = (start + end) * 0.5f;
            rope.sizeDelta = new Vector2(delta.magnitude, 5f);
            rope.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void ConfigureDangerZoneMetrics()
        {
            Canvas.ForceUpdateCanvases();
            var playAreaWidth = playArea != null && playArea.rect.width > 0f ? playArea.rect.width : 900f;
            dangerBoundaryX = (DangerZoneLeftAnchor - 0.5f) * playAreaWidth;

            var shipRightEdge = GetShipRightEdge();
            var visualTravelToDanger = Mathf.Max(80f, dangerBoundaryX - shipRightEdge);
            shipTravelPixelsPerMeter = visualTravelToDanger / Mathf.Max(1f, level.dangerZoneDistance);
            UpdateRemainingDistanceFromShipPosition();
        }

        private void UpdateRemainingDistanceFromShipPosition()
        {
            var visualRemaining = dangerBoundaryX - GetShipRightEdge();
            remainingDistance = Mathf.Clamp(visualRemaining / Mathf.Max(0.01f, shipTravelPixelsPerMeter), 0f, level.dangerZoneDistance);
        }

        private float GetShipRightEdge()
        {
            return ship.anchoredPosition.x + ship.sizeDelta.x * 0.5f;
        }

        private Vector2 GetRopeEndPosition()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                if (simulatedPieces[i].source == build.ropeTiePiece)
                {
                    return anchor.anchoredPosition + simulatedPieces[i].rect.anchoredPosition;
                }
            }

            return anchor.anchoredPosition + new Vector2(0f, 35f);
        }

        private void UpdateMetrics(float remainingStageTime)
        {
            metricText.text =
                $"危险区剩余距离：{remainingDistance:0.0} m\n" +
                $"船速：{shipVelocity:0.0} m/s\n" +
                $"船锚损伤：{anchorDamage * 100f:0}%\n" +
                $"断开连接：{brokenJoints}\n" +
                $"海底接触：{CountSeabedContacts()} 个部件\n" +
                $"稳船剩余：{Mathf.Max(0f, remainingStageTime):0.0} s";
        }

        private void BuildAnchorVisual()
        {
            ClearAnchorVisual();

            var anchorImage = anchor.GetComponent<Image>();
            if (anchorImage != null)
            {
                anchorImage.enabled = false;
            }

            for (var i = 0; i < anchor.childCount; i++)
            {
                var text = anchor.GetChild(i).GetComponent<Text>();
                if (text != null)
                {
                    text.enabled = false;
                }
            }

            if (build == null || build.pieces.Count == 0)
            {
                return;
            }

            var center = GetBuildCenter();
            for (var i = 0; i < build.pieces.Count; i++)
            {
                var source = build.pieces[i];
                var rect = CreateSimPieceRect(source);
                rect.sizeDelta = source.RectTransform.sizeDelta * anchorScale;
                rect.anchoredPosition = (source.RectTransform.anchoredPosition - center) * anchorScale;
                rect.localRotation = source.RectTransform.localRotation;
                rect.localScale = source.RectTransform.localScale;

                var image = rect.GetComponent<Image>();
                if (image == null)
                {
                    image = rect.gameObject.AddComponent<Image>();
                }

                if (image.sprite == null)
                {
                    image.color = source.Config.color;
                }

                var outline = rect.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = rect.gameObject.AddComponent<Outline>();
                }

                outline.effectColor = IsRopeTiePiece(source) ? new Color(1f, 0.9f, 0.25f, 0.95f) : new Color(0.9f, 1f, 1f, 0.65f);
                outline.effectDistance = IsRopeTiePiece(source) ? new Vector2(4f, -4f) : new Vector2(2f, -2f);

                var label = GetOrCreateSimLabel(rect, source.Config.displayName);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;

                simulatedPieces.Add(new SimulatedPiece
                {
                    source = source,
                    rect = rect,
                    baseLocalPosition = rect.anchoredPosition,
                    baseRotation = rect.localEulerAngles.z,
                    waterDriftSeed = 0.55f + i * 0.37f,
                    weaklyConnected = CountJointsForPiece(source) <= 1
                });
            }
        }

        private RectTransform CreateSimPieceRect(AnchorPiece source)
        {
            GameObject go = null;
            var prefab = LoadPiecePrefab(source.Config.prefabAssetPath);
            if (prefab != null)
            {
                go = Instantiate(prefab, anchor, false);
                if (go.GetComponent<RectTransform>() == null)
                {
                    Destroy(go);
                    go = null;
                }
            }

            if (go == null)
            {
                return UIBuilder.CreateRect(anchor, "SimPiece_" + source.Config.id, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            }

            go.name = "SimPiece_" + source.Config.id;
            return go.GetComponent<RectTransform>();
        }

        private static GameObject LoadPiecePrefab(string assetPath)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(assetPath))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif
            return null;
        }
        private Text GetOrCreateSimLabel(RectTransform rect, string text)
        {
            var labelTransform = rect.Find("Label");
            var label = labelTransform == null ? null : labelTransform.GetComponent<Text>();
            if (label == null)
            {
                label = UIBuilder.CreateText(rect, "Label", text, 11, Color.white, TextAnchor.MiddleCenter);
            }
            else
            {
                label.text = text;
                label.font = UIBuilder.Font;
                label.fontSize = 11;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
            }

            return label;
        }

        private Vector2 GetBuildCenter()
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < build.pieces.Count; i++)
            {
                var piece = build.pieces[i];
                var half = piece.RectTransform.sizeDelta * 0.5f;
                var position = piece.RectTransform.anchoredPosition;
                min = Vector2.Min(min, position - half);
                max = Vector2.Max(max, position + half);
            }

            return (min + max) * 0.5f;
        }

        private void AnimateAnchorPieces(float t, float forceScale, bool collideWithSeabed)
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var weakness = piece.weaklyConnected ? 1.35f : 0.62f;
                var materialDrag = piece.source.Config.dragCoeff;
                var wave = Mathf.Sin(Time.time * (2.2f + piece.waterDriftSeed) + i * 0.9f);
                var shear = forceScale * weakness * materialDrag;
                var targetOffset = new Vector2(16f * shear * t, wave * 8f * shear);
                var targetRotation = piece.baseRotation + wave * 18f * shear + anchorDamage * weakness * 34f;

                piece.currentOffset = Vector2.Lerp(piece.currentOffset, targetOffset, Time.deltaTime * 5f);
                piece.rect.anchoredPosition = piece.baseLocalPosition + piece.currentOffset;
                piece.rect.localRotation = Quaternion.Euler(0f, 0f, targetRotation);

                if (collideWithSeabed)
                {
                    ResolveSeabedCollision(piece);
                }
            }
        }

        private void ResolveSeabedCollision(SimulatedPiece piece)
        {
            var worldPosition = anchor.anchoredPosition + piece.rect.anchoredPosition;
            var halfHeight = piece.rect.sizeDelta.y * 0.5f;
            var bottom = worldPosition.y - halfHeight;
            var seabedY = GetSeabedHeight(worldPosition.x);
            if (bottom > seabedY)
            {
                return;
            }

            var penetration = seabedY - bottom;
            piece.rect.anchoredPosition += new Vector2(0f, penetration);
            var slopeAngle = Mathf.Atan(GetSeabedSlope(worldPosition.x)) * Mathf.Rad2Deg;
            piece.rect.localRotation = Quaternion.Lerp(piece.rect.localRotation, Quaternion.Euler(0f, 0f, slopeAngle), Time.deltaTime * 6f);
        }

        private void BuildUnevenSeabed()
        {
            ClearSeabedSegments();
            var points = new[]
            {
                new Vector2(-430f, -176f),
                new Vector2(-330f, -148f),
                new Vector2(-230f, -170f),
                new Vector2(-120f, -132f),
                new Vector2(-10f, -160f),
                new Vector2(110f, -118f),
                new Vector2(230f, -152f),
                new Vector2(360f, -126f),
                new Vector2(470f, -166f)
            };

            for (var i = 0; i < points.Length - 1; i++)
            {
                var segment = UIBuilder.CreateRect(playArea, "UnevenSeabedSegment", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var image = segment.gameObject.AddComponent<Image>();
                image.color = new Color(0.58f, 0.42f, 0.23f, 1f);
                var delta = points[i + 1] - points[i];
                segment.anchoredPosition = (points[i] + points[i + 1]) * 0.5f;
                segment.sizeDelta = new Vector2(delta.magnitude, 10f);
                segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                seabedSegments.Add(segment);
            }
        }

        private float GetSeabedHeight(float x)
        {
            return -151f + Mathf.Sin((x + 40f) * 0.028f) * 22f + Mathf.Sin(x * 0.071f) * 9f;
        }

        private float GetSeabedSlope(float x)
        {
            var left = GetSeabedHeight(x - 8f);
            var right = GetSeabedHeight(x + 8f);
            return (right - left) / 16f;
        }

        private int CountSeabedContacts()
        {
            var contacts = 0;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var worldPosition = anchor.anchoredPosition + piece.rect.anchoredPosition;
                var bottom = worldPosition.y - piece.rect.sizeDelta.y * 0.5f;
                if (bottom <= GetSeabedHeight(worldPosition.x) + 2f)
                {
                    contacts++;
                }
            }

            return contacts;
        }

        private bool IsRopeTiePiece(AnchorPiece piece)
        {
            return build != null && build.ropeTiePiece == piece;
        }

        private int CountJointsForPiece(AnchorPiece piece)
        {
            var count = 0;
            for (var i = 0; i < build.joints.Count; i++)
            {
                if (build.joints[i].pieceA == piece || build.joints[i].pieceB == piece)
                {
                    count++;
                }
            }

            return count;
        }

        private void ClearAnchorVisual()
        {
            for (var i = simulatedPieces.Count - 1; i >= 0; i--)
            {
                if (simulatedPieces[i].rect != null)
                {
                    Destroy(simulatedPieces[i].rect.gameObject);
                }
            }

            simulatedPieces.Clear();
        }

        private void ClearSeabedSegments()
        {
            for (var i = seabedSegments.Count - 1; i >= 0; i--)
            {
                if (seabedSegments[i] != null)
                {
                    Destroy(seabedSegments[i].gameObject);
                }
            }

            seabedSegments.Clear();
        }
    }
}
