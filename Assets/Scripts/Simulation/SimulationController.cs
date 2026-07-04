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
            public Rigidbody2D body;
            public readonly List<FixedJoint2D> joints = new List<FixedJoint2D>();
            public bool weaklyConnected;
            public bool grounded;
        }

        private class SeabedSegment
        {
            public EdgeCollider2D collider;
            public Vector2 start;
            public Vector2 end;
        }

        private readonly List<SimulatedPiece> simulatedPieces = new List<SimulatedPiece>();
        private readonly List<SeabedSegment> seabedSegments = new List<SeabedSegment>();
        private readonly List<GameObject> physicsObjects = new List<GameObject>();
        private readonly Dictionary<AnchorPiece, SimulatedPiece> pieceLookup = new Dictionary<AnchorPiece, SimulatedPiece>();

        private RectTransform playArea;
        private RectTransform ship;
        private RectTransform anchor;
        private RectTransform rope;
        private RectTransform dangerZoneIndicator;
        private Text stageText;
        private Text metricText;
        private Slider progressSlider;
        private SeabedFillGraphic seabedFill;
        private Action<SimulationResult> onFinished;

        private AnchorBuildResult build;
        private LevelConfig level;
        private GameObject physicsRoot;
        private PhysicsMaterial2D seabedMaterial;
        private PhysicsMaterial2D pieceMaterial;
        private float remainingDistance;
        private float anchorDamage;
        private int brokenJoints;
        private float simulationElapsed;
        private float shipVelocity;
        private float anchorMotionPixelsPerSecond;
        private float dangerBoundaryX;
        private float shipTravelPixelsPerMeter = 4f;
        private float shipStartX;
        private float cameraOffsetY;
        private float ropeTieLockPhysicsX;
        private float seabedNoiseSeed;
        private float seabedSampleOffsetX;
        private bool anchorOnSeabed;
        private bool hasRopeTieHorizontalLock;
        private Coroutine running;
        private readonly List<Vector2> seabedFillPoints = new List<Vector2>();

        private const float WaterEntryDuration = 0.8f;
        private const float DangerZoneLeftAnchor = 0.88f;
        private const float AnchorScale = 0.55f;
        private const float PhysicsScale = 0.012f;
        private const float InversePhysicsScale = 1f / PhysicsScale;
        private const float InitialGravityScale = 6f;
        private const float SeabedGravityScale = 3.6f;
        private const float MinEffectiveJointHealth = 0.01f;
        private const float AnchorSpeedPixelsPerMeter = 24f;
        private const float DangerZoneRevealDistance = 120f;
        private const float RopeVisualAngleDegrees = 60f;
        private const float RopePullAngleDegrees = 30f;
        private const float RopeVisualLengthPadding = 220f;
        private const float RopePullForceMultiplier = 1f;
        private const float RopePullBaseForce = 8f;
        private const float RopePullForcePerCurrent = 1.4f;
        private const float RopeAttachLocalXFactor = 0.45f;
        private const float RopeAttachLocalYFactor = 0.32f;
        private const float AntiLiftDragLinear = 0.08f;
        private const float AntiLiftDragMaxAcceleration = 18f;
        private const float JointStrengthMultiplier = 3f;
        private const float JointBreakForceScale = 0.075f;
        private const float JointBreakTorqueScale = 0.035f;
        private const float SurfaceY = 130f;
        private const float CameraAnchorTargetY = 35f;
        private const float MaxCameraOffsetY = 1250f;
        private const float SeabedSegmentLength = 36f;
        private const float SeabedVisualThickness = 100f;
        private const float SeabedColliderRadius = 0.18f;
        private const float SeabedSpawnPadding = 360f;
        private const float SeabedDespawnPadding = 260f;
        private const float SeabedMinY = -1172f;
        private const float SeabedMaxY = -890f;
        private const float SeabedMaxStepY = 14f;

        private static readonly Vector2 InitialAnchorPosition = new Vector2(-330f, SurfaceY);

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
            dangerZoneIndicator = playArea != null ? playArea.Find("DangerZone") as RectTransform : null;
            this.stageText = stageText;
            this.metricText = metricText;
            this.progressSlider = progressSlider;
            this.onFinished = onFinished;
            SetDangerZoneVisible(false);
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
            BuildUnevenSeabed();
            BuildAnchorVisualAndPhysics();

            while (simulationElapsed < level.stableDuration && remainingDistance > 0f)
            {
                var deltaTime = Time.deltaTime;
                simulationElapsed += deltaTime;

                if (simulationElapsed <= WaterEntryDuration)
                {
                    stageText.text = "\u5165\u6c34\u51b2\u51fb";
                    WaterEntryTick();
                }
                else if (!anchorOnSeabed)
                {
                    stageText.text = "\u4e0b\u6c89\u4e2d";
                    SinkTick();
                }
                else
                {
                    stageText.text = "\u89e6\u5e95\u62d6\u62fd";
                    SeabedTick();
                }

                ApplyRopeForce();
                ApplyAntiLiftResistance();
                DetectSeabedContacts();
                SyncJointBreakStates();
                ApplyAnchorDrivenShipMotion(deltaTime);
                ConstrainAnchorHorizontalDrift();
                SyncVisualsFromPhysics();
                UpdateSeabedTerrain(deltaTime);
                UpdateCameraFollow(deltaTime);
                DrawRope();
                UpdateMetrics(Mathf.Max(0f, level.stableDuration - simulationElapsed));
                yield return null;
            }

            var result = ResultEvaluator.Evaluate(build, level, remainingDistance, anchorDamage, brokenJoints);
            onFinished?.Invoke(result);
        }

        private void WaterEntryTick()
        {
            var t = Mathf.Clamp01(simulationElapsed / WaterEntryDuration);
            var entryForce = Vector2.Lerp(new Vector2(0.2f, -1.3f), new Vector2(1.2f, -0.5f), t);

            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var massFactor = Mathf.Max(0.2f, piece.source.Config.weight * 0.012f);
                piece.body.gravityScale = InitialGravityScale;
                piece.body.AddForce(entryForce * massFactor, ForceMode2D.Force);
                piece.body.AddTorque((piece.weaklyConnected ? 0.22f : 0.08f) * Mathf.Sin(Time.time + i), ForceMode2D.Force);
            }
        }

        private void SinkTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var material = piece.source.Config;
                var downwardBias = Mathf.Clamp(material.weight * 0.008f - material.dragCoeff * 0.18f, 0.08f, 1.4f);
                var waterDrag = -piece.body.velocity * Mathf.Clamp(material.dragCoeff * 0.12f, 0.02f, 0.22f);

                piece.body.gravityScale = InitialGravityScale;
                piece.body.AddForce(new Vector2(0f, -downwardBias) + waterDrag, ForceMode2D.Force);
            }
        }

        private void SeabedTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var material = piece.source.Config;
                var drag = piece.body.velocity * Mathf.Clamp(material.frictionCoeff * 0.35f, 0.05f, 0.75f);

                piece.body.gravityScale = SeabedGravityScale;
                piece.body.AddForce(-drag, ForceMode2D.Force);
            }
        }

        private void ApplyAnchorDrivenShipMotion(float deltaTime)
        {
            anchorMotionPixelsPerSecond = GetAnchorHorizontalSpeedPixels();
            shipVelocity = Mathf.Clamp(anchorMotionPixelsPerSecond / AnchorSpeedPixelsPerMeter, -42f, 42f);
            remainingDistance = Mathf.Clamp(remainingDistance - shipVelocity * deltaTime, 0f, level.dangerZoneDistance);
            UpdateShipPositionFromRemainingDistance();

            progressSlider.value = 1f - remainingDistance / level.dangerZoneDistance;
            UpdateDangerZoneVisibility();
        }

        private void ApplyRopeForce()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                return;
            }

            var pullForce = (RopePullBaseForce + Mathf.Max(0f, level.currentForceBase) * RopePullForcePerCurrent) * RopePullForceMultiplier;
            tiePiece.body.AddForceAtPosition(GetRopePullDirection() * pullForce * tiePiece.body.mass, GetRopeAttachPhysicsPosition(tiePiece), ForceMode2D.Force);
        }

        private void ApplyAntiLiftResistance()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var body = simulatedPieces[i].body;
                if (body.velocity.y <= 0f)
                {
                    continue;
                }

                var upwardSpeedPixels = body.velocity.y * InversePhysicsScale;
                var acceleration = Mathf.Min(AntiLiftDragMaxAcceleration, upwardSpeedPixels * AntiLiftDragLinear);
                body.AddForce(Vector2.down * acceleration * body.mass, ForceMode2D.Force);
            }
        }

        private void SyncJointBreakStates()
        {
            for (var i = 0; i < build.joints.Count; i++)
            {
                var joint = build.joints[i];
                if (!pieceLookup.TryGetValue(joint.pieceA, out var pieceA) ||
                    !pieceLookup.TryGetValue(joint.pieceB, out var pieceB))
                {
                    continue;
                }

                if (!joint.isBroken && !HasLiveJoint(pieceA, pieceB.body))
                {
                    BreakJoint(joint, pieceA, pieceB);
                    continue;
                }

                joint.damage = joint.isBroken ? 1f : 0f;
                joint.currentHealth = joint.isBroken ? 0f : joint.maxHealth;
                joint.currentStrength = joint.currentHealth;
                joint.jointState = joint.isBroken ? JointState.Broken : JointState.Stable;
            }

            anchorDamage = CalculateAnchorDamage();
        }

        private void BreakJoint(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            joint.isBroken = true;
            joint.jointState = JointState.Broken;
            brokenJoints++;

            DestroyMatchingJoint(pieceA, pieceB.body);
            DestroyMatchingJoint(pieceB, pieceA.body);
        }

        private static bool HasLiveJoint(SimulatedPiece owner, Rigidbody2D connectedBody)
        {
            for (var i = owner.joints.Count - 1; i >= 0; i--)
            {
                var joint = owner.joints[i];
                if (joint == null)
                {
                    owner.joints.RemoveAt(i);
                    continue;
                }

                if (joint.connectedBody == connectedBody)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyMatchingJoint(SimulatedPiece owner, Rigidbody2D connectedBody)
        {
            for (var i = owner.joints.Count - 1; i >= 0; i--)
            {
                var joint = owner.joints[i];
                if (joint == null)
                {
                    owner.joints.RemoveAt(i);
                    continue;
                }

                if (joint.connectedBody == connectedBody)
                {
                    Destroy(joint);
                    owner.joints.RemoveAt(i);
                }
            }
        }

        private float GetAnchorHorizontalSpeedPixels()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece != null)
            {
                return tiePiece.body.velocity.x * InversePhysicsScale;
            }

            if (simulatedPieces.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                total += simulatedPieces[i].body.velocity.x * InversePhysicsScale;
            }

            return total / simulatedPieces.Count;
        }

        private float CalculateAnchorDamage()
        {
            if (build == null || build.joints.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < build.joints.Count; i++)
            {
                total += build.joints[i].isBroken ? 1f : build.joints[i].damage;
            }

            return Mathf.Clamp01(total / build.joints.Count);
        }

        private void DetectSeabedContacts()
        {
            var contacts = 0;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var uiPosition = ToUiPosition(piece.body.position);
                var bottom = uiPosition.y - piece.rect.sizeDelta.y * 0.5f;
                piece.grounded = bottom <= GetSeabedHeight(uiPosition.x) + 6f;
                if (piece.grounded)
                {
                    contacts++;
                }
            }

            if (contacts > 0)
            {
                anchorOnSeabed = true;
            }
        }

        private void ResetVisuals()
        {
            ship.anchoredPosition = new Vector2(-250f, 150f);
            anchor.anchoredPosition = Vector2.zero;
            anchor.localRotation = Quaternion.identity;
            progressSlider.value = 0f;
            simulationElapsed = 0f;
            shipVelocity = 0f;
            anchorMotionPixelsPerSecond = 0f;
            cameraOffsetY = 0f;
            anchorOnSeabed = false;
            hasRopeTieHorizontalLock = false;
            ConfigureDangerZoneMetrics();
            ClearAnchorVisual();
            ClearPhysicsObjects();
            SetStaticSeabedVisible(false);
            DrawRope();
        }

        private void DrawRope()
        {
            var start = GetRopeStartPosition();
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
            shipStartX = ship.anchoredPosition.x;
            UpdateShipPositionFromRemainingDistance();
            UpdateDangerZoneVisibility();
        }

        private void UpdateDangerZoneVisibility()
        {
            if (level == null)
            {
                SetDangerZoneVisible(false);
                return;
            }

            var revealDistance = Mathf.Min(DangerZoneRevealDistance, level.dangerZoneDistance * 0.25f);
            SetDangerZoneVisible(remainingDistance <= revealDistance);
        }

        private void SetDangerZoneVisible(bool visible)
        {
            if (dangerZoneIndicator != null && dangerZoneIndicator.gameObject.activeSelf != visible)
            {
                dangerZoneIndicator.gameObject.SetActive(visible);
            }
        }

        private void UpdateShipPositionFromRemainingDistance()
        {
            var traveledDistance = level.dangerZoneDistance - remainingDistance;
            ship.anchoredPosition = new Vector2(shipStartX + traveledDistance * shipTravelPixelsPerMeter, 150f + cameraOffsetY);
        }

        private float GetShipRightEdge()
        {
            return ship.anchoredPosition.x + ship.sizeDelta.x * 0.5f;
        }

        private Vector2 GetRopeStartPosition()
        {
            return GetRopeEndPosition() + GetRopeVisualDirection() * GetRopeVisualLength();
        }

        private float GetRopeVisualLength()
        {
            if (playArea == null)
            {
                return 900f;
            }

            return Mathf.Max(playArea.rect.width, playArea.rect.height) + RopeVisualLengthPadding;
        }

        private static Vector2 GetRopePullDirection()
        {
            return GetDirection(RopePullAngleDegrees);
        }

        private static Vector2 GetRopeVisualDirection()
        {
            return GetDirection(RopeVisualAngleDegrees);
        }

        private static Vector2 GetDirection(float angleDegrees)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }

        private Vector2 GetRopeEndPosition()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece != null)
            {
                return ToUiPosition(GetRopeAttachPhysicsPosition(tiePiece)) + new Vector2(0f, cameraOffsetY);
            }

            return InitialAnchorPosition + new Vector2(0f, 35f + cameraOffsetY);
        }

        private static Vector2 GetRopeAttachPhysicsPosition(SimulatedPiece tiePiece)
        {
            var localOffset = new Vector2(
                tiePiece.rect.sizeDelta.x * RopeAttachLocalXFactor,
                tiePiece.rect.sizeDelta.y * RopeAttachLocalYFactor) * PhysicsScale;
            return tiePiece.body.position + Rotate(localOffset, tiePiece.body.rotation);
        }

        private static Vector2 Rotate(Vector2 value, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(radians);
            var cos = Mathf.Cos(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }

        private SimulatedPiece GetRopeTieSimPiece()
        {
            if (build == null || build.ropeTiePiece == null)
            {
                return null;
            }

            pieceLookup.TryGetValue(build.ropeTiePiece, out var piece);
            return piece;
        }

        private void UpdateMetrics(float remainingStageTime)
        {
            metricText.text =
                $"\u5371\u9669\u533a\u5269\u4f59\u8ddd\u79bb\uff1a{remainingDistance:0.0} m\n" +
                $"\u8239\u901f\uff1a{shipVelocity:0.0} m/s\n" +
                $"\u8239\u951a\u635f\u4f24\uff1a{anchorDamage * 100f:0}%\n" +
                $"\u65ad\u5f00\u8fde\u63a5\uff1a{brokenJoints}\n" +
                $"\u6d77\u5e95\u63a5\u89e6\uff1a{CountSeabedContacts()} \u4e2a\u90e8\u4ef6\n" +
                $"\u7a33\u8239\u5269\u4f59\uff1a{Mathf.Max(0f, remainingStageTime):0.0} s";
        }

        private void BuildAnchorVisualAndPhysics()
        {
            ClearAnchorVisual();
            EnsurePhysicsRoot();

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
                rect.sizeDelta = source.RectTransform.sizeDelta * AnchorScale;
                rect.anchoredPosition = InitialAnchorPosition + (source.RectTransform.anchoredPosition - center) * AnchorScale;
                rect.localRotation = source.RectTransform.localRotation;
                rect.localScale = source.RectTransform.localScale;

                ConfigureVisual(source, rect);

                var bodyObject = new GameObject("PhysicsPiece_" + source.Config.id);
                bodyObject.transform.SetParent(physicsRoot.transform, false);
                physicsObjects.Add(bodyObject);

                var body = bodyObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = InitialGravityScale;
                body.mass = Mathf.Max(0.1f, source.Config.weight * 0.02f);
                body.drag = Mathf.Clamp(source.Config.dragCoeff * 0.35f, 0.02f, 1.2f);
                body.angularDrag = Mathf.Clamp(source.Config.frictionCoeff * 0.75f, 0.05f, 1.5f);
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.position = ToPhysicsPosition(rect.anchoredPosition);
                body.rotation = rect.localEulerAngles.z;

                var collider = bodyObject.AddComponent<BoxCollider2D>();
                collider.size = rect.sizeDelta * PhysicsScale;
                collider.sharedMaterial = pieceMaterial;

                var simPiece = new SimulatedPiece
                {
                    source = source,
                    rect = rect,
                    body = body,
                    weaklyConnected = CountJointsForPiece(source) <= 1
                };

                simulatedPieces.Add(simPiece);
                pieceLookup[source] = simPiece;
            }

            ApplyJointDamageFalloff(center);
            BuildPhysicsJoints();
            CaptureRopeTieHorizontalLock();
        }

        private void CaptureRopeTieHorizontalLock()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                hasRopeTieHorizontalLock = false;
                return;
            }

            ropeTieLockPhysicsX = tiePiece.body.position.x;
            hasRopeTieHorizontalLock = true;
        }

        private void ConstrainAnchorHorizontalDrift()
        {
            if (!hasRopeTieHorizontalLock)
            {
                return;
            }

            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                hasRopeTieHorizontalLock = false;
                return;
            }

            var deltaX = ropeTieLockPhysicsX - tiePiece.body.position.x;
            if (Mathf.Abs(deltaX) <= 0.0001f)
            {
                return;
            }

            var correction = new Vector2(deltaX, 0f);
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                simulatedPieces[i].body.position += correction;
            }
        }

        private void ApplyJointDamageFalloff(Vector2 anchorCenter)
        {
            if (build == null || build.joints.Count == 0)
            {
                return;
            }

            var maxDistance = 0f;
            for (var i = 0; i < build.joints.Count; i++)
            {
                var distance = Vector2.Distance(GetJointCenter(build.joints[i]), anchorCenter);
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            if (maxDistance <= 0.01f)
            {
                for (var i = 0; i < build.joints.Count; i++)
                {
                    build.joints[i].damageFalloff = 0f;
                }

                return;
            }

            for (var i = 0; i < build.joints.Count; i++)
            {
                var distance = Vector2.Distance(GetJointCenter(build.joints[i]), anchorCenter);
                build.joints[i].damageFalloff = Mathf.Clamp01(distance / maxDistance);
            }
        }

        private static Vector2 GetJointCenter(AttachJoint joint)
        {
            return (joint.pieceA.RectTransform.anchoredPosition + joint.pieceB.RectTransform.anchoredPosition) * 0.5f;
        }

        private void ConfigureVisual(AnchorPiece source, RectTransform rect)
        {
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
        }

        private void BuildPhysicsJoints()
        {
            for (var i = 0; i < build.joints.Count; i++)
            {
                var sourceJoint = build.joints[i];
                sourceJoint.currentHealth = sourceJoint.maxHealth;
                sourceJoint.currentStrength = sourceJoint.currentHealth;
                sourceJoint.damage = 0f;
                sourceJoint.isBroken = false;
                sourceJoint.jointState = JointState.Stable;

                if (!pieceLookup.TryGetValue(sourceJoint.pieceA, out var pieceA) ||
                    !pieceLookup.TryGetValue(sourceJoint.pieceB, out var pieceB))
                {
                    continue;
                }

                if (sourceJoint.maxHealth <= MinEffectiveJointHealth)
                {
                    continue;
                }

                var joint = pieceA.body.gameObject.AddComponent<FixedJoint2D>();
                joint.connectedBody = pieceB.body;
                joint.autoConfigureConnectedAnchor = true;
                joint.enableCollision = false;
                joint.frequency = Mathf.Clamp(sourceJoint.currentStrength * 2.5f, 1.5f, 10f);
                joint.dampingRatio = 0.65f;
                joint.breakForce = CalculateJointBreakForce(sourceJoint, pieceA, pieceB);
                joint.breakTorque = CalculateJointBreakTorque(sourceJoint, pieceA, pieceB);
                pieceA.joints.Add(joint);
            }
        }

        private static float CalculateJointBreakForce(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            var massFactor = Mathf.Max(1f, pieceA.body.mass + pieceB.body.mass);
            var healthFactor = Mathf.Max(1f, joint.maxHealth + joint.defense);
            return Mathf.Max(0.01f, healthFactor * massFactor * JointBreakForceScale * JointStrengthMultiplier);
        }

        private static float CalculateJointBreakTorque(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            var sizeFactor = Mathf.Max(1f, (pieceA.rect.sizeDelta.magnitude + pieceB.rect.sizeDelta.magnitude) * 0.5f);
            var healthFactor = Mathf.Max(1f, joint.maxHealth + joint.defense);
            return Mathf.Max(0.01f, healthFactor * sizeFactor * JointBreakTorqueScale * JointStrengthMultiplier);
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

        private void SyncVisualsFromPhysics()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                piece.rect.anchoredPosition = ToUiPosition(piece.body.position);
                piece.rect.localRotation = Quaternion.Euler(0f, 0f, piece.body.rotation);
            }
        }

        private void EnsurePhysicsRoot()
        {
            if (physicsRoot != null)
            {
                return;
            }

            physicsRoot = new GameObject("SimulationPhysicsRoot");
            physicsRoot.transform.position = Vector3.zero;
            physicsRoot.transform.rotation = Quaternion.identity;
            physicsRoot.transform.localScale = Vector3.one;
            physicsRoot.hideFlags = HideFlags.HideInHierarchy;
            physicsObjects.Add(physicsRoot);

            seabedMaterial = new PhysicsMaterial2D("SimulationSeabed")
            {
                friction = 0.82f,
                bounciness = 0.02f
            };

            pieceMaterial = new PhysicsMaterial2D("SimulationPiece")
            {
                friction = 0.55f,
                bounciness = 0.05f
            };
        }

        private void BuildUnevenSeabed()
        {
            ClearSeabedSegments();
            EnsurePhysicsRoot();
            EnsureSeabedFill();

            var bounds = GetVisibleWorldBounds();
            seabedNoiseSeed = UnityEngine.Random.Range(0f, 10000f);
            seabedSampleOffsetX = 0f;
            var nextX = bounds.xMin - SeabedSpawnPadding;
            while (nextX < bounds.xMax + SeabedSpawnPadding)
            {
                var lastSegment = GetRightmostSeabedSegment();
                AddSeabedSegment(nextX, lastSegment == null ? (float?)null : lastSegment.end.y, null);
                nextX += SeabedSegmentLength;
            }

            RefreshSeabedFill();
        }

        private void UpdateSeabedTerrain(float deltaTime)
        {
            var ropeTieSpeed = GetAnchorHorizontalSpeedPixels();
            var scrollSpeed = Mathf.Abs(ropeTieSpeed) < 0.01f ? 8f : ropeTieSpeed;
            var dx = scrollSpeed * deltaTime;
            seabedSampleOffsetX += dx;
            for (var i = seabedSegments.Count - 1; i >= 0; i--)
            {
                var segment = seabedSegments[i];
                segment.start.x -= dx;
                segment.end.x -= dx;
                UpdateSeabedSegment(segment);

                if (segment.end.x < GetVisibleWorldBounds().xMin - SeabedDespawnPadding)
                {
                    RemoveSeabedSegmentAt(i);
                    continue;
                }

                if (segment.start.x > GetVisibleWorldBounds().xMax + SeabedDespawnPadding)
                {
                    RemoveSeabedSegmentAt(i);
                }
            }

            EnsureSeabedCoverage();
            RefreshSeabedFill();
        }

        private void EnsureSeabedCoverage()
        {
            var bounds = GetVisibleWorldBounds();
            if (seabedSegments.Count == 0)
            {
                var nextX = bounds.xMin - SeabedSpawnPadding;
                while (nextX < bounds.xMax + SeabedSpawnPadding)
                {
                    var lastSegment = GetRightmostSeabedSegment();
                    AddSeabedSegment(nextX, lastSegment == null ? (float?)null : lastSegment.end.y, null);
                    nextX += SeabedSegmentLength;
                }

                return;
            }

            var rightSegment = GetRightmostSeabedSegment();
            var rightEdge = rightSegment == null ? bounds.xMin : rightSegment.end.x;
            while (rightSegment != null && rightEdge < bounds.xMax + SeabedSpawnPadding)
            {
                AddSeabedSegment(rightEdge, rightSegment.end.y, null);
                rightSegment = GetRightmostSeabedSegment();
                rightEdge += SeabedSegmentLength;
            }

            var leftSegment = GetLeftmostSeabedSegment();
            var leftEdge = leftSegment == null ? bounds.xMin : leftSegment.start.x;
            while (leftSegment != null && leftEdge > bounds.xMin - SeabedSpawnPadding)
            {
                leftEdge -= SeabedSegmentLength;
                AddSeabedSegment(leftEdge, null, leftSegment.start.y);
                leftSegment = GetLeftmostSeabedSegment();
            }
        }

        private float GetSeabedHeight(float x)
        {
            for (var i = 0; i < seabedSegments.Count; i++)
            {
                var segment = seabedSegments[i];
                if (x >= segment.start.x && x <= segment.end.x)
                {
                    var t = Mathf.InverseLerp(segment.start.x, segment.end.x, x);
                    return Mathf.Lerp(segment.start.y, segment.end.y, t);
                }
            }

            return -245f;
        }

        private void AddSeabedSegment(float startX, float? forcedStartY, float? forcedEndY)
        {
            var sampleStartX = startX + seabedSampleOffsetX;
            var sampleEndX = sampleStartX + SeabedSegmentLength;
            var targetStartY = forcedStartY ?? SampleSmoothSeabedY(sampleStartX);
            var targetEndY = forcedEndY ?? SampleSmoothSeabedY(sampleEndX);

            if (forcedStartY.HasValue && !forcedEndY.HasValue)
            {
                targetEndY = Mathf.Clamp(targetEndY, targetStartY - SeabedMaxStepY, targetStartY + SeabedMaxStepY);
            }
            else if (!forcedStartY.HasValue && forcedEndY.HasValue)
            {
                targetStartY = Mathf.Clamp(targetStartY, targetEndY - SeabedMaxStepY, targetEndY + SeabedMaxStepY);
            }

            var start = new Vector2(startX, Mathf.Clamp(targetStartY, SeabedMinY, SeabedMaxY));
            var end = new Vector2(startX + SeabedSegmentLength, Mathf.Clamp(targetEndY, SeabedMinY, SeabedMaxY));

            var segmentObject = new GameObject("PhysicsSeabedSegment");
            segmentObject.transform.SetParent(physicsRoot.transform, false);
            physicsObjects.Add(segmentObject);

            var collider = segmentObject.AddComponent<EdgeCollider2D>();
            collider.edgeRadius = SeabedColliderRadius;
            collider.sharedMaterial = seabedMaterial;

            var segment = new SeabedSegment
            {
                collider = collider,
                start = start,
                end = end
            };

            seabedSegments.Add(segment);
            UpdateSeabedSegment(segment);
        }

        private void UpdateSeabedSegment(SeabedSegment segment)
        {
            if (segment.collider != null)
            {
                segment.collider.points = new[]
                {
                    ToPhysicsPosition(segment.start),
                    ToPhysicsPosition(segment.end)
                };
            }
        }

        private void EnsureSeabedFill()
        {
            if (seabedFill != null)
            {
                return;
            }

            var fillRect = UIBuilder.CreateRect(playArea, "SmoothSeabedFill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fillRect.gameObject.AddComponent<CanvasRenderer>();
            seabedFill = fillRect.gameObject.AddComponent<SeabedFillGraphic>();
            seabedFill.color = new Color(0.58f, 0.42f, 0.23f, 1f);
            seabedFill.raycastTarget = false;
            fillRect.SetAsFirstSibling();
        }

        private void RefreshSeabedFill()
        {
            if (seabedFill == null)
            {
                return;
            }

            seabedSegments.Sort((a, b) => a.start.x.CompareTo(b.start.x));
            seabedFillPoints.Clear();
            for (var i = 0; i < seabedSegments.Count; i++)
            {
                var segment = seabedSegments[i];
                if (i == 0)
                {
                    seabedFillPoints.Add(segment.start + new Vector2(0f, cameraOffsetY));
                }

                seabedFillPoints.Add(segment.end + new Vector2(0f, cameraOffsetY));
            }

            seabedFill.SetShape(seabedFillPoints, GetSeabedFillBottomY());
        }

        private float GetSeabedFillBottomY()
        {
            var height = playArea != null && playArea.rect.height > 1f ? playArea.rect.height : 520f;
            return -height * 0.5f - SeabedVisualThickness;
        }

        private float SampleSmoothSeabedY(float x)
        {
            var broad = Mathf.PerlinNoise(seabedNoiseSeed + x * 0.0018f, 0.17f);
            var detail = Mathf.PerlinNoise(seabedNoiseSeed * 0.37f + x * 0.006f, 0.61f);
            var normalized = Mathf.Clamp01(broad * 0.78f + detail * 0.22f);
            var smoothed = Mathf.SmoothStep(0f, 1f, normalized);
            return Mathf.Lerp(SeabedMinY, SeabedMaxY, smoothed);
        }

        private SeabedSegment GetLeftmostSeabedSegment()
        {
            if (seabedSegments.Count == 0)
            {
                return null;
            }

            SeabedSegment left = null;
            for (var i = 0; i < seabedSegments.Count; i++)
            {
                if (left == null || seabedSegments[i].start.x < left.start.x)
                {
                    left = seabedSegments[i];
                }
            }

            return left;
        }

        private SeabedSegment GetRightmostSeabedSegment()
        {
            if (seabedSegments.Count == 0)
            {
                return null;
            }

            SeabedSegment right = null;
            for (var i = 0; i < seabedSegments.Count; i++)
            {
                if (right == null || seabedSegments[i].end.x > right.end.x)
                {
                    right = seabedSegments[i];
                }
            }

            return right;
        }

        private Rect GetVisibleWorldBounds()
        {
            var width = playArea != null && playArea.rect.width > 1f ? playArea.rect.width : 900f;
            var height = playArea != null && playArea.rect.height > 1f ? playArea.rect.height : 520f;
            return new Rect(-width * 0.5f, -height * 0.5f - cameraOffsetY, width, height);
        }

        private void UpdateCameraFollow(float deltaTime)
        {
            var focusPiece = GetRopeTieSimPiece();
            var focusY = focusPiece != null ? ToUiPosition(focusPiece.body.position).y : InitialAnchorPosition.y;
            if (focusPiece == null && simulatedPieces.Count > 0)
            {
                focusY = 0f;
                for (var i = 0; i < simulatedPieces.Count; i++)
                {
                    focusY += ToUiPosition(simulatedPieces[i].body.position).y;
                }

                focusY /= simulatedPieces.Count;
            }

            var targetOffset = Mathf.Clamp(CameraAnchorTargetY - focusY, 0f, MaxCameraOffsetY);
            cameraOffsetY = Mathf.Lerp(cameraOffsetY, targetOffset, deltaTime * 2.8f);
            anchor.anchoredPosition = new Vector2(0f, cameraOffsetY);
            ship.anchoredPosition = new Vector2(ship.anchoredPosition.x, 150f + cameraOffsetY);

            var water = FindPlayAreaChild("WaterSurface");
            if (water != null)
            {
                water.anchoredPosition = new Vector2(0f, cameraOffsetY);
            }

            for (var i = 0; i < seabedSegments.Count; i++)
            {
                UpdateSeabedSegment(seabedSegments[i]);
            }

            RefreshSeabedFill();
        }

        private RectTransform FindPlayAreaChild(string childName)
        {
            if (playArea == null)
            {
                return null;
            }

            var child = playArea.Find(childName);
            return child == null ? null : child.GetComponent<RectTransform>();
        }

        private void SetStaticSeabedVisible(bool visible)
        {
            var staticSeabed = FindPlayAreaChild("Seabed");
            if (staticSeabed != null)
            {
                staticSeabed.gameObject.SetActive(visible);
            }
        }

        private int CountSeabedContacts()
        {
            var contacts = 0;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                if (simulatedPieces[i].grounded)
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

        private static Vector2 ToPhysicsPosition(Vector2 uiPosition)
        {
            return uiPosition * PhysicsScale;
        }

        private static Vector2 ToUiPosition(Vector2 physicsPosition)
        {
            return physicsPosition * InversePhysicsScale;
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
            pieceLookup.Clear();
        }

        private void ClearSeabedSegments()
        {
            for (var i = seabedSegments.Count - 1; i >= 0; i--)
            {
                if (seabedSegments[i] != null)
                {
                    if (seabedSegments[i].collider != null)
                    {
                        Destroy(seabedSegments[i].collider.gameObject);
                    }
                }
            }

            seabedSegments.Clear();
            if (seabedFill != null)
            {
                Destroy(seabedFill.gameObject);
                seabedFill = null;
            }

            seabedFillPoints.Clear();
        }

        private void RemoveSeabedSegmentAt(int index)
        {
            var segment = seabedSegments[index];
            if (segment.collider != null)
            {
                physicsObjects.Remove(segment.collider.gameObject);
                Destroy(segment.collider.gameObject);
            }

            seabedSegments.RemoveAt(index);
        }

        private void ClearPhysicsObjects()
        {
            for (var i = physicsObjects.Count - 1; i >= 0; i--)
            {
                if (physicsObjects[i] != null)
                {
                    Destroy(physicsObjects[i]);
                }
            }

            physicsObjects.Clear();
            physicsRoot = null;
            seabedMaterial = null;
            pieceMaterial = null;
        }

        private void OnDestroy()
        {
            ClearAnchorVisual();
            ClearSeabedSegments();
            ClearPhysicsObjects();
        }
    }
}
