using UnityEngine;

namespace RunningLate
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Tooltip(
            "Extra inset inside the phone safe area, in Canvas units " +
            "(left, bottom, right, top)."
        )]
        [SerializeField]
        private Vector4 padding =
            new Vector4(72f, 220f, 72f, 220f);

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform =
                GetComponent<RectTransform>();

            ApplySafeArea();
        }

        private void OnEnable()
        {
            if (rectTransform == null)
            {
                rectTransform =
                    GetComponent<RectTransform>();
            }

            ApplySafeArea();
        }

        private void Update()
        {
            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize =
                new Vector2Int(
                    Screen.width,
                    Screen.height
                );

            if (safeArea != lastSafeArea ||
                screenSize != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (rectTransform == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax =
                safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            rectTransform.offsetMin =
                new Vector2(
                    padding.x,
                    padding.y
                );

            rectTransform.offsetMax =
                new Vector2(
                    -padding.z,
                    -padding.w
                );

            lastSafeArea = safeArea;
            lastScreenSize =
                new Vector2Int(
                    Screen.width,
                    Screen.height
                );
        }
    }
}
