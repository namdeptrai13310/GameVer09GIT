using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// Hiển thị tên khu vực / phòng khi người chơi bước vào từng gian phòng trong căn biệt thự.
    /// Giúp người chơi định hướng không gian giống 'Fears to Fathom' & 'Tai Ương' mà không cần dựa dẫm vào đốm sáng waypoint.
    /// </summary>
    public class RoomNotificationUI : MonoBehaviour
    {
        public static RoomNotificationUI Instance { get; private set; }

        private GameObject canvasObject;
        private GameObject bannerContainer;
        private CanvasGroup bannerCanvasGroup;
        private Text titleText;
        private Text subtitleText;
        private RectTransform bannerRect;

        private Font defaultFont;
        private Coroutine displayRoutine;
        private string currentRoomName = "";
        private float lastShowTime = -99f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadFont();
            InitializeUI();
        }

        private void LoadFont()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 22);
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 22);
        }

        private void InitializeUI()
        {
            canvasObject = new GameObject("RoomNotificationCanvas");
            canvasObject.transform.SetParent(transform);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 920;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            // Banner Container
            bannerContainer = new GameObject("RoomBanner");
            bannerContainer.transform.SetParent(canvasObject.transform, false);

            bannerRect = bannerContainer.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.sizeDelta = new Vector2(460, 68);
            bannerRect.anchoredPosition = new Vector2(0, -60);

            bannerCanvasGroup = bannerContainer.AddComponent<CanvasGroup>();
            bannerCanvasGroup.alpha = 0f;
            bannerCanvasGroup.blocksRaycasts = false;

            // Nền tối mờ sang trọng
            Image bg = bannerContainer.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.05f, 0.85f);

            // Viền vàng hổ phách mảnh phía dưới
            GameObject line = new GameObject("AccentLine");
            line.transform.SetParent(bannerContainer.transform, false);
            RectTransform lineRt = line.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.12f, 0.04f);
            lineRt.anchorMax = new Vector2(0.88f, 0.07f);
            lineRt.sizeDelta = Vector2.zero;
            Image lineImg = line.AddComponent<Image>();
            lineImg.color = new Color(0.95f, 0.76f, 0.20f, 0.85f);

            // Tiêu đề phòng
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bannerContainer.transform, false);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 21;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.96f, 0.94f, 0.90f, 1f);

            RectTransform titleRt = titleText.rectTransform;
            titleRt.anchorMin = new Vector2(0, 0.32f);
            titleRt.anchorMax = new Vector2(1, 0.95f);
            titleRt.sizeDelta = Vector2.zero;

            // Phụ đề (Tầng 1, Tầng 2, Sân vườn, v.v.)
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(bannerContainer.transform, false);
            subtitleText = subObj.AddComponent<Text>();
            subtitleText.font = defaultFont;
            subtitleText.fontSize = 13;
            subtitleText.fontStyle = FontStyle.Italic;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.72f, 0.68f, 0.60f, 0.9f);

            RectTransform subRt = subtitleText.rectTransform;
            subRt.anchorMin = new Vector2(0, 0.08f);
            subRt.anchorMax = new Vector2(1, 0.36f);
            subRt.sizeDelta = Vector2.zero;

            bannerContainer.SetActive(false);
        }

        public void ShowRoom(string roomName, string subtitle = "")
        {
            if (string.IsNullOrEmpty(roomName)) return;

            // Chống spam nếu cùng 1 phòng và chưa qua 3.5 giây
            if (roomName == currentRoomName && (Time.time - lastShowTime) < 3.5f)
            {
                return;
            }

            currentRoomName = roomName;
            lastShowTime = Time.time;

            if (titleText != null)
            {
                titleText.text = $"📍  {roomName.ToUpper()}";
            }

            if (subtitleText != null)
            {
                subtitleText.text = string.IsNullOrEmpty(subtitle) ? "BIỆT THỰ GIA TỘC" : subtitle.ToUpper();
            }

            if (displayRoutine != null)
            {
                StopCoroutine(displayRoutine);
            }
            displayRoutine = StartCoroutine(RoutineAnimateBanner());
        }

        private IEnumerator RoutineAnimateBanner()
        {
            bannerContainer.SetActive(true);

            // Fade In (0.35s)
            float elapsed = 0f;
            float fadeInDur = 0.35f;
            Vector2 startPos = new Vector2(0, -45);
            Vector2 targetPos = new Vector2(0, -60);

            while (elapsed < fadeInDur)
            {
                elapsed += Time.deltaTime;
                float frac = Mathf.Clamp01(elapsed / fadeInDur);
                bannerCanvasGroup.alpha = frac;
                bannerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, frac);
                yield return null;
            }

            bannerCanvasGroup.alpha = 1f;
            bannerRect.anchoredPosition = targetPos;

            // Hiển thị 2.6 giây
            yield return new WaitForSeconds(2.6f);

            // Fade Out (0.65s)
            elapsed = 0f;
            float fadeOutDur = 0.65f;
            Vector2 endPos = new Vector2(0, -75);

            while (elapsed < fadeOutDur)
            {
                elapsed += Time.deltaTime;
                float frac = Mathf.Clamp01(elapsed / fadeOutDur);
                bannerCanvasGroup.alpha = 1f - frac;
                bannerRect.anchoredPosition = Vector2.Lerp(targetPos, endPos, frac);
                yield return null;
            }

            bannerCanvasGroup.alpha = 0f;
            bannerContainer.SetActive(false);
            displayRoutine = null;
        }
    }
}
