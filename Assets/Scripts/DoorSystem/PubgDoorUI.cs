using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.DoorSystem
{
    public class PubgDoorUI : MonoBehaviour
    {
        public static PubgDoorUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject promptContainer;
        public Text keyText;
        public Text actionText;
        public CanvasGroup canvasGroup;

        [Header("Settings")]
        public float fadeSpeed = 14f;

        private bool isVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureResponsiveLayout();

            if (promptContainer == null)
            {
                BuildDynamicUI();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        public void EnsureResponsiveLayout()
        {
            if (promptContainer == null) return;

            // Đảm bảo promptContainer có ContentSizeFitter để tự co giãn theo chiều ngang
            var csf = promptContainer.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = promptContainer.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var hlg = promptContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = promptContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14f;
            hlg.padding = new RectOffset(16, 24, 8, 8);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            if (keyText != null)
            {
                Transform keyBox = keyText.transform.parent;
                if (keyBox != null)
                {
                    var leKey = keyBox.GetComponent<LayoutElement>();
                    if (leKey == null) leKey = keyBox.gameObject.AddComponent<LayoutElement>();
                    leKey.minWidth = 34f;
                    leKey.minHeight = 34f;
                    leKey.preferredWidth = 34f;
                    leKey.preferredHeight = 34f;
                    leKey.flexibleWidth = 0f;
                    leKey.flexibleHeight = 0f;
                }
            }

            if (actionText != null)
            {
                actionText.horizontalOverflow = HorizontalWrapMode.Overflow;
                actionText.verticalOverflow = VerticalWrapMode.Overflow;

                var le = actionText.GetComponent<LayoutElement>();
                if (le == null) le = actionText.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 60f;
                le.preferredWidth = -1f;
                le.flexibleWidth = 1f;
                le.minHeight = 34f;
                le.preferredHeight = 34f;
            }
        }

        private void Update()
        {
            if (canvasGroup != null)
            {
                float targetAlpha = isVisible ? 1f : 0f;
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

                if (promptContainer != null)
                {
                    bool shouldBeActive = canvasGroup.alpha > 0.01f || isVisible;
                    if (promptContainer.activeSelf != shouldBeActive)
                    {
                        promptContainer.SetActive(shouldBeActive);
                    }
                }
            }
        }

        public void Show(string action, string key = "F")
        {
            if (keyText != null) keyText.text = key;
            if (actionText != null)
            {
                actionText.text = action;
            }
            isVisible = true;

            if (promptContainer != null)
            {
                if (!promptContainer.activeSelf)
                {
                    promptContainer.SetActive(true);
                }
                EnsureResponsiveLayout();
                var rt = promptContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (actionText != null) LayoutRebuilder.ForceRebuildLayoutImmediate(actionText.rectTransform);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                }
            }
        }

        public void Hide()
        {
            isVisible = false;
        }

        /// <summary>
        /// Tìm font chữ sắc nét cao cấp (ưu tiên Inter-SemiBold)
        /// </summary>
        public static Font GetSafeFont()
        {
            // 1. Ưu tiên font Inter-SemiBold cao cấp
            Font font = Resources.Load<Font>("Fonts/Inter-SemiBold");
            if (font != null) return font;

            // 2. Thử tạo font từ hệ điều hành Windows
            font = Font.CreateDynamicFontFromOSFont("Segoe UI", 28);
            if (font != null) return font;

            font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            if (font != null) return font;

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// Tự động dựng giao diện HUD tương tác phong cách Tai Ương / PUBG hiện đại,
        /// co giãn thông minh không bao giờ bị đè chữ hay lẹm viền.
        /// </summary>
        public void BuildDynamicUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform, false);
                }
            }

            Font safeFont = GetSafeFont();

            // Root HUD Container
            promptContainer = new GameObject("Horror_InteractionPrompt");
            promptContainer.transform.SetParent(transform, false);

            RectTransform rectPrompt = promptContainer.AddComponent<RectTransform>();
            rectPrompt.anchorMin = new Vector2(0.5f, 0.5f);
            rectPrompt.anchorMax = new Vector2(0.5f, 0.5f);
            rectPrompt.pivot = new Vector2(0.5f, 0.5f);
            rectPrompt.anchoredPosition = new Vector2(0f, -85f); // Nằm ngay dưới tâm ngắm
            rectPrompt.sizeDelta = new Vector2(220f, 48f);

            canvasGroup = promptContainer.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Nền đen than mờ phong cách game kinh dị Tai Ương
            Image bg = promptContainer.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.07f, 0.90f);

            // Viền sáng mạ vàng nhạt huyền bí
            Outline outline = promptContainer.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.75f, 0.35f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Bố cục Layout tự động co giãn theo độ dài nội dung
            HorizontalLayoutGroup layout = promptContainer.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.padding = new RectOffset(16, 26, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter containerFitter = promptContainer.AddComponent<ContentSizeFitter>();
            containerFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 1. Ô Phím vuông [F]
            GameObject keyBox = new GameObject("KeyBox");
            keyBox.transform.SetParent(promptContainer.transform, false);

            RectTransform keyRect = keyBox.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(36f, 36f);

            LayoutElement keyLE = keyBox.AddComponent<LayoutElement>();
            keyLE.preferredWidth = 36f;
            keyLE.preferredHeight = 36f;
            keyLE.minWidth = 36f;
            keyLE.minHeight = 36f;

            Image keyBg = keyBox.AddComponent<Image>();
            keyBg.color = new Color(0.95f, 0.88f, 0.65f, 0.95f); // Vàng sáng hổ phách

            Outline keyOutline = keyBox.AddComponent<Outline>();
            keyOutline.effectColor = new Color(0.15f, 0.12f, 0.05f, 0.8f);
            keyOutline.effectDistance = new Vector2(1f, -1f);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyBox.transform, false);
            RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.anchorMin = Vector2.zero;
            keyTextRect.anchorMax = Vector2.one;
            keyTextRect.sizeDelta = Vector2.zero;

            keyText = keyTextObj.AddComponent<Text>();
            keyText.text = "F";
            keyText.font = safeFont;
            keyText.fontSize = 20;
            keyText.fontStyle = FontStyle.Bold;
            keyText.alignment = TextAnchor.MiddleCenter;
            keyText.color = new Color(0.1f, 0.08f, 0.04f, 1f); // Chữ F đen đậm nổi trên nút vàng
            keyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            keyText.verticalOverflow = VerticalWrapMode.Overflow;

            // 2. Chữ hành động
            GameObject actionTextObj = new GameObject("ActionText");
            actionTextObj.transform.SetParent(promptContainer.transform, false);
            RectTransform actionRect = actionTextObj.AddComponent<RectTransform>();

            ContentSizeFitter actionFitter = actionTextObj.AddComponent<ContentSizeFitter>();
            actionFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            actionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement actionLE = actionTextObj.AddComponent<LayoutElement>();
            actionLE.minHeight = 36f;
            actionLE.preferredHeight = 36f;

            actionText = actionTextObj.AddComponent<Text>();
            actionText.text = "Tương tác";
            actionText.font = safeFont;
            actionText.fontSize = 18;
            actionText.fontStyle = FontStyle.Bold;
            actionText.alignment = TextAnchor.MiddleLeft;
            actionText.color = new Color(0.98f, 0.98f, 0.98f, 1f); // Trắng sáng không bị lóa
            actionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            actionText.verticalOverflow = VerticalWrapMode.Overflow;

            // Đổ bóng chữ đen chống chói
            Shadow textShadow = actionTextObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            textShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // Đảm bảo vẽ trên cùng Canvas
            promptContainer.transform.SetAsLastSibling();
            promptContainer.SetActive(false);
        }
    }
}
