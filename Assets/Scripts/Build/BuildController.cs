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
        private readonly List<AnchorPiece> pieces = new List<AnchorPiece>();
        private readonly List<AttachJoint> joints = new List<AttachJoint>();
        private readonly List<Image> jointLines = new List<Image>();

        private const float ColliderSampleStep = 8f;
        private const int CircleSampleCount = 32;

        private RectTransform workspace;
        private RectTransform connectionLayer;
        private RectTransform materialTray;
        private Text riskText;
        private Text statusText;
        private AttachConfig attachConfig;
        private AnchorPiece selectedPiece;
        private AnchorPiece ropeTiePiece;
        private Action<AnchorBuildResult> onSubmit;

        public IReadOnlyList<AnchorPiece> Pieces => pieces;
        public IReadOnlyList<AttachJoint> Joints => joints;
        public AnchorPiece RopeTiePiece => ropeTiePiece;

        public void Initialize(
            RectTransform workspace,
            RectTransform connectionLayer,
            RectTransform materialTray,
            Text riskText,
            Text statusText,
            AttachConfig attachConfig,
            Action<AnchorBuildResult> onSubmit)
        {
            this.workspace = workspace;
            this.connectionLayer = connectionLayer;
            this.materialTray = materialTray;
            this.riskText = riskText;
            this.statusText = statusText;
            this.attachConfig = attachConfig;
            this.onSubmit = onSubmit;
        }

        public void PopulateMaterialTray(IReadOnlyList<MaterialConfig> materials)
        {
            ClearChildren(materialTray);
            for (var i = 0; i < materials.Count; i++)
            {
                var material = materials[i];
                var button = UIBuilder.CreateButton(materialTray, material.id + "Button", material.displayName, () => SpawnPiece(material));
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, 58f);
                rect.anchoredPosition = new Vector2(0f, -8f - i * 66f);
            }
        }

        public void SpawnPiece(MaterialConfig config)
        {
            var piece = CreatePieceInstance(config);
            piece.Initialize(config, this, workspace);
            piece.RectTransform.anchoredPosition = new Vector2(-180f + pieces.Count * 44f, 80f - pieces.Count * 20f);
            pieces.Add(piece);
            SelectPiece(piece);
            RefreshConnections();
        }

        private AnchorPiece CreatePieceInstance(MaterialConfig config)
        {
            GameObject go = null;
            var prefab = LoadPiecePrefab(config.prefabAssetPath);
            if (prefab != null)
            {
                go = Instantiate(prefab, workspace, false);
                if (go.GetComponent<RectTransform>() == null)
                {
                    Destroy(go);
                    go = null;
                }
            }

            if (go == null)
            {
                go = new GameObject(config.id, typeof(RectTransform));
                go.transform.SetParent(workspace, false);
            }

            go.name = config.id;
            var piece = go.GetComponent<AnchorPiece>();
            if (piece == null)
            {
                piece = go.AddComponent<AnchorPiece>();
            }

            return piece;
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

        public void SetRopeTiePoint()
        {
            ropeTiePiece = selectedPiece;
            UpdateStatus();
            UpdateRisks();
        }

        public void ClearBuild()
        {
            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                if (pieces[i] != null)
                {
                    Destroy(pieces[i].gameObject);
                }
            }

            pieces.Clear();
            joints.Clear();
            ropeTiePiece = null;
            selectedPiece = null;
            ClearJointLines();
            UpdateRisks();
            UpdateStatus();
        }

        public void RefreshConnections()
        {
            joints.Clear();
            for (var i = 0; i < pieces.Count; i++)
            {
                for (var j = i + 1; j < pieces.Count; j++)
                {
                    var attachLength = GetAttachLength(pieces[i], pieces[j]);
                    if (attachLength >= attachConfig.minAttachLength)
                    {
                        joints.Add(new AttachJoint(pieces[i], pieces[j], attachLength));
                    }
                }
            }

            DrawJointLines();
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
            result.isConnected = AttachGraph.IsFullyConnected(pieces, joints);
            for (var i = 0; i < pieces.Count; i++)
            {
                result.pieces.Add(pieces[i]);
                result.totalWeight += pieces[i].Config.weight;
                result.gripScore += pieces[i].Config.gripCoeff;
                result.dragScore += pieces[i].Config.dragCoeff;
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
                    return EstimateColliderAttachLength(colliderA, colliderB);
                }

                return 0f;
            }

            return GetRectAttachLength(a, b);
        }

        private float EstimateColliderAttachLength(Collider2D colliderA, Collider2D colliderB)
        {
            var outlineA = new List<Vector2>();
            var outlineB = new List<Vector2>();
            BuildColliderOutline(colliderA, outlineA);
            BuildColliderOutline(colliderB, outlineB);

            if (outlineA.Count < 2 || outlineB.Count < 2)
            {
                return GetBoundsAttachLength(colliderA.bounds, colliderB.bounds);
            }

            var lengthFromA = EstimateOutlineProximityLength(outlineA, colliderB, IsClosedCollider(colliderA));
            var lengthFromB = EstimateOutlineProximityLength(outlineB, colliderA, IsClosedCollider(colliderB));
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

        private float EstimateOutlineProximityLength(List<Vector2> outline, Collider2D target, bool closed)
        {
            var total = 0f;
            var segmentCount = closed ? outline.Count : outline.Count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var start = outline[i];
                var end = outline[(i + 1) % outline.Count];
                total += EstimateSegmentProximityLength(start, end, target);
            }

            return total;
        }

        private float EstimateSegmentProximityLength(Vector2 start, Vector2 end, Collider2D target)
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
                if (Vector2.Distance(samplePoint, closest) <= attachConfig.snapTolerance)
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

        private float GetRectAttachLength(AnchorPiece a, AnchorPiece b)
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
                attachLength = Mathf.Max(attachLength, overlapAttachLength, attachConfig.minAttachLength);
            }

            if ((leftToRight <= attachConfig.snapTolerance || rightToLeft <= attachConfig.snapTolerance) && verticalOverlap > 0f)
            {
                attachLength = Mathf.Max(attachLength, verticalOverlap);
            }

            if ((topToBottom <= attachConfig.snapTolerance || bottomToTop <= attachConfig.snapTolerance) && horizontalOverlap > 0f)
            {
                attachLength = Mathf.Max(attachLength, horizontalOverlap);
            }

            return attachLength;
        }

        private Rect GetLocalRect(AnchorPiece piece)
        {
            var size = piece.RectTransform.sizeDelta;
            var center = piece.RectTransform.anchoredPosition;
            return new Rect(center - size * 0.5f, size);
        }

        private void DrawJointLines()
        {
            ClearJointLines();
            for (var i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                var lineGo = new GameObject("JointLine", typeof(RectTransform));
                lineGo.transform.SetParent(connectionLayer, false);
                var image = lineGo.AddComponent<Image>();
                image.color = new Color(0.37f, 0.94f, 0.82f, 0.9f);
                var rect = image.rectTransform;
                var start = joint.pieceA.RectTransform.anchoredPosition;
                var end = joint.pieceB.RectTransform.anchoredPosition;
                var delta = end - start;
                rect.anchoredPosition = (start + end) * 0.5f;
                rect.sizeDelta = new Vector2(delta.magnitude, 5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                jointLines.Add(image);
            }
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

        private void UpdateRisks()
        {
            if (riskText == null)
            {
                return;
            }

            var risks = BuildRiskEvaluator.Evaluate(pieces, joints, ropeTiePiece);
            riskText.text = string.Join("\n", risks.ToArray());
        }

        private void UpdateStatus()
        {
            if (statusText == null)
            {
                return;
            }

            var selectedName = selectedPiece == null ? "未选择" : selectedPiece.Config.displayName.Replace("\n", " / ");
            var tieName = ropeTiePiece == null ? "未设置" : ropeTiePiece.Config.displayName.Replace("\n", " / ");
            statusText.text = $"材料 {pieces.Count}  连接 {joints.Count}  选中 {selectedName}  绑点 {tieName}";
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
