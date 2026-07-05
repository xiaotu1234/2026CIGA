using System;
using System.Collections.Generic;
using BrokenAnchor.Config;
using BrokenAnchor.Pieces;
using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.Build
{
    public class BuildController : MonoBehaviour
    {
        private readonly List<AnchorPiece> materialPieces = new List<AnchorPiece>();
        private readonly Dictionary<AnchorPiece, Vector2> pilePositions = new Dictionary<AnchorPiece, Vector2>();
        private readonly Dictionary<AnchorPiece, float> pileRotations = new Dictionary<AnchorPiece, float>();
        private readonly Dictionary<AnchorPiece, Vector2> pileSizes = new Dictionary<AnchorPiece, Vector2>();
        private readonly Dictionary<AnchorPiece, Vector3> pileScales = new Dictionary<AnchorPiece, Vector3>();
        private readonly List<AnchorPiece> pieces = new List<AnchorPiece>();
        private readonly List<AttachJoint> joints = new List<AttachJoint>();
        private readonly List<Image> jointLines = new List<Image>();
        private readonly List<Image> previewLines = new List<Image>();

        private const float ColliderSampleStep = 8f;
        private const int CircleSampleCount = 32;
        private const int SeparationIterations = 5;
        private const int SeparationDirectionSamples = 24;
        private const int SeparationSearchIterations = 10;
        private const float SeparationPadding = 1.5f;
        private const float SeparationIterationPaddingStep = 3f;
        private const float PostSeparationSnapRangeMultiplier = 8f;
        private const float PreviewSnapToleranceMultiplier = 2f;
        private const float PreviewMinAttachLengthMultiplier = 0.6f;
        private const float RopeMountMinCoverage = 0.35f;
        private const float RopeMountAreaBorderThickness = 3f;
        private const float MaterialPileHorizontalPadding = 48f;
        private const float MaterialPileVerticalPadding = 52f;
        private const float MaterialPileJitterRatio = 0.22f;

        private RectTransform dragSurface;
        private RectTransform workspace;
        private RectTransform connectionLayer;
        private RectTransform ropeMountPoint;
        [SerializeField] private RectTransform ropeMountAreaRect;
        [SerializeField] private Image ropeMountAreaFill;
        [SerializeField] private Image[] ropeMountAreaBorderImages = Array.Empty<Image>();
        private readonly List<Image> ropeMountAreaBorders = new List<Image>();
        private RectTransform materialPile;
        private Text riskText;
        private Text statusText;
        private Text selectedItemInfoText;
        private AttachConfig attachConfig;
        private AnchorPiece selectedPiece;
        private AnchorPiece ropeTiePiece;
        private AnchorPiece draggingPiece;
        private bool warnedMissingRopeMountArea;
        private bool warnedMissingRopeMountFill;
        private bool warnedMissingRopeMountBorders;
        private Action<AnchorBuildResult> onSubmit;

        public IReadOnlyList<AnchorPiece> Pieces => pieces;
        public IReadOnlyList<AttachJoint> Joints => joints;
        public AnchorPiece RopeTiePiece => ropeTiePiece;

        public void Initialize(
            RectTransform dragSurface,
            RectTransform workspace,
            RectTransform connectionLayer,
            RectTransform ropeMountPoint,
            RectTransform materialPile,
            Text riskText,
            Text statusText,
            Text selectedItemInfoText,
            AttachConfig attachConfig,
            Action<AnchorBuildResult> onSubmit)
        {
            this.dragSurface = dragSurface;
            this.workspace = workspace;
            this.connectionLayer = connectionLayer;
            this.ropeMountPoint = ropeMountPoint;
            this.materialPile = materialPile;
            this.riskText = riskText;
            this.statusText = statusText;
            this.selectedItemInfoText = selectedItemInfoText;
            this.attachConfig = attachConfig;
            this.onSubmit = onSubmit;
            ResolveRopeMountAreaReferences();
            UpdateRopeMountVisual();
        }

        public void PopulateMaterialPile(IReadOnlyList<MaterialConfig> materials)
        {
            ClearGeneratedPieces();
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                var piece = CreatePieceInstance(material);
                piece.Initialize(material, this, dragSurface);
                var pileRotation = GetRandomPileRotation();
                piece.RectTransform.anchoredPosition = GetPilePosition(i, materials.Count);
                piece.RectTransform.localRotation = Quaternion.Euler(0f, 0f, pileRotation);
                piece.RectTransform.SetAsLastSibling();

                materialPieces.Add(piece);
                pilePositions[piece] = piece.RectTransform.anchoredPosition;
                pileRotations[piece] = pileRotation;
                pileSizes[piece] = piece.RectTransform.sizeDelta;
                pileScales[piece] = piece.RectTransform.localScale;
            }

            UpdateRisks();
            UpdateStatus();
            UpdateSelectedItemInfo();
        }

        public void RefreshPiecePlacement(AnchorPiece piece)
        {
            RefreshPieceRegistration(piece);
            RefreshConnections();
        }

        public void BeginPieceDrag(AnchorPiece piece)
        {
            draggingPiece = piece;
            SelectPiece(piece);
            RefreshConnections();
            DrawPreviewLines(piece);
            UpdateStatus();
        }

        public void DragPiece(AnchorPiece piece)
        {
            RefreshPieceRegistration(piece);
            RefreshConnections();
            DrawPreviewLines(piece);
        }

        public void EndPieceDrag(AnchorPiece piece)
        {
            ClearPreviewLines();
            RefreshPieceRegistration(piece);
            if (pieces.Contains(piece))
            {
                ResolveDroppedPiece(piece);
            }

            draggingPiece = null;
            Physics2D.SyncTransforms();
            RefreshConnections();
        }

        private void RefreshPieceRegistration(AnchorPiece piece)
        {
            var isInWorkspace = IsPieceInWorkspace(piece);
            var isRegistered = pieces.Contains(piece);
            if (isInWorkspace && !isRegistered)
            {
                pieces.Add(piece);
            }
            else if (!isInWorkspace && isRegistered)
            {
                pieces.Remove(piece);
                if (ropeTiePiece == piece)
                {
                    ropeTiePiece = null;
                }
            }

            RefreshRopeTiePiece();
        }

        private AnchorPiece CreatePieceInstance(MaterialConfig config)
        {
            GameObject go = null;
            var prefab = LoadPiecePrefab(config.prefabAssetPath);
            if (prefab != null)
            {
                go = Instantiate(prefab, dragSurface, false);
                if (go.GetComponent<RectTransform>() == null)
                {
                    Destroy(go);
                    go = null;
                }
            }

            if (go == null)
            {
                go = new GameObject(config.id, typeof(RectTransform));
                go.transform.SetParent(dragSurface, false);
            }

            go.name = config.id;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var piece = go.GetComponent<AnchorPiece>();
            if (piece == null)
            {
                piece = go.AddComponent<AnchorPiece>();
            }

            return piece;
        }

        private static GameObject LoadPiecePrefab(string assetPath)
        {
            return PiecePrefabCatalog.LoadPrefab(assetPath);
        }

        public void SelectPiece(AnchorPiece piece)
        {
            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(false);
            }

            selectedPiece = piece;
            if (selectedPiece != null)
            {
                selectedPiece.SetSelected(true);
            }

            UpdateStatus();
            UpdateSelectedItemInfo();
        }

        public void RotateSelected()
        {
            if (selectedPiece != null)
            {
                selectedPiece.RotateClockwise();
            }
        }

        public void FlipSelected()
        {
            if (selectedPiece != null)
            {
                selectedPiece.Flip();
            }
        }

        public void ClearBuild()
        {
            for (var i = 0; i < materialPieces.Count; i++)
            {
                var piece = materialPieces[i];
                if (piece != null && pilePositions.TryGetValue(piece, out var pilePosition))
                {
                    RestorePieceToPileTransform(piece, pilePosition);
                    piece.SetSelected(false);
                }
            }

            pieces.Clear();
            joints.Clear();
            ropeTiePiece = null;
            selectedPiece = null;
            draggingPiece = null;
            ClearJointLines();
            ClearPreviewLines();
            UpdateRopeMountVisual();
            UpdateRisks();
            UpdateStatus();
            UpdateSelectedItemInfo();
        }

        public void RefreshConnections()
        {
            ClearPreviewLines();
            joints.Clear();
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == draggingPiece)
                {
                    continue;
                }

                for (var j = i + 1; j < pieces.Count; j++)
                {
                    if (pieces[j] == draggingPiece)
                    {
                        continue;
                    }

                    var attachLength = GetAttachLength(pieces[i], pieces[j]);
                    if (attachLength >= attachConfig.minAttachLength)
                    {
                        joints.Add(new AttachJoint(pieces[i], pieces[j], attachLength));
                    }
                }
            }

            DrawJointLines();
            RefreshRopeTiePiece();
            UpdateRisks();
            UpdateStatus();
        }

        public void Submit()
        {
            RefreshConnections();
            var result = BuildResult();
            onSubmit?.Invoke(result);
        }

        public AnchorBuildResult BuildResult()
        {
            var result = new AnchorBuildResult();
            result.ropeTiePiece = ropeTiePiece;
            result.isConnected = pieces.Count > 0 && AttachGraph.IsFullyConnected(pieces, joints);
            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                result.pieces.Add(piece);
                result.pieceSnapshots.Add(new AnchorBuildResult.PieceSnapshot
                {
                    source = piece,
                    anchoredPosition = piece.RectTransform.anchoredPosition,
                    sizeDelta = piece.RectTransform.sizeDelta,
                    localRotation = piece.RectTransform.localRotation,
                    localScale = piece.RectTransform.localScale
                });
                result.totalWeight += piece.Config.weight;
                result.gripScore += piece.Config.gripCoeff;
                result.dragScore += piece.Config.dragCoeff;
            }

            for (var i = 0; i < joints.Count; i++)
            {
                result.joints.Add(joints[i]);
            }

            result.risks.AddRange(BuildRiskEvaluator.Evaluate(pieces, joints, ropeTiePiece));
            return result;
        }

        private float GetAttachLength(AnchorPiece a, AnchorPiece b)
        {
            var colliderA = a.ShapeCollider;
            var colliderB = b.ShapeCollider;
            if (colliderA != null && colliderB != null)
            {
                var distance = colliderA.Distance(colliderB);
                if (distance.isValid && (distance.isOverlapped || distance.distance <= attachConfig.snapTolerance))
                {
                    return EstimateColliderAttachLength(colliderA, colliderB, attachConfig.snapTolerance);
                }

                return 0f;
            }

            return GetRectAttachLength(a, b, attachConfig.snapTolerance, attachConfig.minAttachLength);
        }

        private float EstimateColliderAttachLength(Collider2D colliderA, Collider2D colliderB, float proximityTolerance)
        {
            var outlineA = new List<Vector2>();
            var outlineB = new List<Vector2>();
            BuildColliderOutline(colliderA, outlineA);
            BuildColliderOutline(colliderB, outlineB);

            if (outlineA.Count < 2 || outlineB.Count < 2)
            {
                return GetBoundsAttachLength(colliderA.bounds, colliderB.bounds);
            }

            var lengthFromA = EstimateOutlineProximityLength(outlineA, colliderB, IsClosedCollider(colliderA), proximityTolerance);
            var lengthFromB = EstimateOutlineProximityLength(outlineB, colliderA, IsClosedCollider(colliderB), proximityTolerance);
            return Mathf.Max(lengthFromA, lengthFromB);
        }

        private void BuildColliderOutline(Collider2D source, List<Vector2> outline)
        {
            if (source is BoxCollider2D box)
            {
                var half = box.size * 0.5f;
                AddWorldPoint(outline, box.transform, box.offset + new Vector2(-half.x, -half.y));
                AddWorldPoint(outline, box.transform, box.offset + new Vector2(-half.x, half.y));
                AddWorldPoint(outline, box.transform, box.offset + new Vector2(half.x, half.y));
                AddWorldPoint(outline, box.transform, box.offset + new Vector2(half.x, -half.y));
                return;
            }

            if (source is CircleCollider2D circle)
            {
                for (var i = 0; i < CircleSampleCount; i++)
                {
                    var angle = i * Mathf.PI * 2f / CircleSampleCount;
                    var local = circle.offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circle.radius;
                    AddWorldPoint(outline, circle.transform, local);
                }

                return;
            }

            if (source is PolygonCollider2D polygon)
            {
                for (var path = 0; path < polygon.pathCount; path++)
                {
                    var points = polygon.GetPath(path);
                    for (var i = 0; i < points.Length; i++)
                    {
                        AddWorldPoint(outline, polygon.transform, polygon.offset + points[i]);
                    }
                }

                return;
            }

            if (source is EdgeCollider2D edge)
            {
                for (var i = 0; i < edge.points.Length; i++)
                {
                    AddWorldPoint(outline, edge.transform, edge.offset + edge.points[i]);
                }
            }
        }

        private static void AddWorldPoint(List<Vector2> points, Transform transform, Vector2 localPoint)
        {
            points.Add(transform.TransformPoint(localPoint));
        }

        private float EstimateOutlineProximityLength(List<Vector2> outline, Collider2D target, bool closed, float proximityTolerance)
        {
            var total = 0f;
            var segmentCount = closed ? outline.Count : outline.Count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var start = outline[i];
                var end = outline[(i + 1) % outline.Count];
                total += EstimateSegmentProximityLength(start, end, target, proximityTolerance);
            }

            return total;
        }

        private float EstimateSegmentProximityLength(Vector2 start, Vector2 end, Collider2D target, float proximityTolerance)
        {
            var segmentLength = Vector2.Distance(start, end);
            if (segmentLength <= 0f)
            {
                return 0f;
            }

            var steps = Mathf.Max(1, Mathf.CeilToInt(segmentLength / ColliderSampleStep));
            var attachedSteps = 0;
            for (var i = 0; i < steps; i++)
            {
                var t = (i + 0.5f) / steps;
                var samplePoint = Vector2.Lerp(start, end, t);
                var closest = target.ClosestPoint(samplePoint);
                if (Vector2.Distance(samplePoint, closest) <= proximityTolerance)
                {
                    attachedSteps++;
                }
            }

            return segmentLength * attachedSteps / steps;
        }

        private static bool IsClosedCollider(Collider2D source)
        {
            return !(source is EdgeCollider2D);
        }

        private float GetBoundsAttachLength(Bounds boundsA, Bounds boundsB)
        {
            var horizontalOverlap = Mathf.Min(boundsA.max.x, boundsB.max.x) - Mathf.Max(boundsA.min.x, boundsB.min.x);
            var verticalOverlap = Mathf.Min(boundsA.max.y, boundsB.max.y) - Mathf.Max(boundsA.min.y, boundsB.min.y);
            return Mathf.Max(0f, horizontalOverlap, verticalOverlap);
        }

        private float GetRectAttachLength(AnchorPiece a, AnchorPiece b, float snapTolerance, float minAttachLength)
        {
            var rectA = GetLocalRect(a);
            var rectB = GetLocalRect(b);
            var horizontalOverlap = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
            var verticalOverlap = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);

            var leftToRight = Mathf.Abs(rectA.xMax - rectB.xMin);
            var rightToLeft = Mathf.Abs(rectB.xMax - rectA.xMin);
            var topToBottom = Mathf.Abs(rectA.yMax - rectB.yMin);
            var bottomToTop = Mathf.Abs(rectB.yMax - rectA.yMin);

            var attachLength = 0f;
            if (horizontalOverlap > 0f && verticalOverlap > 0f)
            {
                var overlapAttachLength = Mathf.Max(horizontalOverlap, verticalOverlap);
                attachLength = Mathf.Max(attachLength, overlapAttachLength, minAttachLength);
            }

            if ((leftToRight <= snapTolerance || rightToLeft <= snapTolerance) && verticalOverlap > 0f)
            {
                attachLength = Mathf.Max(attachLength, verticalOverlap);
            }

            if ((topToBottom <= snapTolerance || bottomToTop <= snapTolerance) && horizontalOverlap > 0f)
            {
                attachLength = Mathf.Max(attachLength, horizontalOverlap);
            }

            return attachLength;
        }

        private Rect GetLocalRect(AnchorPiece piece)
        {
            var corners = new Vector3[4];
            piece.RectTransform.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < corners.Length; i++)
            {
                var local = dragSurface.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void DrawJointLines()
        {
            ClearJointLines();
            for (var i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                var image = CreateConnectionLine("JointLine", joint.pieceA, joint.pieceB, new Color(0.37f, 0.94f, 0.82f, 0.9f), 5f);
                jointLines.Add(image);
            }
        }

        private void DrawPreviewLines(AnchorPiece piece)
        {
            ClearPreviewLines();
            if (piece == null || !pieces.Contains(piece))
            {
                return;
            }

            for (var i = 0; i < pieces.Count; i++)
            {
                var other = pieces[i];
                if (other == null || other == piece)
                {
                    continue;
                }

                if (IsPreviewAttachable(piece, other))
                {
                    var image = CreateConnectionLine("PreviewJointLine", piece, other, new Color(1f, 0.86f, 0.28f, 0.95f), 4f);
                    previewLines.Add(image);
                }
            }
        }

        private Image CreateConnectionLine(string name, AnchorPiece a, AnchorPiece b, Color color, float thickness)
        {
            var lineGo = new GameObject(name, typeof(RectTransform));
            lineGo.transform.SetParent(connectionLayer, false);
            var image = lineGo.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            GetConnectionLineEndpoints(a, b, out var start, out var end);
            var delta = end - start;
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            return image;
        }

        private void GetConnectionLineEndpoints(AnchorPiece a, AnchorPiece b, out Vector2 start, out Vector2 end)
        {
            var colliderA = a.ShapeCollider;
            var colliderB = b.ShapeCollider;
            if (colliderA != null && colliderB != null)
            {
                var distance = colliderA.Distance(colliderB);
                if (distance.isValid)
                {
                    start = connectionLayer.InverseTransformPoint(distance.pointA);
                    end = connectionLayer.InverseTransformPoint(distance.pointB);
                    if ((end - start).sqrMagnitude > 0.001f)
                    {
                        return;
                    }

                    var direction = GetPieceWorldCenter(b) - GetPieceWorldCenter(a);
                    if (direction.sqrMagnitude < 0.001f)
                    {
                        direction = Vector2.right;
                    }

                    direction.Normalize();
                    var center = (Vector2)distance.pointA;
                    start = connectionLayer.InverseTransformPoint(center - direction * 4f);
                    end = connectionLayer.InverseTransformPoint(center + direction * 4f);
                    return;
                }
            }

            GetRectConnectionEndpoints(a, b, out start, out end);
        }

        private void GetRectConnectionEndpoints(AnchorPiece a, AnchorPiece b, out Vector2 start, out Vector2 end)
        {
            var rectA = GetLocalRect(a);
            var rectB = GetLocalRect(b);
            var centerA = rectA.center;
            var centerB = rectB.center;
            var horizontalOverlap = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
            var verticalOverlap = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);
            var gapX = centerB.x >= centerA.x ? rectB.xMin - rectA.xMax : rectA.xMin - rectB.xMax;
            var gapY = centerB.y >= centerA.y ? rectB.yMin - rectA.yMax : rectA.yMin - rectB.yMax;

            if (verticalOverlap > 0f && (horizontalOverlap <= 0f || Mathf.Abs(gapX) <= Mathf.Abs(gapY)))
            {
                var y = Mathf.Clamp(centerB.y, rectA.yMin, rectA.yMax);
                var targetY = Mathf.Clamp(y, rectB.yMin, rectB.yMax);
                start = new Vector2(centerB.x >= centerA.x ? rectA.xMax : rectA.xMin, y);
                end = new Vector2(centerB.x >= centerA.x ? rectB.xMin : rectB.xMax, targetY);
                return;
            }

            if (horizontalOverlap > 0f)
            {
                var x = Mathf.Clamp(centerB.x, rectA.xMin, rectA.xMax);
                var targetX = Mathf.Clamp(x, rectB.xMin, rectB.xMax);
                start = new Vector2(x, centerB.y >= centerA.y ? rectA.yMax : rectA.yMin);
                end = new Vector2(targetX, centerB.y >= centerA.y ? rectB.yMin : rectB.yMax);
                return;
            }

            start = GetConnectionLayerPosition(a);
            end = GetConnectionLayerPosition(b);
        }

        private void ClearJointLines()
        {
            for (var i = jointLines.Count - 1; i >= 0; i--)
            {
                if (jointLines[i] != null)
                {
                    Destroy(jointLines[i].gameObject);
                }
            }

            jointLines.Clear();
        }

        private void ClearPreviewLines()
        {
            for (var i = previewLines.Count - 1; i >= 0; i--)
            {
                if (previewLines[i] != null)
                {
                    Destroy(previewLines[i].gameObject);
                }
            }

            previewLines.Clear();
        }

        private void UpdateRisks()
        {
            if (riskText == null)
            {
                return;
            }

            RefreshRopeTiePiece();
            var risks = BuildRiskEvaluator.Evaluate(pieces, joints, ropeTiePiece);
            riskText.text = string.Join("\n", risks.ToArray());
        }

        private void UpdateStatus()
        {
            if (statusText == null)
            {
                return;
            }

            RefreshRopeTiePiece();
            var selectedName = selectedPiece == null ? "未选择" : selectedPiece.Config.displayName.Replace("\n", " / ");
            var tieName = ropeTiePiece == null ? "未覆盖" : ropeTiePiece.Config.displayName.Replace("\n", " / ");
            statusText.text = $"拼装区 {pieces.Count}/{materialPieces.Count}  连接 {joints.Count}  选中 {selectedName}  挂点 {tieName}";
        }

        private void UpdateSelectedItemInfo()
        {
            if (selectedItemInfoText == null)
            {
                return;
            }

            if (selectedPiece == null)
            {
                selectedItemInfoText.text = "选中道具\n未选择";
                return;
            }

            var config = selectedPiece.Config;
            var displayName = string.IsNullOrEmpty(config.displayName) ? config.id : config.displayName.Replace("\n", " / ");
            var materialName = string.IsNullOrEmpty(config.materialName) ? "未配置" : config.materialName;
            selectedItemInfoText.text = $"选中道具\n名称：{displayName}\n材质：{materialName}\n重量：{config.weight:0.######} kg";
        }

        private void RefreshRopeTiePiece()
        {
            var mountedPiece = FindMountedPiece();
            if (ropeTiePiece == mountedPiece)
            {
                return;
            }

            ropeTiePiece = mountedPiece;
            UpdateRopeMountVisual();
        }

        private AnchorPiece FindMountedPiece()
        {
            if (ropeMountPoint == null)
            {
                return null;
            }

            var mountRect = GetRopeMountRect();
            var mountArea = Mathf.Max(1f, mountRect.width * mountRect.height);
            AnchorPiece bestPiece = null;
            var bestCoverage = 0f;
            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece == null)
                {
                    continue;
                }

                var coverage = GetRectOverlapArea(GetLocalRect(piece), mountRect) / mountArea;
                if (coverage > bestCoverage)
                {
                    bestCoverage = coverage;
                    bestPiece = piece;
                }
            }

            return bestCoverage >= RopeMountMinCoverage ? bestPiece : null;
        }

        private Rect GetRopeMountRect()
        {
            if (ropeMountAreaRect != null)
            {
                return GetRectTransformLocalRect(ropeMountAreaRect);
            }

            return GetRectTransformLocalRect(ropeMountPoint);
        }

        private Rect GetRectTransformLocalRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < corners.Length; i++)
            {
                var local = (Vector2)dragSurface.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static float GetRectOverlapArea(Rect a, Rect b)
        {
            var width = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            var height = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            if (width <= 0f || height <= 0f)
            {
                return 0f;
            }

            return width * height;
        }

        private void UpdateRopeMountVisual()
        {
            if (ropeMountPoint == null)
            {
                return;
            }

            var image = ropeMountPoint.GetComponent<Image>();
            if (image != null)
            {
                image.color = ropeTiePiece == null
                    ? new Color(0.92f, 0.68f, 0.28f, 0.75f)
                    : new Color(0.45f, 0.98f, 0.72f, 0.85f);
            }

            ResolveRopeMountAreaReferences();
        }

        private void ResolveRopeMountAreaReferences()
        {
            ropeMountAreaBorders.Clear();
            if (ropeMountAreaRect == null)
            {
                LogMissingPrefabReference("BuildController prefab is missing RopeMountDetectionArea. Rope mount area visual will be hidden.", ref warnedMissingRopeMountArea);
                return;
            }

            if (ropeMountAreaFill == null)
            {
                LogMissingPrefabReference("BuildController prefab is missing RopeMountDetectionArea Image. Rope mount area fill will be hidden.", ref warnedMissingRopeMountFill);
            }
            else
            {
                ropeMountAreaFill.enabled = false;
                ropeMountAreaFill.raycastTarget = false;
            }

            if (ropeMountAreaBorderImages != null)
            {
                for (var i = 0; i < ropeMountAreaBorderImages.Length; i++)
                {
                    var image = ropeMountAreaBorderImages[i];
                    if (image == null)
                    {
                        continue;
                    }

                    image.enabled = false;
                    image.raycastTarget = false;
                    ropeMountAreaBorders.Add(image);
                }
            }

            if (ropeMountAreaBorders.Count == 0)
            {
                LogMissingPrefabReference("BuildController prefab is missing rope mount border Images. Rope mount area border will be hidden.", ref warnedMissingRopeMountBorders);
            }

            ropeMountAreaRect.SetAsFirstSibling();
        }

        private static void LogMissingPrefabReference(string message, ref bool alreadyWarned)
        {
            if (alreadyWarned)
            {
                return;
            }

            alreadyWarned = true;
            Debug.LogWarning(message);
        }

        private Vector2 GetPilePosition(int index, int totalCount)
        {
            if (materialPile == null || dragSurface == null)
            {
                return Vector2.zero;
            }

            var rect = materialPile.rect;
            var horizontalPadding = Mathf.Min(MaterialPileHorizontalPadding, rect.width * 0.45f);
            var verticalPadding = Mathf.Min(MaterialPileVerticalPadding, rect.height * 0.45f);
            var usableWidth = Mathf.Max(1f, rect.width - horizontalPadding * 2f);
            var usableHeight = Mathf.Max(1f, rect.height - verticalPadding * 2f);
            var count = Mathf.Max(1, totalCount);
            var aspect = Mathf.Max(0.35f, usableWidth / Mathf.Max(1f, usableHeight));
            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * aspect)));
            var rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            var column = index % columns;
            var row = index / columns;
            var cellWidth = usableWidth / columns;
            var cellHeight = usableHeight / rows;
            var x = rect.xMin + horizontalPadding + cellWidth * (column + 0.5f);
            var y = rect.yMax - verticalPadding - cellHeight * (row + 0.5f);
            var jitter = GetDeterministicPileJitter(index);
            x += jitter.x * cellWidth * MaterialPileJitterRatio;
            y += jitter.y * cellHeight * MaterialPileJitterRatio;

            var localPoint = new Vector2(
                Mathf.Clamp(x, rect.xMin + horizontalPadding, rect.xMax - horizontalPadding),
                Mathf.Clamp(y, rect.yMin + verticalPadding, rect.yMax - verticalPadding));
            var worldPoint = materialPile.TransformPoint(localPoint);
            return dragSurface.InverseTransformPoint(worldPoint);
        }

        private static Vector2 GetDeterministicPileJitter(int index)
        {
            var seed = (uint)(index + 1) * 747796405u + 2891336453u;
            seed = (seed >> ((int)(seed >> 28) + 4)) ^ seed;
            var x = ((seed & 1023u) / 1023f) * 2f - 1f;
            seed = seed * 277803737u + 3812015801u;
            seed = (seed >> ((int)(seed >> 28) + 4)) ^ seed;
            var y = ((seed & 1023u) / 1023f) * 2f - 1f;
            return new Vector2(x, y);
        }

        private static float GetRandomPileRotation()
        {
            return UnityEngine.Random.Range(-180f, 180f);
        }

        private float GetPileRotation(AnchorPiece piece)
        {
            return pileRotations.TryGetValue(piece, out var rotation) ? rotation : 0f;
        }

        private bool IsPieceInWorkspace(AnchorPiece piece)
        {
            if (piece == null || workspace == null)
            {
                return false;
            }

            var worldCenter = piece.RectTransform.TransformPoint(piece.RectTransform.rect.center);
            var localCenter = workspace.InverseTransformPoint(worldCenter);
            return workspace.rect.Contains(localCenter);
        }

        private Vector2 GetConnectionLayerPosition(AnchorPiece piece)
        {
            var worldCenter = piece.RectTransform.TransformPoint(piece.RectTransform.rect.center);
            return connectionLayer.InverseTransformPoint(worldCenter);
        }

        private void ResolveDroppedPiece(AnchorPiece piece)
        {
            var previewTargets = GetPreviewLinkTargets(piece);
            if (!SeparateFromPlacedPieces(piece, null))
            {
                ReturnPieceToMaterialPile(piece);
                return;
            }

            Physics2D.SyncTransforms();

            if (previewTargets.Count > 0)
            {
                SnapToPreviewLink(piece, previewTargets);
                if (!SeparateFromPlacedPieces(piece, null))
                {
                    ReturnPieceToMaterialPile(piece);
                }

                return;
            }
        }

        private List<AnchorPiece> GetPreviewLinkTargets(AnchorPiece piece)
        {
            var targets = new List<AnchorPiece>();
            for (var i = 0; i < pieces.Count; i++)
            {
                var other = pieces[i];
                if (other == null || other == piece)
                {
                    continue;
                }

                if (IsPreviewAttachable(piece, other))
                {
                    targets.Add(other);
                }
            }

            return targets;
        }

        private bool IsPreviewAttachable(AnchorPiece a, AnchorPiece b)
        {
            var previewTolerance = attachConfig.snapTolerance * PreviewSnapToleranceMultiplier;
            var previewMinLength = attachConfig.minAttachLength * PreviewMinAttachLengthMultiplier;
            var colliderA = a.ShapeCollider;
            var colliderB = b.ShapeCollider;
            if (colliderA != null && colliderB != null)
            {
                var distance = colliderA.Distance(colliderB);
                if (!distance.isValid || (!distance.isOverlapped && distance.distance > previewTolerance))
                {
                    return false;
                }

                return EstimateColliderAttachLength(colliderA, colliderB, previewTolerance) >= previewMinLength;
            }

            return GetRectAttachLength(a, b, previewTolerance, previewMinLength) >= previewMinLength;
        }

        private bool SeparateFromPlacedPieces(AnchorPiece piece, IReadOnlyList<AnchorPiece> ignoredPieces)
        {
            for (var iteration = 0; iteration < SeparationIterations; iteration++)
            {
                Physics2D.SyncTransforms();
                var moved = false;
                for (var i = 0; i < pieces.Count; i++)
                {
                    var other = pieces[i];
                    if (other == null || other == piece)
                    {
                        continue;
                    }

                    if (ContainsPiece(ignoredPieces, other))
                    {
                        continue;
                    }

                    if (TryGetSeparationDelta(piece, other, out var delta))
                    {
                        MovePieceInWorkspace(piece, AddIterationSeparationPadding(delta, iteration));
                        Physics2D.SyncTransforms();
                        moved = true;
                    }
                }

                if (!moved)
                {
                    return true;
                }
            }

            Physics2D.SyncTransforms();
            return !HasOverlapWithPlacedPieces(piece, ignoredPieces);
        }

        private bool HasOverlapWithPlacedPieces(AnchorPiece piece, IReadOnlyList<AnchorPiece> ignoredPieces)
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                var other = pieces[i];
                if (other == null || other == piece || ContainsPiece(ignoredPieces, other))
                {
                    continue;
                }

                if (TryGetSeparationDelta(piece, other, out _))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReturnPieceToMaterialPile(AnchorPiece piece)
        {
            if (piece == null)
            {
                return;
            }

            pieces.Remove(piece);
            if (ropeTiePiece == piece)
            {
                ropeTiePiece = null;
                UpdateRopeMountVisual();
            }

            if (selectedPiece == piece)
            {
                selectedPiece.SetSelected(false);
                selectedPiece = null;
                UpdateSelectedItemInfo();
            }

            if (pilePositions.TryGetValue(piece, out var pilePosition))
            {
                RestorePieceToPileTransform(piece, pilePosition);
            }
            Physics2D.SyncTransforms();
        }

        private void RestorePieceToPileTransform(AnchorPiece piece, Vector2 pilePosition)
        {
            var rect = piece.RectTransform;
            rect.anchoredPosition = pilePosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, GetPileRotation(piece));
            rect.sizeDelta = pileSizes.TryGetValue(piece, out var size) ? size : rect.sizeDelta;
            rect.localScale = pileScales.TryGetValue(piece, out var scale) ? scale : Vector3.one;
        }

        private static Vector2 AddIterationSeparationPadding(Vector2 delta, int iteration)
        {
            if (delta.sqrMagnitude < 0.001f)
            {
                return delta;
            }

            return delta + delta.normalized * (SeparationIterationPaddingStep * iteration);
        }

        private static bool ContainsPiece(IReadOnlyList<AnchorPiece> piecesToCheck, AnchorPiece piece)
        {
            if (piecesToCheck == null)
            {
                return false;
            }

            for (var i = 0; i < piecesToCheck.Count; i++)
            {
                if (piecesToCheck[i] == piece)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSeparationDelta(AnchorPiece moving, AnchorPiece fixedPiece, out Vector2 delta)
        {
            var movingCollider = moving.ShapeCollider;
            var fixedCollider = fixedPiece.ShapeCollider;
            if (movingCollider != null && fixedCollider != null)
            {
                return TryGetColliderSeparationDelta(moving, fixedPiece, out delta);
            }

            return TryGetRectSeparationDelta(moving, fixedPiece, out delta);
        }

        private bool TryGetColliderSeparationDelta(AnchorPiece moving, AnchorPiece fixedPiece, out Vector2 delta)
        {
            delta = Vector2.zero;
            var movingCollider = moving.ShapeCollider;
            var fixedCollider = fixedPiece.ShapeCollider;
            if (movingCollider == null || fixedCollider == null)
            {
                return false;
            }

            var distance = movingCollider.Distance(fixedCollider);
            if (!distance.isValid || !distance.isOverlapped)
            {
                return false;
            }

            return TryFindPreciseColliderSeparationDelta(moving, fixedPiece, distance, out delta);
        }

        private bool TryFindPreciseColliderSeparationDelta(
            AnchorPiece moving,
            AnchorPiece fixedPiece,
            ColliderDistance2D initialDistance,
            out Vector2 delta)
        {
            delta = Vector2.zero;
            var directions = new List<Vector2>();
            AddUniqueDirection(directions, GetPushDirection(moving, fixedPiece, initialDistance.normal));
            AddUniqueDirection(directions, GetPieceWorldCenter(moving) - GetPieceWorldCenter(fixedPiece));

            for (var i = 0; i < SeparationDirectionSamples; i++)
            {
                var angle = Mathf.PI * 2f * i / SeparationDirectionSamples;
                AddUniqueDirection(directions, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            }

            var originalPosition = moving.RectTransform.anchoredPosition;
            var bestTravel = float.MaxValue;
            var bestWorldDelta = Vector2.zero;
            var searchLimit = GetSeparationSearchLimit(moving, fixedPiece);
            for (var i = 0; i < directions.Count; i++)
            {
                if (TryFindClearanceAlongDirection(moving, fixedPiece, directions[i], originalPosition, searchLimit, out var travel))
                {
                    if (travel < bestTravel)
                    {
                        bestTravel = travel;
                        bestWorldDelta = directions[i] * (travel + SeparationPadding);
                    }
                }
            }

            moving.RectTransform.anchoredPosition = originalPosition;
            Physics2D.SyncTransforms();

            if (bestTravel >= float.MaxValue)
            {
                return false;
            }

            delta = dragSurface.InverseTransformVector(bestWorldDelta);
            return delta.sqrMagnitude > 0.001f;
        }

        private bool TryFindClearanceAlongDirection(
            AnchorPiece moving,
            AnchorPiece fixedPiece,
            Vector2 worldDirection,
            Vector2 originalPosition,
            float searchLimit,
            out float travel)
        {
            travel = 0f;
            var high = 1f;
            while (high < searchLimit && IsOverlappedAtTravel(moving, fixedPiece, worldDirection, originalPosition, high))
            {
                high *= 2f;
            }

            if (high >= searchLimit && IsOverlappedAtTravel(moving, fixedPiece, worldDirection, originalPosition, searchLimit))
            {
                return false;
            }

            var low = 0f;
            high = Mathf.Min(high, searchLimit);
            for (var i = 0; i < SeparationSearchIterations; i++)
            {
                var mid = (low + high) * 0.5f;
                if (IsOverlappedAtTravel(moving, fixedPiece, worldDirection, originalPosition, mid))
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            travel = high;
            return true;
        }

        private bool IsOverlappedAtTravel(
            AnchorPiece moving,
            AnchorPiece fixedPiece,
            Vector2 worldDirection,
            Vector2 originalPosition,
            float travel)
        {
            moving.RectTransform.anchoredPosition = originalPosition + (Vector2)dragSurface.InverseTransformVector(worldDirection * travel);
            Physics2D.SyncTransforms();
            var movingCollider = moving.ShapeCollider;
            var fixedCollider = fixedPiece.ShapeCollider;
            if (movingCollider == null || fixedCollider == null)
            {
                return false;
            }

            var distance = movingCollider.Distance(fixedCollider);
            return distance.isValid && distance.isOverlapped;
        }

        private float GetSeparationSearchLimit(AnchorPiece moving, AnchorPiece fixedPiece)
        {
            var movingSize = moving.ShapeCollider == null ? moving.RectTransform.sizeDelta.magnitude : moving.ShapeCollider.bounds.size.magnitude;
            var fixedSize = fixedPiece.ShapeCollider == null ? fixedPiece.RectTransform.sizeDelta.magnitude : fixedPiece.ShapeCollider.bounds.size.magnitude;
            return Mathf.Max(64f, movingSize + fixedSize + attachConfig.snapTolerance);
        }

        private static void AddUniqueDirection(List<Vector2> directions, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            direction.Normalize();
            for (var i = 0; i < directions.Count; i++)
            {
                if (Vector2.Dot(directions[i], direction) > 0.995f)
                {
                    return;
                }
            }

            directions.Add(direction);
        }

        private bool TryGetRectSeparationDelta(AnchorPiece moving, AnchorPiece fixedPiece, out Vector2 delta)
        {
            delta = Vector2.zero;
            var movingRect = GetLocalRect(moving);
            var fixedRect = GetLocalRect(fixedPiece);
            if (!movingRect.Overlaps(fixedRect))
            {
                return false;
            }

            var moveRight = fixedRect.xMax - movingRect.xMin;
            var moveLeft = fixedRect.xMin - movingRect.xMax;
            var moveUp = fixedRect.yMax - movingRect.yMin;
            var moveDown = fixedRect.yMin - movingRect.yMax;
            delta = Mathf.Min(Mathf.Abs(moveRight), Mathf.Abs(moveLeft)) < Mathf.Min(Mathf.Abs(moveUp), Mathf.Abs(moveDown))
                ? new Vector2(Mathf.Abs(moveRight) < Mathf.Abs(moveLeft) ? moveRight : moveLeft, 0f)
                : new Vector2(0f, Mathf.Abs(moveUp) < Mathf.Abs(moveDown) ? moveUp : moveDown);
            delta += delta.normalized * SeparationPadding;
            return delta.sqrMagnitude > 0.001f;
        }

        private Vector2 GetPushDirection(AnchorPiece moving, AnchorPiece fixedPiece, Vector2 collisionNormal)
        {
            var centerDirection = GetPieceWorldCenter(moving) - GetPieceWorldCenter(fixedPiece);
            if (centerDirection.sqrMagnitude < 0.001f)
            {
                centerDirection = Vector2.right;
            }

            if (collisionNormal.sqrMagnitude < 0.001f)
            {
                return centerDirection.normalized;
            }

            return Vector2.Dot(collisionNormal, centerDirection) >= 0f ? collisionNormal.normalized : -collisionNormal.normalized;
        }

        private void SnapToPreviewLink(AnchorPiece piece, IReadOnlyList<AnchorPiece> previewTargets)
        {
            Vector2 bestPosition = piece.RectTransform.anchoredPosition;
            var bestScore = float.MaxValue;

            for (var i = 0; i < previewTargets.Count; i++)
            {
                var other = previewTargets[i];
                if (other == null || other == piece)
                {
                    continue;
                }

                if (TryGetPreviewSnapDelta(piece, other, out var delta, out var score))
                {
                    var candidatePosition = ClampPieceToWorkspace(piece, piece.RectTransform.anchoredPosition + delta);
                    EvaluateSnapPosition(piece, previewTargets, candidatePosition, ref bestPosition, ref bestScore);
                }
            }

            if (bestScore >= float.MaxValue)
            {
                EvaluateSnapPosition(piece, previewTargets, bestPosition, ref bestPosition, ref bestScore);
            }

            piece.RectTransform.anchoredPosition = bestPosition;
            Physics2D.SyncTransforms();
        }

        private void EvaluateSnapPosition(
            AnchorPiece piece,
            IReadOnlyList<AnchorPiece> previewTargets,
            Vector2 candidatePosition,
            ref Vector2 bestPosition,
            ref float bestScore)
        {
            var originalPosition = piece.RectTransform.anchoredPosition;
            piece.RectTransform.anchoredPosition = candidatePosition;
            Physics2D.SyncTransforms();

            var linkedCount = 0;
            var attachGapPenalty = 0f;
            for (var i = 0; i < previewTargets.Count; i++)
            {
                var target = previewTargets[i];
                if (target == null || target == piece)
                {
                    continue;
                }

                if (GetAttachLength(piece, target) >= attachConfig.minAttachLength)
                {
                    attachGapPenalty += GetColliderOrRectGap(piece, target);
                    linkedCount++;
                }
            }

            var overlapPenalty = 0f;
            for (var i = 0; i < pieces.Count; i++)
            {
                var other = pieces[i];
                if (other == null || other == piece)
                {
                    continue;
                }

                if (TryGetSeparationDelta(piece, other, out var separation))
                {
                    overlapPenalty += separation.magnitude;
                }
            }

            var dragDistance = Vector2.Distance(originalPosition, candidatePosition);
            var overlapScore = overlapPenalty > 0f ? 1000000f + overlapPenalty * 10000f : 0f;
            var score = overlapScore - linkedCount * 100000f + attachGapPenalty * 1000f + dragDistance;
            if (score < bestScore)
            {
                bestScore = score;
                bestPosition = candidatePosition;
            }

            piece.RectTransform.anchoredPosition = originalPosition;
            Physics2D.SyncTransforms();
        }

        private float GetColliderOrRectGap(AnchorPiece a, AnchorPiece b)
        {
            var colliderA = a.ShapeCollider;
            var colliderB = b.ShapeCollider;
            if (colliderA != null && colliderB != null)
            {
                var distance = colliderA.Distance(colliderB);
                if (distance.isValid)
                {
                    return Mathf.Abs(distance.distance);
                }
            }

            return Mathf.Abs(GetRectGap(a, b));
        }

        private bool TryGetPreviewSnapDelta(AnchorPiece moving, AnchorPiece target, out Vector2 delta, out float score)
        {
            delta = Vector2.zero;
            score = float.MaxValue;
            if (TryGetColliderSnapDelta(moving, target, out delta, out score))
            {
                return true;
            }

            return TryGetRectSnapDelta(moving, target, out delta, out score);
        }

        private bool TryGetColliderSnapDelta(AnchorPiece moving, AnchorPiece target, out Vector2 delta, out float score)
        {
            delta = Vector2.zero;
            score = float.MaxValue;
            var movingCollider = moving.ShapeCollider;
            var targetCollider = target.ShapeCollider;
            if (movingCollider == null || targetCollider == null)
            {
                return false;
            }

            var distance = movingCollider.Distance(targetCollider);
            var snapRange = attachConfig.snapTolerance * PostSeparationSnapRangeMultiplier;
            if (!distance.isValid || (!distance.isOverlapped && distance.distance > snapRange))
            {
                return false;
            }

            var worldDelta = distance.pointB - distance.pointA;
            if (worldDelta.sqrMagnitude < 0.001f && Mathf.Abs(distance.distance) > 0.001f)
            {
                worldDelta = distance.normal * distance.distance;
            }

            delta = dragSurface.InverseTransformVector(worldDelta);
            score = Mathf.Abs(distance.distance);
            return true;
        }

        private bool TryGetRectSnapDelta(AnchorPiece moving, AnchorPiece target, out Vector2 delta, out float score)
        {
            delta = Vector2.zero;
            score = float.MaxValue;
            var movingRect = GetLocalRect(moving);
            var targetRect = GetLocalRect(target);
            var verticalOverlap = Mathf.Min(movingRect.yMax, targetRect.yMax) - Mathf.Max(movingRect.yMin, targetRect.yMin);
            var horizontalOverlap = Mathf.Min(movingRect.xMax, targetRect.xMax) - Mathf.Max(movingRect.xMin, targetRect.xMin);

            var leftGap = targetRect.xMin - movingRect.xMax;
            var rightGap = targetRect.xMax - movingRect.xMin;
            var downGap = targetRect.yMin - movingRect.yMax;
            var upGap = targetRect.yMax - movingRect.yMin;

            TryUseSnapCandidate(new Vector2(leftGap, 0f), Mathf.Abs(leftGap), verticalOverlap, ref delta, ref score);
            TryUseSnapCandidate(new Vector2(rightGap, 0f), Mathf.Abs(rightGap), verticalOverlap, ref delta, ref score);
            TryUseSnapCandidate(new Vector2(0f, downGap), Mathf.Abs(downGap), horizontalOverlap, ref delta, ref score);
            TryUseSnapCandidate(new Vector2(0f, upGap), Mathf.Abs(upGap), horizontalOverlap, ref delta, ref score);
            return score < float.MaxValue;
        }

        private float GetRectGap(AnchorPiece a, AnchorPiece b)
        {
            var rectA = GetLocalRect(a);
            var rectB = GetLocalRect(b);
            var horizontalGap = Mathf.Max(rectB.xMin - rectA.xMax, rectA.xMin - rectB.xMax, 0f);
            var verticalGap = Mathf.Max(rectB.yMin - rectA.yMax, rectA.yMin - rectB.yMax, 0f);
            if (horizontalGap > 0f || verticalGap > 0f)
            {
                return Mathf.Sqrt(horizontalGap * horizontalGap + verticalGap * verticalGap);
            }

            var horizontalOverlap = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
            var verticalOverlap = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);
            return -Mathf.Min(horizontalOverlap, verticalOverlap);
        }

        private void TryUseSnapCandidate(Vector2 candidateDelta, float gap, float overlap, ref Vector2 bestDelta, ref float bestScore)
        {
            var snapRange = attachConfig.snapTolerance * PostSeparationSnapRangeMultiplier;
            if (gap > snapRange || overlap < attachConfig.minAttachLength)
            {
                return;
            }

            if (gap < bestScore)
            {
                bestDelta = candidateDelta;
                bestScore = gap;
            }
        }

        private void MovePieceInWorkspace(AnchorPiece piece, Vector2 delta)
        {
            piece.RectTransform.anchoredPosition = ClampPieceToWorkspace(piece, piece.RectTransform.anchoredPosition + delta);
        }

        private Vector2 ClampPieceToWorkspace(AnchorPiece piece, Vector2 position)
        {
            var worldCenter = dragSurface.TransformPoint(position);
            var workspaceCenter = (Vector2)workspace.InverseTransformPoint(worldCenter);
            var halfWorkspace = workspace.rect.size * 0.5f;
            var halfPiece = piece.RectTransform.sizeDelta * 0.5f;
            workspaceCenter.x = Mathf.Clamp(workspaceCenter.x, -halfWorkspace.x + halfPiece.x, halfWorkspace.x - halfPiece.x);
            workspaceCenter.y = Mathf.Clamp(workspaceCenter.y, -halfWorkspace.y + halfPiece.y, halfWorkspace.y - halfPiece.y);
            return dragSurface.InverseTransformPoint(workspace.TransformPoint(workspaceCenter));
        }

        private Vector2 GetPieceWorldCenter(AnchorPiece piece)
        {
            return piece.RectTransform.TransformPoint(piece.RectTransform.rect.center);
        }

        private void ClearGeneratedPieces()
        {
            ClearBuild();
            for (var i = materialPieces.Count - 1; i >= 0; i--)
            {
                if (materialPieces[i] != null)
                {
                    Destroy(materialPieces[i].gameObject);
                }
            }

            materialPieces.Clear();
            pilePositions.Clear();
            pileRotations.Clear();
            pileSizes.Clear();
            pileScales.Clear();
        }
    }
}
