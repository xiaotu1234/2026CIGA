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
        [SerializeField] private MaterialConfig config = new MaterialConfig();

        public MaterialConfig Config => config;
        public RectTransform RectTransform { get; private set; }
        public Collider2D ShapeCollider { get; private set; }

        private BuildController owner;
        private RectTransform dragSurface;
        private Image image;
        private Text label;
        private Color normalColor;
        private Vector2 dragOffset;

        public void Initialize(MaterialConfig config, BuildController owner, RectTransform dragSurface)
        {
            if (config != null)
            {
                this.config = config.Clone(config.prefabAssetPath);
            }

            if (this.config == null)
            {
                this.config = new MaterialConfig();
            }

            this.owner = owner;
            this.dragSurface = dragSurface;

            RectTransform = GetComponent<RectTransform>();
            RectTransform.sizeDelta = this.config.size;
            ShapeCollider = GetComponent<Collider2D>();

            image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            normalColor = image.sprite == null ? this.config.color : image.color;
            image.color = normalColor;

            var outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(1f, 1f, 1f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);

            var labelTransform = transform.Find("Label");
            label = labelTransform == null ? null : labelTransform.GetComponent<Text>();
            if (label == null)
            {
                label = UIBuilder.CreateText(transform, "Label", this.config.displayName, 15, Color.white, TextAnchor.MiddleCenter);
            }
            else
            {
                label.text = this.config.displayName;
                label.font = UIBuilder.Font;
                label.fontSize = 15;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
            }
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            RectTransform.SetAsLastSibling();
        }

        public MaterialConfig CreateConfigSnapshot(string prefabAssetPath = null)
        {
            if (config == null)
            {
                config = new MaterialConfig();
            }

            var snapshot = config.Clone(prefabAssetPath);
            if (string.IsNullOrEmpty(snapshot.id))
            {
                snapshot.id = gameObject.name;
            }

            if (string.IsNullOrEmpty(snapshot.displayName))
            {
                snapshot.displayName = gameObject.name;
            }

            if (snapshot.size == Vector2.zero)
            {
                var rect = GetComponent<RectTransform>();
                snapshot.size = rect == null ? new Vector2(100f, 80f) : rect.sizeDelta;
            }

            return snapshot;
        }

        public void SetSelected(bool selected)
        {
            if (image == null)
            {
                return;
            }

            image.color = selected ? Color.Lerp(normalColor, Color.white, 0.28f) : normalColor;
        }

        public void RotateClockwise()
        {
            RectTransform.localRotation *= Quaternion.Euler(0f, 0f, -45f);
            owner.RefreshPiecePlacement(this);
        }

        public void Flip()
        {
            var scale = RectTransform.localScale;
            scale.x *= -1f;
            RectTransform.localScale = scale;
            owner.RefreshPiecePlacement(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            owner.SelectPiece(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner.BeginPieceDrag(this);
            RectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragSurface, eventData.position, eventData.pressEventCamera, out var localPoint);
            dragOffset = RectTransform.anchoredPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dragSurface, eventData.position, eventData.pressEventCamera, out var localPoint);
            RectTransform.anchoredPosition = ClampToDragSurface(localPoint + dragOffset);
            owner.DragPiece(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner.EndPieceDrag(this);
        }

        private Vector2 ClampToDragSurface(Vector2 position)
        {
            var halfWorkspace = dragSurface.rect.size * 0.5f;
            var halfPiece = RectTransform.sizeDelta * 0.5f;
            position.x = Mathf.Clamp(position.x, -halfWorkspace.x + halfPiece.x, halfWorkspace.x - halfPiece.x);
            position.y = Mathf.Clamp(position.y, -halfWorkspace.y + halfPiece.y, halfWorkspace.y - halfPiece.y);
            return position;
        }
    }
}
