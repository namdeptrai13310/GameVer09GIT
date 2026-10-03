using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.UI
{
    /// <summary>
    /// Bảng theo dõi tiến độ nhiệm vụ (Horror Quest Progress Tracker HUD) hiển thị góc trên màn hình:
    /// Phong cách kinh dị cổ điển (Obsidian & Antique Gold), hỗ trợ header badge, tiêu đề nổi bật,
    /// mô tả chi tiết không bị cắt chữ, bộ đếm tiến độ [ 3 / 4 ] và thanh nạp fill bar trực quan.
    /// </summary>
    public class QuestTrackerHUD : MonoBehaviour
    {
        public static QuestTrackerHUD Instance { get; private set; }

        [Header("UI References")]
        public CanvasGroup canvasGroup;
        public Text badgeText;
        public Text questTitleText;
        public Text questDescriptionText;
        public GameObject progressRow;
        public Text progressCounterText;
        public Image progressBarFill;
        public Image leftAccentBar;

        [Header("Colors")]
        public Color normalTitleColor = new Color(1.0f, 0.94f, 0.86f, 1.0f); // Trắng ngà cổ điển
        public Color completedColor = new Color(0.35f, 1.0f, 0.45f, 1.0f);   // Xanh ngọc hoàn thành
        public Color badgeColor = new Color(0.92f, 0.72f, 0.22f, 1.0f);       // Vàng hổ phách Antique Gold
        public Color progressCounterColor = new Color(1.0f, 0.82f, 0.35f, 1.0f);

        private int lastCurrent = -1;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            SetupLayout();
        }

        private void Start()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            // TUYỆT ĐỐI ẩn Quest HUD khi đang mở Menu Ký Di Chúc hoặc đang chạy Cinematic Intro
            if ((TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) ||
                (HorrorGame.Story.CinematicIntroManager.Instance != null && HorrorGame.Story.CinematicIntroManager.Instance.IsPlayingIntro))
            {
                if (canvasGroup != null && canvasGroup.alpha > 0f)
                {
                    canvasGroup.alpha = 0f;
                }
                return;
            }
        }

        public void SetupLayout()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(30f, -30f);
                rt.sizeDelta = new Vector2(450f, 140f);
            }

            if (badgeText != null)
            {
                badgeText.fontSize = 12;
                badgeText.fontStyle = FontStyle.Bold;
                badgeText.color = badgeColor;
                badgeText.horizontalOverflow = HorizontalWrapMode.Wrap;
                badgeText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (questTitleText != null)
            {
                questTitleText.fontSize = 18;
                questTitleText.fontStyle = FontStyle.Bold;
                questTitleText.color = normalTitleColor;
                questTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                questTitleText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.fontSize = 14;
                questDescriptionText.lineSpacing = 1.15f;
                questDescriptionText.color = new Color(0.78f, 0.83f, 0.90f, 0.95f);
                questDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                questDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (progressCounterText != null)
            {
                progressCounterText.fontSize = 13;
                progressCounterText.fontStyle = FontStyle.Bold;
                progressCounterText.color = progressCounterColor;
            }
        }

        public void UpdateTracker(string title, string description, int current = -1, int total = -1)
        {
            if (badgeText != null)
            {
                badgeText.text = "◈  MỤC TIÊU HIỆN TẠI  ◈";
            }

            if (questTitleText != null)
            {
                questTitleText.text = title;
                questTitleText.color = (total > 0 && current >= total) ? completedColor : normalTitleColor;
            }

            if (questDescriptionText != null)
            {
                questDescriptionText.text = description;
            }

            if (total > 0)
            {
                if (progressRow != null) progressRow.SetActive(true);

                if (progressCounterText != null)
                {
                    progressCounterText.text = $"Tiến độ:  <b>{current} / {total}</b>";
                    progressCounterText.color = progressCounterColor;
                }

                if (progressBarFill != null)
                {
                    float pct = Mathf.Clamp01((float)current / total);
                    progressBarFill.fillAmount = pct;
                }

                // Xung nhịp sáng lên mỗi khi tăng tiến độ
                if (current != lastCurrent && lastCurrent != -1)
                {
                    TriggerProgressPulse();
                }
                lastCurrent = current;
            }
            else
            {
                if (progressRow != null) progressRow.SetActive(false);
                lastCurrent = -1;
            }

            // Nếu đang trong menu Di Chúc hoặc Cutscene thì KHÔNG bật alpha
            if ((TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) ||
                (HorrorGame.Story.CinematicIntroManager.Instance != null && HorrorGame.Story.CinematicIntroManager.Instance.IsPlayingIntro))
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                return;
            }

            // Hiện tracker khi trong gameplay thực tế
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1.0f;
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        private void TriggerProgressPulse()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(RoutineProgressPulse());
        }

        private IEnumerator RoutineProgressPulse()
        {
            if (progressCounterText == null) yield break;

            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.35f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float frac = elapsed / dur;
                float scale = 1f + Mathf.Sin(frac * Mathf.PI) * 0.22f;
                progressCounterText.transform.localScale = origScale * scale;
                yield return null;
            }

            progressCounterText.transform.localScale = origScale;
            pulseRoutine = null;
        }
    }
}
