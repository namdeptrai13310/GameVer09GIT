using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using HorrorGame.DoorSystem;
using HorrorGame.Player;

namespace HorrorGame.Story
{
    public class HorrorEndingManager : MonoBehaviour
    {
        public static HorrorEndingManager Instance { get; private set; }

        public enum EndingType
        {
            None,
            EscapeCoward,     // Kết thúc 1: Trốn chạy vô vọng
            CurseBadEnding,   // Kết thúc 2: Hiến tế linh hồn (không có bùa)
            TruePurification  // Kết thúc 3: Hóa giải lời nguyền (có bùa trấn yểm)
        }

        [Header("State")]
        public EndingType currentEnding = EndingType.None;
        public bool isEndingTriggered = false;

        [Header("Audio SFX")]
        public AudioSource endingAudioSource;
        public AudioClip sfxCarEngine;
        public AudioClip sfxCarScreech;
        public AudioClip sfxDoorSlam;
        public AudioClip sfxHeartbeat;
        public AudioClip sfxMonsterBreath;
        public AudioClip sfxJumpscareSting;
        public AudioClip sfxHolyChime;
        public AudioClip sfxPurificationWind;

        [Header("UI Canvas")]
        public GameObject endingCanvasObj;
        public CanvasGroup canvasGroup;
        public Image fadeOverlay;
        public Text endingTitleText;
        public Text endingSubtitleText;
        public Text endingDescriptionText;
        public Button restartButton;
        public Text restartButtonText;

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

            if (endingAudioSource == null)
            {
                endingAudioSource = gameObject.AddComponent<AudioSource>();
                endingAudioSource.playOnAwake = false;
                endingAudioSource.spatialBlend = 0f; // 2D Full stereo
            }

            BuildEndingUI();
        }

        private void Update()
        {
            if (isEndingTriggered && endingCanvasObj != null && endingCanvasObj.activeSelf)
            {
                // Cho phép bấm R để chơi lại
                if (UnityEngine.InputSystem.Keyboard.current != null && 
                    UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartGame();
                }
            }
        }

        #region Trigger Endings

        /// <summary>
        /// KẾT THÚC 1: Lên xe bán tải trốn chạy
        /// </summary>
        public void TriggerEscapeEnding()
        {
            if (isEndingTriggered) return;
            isEndingTriggered = true;
            currentEnding = EndingType.EscapeCoward;

            StartCoroutine(RoutineEscapeEnding());
        }

        private IEnumerator RoutineEscapeEnding()
        {
            LockPlayerMovement();

            if (sfxCarEngine != null) endingAudioSource.PlayOneShot(sfxCarEngine, 0.95f);
            StoryObjectiveBanner.ShowObjective("TRỐN CHẠY", "Tiếng nổ máy rền vang! Bạn đạp hết ga lao chiếc xe ra khỏi cổng biệt thự...", 4.5f);

            yield return new WaitForSeconds(3.0f);

            if (sfxCarScreech != null) endingAudioSource.PlayOneShot(sfxCarScreech, 0.85f);
            yield return new WaitForSeconds(1.5f);

            if (sfxJumpscareSting != null) endingAudioSource.PlayOneShot(sfxJumpscareSting, 1.0f);

            yield return StartCoroutine(FadeToColor(Color.black, 1.5f));

            ShowEndingScreen(
                "KẾT THÚC 1: TRỐN CHẠY VÔ VỌNG",
                "— Nỗi Sợ Hãi Vĩnh Cửu —",
                "Bạn đã vội vã vứt bỏ ngôi biệt thự và phóng chiếc xe bán tải lao vào màn sương mù mịt mùng...\n\n" +
                "Nhưng khi liếc nhìn vào gương chiếu hậu giữa cabin, một bóng đen cao lớn với đôi mắt đỏ rực đang ngồi im lìm ở thùng xe sau lưng bạn.\n\n" +
                "Lời nguyền dòng họ không bao giờ ở lại căn biệt thự. Nó sẽ bám theo bạn đến hơi thở cuối cùng.",
                new Color(0.95f, 0.35f, 0.25f, 1f)
            );
        }

        /// <summary>
        /// KẾT THÚC 2: Xuống hầm không mang Bùa Trấn Yểm
        /// </summary>
        public void TriggerCurseBadEnding()
        {
            if (isEndingTriggered) return;
            isEndingTriggered = true;
            currentEnding = EndingType.CurseBadEnding;

            StartCoroutine(RoutineCurseBadEnding());
        }

        private IEnumerator RoutineCurseBadEnding()
        {
            LockPlayerMovement();

            if (sfxDoorSlam != null) endingAudioSource.PlayOneShot(sfxDoorSlam, 1.0f);
            StoryObjectiveBanner.ShowObjective("BẪY TỬ THẦN", "CỬA HẦM BỊ SẬP KÍN! Tiếng kim loại khóa chặt vang lên chát chúa...", 3.5f);

            yield return new WaitForSeconds(2.0f);

            if (sfxMonsterBreath != null) endingAudioSource.PlayOneShot(sfxMonsterBreath, 0.9f);
            if (sfxHeartbeat != null) endingAudioSource.PlayOneShot(sfxHeartbeat, 1.0f);

            // Màn hình nhấp nháy chuyển sang đỏ máu
            yield return StartCoroutine(FadeToColor(new Color(0.45f, 0.02f, 0.02f, 1f), 2.2f));

            if (sfxJumpscareSting != null) endingAudioSource.PlayOneShot(sfxJumpscareSting, 1.0f);

            ShowEndingScreen(
                "KẾT THÚC 2: HIẾN TẾ LINH HỒN",
                "— Lời Nguyền Thức Tỉnh —",
                "Bạn đã liều lĩnh bước chân vào hang ổ phong ấn của tà thuật mà không mang theo Bùa Trấn Yểm gia tộc.\n\n" +
                "Bóng tối lạnh lẽo bò dọc sống lưng. Hàng ngàn tiếng thì thầm ma quái tràn ngập tâm trí bạn.\n\n" +
                "Linh hồn bạn đã bị nuốt chửng dưới hầm sâu vĩnh viễn, trở thành con rối tiếp theo phục vụ cho khế ước máu của dòng họ.",
                new Color(1.0f, 0.15f, 0.15f, 1f)
            );
        }

        /// <summary>
        /// KẾT THÚC 3: Đặt bùa trấn yểm lên bàn thờ tầng hầm (True Ending)
        /// </summary>
        public void TriggerTruePurificationEnding()
        {
            if (isEndingTriggered) return;
            isEndingTriggered = true;
            currentEnding = EndingType.TruePurification;

            StartCoroutine(RoutineTrueEnding());
        }

        private IEnumerator RoutineTrueEnding()
        {
            LockPlayerMovement();

            if (sfxHolyChime != null) endingAudioSource.PlayOneShot(sfxHolyChime, 1.0f);
            if (sfxPurificationWind != null) endingAudioSource.PlayOneShot(sfxPurificationWind, 0.85f);

            StoryObjectiveBanner.ShowObjective(
                "HÓA GIẢI LỜI NGUYÊN!", 
                "Ánh sáng kim sắc bùng nổ từ lá bùa! Toàn bộ oán khí hàng chục năm tan biến thành tro bụi...", 
                5.0f
            );

            // Chuyển màn hình sang trắng vàng rực rỡ
            yield return StartCoroutine(FadeToColor(new Color(1f, 0.96f, 0.85f, 1f), 3.0f));

            ShowEndingScreen(
                "KẾT THÚC 3: SỰ THẬT & GIẢI THOÁT",
                "— Hóa Giải Lời Nguyền Dòng Họ (TRUE ENDING) —",
                "Lá bùa gia tộc rực cháy ánh vàng thuần khiết, phá vỡ hoàn toàn khế ước tà thuật đã xiềng xích ngôi biệt thự suốt nửa thế kỷ.\n\n" +
                "Tiếng rên xiết ai oán chuyển thành làn gió mát dịu. Những linh hồn oan khuất cuối cùng đã được siêu thoát.\n\n" +
                "Ánh bình minh đầu tiên sau hàng chục năm đã chiếu rọi xuống mảnh đất hoang tàn. Bạn đã sống sót, tìm ra chân tướng và cứu rỗi toàn bộ gia tộc.",
                new Color(1.0f, 0.88f, 0.35f, 1f),
                new Color(0.04f, 0.06f, 0.09f, 0.96f)
            );
        }

        #endregion

        private void LockPlayerMovement()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private IEnumerator FadeToColor(Color targetColor, float duration)
        {
            if (fadeOverlay == null) yield break;

            fadeOverlay.gameObject.SetActive(true);
            Color start = fadeOverlay.color;
            start.a = 0f;
            targetColor.a = 1f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(start, targetColor, elapsed / duration);
                yield return null;
            }
            fadeOverlay.color = targetColor;
        }

        private void ShowEndingScreen(string title, string subtitle, string description, Color titleColor, Color? bgColor = null)
        {
            if (endingCanvasObj != null)
            {
                endingCanvasObj.SetActive(true);
            }

            if (fadeOverlay != null && bgColor.HasValue)
            {
                fadeOverlay.color = bgColor.Value;
            }

            if (endingTitleText != null)
            {
                endingTitleText.text = title;
                endingTitleText.color = titleColor;
            }

            if (endingSubtitleText != null)
            {
                endingSubtitleText.text = subtitle;
            }

            if (endingDescriptionText != null)
            {
                endingDescriptionText.text = description;
            }

            if (canvasGroup != null)
            {
                StartCoroutine(RoutineFadeInCanvasGroup(canvasGroup, 1.2f));
            }
        }

        private IEnumerator RoutineFadeInCanvasGroup(CanvasGroup cg, float dur)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / dur);
                yield return null;
            }
            cg.alpha = 1f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void BuildEndingUI()
        {
            if (endingCanvasObj != null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = FindAnyObjectByType<Canvas>();
            }

            endingCanvasObj = new GameObject("Horror_Ending_Screen");
            if (rootCanvas != null)
            {
                endingCanvasObj.transform.SetParent(rootCanvas.transform, false);
            }

            RectTransform rt = endingCanvasObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            canvasGroup = endingCanvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Nền đen phủ toàn màn hình
            GameObject bgObj = new GameObject("Fade_Overlay");
            bgObj.transform.SetParent(endingCanvasObj.transform, false);
            RectTransform bgRt = bgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            fadeOverlay = bgObj.AddComponent<Image>();
            fadeOverlay.color = new Color(0.02f, 0.03f, 0.05f, 0.98f);

            Font safeFont = PubgDoorUI.GetSafeFont();

            // Container nội dung
            GameObject container = new GameObject("Ending_Content");
            container.transform.SetParent(endingCanvasObj.transform, false);
            RectTransform cRt = container.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.12f, 0.1f);
            cRt.anchorMax = new Vector2(0.88f, 0.9f);
            cRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vLayout = container.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 22f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;

            // 1. Title
            GameObject titleObj = new GameObject("Ending_Title");
            titleObj.transform.SetParent(container.transform, false);
            endingTitleText = titleObj.AddComponent<Text>();
            endingTitleText.font = safeFont;
            endingTitleText.fontSize = 32;
            endingTitleText.fontStyle = FontStyle.Bold;
            endingTitleText.alignment = TextAnchor.MiddleCenter;
            endingTitleText.color = new Color(1f, 0.85f, 0.35f, 1f);
            endingTitleText.text = "KẾT THÚC CÂU CHUYỆN";

            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = Color.black;
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // 2. Subtitle
            GameObject subObj = new GameObject("Ending_Subtitle");
            subObj.transform.SetParent(container.transform, false);
            endingSubtitleText = subObj.AddComponent<Text>();
            endingSubtitleText.font = safeFont;
            endingSubtitleText.fontSize = 20;
            endingSubtitleText.fontStyle = FontStyle.Italic;
            endingSubtitleText.alignment = TextAnchor.MiddleCenter;
            endingSubtitleText.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
            endingSubtitleText.text = "— Biệt Thự Tai Ương —";

            // 3. Description
            GameObject descObj = new GameObject("Ending_Description");
            descObj.transform.SetParent(container.transform, false);
            endingDescriptionText = descObj.AddComponent<Text>();
            endingDescriptionText.font = safeFont;
            endingDescriptionText.fontSize = 19;
            endingDescriptionText.alignment = TextAnchor.MiddleCenter;
            endingDescriptionText.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            endingDescriptionText.lineSpacing = 1.35f;
            endingDescriptionText.text = "Nội dung kết thúc câu chuyện...";

            var descShadow = descObj.AddComponent<Shadow>();
            descShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            descShadow.effectDistance = new Vector2(1.5f, -1.5f);

            // 4. Restart Button
            GameObject btnObj = new GameObject("Restart_Button");
            btnObj.transform.SetParent(container.transform, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(260f, 54f);

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 260f;
            le.preferredHeight = 54f;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.85f, 0.72f, 0.28f, 0.95f);

            var btnOutline = btnObj.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            btnOutline.effectDistance = new Vector2(2f, -2f);

            restartButton = btnObj.AddComponent<Button>();
            restartButton.onClick.AddListener(RestartGame);

            GameObject btnTextObj = new GameObject("Btn_Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTextObj.AddComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            restartButtonText = btnTextObj.AddComponent<Text>();
            restartButtonText.font = safeFont;
            restartButtonText.fontSize = 20;
            restartButtonText.fontStyle = FontStyle.Bold;
            restartButtonText.alignment = TextAnchor.MiddleCenter;
            restartButtonText.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            restartButtonText.text = "CHƠI LẠI [R]";

            endingCanvasObj.SetActive(false);
        }
    }
}
