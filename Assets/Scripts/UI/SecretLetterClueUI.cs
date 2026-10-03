using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HorrorGame.Story;

namespace HorrorGame.Story
{
    /// <summary>
    /// Giao diện đọc lá thư bí mật của người cha hé lộ trong ô tủ cũ ở bãi rác sân sau.
    /// </summary>
    public class SecretLetterClueUI : MonoBehaviour
    {
        public static SecretLetterClueUI Instance { get; private set; }

        [Header("UI Components")]
        public CanvasGroup letterCanvasGroup;
        public Text titleText;
        public Text bodyContentText;
        public Text closeHintText;
        public Button closeButton;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxPaperOpen;
        public AudioClip sfxPaperClose;

        [Header("Content")]
        [TextArea(3, 5)]
        public string defaultTitle = "DI THƯ CỦA NGƯỜI CHA (CẢNH BÁO NGUY HIỂM)";

        [TextArea(10, 25)]
        public string defaultBody = 
            "Gửi An, con trai của cha...\n\n" +
            "Nếu con đang đọc được những dòng chữ này, nghĩa là con đã trở về căn biệt thự và phá vỡ đống chướng ngại vật cha chặn ở chân cầu thang.\n\n" +
            "Cha van xin con, hãy dừng lại và rời khỏi đây ngay! Mười năm trước, mẹ con không hề mất vì bạo bệnh như cha đã từng nói dối con...\n\n" +
            "Bà ấy đã bị nuốt chửng bởi một thực thể tà ác trú ngụ bên trong căn phòng thờ niêm phong ở tầng hai!\n\n" +
            "Mỗi đêm khi tiếng chuông điểm, nó lại thức giấc, cào cấu gào thét dưới sàn gỗ. Để giam cầm nó và ngăn nó tràn xuống tầng trệt, cha đã chất đồ chặn kín cầu thang và dùng rìu đập sập hoàn toàn một mảng sàn lớn ở tầng hai.\n\n" +
            "Nếu con vẫn quyết tâm đi tìm sự thật về cái chết của mẹ... hãy nhặt những mảnh ván gỗ chắc chắn vừa rơi ra từ đống đổ nát này, leo lên tầng hai và đóng vá lại mảng sàn bị sập.\n\n" +
            "Nhưng hãy cẩn thận... Trong phòng ngủ có Chìa Khóa Tầng Hầm, và trên bàn thờ có Bùa Trấn Yểm. Con bắt buộc phải có bùa hộ thân nếu muốn bước xuống căn hầm sâu bên dưới ngôi nhà này...";

        public bool isReading { get; private set; } = false;
        private PlayerController playerController;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (letterCanvasGroup != null)
            {
                letterCanvasGroup.alpha = 0f;
                letterCanvasGroup.blocksRaycasts = false;
                letterCanvasGroup.interactable = false;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseLetter);
            }
        }

        private void Update()
        {
            if (!isReading) return;

            // Hỗ trợ phím F, E hoặc Escape để gấp thư lại
            bool closePressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.fKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame ||
                    Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    closePressed = true;
                }
            }

            if (closePressed)
            {
                CloseLetter();
            }
        }

        public void OpenLetter()
        {
            if (isReading) return;
            isReading = true;

            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                if (playerController != null) playerController.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (titleText != null) titleText.text = defaultTitle;
            if (bodyContentText != null) bodyContentText.text = defaultBody;

            if (sfxPaperOpen != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxPaperOpen, 0.85f);
            }

            StartCoroutine(FadeCanvasGroup(true, 0.35f));
        }

        public void CloseLetter()
        {
            if (!isReading) return;
            isReading = false;

            if (sfxPaperClose != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxPaperClose, 0.85f);
            }

            StartCoroutine(RoutineCloseLetter());
        }

        private IEnumerator RoutineCloseLetter()
        {
            yield return StartCoroutine(FadeCanvasGroup(false, 0.25f));

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Báo cho Quest Manager
            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnSecretLetterRead();
            }
        }

        private IEnumerator FadeCanvasGroup(bool show, float duration)
        {
            if (letterCanvasGroup == null) yield break;

            float startAlpha = letterCanvasGroup.alpha;
            float targetAlpha = show ? 1f : 0f;
            float elapsed = 0f;

            letterCanvasGroup.blocksRaycasts = show;
            letterCanvasGroup.interactable = show;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                letterCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            letterCanvasGroup.alpha = targetAlpha;
        }
    }
}
