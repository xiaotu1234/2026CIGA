using UnityEngine;
using UnityEngine.UI;

namespace BrokenAnchor.UI
{
    public class BuildHandsOverlay : MonoBehaviour
    {
        private const int TopSortingOrder = short.MaxValue;

        [SerializeField] private RectTransform leftHand;
        [SerializeField] private RectTransform rightHand;
        [SerializeField] private Sprite leftHandSprite;
        [SerializeField] private Sprite rightHandSprite;
        [SerializeField] private Vector2 leftAnchor = new Vector2(0.42f, -0.12f);
        [SerializeField] private Vector2 rightAnchor = new Vector2(0.58f, -0.12f);
        [SerializeField] private Vector2 leftTargetOffset = new Vector2(-58f, -18f);
        [SerializeField] private Vector2 rightTargetOffset = new Vector2(58f, -18f);
        [SerializeField] private float leftWidth = 96f;
        [SerializeField] private float rightWidth = 86f;
        [SerializeField] private float minLength = 140f;
        [SerializeField] private float maxLength = 1400f;

        private RectTransform selfRect;
        private Canvas overlayCanvas;

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
            UpdateHands();
        }

        private void Update()
        {
            EnsureTopLayer();
            UpdateHands();
        }

        private void LateUpdate()
        {
            EnsureTopLayer();
        }

        private void EnsureSetup()
        {
            selfRect = selfRect != null ? selfRect : transform as RectTransform;
            if (selfRect == null)
            {
                return;
            }

            leftHand = leftHand != null ? leftHand : FindHand("LeftBuildHand");
            rightHand = rightHand != null ? rightHand : FindHand("RightBuildHand");

            ConfigureHand(leftHand, leftHandSprite, leftWidth);
            ConfigureHand(rightHand, rightHandSprite, rightWidth);
            EnsureTopLayer();
        }

        private void EnsureTopLayer()
        {
            transform.SetAsLastSibling();

            overlayCanvas = overlayCanvas != null ? overlayCanvas : GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = TopSortingOrder;
        }

        private RectTransform FindHand(string handName)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == handName)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private void UpdateHands()
        {
            if (selfRect == null || leftHand == null || rightHand == null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(selfRect, Input.mousePosition, eventCamera, out var mouseLocal))
            {
                return;
            }

            StretchHand(leftHand, NormalizedPointToLocal(leftAnchor), mouseLocal + leftTargetOffset, leftWidth);
            StretchHand(rightHand, NormalizedPointToLocal(rightAnchor), mouseLocal + rightTargetOffset, rightWidth);
        }

        private void ConfigureHand(RectTransform hand, Sprite sprite, float width)
        {
            if (hand == null)
            {
                return;
            }

            hand.anchorMin = new Vector2(0.5f, 0.5f);
            hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = new Vector2(0.5f, 0f);

            var image = hand.GetComponent<Image>();
            if (image == null)
            {
                image = hand.gameObject.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            var size = hand.sizeDelta;
            size.x = width;
            hand.sizeDelta = size;
        }

        private void StretchHand(RectTransform hand, Vector2 start, Vector2 target, float width)
        {
            var direction = target - start;
            var length = Mathf.Clamp(direction.magnitude, minLength, maxLength);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            hand.anchoredPosition = start;
            hand.sizeDelta = new Vector2(width, length);
            hand.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private Vector2 NormalizedPointToLocal(Vector2 normalizedPoint)
        {
            var rect = selfRect.rect;
            return new Vector2(
                rect.xMin + rect.width * normalizedPoint.x,
                rect.yMin + rect.height * normalizedPoint.y);
        }
    }
}
