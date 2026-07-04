using BrokenAnchor.Build;
using BrokenAnchor.Config;
using BrokenAnchor.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrokenAnchor.Pieces
{
    [RequireComponent(typeof(RectTransform))]
    public class AnchorPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public MaterialConfig Config { get; private set; }
        public RectTransform RectTransform { get; private set; }

        private BuildController owner;
        private RectTransform workspace;
        private Image image;
        private Text label;
        private Vector2 dragOffset;

        public void Initialize(MaterialConfig config, BuildController owner, RectTransform workspace)
        {
            Config = config;
            this.owner = owner;
            this.workspace = workspace;

            RectTransform = GetComponent<RectTransform>();
            RectTransform.sizeDelta = config.size;

            image = gameObject.AddComponent<Image>();
            image.color = config.color;

            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);

            label = UIBuilder.CreateText(transform, "Label", config.displayName, 15, Color.white, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetSelected(bool selected)
        {
            if (image == null)
            {
                return;
            }

            image.color = selected ? Color.Lerp(Config.color, Color.white, 0.28f) : Config.color;
        }

        public void RotateClockwise()
        {
            RectTransform.localRotation *= Quaternion.Euler(0f, 0f, -45f);
            owner.RefreshConnections();
        }

        public void Flip()
        {
            var scale = RectTransform.localScale;
            scale.x *= -1f;
            RectTransform.localScale = scale;
            owner.RefreshConnections();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner.SelectPiece(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner.SelectPiece(this);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(workspace, eventData.position, eventData.pressEventCamera, out var localPoint);
            dragOffset = RectTransform.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(workspace, eventData.position, eventData.pressEventCamera, out var localPoint);
            RectTransform.anchoredPosition = ClampToWorkspace(localPoint + dragOffset);
            owner.RefreshConnections();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner.RefreshConnections();
        }

        private Vector2 ClampToWorkspace(Vector2 position)
        {
            var halfWorkspace = workspace.rect.size * 0.5f;
            var halfPiece = RectTransform.sizeDelta * 0.5f;
            position.x = Mathf.Clamp(position.x, -halfWorkspace.x + halfPiece.x, halfWorkspace.x - halfPiece.x);
            position.y = Mathf.Clamp(position.y, -halfWorkspace.y + halfPiece.y, halfWorkspace.y - halfPiece.y);
            return position;
        }
    }
}
