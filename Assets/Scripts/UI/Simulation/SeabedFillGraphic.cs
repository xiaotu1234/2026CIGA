using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class SeabedFillGraphic : Graphic
    {
        private readonly List<Vector2> topPoints = new List<Vector2>();
        private float bottomY;

        public void SetShape(IReadOnlyList<Vector2> points, float fillBottomY)
        {
            topPoints.Clear();
            if (points != null)
            {
                for (var i = 0; i < points.Count; i++)
                {
                    topPoints.Add(points[i]);
                }
            }

            bottomY = fillBottomY;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (topPoints.Count < 2)
            {
                return;
            }

            for (var i = 0; i < topPoints.Count; i++)
            {
                var top = topPoints[i];
                vertexHelper.AddVert(top, color, Vector2.zero);
                vertexHelper.AddVert(new Vector2(top.x, bottomY), color, Vector2.zero);
            }

            for (var i = 0; i < topPoints.Count - 1; i++)
            {
                var index = i * 2;
                vertexHelper.AddTriangle(index, index + 2, index + 1);
                vertexHelper.AddTriangle(index + 2, index + 3, index + 1);
            }
        }
    }
}
