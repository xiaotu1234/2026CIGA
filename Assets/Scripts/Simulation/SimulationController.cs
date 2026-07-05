using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
            public Collider2D collider;
            public readonly List<FixedJoint2D> joints = new List<FixedJoint2D>();
            public bool weaklyConnected;
            public bool grounded;
            public bool detachedDebris;
            public bool waterImpactTriggered;
            public float waterImpactFeedbackRemaining;
            public float unstickCooldown;
        }

        private class SeabedSegment
        {
            public Vector2 start;
            public Vector2 end;
        }

        private readonly List<SimulatedPiece> simulatedPieces = new List<SimulatedPiece>();
        private readonly List<SeabedSegment> seabedSegments = new List<SeabedSegment>();
        private readonly List<GameObject> physicsObjects = new List<GameObject>();
        private readonly Dictionary<AnchorPiece, SimulatedPiece> pieceLookup = new Dictionary<AnchorPiece, SimulatedPiece>();
        private readonly List<AttachJoint> jointHealthDisplayBuffer = new List<AttachJoint>();

        private RectTransform playArea;
        private RectTransform ship;
        private RectTransform anchor;
        private RectTransform rope;
        private RectTransform dangerZoneIndicator;
        private Text countdownText;
        private Text stageText;
        private Text metricText;
        private Slider progressSlider;
        private SeabedFillGraphic seabedFill;
        private Action<SimulationResult> onFinished;

        private AnchorBuildResult build;
        private LevelConfig level;
        private GameObject physicsRoot;
        private EdgeCollider2D seabedCollider;
        private PhysicsMaterial2D seabedMaterial;
        private PhysicsMaterial2D pieceMaterial;
        private float remainingDistance;
        private float anchorDamage;
        private int brokenJoints;
        private float simulationElapsed;
        private float shipVelocity;
        private float anchorMotionPixelsPerSecond;
        private float shipTravelPixelsPerMeter = 4f;
        private float shipStartX;
        private float cameraOffsetX;
        private float cameraOffsetY;
        private float seabedNoiseSeed;
        private float seabedSampleOffsetX;
        private bool anchorOnSeabed;
        private bool ropeForceEnabled;
        private bool waterEntryInitialSpeedActive;
        private Coroutine running;
        private readonly List<Vector2> seabedFillPoints = new List<Vector2>();

        private const float WaterEntryDuration = 0.8f;
        private const float DangerZoneVisiblePadding = 80f;
        private const float AnchorScale = 0.55f;
        private const float PhysicsScale = 0.012f;
        private const float InversePhysicsScale = 1f / PhysicsScale;
        private const float InitialGravityScale = 6f;
        private const float SeabedGravityScale = 3.6f;
        private const float WaterImpactFeedbackDuration = 0.16f;
        private const float WaterImpactFeedbackGravityScale = 0.35f;
        private const float WaterImpactMinReboundSpeed = 1.2f;
        private const float WaterImpactReboundSpeedFactor = 0.18f;
        private const float WaterImpactMaxReboundSpeed = 4f;
        private const float MinEffectiveJointHealth = 0.01f;
        private const float AnchorSpeedPixelsPerMeter = 24f;
        private const float DangerZoneRevealDistance = 120f;
        private const float RopeVisualAngleDegrees = 60f;
        private const float RopePullAngleDegrees = 45f;
        private const float RopeVisualLengthPadding = 220f;
        private const float RopePullForceMultiplier = 1f;
        private const float RopePullBaseForce = 8f;
        private const float RopePullForcePerCurrent = 1.4f;
        private const float DragAssistForce = 10f;
        private const float DragAssistMinSpeedPixels = 70f;
        private const float DragAssistClimbForce = 5f;
        private const float DragAssistStuckSpeedPixels = 25f;
        private const float DragAssistSlopeSamplePixels = 24f;
        private const float DragAssistGroundClearancePixels = 1.5f;
        private const float DragAssistMaxNudgePixels = 1.5f;
        private const float DragAssistMaxClimbSpeedPixels = 35f;
        private const float SeabedUnstickSpeedThresholdPixels = 28f;
        private const float SeabedUnstickUpSpeedPixels = 95f;
        private const float SeabedUnstickForwardSpeedPixels = 45f;
        private const float SeabedUnstickClearancePixels = 6f;
        private const float SeabedUnstickMaxLiftPixels = 10f;
        private const float SeabedUnstickCooldownSeconds = 0.08f;
        private const float RopeReleaseWaterDepthRatio = 0.2f;
        private const float RopeAttachLocalXFactor = 0.45f;
        private const float RopeAttachLocalYFactor = 0.32f;
        private const float AntiLiftDragLinear = 0.08f;
        private const float AntiLiftDragMaxAcceleration = 18f;
        private const float AntiLiftMinUpwardSpeedPixels = 80f;
        private const float AntiLiftStartHeightPixels = 30f;
        private const float MaxLiftWaterDepthRatio = 1f / 3f;
        private const float JointStrengthMultiplier = 3f;
        private const float JointBreakTorqueScale = 0.035f;
        private const float RigidJointFrequency = 55f;
        private const float RigidJointDampingRatio = 1f;
        private const float DebrisFlySpeedPixels = 620f;
        private const float DebrisFlyUpSpeedPixels = 260f;
        private const float DebrisSpinDegreesPerSecond = 540f;
        private const float DebrisDestroyDelay = 1.35f;
        private const int JointHealthDisplayLimit = 10;
        private const float SurfaceY = 130f;
        private const float CameraAnchorTargetY = 35f;
        private const float MaxCameraOffsetY = 1250f;
        private const float SeabedSegmentLength = 36f;
        private const float SeabedVisualThickness = 100f;
        private const float SeabedColliderRadius = 0.06f;
        private const float SeabedContactPadding = 14f;
        private const int SeabedContactSampleCount = 3;
        private const float PieceColliderContactSkin = 1f;
        private const float SeabedLeftSpawnPadding = 360f;
        private const float SeabedRightSpawnPadding = 900f;
        private const float SeabedLeftDespawnPadding = 260f;
        private const float SeabedRightDespawnPadding = 960f;
        private const float SeabedMinY = -1172f;
        private const float SeabedMaxY = -890f;
        private const float SeabedMaxStepY = 14f;

        private static readonly Vector2 InitialAnchorPosition = new Vector2(-330f, SurfaceY);

        public void Initialize(
            RectTransform playArea,
            RectTransform ship,
            RectTransform anchor,
            RectTransform rope,
            Text countdownText,
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
            if (dangerZoneIndicator == null && anchor != null)
            {
                dangerZoneIndicator = anchor.Find("DangerZone") as RectTransform;
            }

            this.countdownText = countdownText;
            this.stageText = stageText;
            this.metricText = metricText;
            if (this.metricText != null)
            {
                this.metricText.fontSize = 14;
            }

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
                    UpdateWaterEntryInitialSpeed();
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

                DetectSeabedContacts();
                ApplyRopeForce();
                ApplyDragAssist();
                ApplySeabedUnstick(deltaTime);
                ApplyAntiLiftResistance();
                ClampLiftHeightToWaterDepth();
                ApplyUnderwaterRightForceAndSpeedLimit();
                SyncJointBreakStates();
                ApplyAnchorDrivenShipMotion(deltaTime);
                SyncVisualsFromPhysics();
                UpdateSeabedTerrain(deltaTime);
                UpdateCameraFollow(deltaTime);
                UpdateDangerZoneVisibility();
                DrawRope();
                UpdateMetrics(Mathf.Max(0f, level.stableDuration - simulationElapsed));
                yield return null;
            }

            var result = ResultEvaluator.Evaluate(build, level, remainingDistance, anchorDamage, brokenJoints);
            onFinished?.Invoke(result);
        }

        private void WaterEntryTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                piece.body.gravityScale = InitialGravityScale;
                ApplyWaterSurfaceImpact(piece);
                ApplyWaterImpactMotionFeedback(piece);
            }
        }

        private void ApplyWaterSurfaceImpact(SimulatedPiece piece)
        {
            if (piece.waterImpactTriggered || GetColliderBottomUi(piece) > SurfaceY)
            {
                return;
            }

            piece.waterImpactTriggered = true;
            var downwardSpeed = Mathf.Max(0f, -piece.body.velocity.y);
            var impactImpulse = Mathf.Max(0f, level.waterSurfaceTensionForce) * downwardSpeed;
            if (impactImpulse <= 0f)
            {
                return;
            }

            piece.body.AddForce(Vector2.up * impactImpulse * piece.body.mass, ForceMode2D.Impulse);
            piece.waterImpactFeedbackRemaining = WaterImpactFeedbackDuration;
            var reboundSpeed = Mathf.Clamp(impactImpulse * WaterImpactReboundSpeedFactor, WaterImpactMinReboundSpeed, WaterImpactMaxReboundSpeed);
            piece.body.velocity = new Vector2(piece.body.velocity.x, Mathf.Max(piece.body.velocity.y, reboundSpeed));
        }

        private void ApplyWaterImpactMotionFeedback(SimulatedPiece piece)
        {
            if (piece.waterImpactFeedbackRemaining <= 0f)
            {
                return;
            }

            piece.waterImpactFeedbackRemaining = Mathf.Max(0f, piece.waterImpactFeedbackRemaining - Time.deltaTime);
            piece.body.gravityScale = WaterImpactFeedbackGravityScale;
            if (piece.body.velocity.y < WaterImpactMinReboundSpeed)
            {
                piece.body.velocity = new Vector2(piece.body.velocity.x, WaterImpactMinReboundSpeed);
            }
        }

        private void SinkTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var material = piece.source.Config;
                var downwardBias = Mathf.Clamp(material.weight * 0.008f * Mathf.Max(0f, level.weightDownForceCoefficient) - material.dragCoeff * 0.18f, 0.08f, 1.4f);
                var waterDrag = -piece.body.velocity * Mathf.Clamp(material.dragCoeff * 0.12f * Mathf.Max(0f, level.waterDragCoefficient), 0.02f, 0.22f);

                piece.body.gravityScale = InitialGravityScale;
                piece.body.AddForce(new Vector2(0f, -downwardBias) + waterDrag, ForceMode2D.Force);
            }
        }

        private void SeabedTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris)
                {
                    continue;
                }

                piece.body.gravityScale = SeabedGravityScale;
            }
        }

        private void ApplyAnchorDrivenShipMotion(float deltaTime)
        {
            anchorMotionPixelsPerSecond = GetAnchorHorizontalSpeedPixels();
            var maxShipSpeed = level.maxItemSpeed > 0f ? level.maxItemSpeed : 42f;
            shipVelocity = Mathf.Clamp(anchorMotionPixelsPerSecond / AnchorSpeedPixelsPerMeter, -maxShipSpeed, maxShipSpeed);
            remainingDistance = Mathf.Clamp(remainingDistance - shipVelocity * deltaTime, 0f, level.dangerZoneDistance);
            UpdateShipPositionFromRemainingDistance();

            progressSlider.value = 1f - remainingDistance / level.dangerZoneDistance;
            UpdateDangerZoneVisibility();
        }

        private void ApplyRopeForce()
        {
            if (!ropeForceEnabled)
            {
                return;
            }

            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                return;
            }

            if (!tiePiece.grounded && IsAboveRopeReleaseHeight(tiePiece))
            {
                ropeForceEnabled = false;
                return;
            }

            var pullForce = Mathf.Max(0f, level.currentForceBase) * Mathf.Max(0f, level.forceCoefficient);
            tiePiece.body.AddForceAtPosition(GetRopePullDirection() * pullForce * tiePiece.body.mass, GetRopeAttachPhysicsPosition(tiePiece), ForceMode2D.Force);
        }

        private void ApplyUnderwaterRightForceAndSpeedLimit()
        {
            if (simulationElapsed <= WaterEntryDuration)
            {
                return;
            }

            var rightForce = Mathf.Max(0f, level.underwaterRightForce) * Mathf.Max(0f, level.forceCoefficient);
            var maxSpeed = Mathf.Max(0f, level.maxItemSpeed) * AnchorSpeedPixelsPerMeter * PhysicsScale;
            var totalMass = 0f;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris || piece.body == null)
                {
                    continue;
                }

                totalMass += Mathf.Max(0f, piece.body.mass);
            }

            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris || piece.body == null)
                {
                    continue;
                }

                if (rightForce > 0f && totalMass > 0f)
                {
                    var massRatio = Mathf.Max(0f, piece.body.mass) / totalMass;
                    piece.body.AddForce(Vector2.right * rightForce * totalMass * massRatio, ForceMode2D.Force);
                }

                if (maxSpeed > 0f && Mathf.Abs(piece.body.velocity.x) > maxSpeed)
                {
                    var velocity = piece.body.velocity;
                    velocity.x = Mathf.Sign(velocity.x) * maxSpeed;
                    piece.body.velocity = velocity;
                }
            }
        }

        private void ApplyDragAssist()
        {
            if (!ropeForceEnabled || !anchorOnSeabed)
            {
                return;
            }

            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null || !tiePiece.grounded)
            {
                return;
            }

            var body = tiePiece.body;
            var bodyX = ToUiPosition(body.position).x;
            var tangent = GetSeabedForwardTangent(bodyX);
            body.AddForce(tangent * DragAssistForce * body.mass * Mathf.Max(0f, level.forceCoefficient), ForceMode2D.Force);

            var horizontalSpeedPixels = body.velocity.x * InversePhysicsScale;
            var forwardSpeed = Vector2.Dot(body.velocity, tangent);
            var minSpeed = DragAssistMinSpeedPixels * PhysicsScale;
            if (tangent.y > 0f && forwardSpeed < minSpeed)
            {
                body.velocity += tangent * (minSpeed - forwardSpeed);
            }
            else if (body.velocity.x < minSpeed)
            {
                body.velocity = new Vector2(minSpeed, body.velocity.y);
            }

            if (horizontalSpeedPixels < DragAssistStuckSpeedPixels)
            {
                var climbDirection = tangent.y > 0f ? tangent : Vector2.up;
                body.AddForce(climbDirection * DragAssistClimbForce * body.mass, ForceMode2D.Force);
                NudgeTiePieceAboveSeabed(tiePiece);
            }

            var maxClimbSpeed = DragAssistMaxClimbSpeedPixels * PhysicsScale;
            if (body.velocity.y > maxClimbSpeed)
            {
                body.velocity = new Vector2(body.velocity.x, maxClimbSpeed);
            }
        }

        private Vector2 GetSeabedForwardTangent(float x)
        {
            var y0 = GetSeabedHeight(x);
            var y1 = GetSeabedHeight(x + DragAssistSlopeSamplePixels);
            return new Vector2(DragAssistSlopeSamplePixels, y1 - y0).normalized;
        }

        private void NudgeTiePieceAboveSeabed(SimulatedPiece tiePiece)
        {
            var bottom = GetColliderBottomUi(tiePiece);
            var minBottom = GetHighestSeabedHeightUnderPiece(tiePiece) + DragAssistGroundClearancePixels;
            var liftPixels = Mathf.Min(minBottom - bottom, DragAssistMaxNudgePixels);
            if (liftPixels <= 0f)
            {
                return;
            }

            var body = tiePiece.body;
            body.position += Vector2.up * liftPixels * PhysicsScale;
            if (body.velocity.y < 0f)
            {
                body.velocity = new Vector2(body.velocity.x, 0f);
            }
        }

        private void ApplySeabedUnstick(float deltaTime)
        {
            if (!ropeForceEnabled || !anchorOnSeabed)
            {
                return;
            }

            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris)
                {
                    continue;
                }

                if (piece.unstickCooldown > 0f)
                {
                    piece.unstickCooldown = Mathf.Max(0f, piece.unstickCooldown - deltaTime);
                    continue;
                }

                if (!piece.grounded)
                {
                    continue;
                }

                var body = piece.body;
                var horizontalSpeedPixels = body.velocity.x * InversePhysicsScale;
                if (horizontalSpeedPixels >= SeabedUnstickSpeedThresholdPixels)
                {
                    continue;
                }

                LiftPieceAboveSeabed(piece);

                var bodyX = ToUiPosition(body.position).x;
                var tangent = GetSeabedForwardTangent(bodyX);
                var minForwardSpeed = Mathf.Max(SeabedUnstickForwardSpeedPixels * PhysicsScale, body.velocity.x);
                var forwardSpeed = Vector2.Dot(body.velocity, tangent);
                var minUpSpeed = SeabedUnstickUpSpeedPixels * PhysicsScale;
                if (forwardSpeed < minForwardSpeed)
                {
                    body.velocity += tangent * (minForwardSpeed - forwardSpeed);
                }

                body.velocity = new Vector2(
                    Mathf.Max(body.velocity.x, minForwardSpeed),
                    Mathf.Max(body.velocity.y, minUpSpeed));
                piece.unstickCooldown = SeabedUnstickCooldownSeconds;
            }
        }

        private void LiftPieceAboveSeabed(SimulatedPiece piece)
        {
            var bottom = GetColliderBottomUi(piece);
            var targetBottom = GetHighestSeabedHeightUnderPiece(piece) + SeabedUnstickClearancePixels;
            var liftPixels = Mathf.Min(targetBottom - bottom, SeabedUnstickMaxLiftPixels);
            if (liftPixels <= 0f)
            {
                return;
            }

            piece.body.position += Vector2.up * liftPixels * PhysicsScale;
        }

        private void ApplyAntiLiftResistance()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris)
                {
                    continue;
                }

                var body = piece.body;
                var upwardSpeedPixels = body.velocity.y * InversePhysicsScale;
                if (upwardSpeedPixels <= AntiLiftMinUpwardSpeedPixels)
                {
                    continue;
                }

                var liftHeight = GetColliderBottomUi(piece) - GetHighestSeabedHeightUnderPiece(piece);
                if (liftHeight <= AntiLiftStartHeightPixels)
                {
                    continue;
                }

                var acceleration = Mathf.Min(AntiLiftDragMaxAcceleration, upwardSpeedPixels * AntiLiftDragLinear);
                body.AddForce(Vector2.down * acceleration * body.mass, ForceMode2D.Force);
            }
        }

        private void ClampLiftHeightToWaterDepth()
        {
            if (!anchorOnSeabed)
            {
                return;
            }

            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (piece.detachedDebris)
                {
                    continue;
                }

                var body = piece.body;
                var seabedY = GetHighestSeabedHeightUnderPiece(piece);
                var maxLiftHeight = Mathf.Max(0f, (SurfaceY - seabedY) * MaxLiftWaterDepthRatio);
                var liftHeight = GetColliderBottomUi(piece) - seabedY;
                if (liftHeight <= maxLiftHeight)
                {
                    continue;
                }

                var excessPixels = liftHeight - maxLiftHeight;
                body.position -= Vector2.up * excessPixels * PhysicsScale;
                if (body.velocity.y > 0f)
                {
                    body.velocity = new Vector2(body.velocity.x, 0f);
                }
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

                if (joint.isBroken)
                {
                    joint.damage = 1f;
                    joint.currentHealth = 0f;
                    joint.currentStrength = 0f;
                    joint.jointState = JointState.Broken;
                    continue;
                }

                if (!TryGetLiveJoint(pieceA, pieceB.body, out var physicsJoint))
                {
                    BreakJoint(joint, pieceA, pieceB);
                    continue;
                }

                ApplyLinearJointDamage(joint, physicsJoint);
                if (joint.currentHealth <= 0f ||
                    ExceedsAngularBreakTorque(joint, physicsJoint))
                {
                    BreakJoint(joint, pieceA, pieceB);
                    continue;
                }

                joint.damage = CalculateJointDamageRatio(joint);
                joint.currentStrength = joint.currentHealth;
                joint.jointState = joint.damage > 0f ? JointState.Stretching : JointState.Stable;
            }

            anchorDamage = CalculateAnchorDamage();
        }

        private void ApplyLinearJointDamage(AttachJoint joint, FixedJoint2D physicsJoint)
        {
            joint.lastReactionForce = physicsJoint.reactionForce.magnitude;
            joint.linearDamagePerSecond = Mathf.Max(0f, joint.lastReactionForce - joint.linearForceThreshold) * Mathf.Max(0f, level.damageCoefficient);
            if (joint.linearDamagePerSecond <= 0f)
            {
                return;
            }

            joint.currentHealth = Mathf.Max(0f, joint.currentHealth - joint.linearDamagePerSecond * Time.deltaTime);
        }

        private static bool ExceedsAngularBreakTorque(AttachJoint joint, FixedJoint2D physicsJoint)
        {
            return Mathf.Abs(physicsJoint.reactionTorque) > joint.torqueBreakThreshold;
        }

        private static float CalculateJointDamageRatio(AttachJoint joint)
        {
            if (joint.maxHealth <= Mathf.Epsilon)
            {
                return joint.isBroken ? 1f : 0f;
            }

            return Mathf.Clamp01(1f - joint.currentHealth / joint.maxHealth);
        }

        private void BreakJoint(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            joint.isBroken = true;
            joint.jointState = JointState.Broken;
            brokenJoints++;

            DestroyMatchingJoint(pieceA, pieceB.body);
            DestroyMatchingJoint(pieceB, pieceA.body);
            RefreshDetachedDebris();
        }

        private void RefreshDetachedDebris()
        {
            if (build == null || build.ropeTiePiece == null)
            {
                return;
            }

            var connectedToCore = new HashSet<AnchorPiece>();
            var open = new Queue<AnchorPiece>();
            connectedToCore.Add(build.ropeTiePiece);
            open.Enqueue(build.ropeTiePiece);

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                for (var i = 0; i < build.joints.Count; i++)
                {
                    var joint = build.joints[i];
                    if (joint.isBroken)
                    {
                        continue;
                    }

                    var neighbor = joint.pieceA == current ? joint.pieceB : joint.pieceB == current ? joint.pieceA : null;
                    if (neighbor == null || connectedToCore.Contains(neighbor))
                    {
                        continue;
                    }

                    connectedToCore.Add(neighbor);
                    open.Enqueue(neighbor);
                }
            }

            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                if (!connectedToCore.Contains(piece.source))
                {
                    DetachPieceAsDebris(piece);
                }
            }
        }

        private void DetachPieceAsDebris(SimulatedPiece debris)
        {
            if (debris.detachedDebris)
            {
                return;
            }

            debris.detachedDebris = true;
            PlayDetachedPieceAudio(debris);
            for (var i = debris.joints.Count - 1; i >= 0; i--)
            {
                if (debris.joints[i] != null)
                {
                    Destroy(debris.joints[i]);
                }
            }

            debris.joints.Clear();
            if (debris.collider != null)
            {
                debris.collider.enabled = false;
            }

            var body = debris.body;
            if (body != null)
            {
                var uiPosition = ToUiPosition(body.position);
                var flyX = uiPosition.x >= 0f ? 1f : -1f;
                var flyDirection = new Vector2(flyX * DebrisFlySpeedPixels, DebrisFlyUpSpeedPixels) * PhysicsScale;
                body.gravityScale = 0f;
                body.velocity = flyDirection;
                body.angularVelocity = -flyX * DebrisSpinDegreesPerSecond;
            }

            StartCoroutine(RemoveDebrisAfterFlight(debris));
        }

        private static void PlayDetachedPieceAudio(SimulatedPiece debris)
        {
            var audio = debris.rect == null ? null : debris.rect.GetComponent<PieceInteractionAudio>();
            if (audio == null && debris.source != null)
            {
                audio = debris.source.GetComponent<PieceInteractionAudio>();
            }

            if (audio != null)
            {
                audio.PlayDetachedOnce();
            }
        }

        private IEnumerator RemoveDebrisAfterFlight(SimulatedPiece debris)
        {
            yield return new WaitForSeconds(DebrisDestroyDelay);

            simulatedPieces.Remove(debris);
            if (debris.source != null)
            {
                pieceLookup.Remove(debris.source);
            }

            if (debris.rect != null)
            {
                Destroy(debris.rect.gameObject);
            }

            if (debris.body != null)
            {
                physicsObjects.Remove(debris.body.gameObject);
                Destroy(debris.body.gameObject);
            }
        }

        private static bool TryGetLiveJoint(SimulatedPiece owner, Rigidbody2D connectedBody, out FixedJoint2D liveJoint)
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
                    liveJoint = joint;
                    return true;
                }
            }

            liveJoint = null;
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
                if (piece.detachedDebris)
                {
                    continue;
                }

                var bottom = GetColliderBottomUi(piece);
                var seabedY = GetHighestSeabedHeightUnderPiece(piece);
                piece.grounded = bottom <= seabedY + SeabedContactPadding;
                if (piece.grounded)
                {
                    contacts++;
                }
            }

            if (contacts > 0)
            {
                anchorOnSeabed = true;
            }

            UpdateRopeForceAvailability();
        }

        private void UpdateRopeForceAvailability()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                ropeForceEnabled = false;
                return;
            }

            if (tiePiece.grounded)
            {
                ropeForceEnabled = true;
                return;
            }

            if (ropeForceEnabled && IsAboveRopeReleaseHeight(tiePiece))
            {
                ropeForceEnabled = false;
            }
        }

        private bool IsAboveRopeReleaseHeight(SimulatedPiece piece)
        {
            var x = ToUiPosition(piece.body.position).x;
            var seabedY = GetSeabedHeight(x);
            var waterDepthPixels = Mathf.Max(1f, SurfaceY - seabedY);
            var releaseHeight = seabedY + waterDepthPixels * RopeReleaseWaterDepthRatio;
            return GetColliderBottomUi(piece) > releaseHeight;
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
            cameraOffsetX = 0f;
            cameraOffsetY = 0f;
            anchorOnSeabed = false;
            ropeForceEnabled = false;
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
            var shipRightEdge = GetShipRightEdge();
            shipTravelPixelsPerMeter = AnchorSpeedPixelsPerMeter;
            shipStartX = ship.anchoredPosition.x;
            ConfigureDangerZoneLine();
            UpdateShipPositionFromRemainingDistance();
            UpdateDangerZonePosition();
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
            SetDangerZoneVisible(remainingDistance <= revealDistance || IsDangerZoneLineNearView());
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

        private void ConfigureDangerZoneLine()
        {
            if (dangerZoneIndicator == null)
            {
                return;
            }

            if (anchor != null && dangerZoneIndicator.parent != anchor)
            {
                dangerZoneIndicator.SetParent(anchor, false);
                dangerZoneIndicator.SetAsFirstSibling();
            }

            var lineHeight = playArea != null && playArea.rect.height > 0f ? playArea.rect.height + DangerZoneVisiblePadding * 2f : 680f;
            var regionWidth = playArea != null && playArea.rect.width > 0f ? playArea.rect.width + DangerZoneVisiblePadding * 2f : 1060f;
            dangerZoneIndicator.anchorMin = new Vector2(0.5f, 0.5f);
            dangerZoneIndicator.anchorMax = new Vector2(0.5f, 0.5f);
            dangerZoneIndicator.pivot = new Vector2(0f, 0.5f);
            dangerZoneIndicator.sizeDelta = new Vector2(regionWidth, lineHeight);
        }

        private void UpdateDangerZonePosition()
        {
            if (dangerZoneIndicator == null)
            {
                return;
            }

            dangerZoneIndicator.anchoredPosition = new Vector2(GetDangerBoundaryScreenX() - cameraOffsetX, -cameraOffsetY);
        }

        private float GetDangerBoundaryScreenX()
        {
            return GetRopeEndPosition().x + remainingDistance * shipTravelPixelsPerMeter;
        }

        private bool IsDangerZoneLineNearView()
        {
            if (playArea == null)
            {
                return true;
            }

            var halfWidth = playArea.rect.width * 0.5f;
            var screenX = GetDangerBoundaryScreenX();
            return screenX >= -halfWidth - DangerZoneVisiblePadding && screenX <= halfWidth + DangerZoneVisiblePadding;
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
                return ToUiPosition(GetRopeAttachPhysicsPosition(tiePiece)) + new Vector2(cameraOffsetX, cameraOffsetY);
            }

            return InitialAnchorPosition + new Vector2(cameraOffsetX, 35f + cameraOffsetY);
        }

        private static Vector2 GetRopeAttachPhysicsPosition(SimulatedPiece tiePiece)
        {
            var visualSize = GetVisualSize(tiePiece.rect);
            var localOffset = new Vector2(
                visualSize.x * RopeAttachLocalXFactor,
                visualSize.y * RopeAttachLocalYFactor) * PhysicsScale;
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
            UpdateCountdownText(remainingStageTime);
            metricText.text =
                $"\u5371\u9669\u533a\u5269\u4f59\u8ddd\u79bb\uff1a{remainingDistance:0.0} m\n" +
                $"\u8239\u901f\uff1a{shipVelocity:0.0} m/s\n" +
                $"\u8239\u951a\u635f\u4f24\uff1a{anchorDamage * 100f:0}%\n" +
                $"\u65ad\u5f00\u8fde\u63a5\uff1a{brokenJoints}\n" +
                $"\u6d77\u5e95\u63a5\u89e6\uff1a{CountSeabedContacts()} \u4e2a\u90e8\u4ef6\n" +
                $"\u7a33\u8239\u5269\u4f59\uff1a{Mathf.Max(0f, remainingStageTime):0.0} s\n\n" +
                BuildJointHealthDebugText();
        }

        private void UpdateCountdownText(float remainingStageTime)
        {
            if (countdownText == null)
            {
                return;
            }

            var seconds = Mathf.CeilToInt(Mathf.Max(0f, remainingStageTime));
            countdownText.text = $"\u98ce\u66b4\u505c\u606f\u5012\u8ba1\u65f6\uff1a{seconds / 60:00}:{seconds % 60:00}";
        }

        private string BuildJointHealthDebugText()
        {
            if (level != null && !level.showConnectionHealthDebug)
            {
                return string.Empty;
            }

            if (build == null || build.joints.Count == 0)
            {
                return "\u5173\u8282\u8840\u91cf\uff1a\u65e0";
            }

            jointHealthDisplayBuffer.Clear();
            for (var i = 0; i < build.joints.Count; i++)
            {
                jointHealthDisplayBuffer.Add(build.joints[i]);
            }

            jointHealthDisplayBuffer.Sort(CompareJointHealthPercent);

            var builder = new StringBuilder();
            builder.AppendLine("\u5173\u8282\u8840\u91cf Bottom 10");
            var count = Mathf.Min(JointHealthDisplayLimit, jointHealthDisplayBuffer.Count);
            for (var i = 0; i < count; i++)
            {
                var joint = jointHealthDisplayBuffer[i];
                var percent = GetJointHealthPercent(joint) * 100f;
                builder
                    .Append(i + 1)
                    .Append(". ")
                    .Append(GetShortPieceName(joint.pieceA))
                    .Append("/")
                    .Append(GetShortPieceName(joint.pieceB))
                    .Append(" ")
                    .Append(percent.ToString("0"))
                    .Append("%")
                    .Append(" F:")
                    .Append(joint.lastReactionForce.ToString("0.0"))
                    .Append("/")
                    .Append(joint.linearForceThreshold.ToString("0.0"));

                if (joint.isBroken)
                {
                    builder.Append(" \u65ad");
                }

                if (i < count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static int CompareJointHealthPercent(AttachJoint left, AttachJoint right)
        {
            return GetJointHealthPercent(left).CompareTo(GetJointHealthPercent(right));
        }

        private static float GetJointHealthPercent(AttachJoint joint)
        {
            if (joint.isBroken)
            {
                return 0f;
            }

            if (joint.maxHealth <= Mathf.Epsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(joint.currentHealth / joint.maxHealth);
        }

        private static string GetShortPieceName(AnchorPiece piece)
        {
            if (piece == null || piece.Config == null)
            {
                return "?";
            }

            var name = piece.Config.displayName;
            if (string.IsNullOrEmpty(name))
            {
                name = piece.Config.id;
            }

            return string.IsNullOrEmpty(name) ? "?" : name.Replace("\n", "");
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
                var snapshot = build.GetPieceSnapshot(source);
                var sourcePosition = snapshot == null ? source.RectTransform.anchoredPosition : snapshot.anchoredPosition;
                var sourceSize = snapshot == null ? source.RectTransform.sizeDelta : snapshot.sizeDelta;
                var sourceRotation = snapshot == null ? source.RectTransform.localRotation : snapshot.localRotation;
                var sourceScale = snapshot == null ? source.RectTransform.localScale : snapshot.localScale;
                var rect = CreateSimPieceRect(source);
                rect.sizeDelta = sourceSize;
                rect.anchoredPosition = InitialAnchorPosition + (sourcePosition - center) * AnchorScale;
                rect.localRotation = sourceRotation;
                rect.localScale = new Vector3(sourceScale.x * AnchorScale, sourceScale.y * AnchorScale, sourceScale.z);

                ConfigureVisual(source, rect);

                var bodyObject = new GameObject("PhysicsPiece_" + source.Config.id);
                bodyObject.transform.SetParent(physicsRoot.transform, false);
                physicsObjects.Add(bodyObject);

                var body = bodyObject.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = InitialGravityScale;
                body.mass = Mathf.Max(0.1f, source.Config.weight * 0.02f * Mathf.Max(0.01f, level.rigidbodyMassCoefficient));
                body.drag = Mathf.Clamp(source.Config.dragCoeff * 0.35f, 0.02f, 1.2f);
                body.angularDrag = Mathf.Clamp(source.Config.frictionCoeff * 0.75f, 0.05f, 1.5f);
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.position = ToPhysicsPosition(rect.anchoredPosition);
                body.rotation = rect.localEulerAngles.z;

                var collider = CreateSimCollider(source, bodyObject, sourceScale);

                var simPiece = new SimulatedPiece
                {
                    source = source,
                    rect = rect,
                    body = body,
                    collider = collider,
                    weaklyConnected = CountJointsForPiece(source) <= 1,
                    detachedDebris = false,
                    unstickCooldown = 0f
                };

                simulatedPieces.Add(simPiece);
                pieceLookup[source] = simPiece;
            }

            ApplyJointDamageFalloff(center);
            BuildPhysicsJoints();
            ApplyWaterEntryInitialSpeed();
        }

        private void ApplyWaterEntryInitialSpeed()
        {
            waterEntryInitialSpeedActive = level.waterEntryInitialSpeed > 0f;
            if (!waterEntryInitialSpeedActive)
            {
                return;
            }

            var initialVelocity = Vector2.down * level.waterEntryInitialSpeed;
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                simulatedPieces[i].body.velocity = initialVelocity;
            }
        }

        private void UpdateWaterEntryInitialSpeed()
        {
            if (!waterEntryInitialSpeedActive)
            {
                return;
            }

            if (HasTouchedWaterSurface())
            {
                waterEntryInitialSpeedActive = false;
                return;
            }

            var downwardSpeed = -Mathf.Max(0f, level.waterEntryInitialSpeed);
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var body = simulatedPieces[i].body;
                body.velocity = new Vector2(body.velocity.x, downwardSpeed);
            }
        }

        private bool HasTouchedWaterSurface()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                if (GetColliderBottomUi(simulatedPieces[i]) <= SurfaceY)
                {
                    return true;
                }
            }

            return false;
        }

        private Collider2D CreateSimCollider(AnchorPiece source, GameObject bodyObject, Vector3 sourceScale)
        {
            var scale = new Vector2(sourceScale.x * AnchorScale, sourceScale.y * AnchorScale);
            var sourceCollider = source.ShapeCollider;
            if (sourceCollider is PolygonCollider2D sourcePolygon)
            {
                var collider = bodyObject.AddComponent<PolygonCollider2D>();
                collider.pathCount = sourcePolygon.pathCount;
                collider.offset = ScaleToPhysics(sourcePolygon.offset, scale);
                for (var path = 0; path < sourcePolygon.pathCount; path++)
                {
                    var points = sourcePolygon.GetPath(path);
                    var scaledPoints = new Vector2[points.Length];
                    for (var i = 0; i < points.Length; i++)
                    {
                        scaledPoints[i] = ScaleToPhysics(points[i], scale);
                    }

                    if (scale.x * scale.y < 0f)
                    {
                        Array.Reverse(scaledPoints);
                    }

                    collider.SetPath(path, scaledPoints);
                }

                collider.sharedMaterial = pieceMaterial;
                return collider;
            }

            if (sourceCollider is BoxCollider2D sourceBox)
            {
                var collider = bodyObject.AddComponent<BoxCollider2D>();
                collider.offset = ScaleToPhysics(sourceBox.offset, scale);
                collider.size = AbsScaleToPhysics(sourceBox.size, scale) * PieceColliderContactSkin;
                collider.sharedMaterial = pieceMaterial;
                return collider;
            }

            if (sourceCollider is CircleCollider2D sourceCircle)
            {
                var collider = bodyObject.AddComponent<CircleCollider2D>();
                collider.offset = ScaleToPhysics(sourceCircle.offset, scale);
                collider.radius = sourceCircle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) * PhysicsScale * PieceColliderContactSkin;
                collider.sharedMaterial = pieceMaterial;
                return collider;
            }

            var fallback = bodyObject.AddComponent<BoxCollider2D>();
            fallback.size = GetVisualSize(source.RectTransform) * AnchorScale * PhysicsScale * PieceColliderContactSkin;
            fallback.sharedMaterial = pieceMaterial;
            return fallback;
        }

        private static Vector2 ScaleToPhysics(Vector2 value, Vector2 scale)
        {
            return new Vector2(value.x * scale.x, value.y * scale.y) * PhysicsScale;
        }

        private static Vector2 AbsScaleToPhysics(Vector2 value, Vector2 scale)
        {
            return new Vector2(value.x * Mathf.Abs(scale.x), value.y * Mathf.Abs(scale.y)) * PhysicsScale;
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

        private Vector2 GetJointCenter(AttachJoint joint)
        {
            return (GetSnapshotPosition(joint.pieceA) + GetSnapshotPosition(joint.pieceB)) * 0.5f;
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

                sourceJoint.torqueBreakThreshold = CalculateJointBreakTorque(sourceJoint, pieceA, pieceB);
                sourceJoint.maxHealth = CalculateJointMaxHealth(sourceJoint);
                sourceJoint.currentHealth = sourceJoint.maxHealth;
                sourceJoint.currentStrength = sourceJoint.currentHealth;
                sourceJoint.linearForceThreshold = CalculateJointLinearForceThreshold(sourceJoint);
                sourceJoint.linearDamagePerSecond = 0f;
                sourceJoint.lastReactionForce = 0f;
                sourceJoint.damage = 0f;

                if (sourceJoint.maxHealth <= MinEffectiveJointHealth)
                {
                    continue;
                }

                var joint = pieceA.body.gameObject.AddComponent<FixedJoint2D>();
                joint.connectedBody = pieceB.body;
                joint.autoConfigureConnectedAnchor = true;
                joint.enableCollision = false;
                joint.frequency = RigidJointFrequency;
                joint.dampingRatio = RigidJointDampingRatio;
                joint.breakForce = Mathf.Infinity;
                joint.breakTorque = Mathf.Infinity;
                pieceA.joints.Add(joint);
            }
        }

        private float CalculateJointMaxHealth(AttachJoint joint)
        {
            return Mathf.Max(0.01f, level.healthCoefficient * (joint.pieceA.Config.adhesive + joint.pieceB.Config.adhesive));
        }

        private float CalculateJointLinearForceThreshold(AttachJoint joint)
        {
            var maxDefense = Mathf.Max(joint.pieceA.Config.tensileStrength, joint.pieceB.Config.tensileStrength);
            return level.thresholdCoefficient * maxDefense / (maxDefense + 50f);
        }

        private static float CalculateJointBreakTorque(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            var sizeFactor = Mathf.Max(1f, (GetVisualSize(pieceA.rect).magnitude + GetVisualSize(pieceB.rect).magnitude) * 0.5f);
            var healthFactor = Mathf.Max(1f, joint.maxHealth + joint.defense);
            return Mathf.Max(0.01f, healthFactor * sizeFactor * JointBreakTorqueScale * JointStrengthMultiplier);
        }

        private static Vector2 GetVisualSize(RectTransform rect)
        {
            var scale = rect.localScale;
            return new Vector2(rect.sizeDelta.x * Mathf.Abs(scale.x), rect.sizeDelta.y * Mathf.Abs(scale.y));
        }

        private static float GetColliderBottomUi(SimulatedPiece piece)
        {
            if (piece.collider != null)
            {
                return ToUiPosition(piece.collider.bounds.min).y;
            }

            return ToUiPosition(piece.body.position).y - GetVisualSize(piece.rect).y * 0.5f;
        }

        private float GetHighestSeabedHeightUnderPiece(SimulatedPiece piece)
        {
            var minX = 0f;
            var maxX = 0f;
            if (piece.collider != null)
            {
                var bounds = piece.collider.bounds;
                minX = ToUiPosition(bounds.min).x;
                maxX = ToUiPosition(bounds.max).x;
            }
            else
            {
                var centerX = ToUiPosition(piece.body.position).x;
                var halfWidth = GetVisualSize(piece.rect).x * 0.5f;
                minX = centerX - halfWidth;
                maxX = centerX + halfWidth;
            }

            if (maxX < minX)
            {
                var swap = minX;
                minX = maxX;
                maxX = swap;
            }

            var highest = float.MinValue;
            for (var i = 0; i < SeabedContactSampleCount; i++)
            {
                var t = SeabedContactSampleCount <= 1 ? 0.5f : i / (SeabedContactSampleCount - 1f);
                highest = Mathf.Max(highest, GetSeabedHeight(Mathf.Lerp(minX, maxX, t)));
            }

            return highest;
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
            return PiecePrefabCatalog.LoadPrefab(assetPath);
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
                var half = GetSnapshotSize(piece) * 0.5f;
                var position = GetSnapshotPosition(piece);
                min = Vector2.Min(min, position - half);
                max = Vector2.Max(max, position + half);
            }

            return (min + max) * 0.5f;
        }

        private Vector2 GetSnapshotPosition(AnchorPiece piece)
        {
            var snapshot = build.GetPieceSnapshot(piece);
            return snapshot == null ? piece.RectTransform.anchoredPosition : snapshot.anchoredPosition;
        }

        private Vector2 GetSnapshotSize(AnchorPiece piece)
        {
            var snapshot = build.GetPieceSnapshot(piece);
            return snapshot == null ? piece.RectTransform.sizeDelta : snapshot.sizeDelta;
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
                friction = level == null ? 0.82f : level.seabedPhysicsFrictionCoefficient,
                bounciness = level == null ? 0.02f : level.seabedPhysicsBouncinessCoefficient
            };

            pieceMaterial = new PhysicsMaterial2D("SimulationPiece")
            {
                friction = 0f,
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
            var nextX = bounds.xMin - SeabedLeftSpawnPadding;
            while (nextX < bounds.xMax + SeabedRightSpawnPadding)
            {
                var lastSegment = GetRightmostSeabedSegment();
                AddSeabedSegment(nextX, lastSegment == null ? (float?)null : lastSegment.end.y, null);
                nextX += SeabedSegmentLength;
            }

            RefreshSeabedCollider();
            RefreshSeabedFill();
        }

        private void UpdateSeabedTerrain(float deltaTime)
        {
            var scrollSpeed = 0f;
            var dx = scrollSpeed * deltaTime;
            seabedSampleOffsetX += dx;
            for (var i = seabedSegments.Count - 1; i >= 0; i--)
            {
                var segment = seabedSegments[i];
                segment.start.x -= dx;
                segment.end.x -= dx;

                if (segment.end.x < GetVisibleWorldBounds().xMin - SeabedLeftDespawnPadding)
                {
                    RemoveSeabedSegmentAt(i);
                    continue;
                }

                if (segment.start.x > GetVisibleWorldBounds().xMax + SeabedRightDespawnPadding)
                {
                    RemoveSeabedSegmentAt(i);
                }
            }

            EnsureSeabedCoverage();
            RefreshSeabedCollider();
            RefreshSeabedFill();
        }

        private void EnsureSeabedCoverage()
        {
            var bounds = GetVisibleWorldBounds();
            if (seabedSegments.Count == 0)
            {
                var nextX = bounds.xMin - SeabedLeftSpawnPadding;
                while (nextX < bounds.xMax + SeabedRightSpawnPadding)
                {
                    var lastSegment = GetRightmostSeabedSegment();
                    AddSeabedSegment(nextX, lastSegment == null ? (float?)null : lastSegment.end.y, null);
                    nextX += SeabedSegmentLength;
                }

                return;
            }

            var rightSegment = GetRightmostSeabedSegment();
            var rightEdge = rightSegment == null ? bounds.xMin : rightSegment.end.x;
            while (rightSegment != null && rightEdge < bounds.xMax + SeabedRightSpawnPadding)
            {
                AddSeabedSegment(rightEdge, rightSegment.end.y, null);
                rightSegment = GetRightmostSeabedSegment();
                rightEdge += SeabedSegmentLength;
            }

            var leftSegment = GetLeftmostSeabedSegment();
            var leftEdge = leftSegment == null ? bounds.xMin : leftSegment.start.x;
            while (leftSegment != null && leftEdge > bounds.xMin - SeabedLeftSpawnPadding)
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

            var segment = new SeabedSegment
            {
                start = start,
                end = end
            };

            seabedSegments.Add(segment);
        }

        private void EnsureSeabedCollider()
        {
            if (seabedCollider != null)
            {
                return;
            }

            var colliderObject = new GameObject("PhysicsSeabedSurface");
            colliderObject.transform.SetParent(physicsRoot.transform, false);
            physicsObjects.Add(colliderObject);

            seabedCollider = colliderObject.AddComponent<EdgeCollider2D>();
            seabedCollider.edgeRadius = SeabedColliderRadius;
            seabedCollider.sharedMaterial = seabedMaterial;
        }

        private void RefreshSeabedCollider()
        {
            if (seabedSegments.Count == 0)
            {
                return;
            }

            EnsureSeabedCollider();
            seabedSegments.Sort((a, b) => a.start.x.CompareTo(b.start.x));

            var points = new Vector2[seabedSegments.Count + 1];
            points[0] = ToPhysicsPosition(seabedSegments[0].start);
            for (var i = 0; i < seabedSegments.Count; i++)
            {
                points[i + 1] = ToPhysicsPosition(seabedSegments[i].end);
            }

            seabedCollider.points = points;
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
                    seabedFillPoints.Add(segment.start + new Vector2(cameraOffsetX, cameraOffsetY));
                }

                seabedFillPoints.Add(segment.end + new Vector2(cameraOffsetX, cameraOffsetY));
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
            return new Rect(-width * 0.5f - cameraOffsetX, -height * 0.5f - cameraOffsetY, width, height);
        }

        private void UpdateCameraFollow(float deltaTime)
        {
            var focusPiece = GetRopeTieSimPiece();
            var focusX = focusPiece != null ? ToUiPosition(focusPiece.body.position).x : InitialAnchorPosition.x;
            var focusY = focusPiece != null ? ToUiPosition(focusPiece.body.position).y : InitialAnchorPosition.y;
            if (focusPiece == null && simulatedPieces.Count > 0)
            {
                focusX = 0f;
                focusY = 0f;
                for (var i = 0; i < simulatedPieces.Count; i++)
                {
                    focusX += ToUiPosition(simulatedPieces[i].body.position).x;
                    focusY += ToUiPosition(simulatedPieces[i].body.position).y;
                }

                focusX /= simulatedPieces.Count;
                focusY /= simulatedPieces.Count;
            }

            var targetOffsetX = InitialAnchorPosition.x - focusX;
            var targetOffset = Mathf.Clamp(CameraAnchorTargetY - focusY, 0f, MaxCameraOffsetY);
            cameraOffsetX = targetOffsetX;
            cameraOffsetY = Mathf.Lerp(cameraOffsetY, targetOffset, deltaTime * 2.8f);
            anchor.anchoredPosition = new Vector2(cameraOffsetX, cameraOffsetY);
            ship.anchoredPosition = new Vector2(ship.anchoredPosition.x, 150f + cameraOffsetY);

            var water = FindPlayAreaChild("WaterSurface");
            if (water != null)
            {
                water.anchoredPosition = new Vector2(0f, cameraOffsetY);
            }

            UpdateDangerZonePosition();
            EnsureSeabedCoverage();
            RefreshSeabedCollider();
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
            if (seabedCollider != null)
            {
                physicsObjects.Remove(seabedCollider.gameObject);
                Destroy(seabedCollider.gameObject);
                seabedCollider = null;
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
            seabedCollider = null;
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
