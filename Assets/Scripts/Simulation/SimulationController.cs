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

        private readonly List<SimulatedPiece> simulatedPieces = new List<SimulatedPiece>();
        private readonly List<RectTransform> seabedSegments = new List<RectTransform>();
        private readonly List<GameObject> physicsObjects = new List<GameObject>();
        private readonly Dictionary<AnchorPiece, SimulatedPiece> pieceLookup = new Dictionary<AnchorPiece, SimulatedPiece>();

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
        private GameObject physicsRoot;
        private PhysicsMaterial2D seabedMaterial;
        private PhysicsMaterial2D pieceMaterial;
        private float remainingDistance;
        private float anchorDamage;
        private int brokenJoints;
        private float anchorSlowdown;
        private float simulationElapsed;
        private float shipVelocity;
        private float dangerBoundaryX;
        private float shipTravelPixelsPerMeter = 4f;
        private bool anchorOnSeabed;
        private Coroutine running;

        private const float WaterEntryDuration = 0.8f;
        private const float DangerZoneLeftAnchor = 0.88f;
        private const float AnchorScale = 0.55f;
        private const float PhysicsScale = 0.012f;
        private const float InversePhysicsScale = 1f / PhysicsScale;
        private const float InitialGravityScale = 4.2f;
        private const float MinEffectiveJointHealth = 0.01f;

        private static readonly Vector2 InitialAnchorPosition = new Vector2(-170f, 145f);

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
            anchorSlowdown = 0f;

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
            BuildPhysicsSeabed();
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

                DetectSeabedContacts();
                SyncVisualsFromPhysics();
                ApplyJointDamage(deltaTime);
                anchorSlowdown = CalculateAnchorSlowdown();
                ApplyShipCurrent(deltaTime);
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
                var currentPush = level.currentForceBase * 0.065f * Mathf.Max(0.35f, material.dragCoeff);
                var waterDrag = -piece.body.velocity * Mathf.Clamp(material.dragCoeff * 0.12f, 0.02f, 0.22f);

                piece.body.gravityScale = InitialGravityScale;
                piece.body.AddForce(new Vector2(currentPush, -downwardBias) + waterDrag, ForceMode2D.Force);
            }
        }

        private void SeabedTick()
        {
            for (var i = 0; i < simulatedPieces.Count; i++)
            {
                var piece = simulatedPieces[i];
                var material = piece.source.Config;
                var drag = piece.body.velocity * Mathf.Clamp(material.frictionCoeff * 0.35f, 0.05f, 0.75f);
                var currentPush = level.currentForceBase * 0.035f * Mathf.Max(0.25f, material.dragCoeff);

                piece.body.gravityScale = 2.4f;
                piece.body.AddForce(new Vector2(currentPush, 0f) - drag, ForceMode2D.Force);
            }
        }

        private void ApplyShipCurrent(float deltaTime)
        {
            var waterSpeed = 7f + level.currentForceBase * 0.95f;
            var shipInertiaSpeed = Mathf.Clamp(level.shipWeight * 0.0018f, 3f, 10f);
            var targetVelocity = Mathf.Max(0.8f, waterSpeed + shipInertiaSpeed - anchorSlowdown);

            shipVelocity = Mathf.Lerp(shipVelocity, targetVelocity, deltaTime * 1.8f);
            shipVelocity = Mathf.Clamp(shipVelocity, 0f, 42f);
            ship.anchoredPosition += new Vector2(shipVelocity * deltaTime * shipTravelPixelsPerMeter, 0f);

            ApplyRopeForce();
            UpdateRemainingDistanceFromShipPosition();
            progressSlider.value = 1f - remainingDistance / level.dangerZoneDistance;
        }

        private void ApplyRopeForce()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece == null)
            {
                return;
            }

            var ropeStart = GetRopeStartPosition();
            var ropeEnd = ToUiPosition(tiePiece.body.position);
            var delta = ropeStart - ropeEnd;
            var stretch = Mathf.Max(0f, delta.magnitude - 35f);
            if (stretch <= 0.01f)
            {
                return;
            }

            var force = delta.normalized * Mathf.Clamp(stretch * 0.0035f + shipVelocity * 0.035f, 0f, 2.8f);
            tiePiece.body.AddForceAtPosition(force, tiePiece.body.worldCenterOfMass, ForceMode2D.Force);
        }

        private void ApplyJointDamage(float deltaTime)
        {
            for (var i = 0; i < build.joints.Count; i++)
            {
                var joint = build.joints[i];
                if (joint.isBroken)
                {
                    continue;
                }

                if (!pieceLookup.TryGetValue(joint.pieceA, out var pieceA) ||
                    !pieceLookup.TryGetValue(joint.pieceB, out var pieceB))
                {
                    continue;
                }

                if (joint.maxHealth <= MinEffectiveJointHealth)
                {
                    BreakJoint(joint, pieceA, pieceB);
                    continue;
                }

                var attack = GetCurrentWaterAttack();
                var damagePerSecond = Mathf.Max(0f, attack - joint.defense) * joint.damageFalloff;
                joint.currentHealth = Mathf.Max(0f, joint.currentHealth - damagePerSecond * deltaTime);
                joint.currentStrength = joint.currentHealth;
                joint.damage = Mathf.Clamp01(1f - joint.currentHealth / joint.maxHealth);

                if (joint.damage > 0.72f)
                {
                    joint.jointState = JointState.Stretching;
                }

                if (joint.damage >= 1f)
                {
                    BreakJoint(joint, pieceA, pieceB);
                }
            }

            anchorDamage = CalculateAnchorDamage();
        }

        private float GetCurrentWaterAttack()
        {
            if (simulationElapsed <= WaterEntryDuration)
            {
                return level.waterEntryAttack;
            }

            if (!anchorOnSeabed)
            {
                return level.sinkWaterAttack;
            }

            return level.seabedWaterAttack;
        }

        private void BreakJoint(AttachJoint joint, SimulatedPiece pieceA, SimulatedPiece pieceB)
        {
            joint.isBroken = true;
            joint.jointState = JointState.Broken;
            brokenJoints++;

            DestroyMatchingJoint(pieceA, pieceB.body);
            DestroyMatchingJoint(pieceB, pieceA.body);
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

        private float CalculateAnchorSlowdown()
        {
            if (build == null || !build.isConnected || build.ropeTiePiece == null)
            {
                return 0f;
            }

            var connectedAnchorWeight = CalculateRopeConnectedAnchorWeight();
            var contacts = CountSeabedContacts();
            var seabedMultiplier = anchorOnSeabed
                ? 1.35f + Mathf.Clamp(build.gripScore * 0.08f + contacts * 0.05f, 0f, 0.75f)
                : 0.55f;

            var anchorHold = connectedAnchorWeight * 0.032f * seabedMultiplier;
            anchorHold *= Mathf.Clamp01(1f - anchorDamage * 0.6f);
            return Mathf.Max(0f, anchorHold);
        }

        private float CalculateRopeConnectedAnchorWeight()
        {
            if (build == null || build.ropeTiePiece == null)
            {
                return 0f;
            }

            var visited = new HashSet<AnchorPiece>();
            var queue = new Queue<AnchorPiece>();
            visited.Add(build.ropeTiePiece);
            queue.Enqueue(build.ropeTiePiece);

            while (queue.Count > 0)
            {
                var piece = queue.Dequeue();
                for (var i = 0; i < build.joints.Count; i++)
                {
                    var joint = build.joints[i];
                    if (joint.isBroken)
                    {
                        continue;
                    }

                    AnchorPiece next = null;
                    if (joint.pieceA == piece)
                    {
                        next = joint.pieceB;
                    }
                    else if (joint.pieceB == piece)
                    {
                        next = joint.pieceA;
                    }

                    if (next != null && visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            var totalWeight = 0f;
            foreach (var piece in visited)
            {
                totalWeight += piece.Config.weight;
            }

            return totalWeight;
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
            anchorOnSeabed = false;
            ConfigureDangerZoneMetrics();
            ClearAnchorVisual();
            ClearPhysicsObjects();
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

        private Vector2 GetRopeStartPosition()
        {
            return ship.anchoredPosition + new Vector2(-52f, -30f);
        }

        private Vector2 GetRopeEndPosition()
        {
            var tiePiece = GetRopeTieSimPiece();
            if (tiePiece != null)
            {
                return ToUiPosition(tiePiece.body.position);
            }

            return InitialAnchorPosition + new Vector2(0f, 35f);
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
                pieceA.joints.Add(joint);
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

        private void BuildPhysicsSeabed()
        {
            EnsurePhysicsRoot();
            var points = GetSeabedPoints();
            for (var i = 0; i < points.Length - 1; i++)
            {
                var segmentObject = new GameObject("PhysicsSeabedSegment");
                segmentObject.transform.SetParent(physicsRoot.transform, false);
                physicsObjects.Add(segmentObject);

                var collider = segmentObject.AddComponent<EdgeCollider2D>();
                collider.points = new[]
                {
                    ToPhysicsPosition(points[i]),
                    ToPhysicsPosition(points[i + 1])
                };
                collider.sharedMaterial = seabedMaterial;
            }
        }

        private void BuildUnevenSeabed()
        {
            ClearSeabedSegments();
            var points = GetSeabedPoints();
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

        private static Vector2[] GetSeabedPoints()
        {
            return new[]
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
        }

        private float GetSeabedHeight(float x)
        {
            return -151f + Mathf.Sin((x + 40f) * 0.028f) * 22f + Mathf.Sin(x * 0.071f) * 9f;
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
                    Destroy(seabedSegments[i].gameObject);
                }
            }

            seabedSegments.Clear();
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
