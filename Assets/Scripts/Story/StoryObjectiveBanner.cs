using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame.Story
{
    public class StoryObjectiveBanner : MonoBehaviour
    {
        public static StoryObjectiveBanner Instance { get; private set; }

        public CanvasGroup bannerCanvasGroup;
        public Text titleText;
        public Text descText;

        private Coroutine activeBannerCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
            }

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                // Đặt ngay trên thanh thể lực ở đáy màn hình theo phong cách phụ đề kinh dị
                rt.anchorMin = new Vector2(0.5f, 0.0f);
                rt.anchorMax = new Vector2(0.5f, 0.0f);
                rt.pivot = new Vector2(0.5f, 0.0f);
                rt.anchoredPosition = new Vector2(0f, 155f);
                rt.sizeDelta = new Vector2(860f, 90f);
            }

            if (titleText != null)
            {
                titleText.fontSize = 20;
                titleText.fontStyle = FontStyle.Bold;
            }

            if (descText != null)
            {
                descText.fontSize = 17;
                descText.fontStyle = FontStyle.Bold;
                descText.lineSpacing = 1.15f;
            }
        }

        public static void ShowObjective(string title, string description, float duration = 5.5f)
        {
            if (Instance != null)
            {
                Instance.DisplayBanner(title, description, duration);
            }
        }

        public void DisplayBanner(string title, string description, float duration)
        {
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                return;
            }

            if (activeBannerCoroutine != null) StopCoroutine(activeBannerCoroutine);
            activeBannerCoroutine = StartCoroutine(RoutineDisplay(title, description, duration));
        }

        private IEnumerator RoutineDisplay(string title, string description, float duration)
        {
            if (titleText != null) titleText.text = title;
            if (descText != null) descText.text = description;

            if (bannerCanvasGroup != null)
            {
                // Fade in
                float fElapsed = 0f;
                while (fElapsed < 0.45f)
                {
                    fElapsed += Time.deltaTime;
                    bannerCanvasGroup.alpha = fElapsed / 0.45f;
                    yield return null;
                }
                bannerCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(duration);

                // Fade out
                fElapsed = 0f;
                while (fElapsed < 0.6f)
                {
                    fElapsed += Time.deltaTime;
                    bannerCanvasGroup.alpha = 1f - (fElapsed / 0.6f);
                    yield return null;
                }
                bannerCanvasGroup.alpha = 0f;
            }
        }
    }
}
