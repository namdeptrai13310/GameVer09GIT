using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

namespace HorrorGame.UI
{
    [System.Serializable]
    public class NotebookObjective
    {
        public string id;
        public string title;
        public string description;
        public bool isCompleted;
        public string progress;
    }

    [System.Serializable]
    public class NotebookClue
    {
        public string id;
        public string text;
        public string timestamp;
    }

    /// <summary>
    /// Sổ tay ghi chép cầm tay của nhân vật (lấy cảm hứng từ Sons of the Forest & Fears to Fathom).
    /// Nhân vật có thói quen ghi lại công việc cần làm và các manh mối khi khám phá căn biệt thự.
    /// </summary>
    public class PlayerNotebook : MonoBehaviour
    {
        public static PlayerNotebook Instance { get; private set; }

        private List<NotebookObjective> objectives = new List<NotebookObjective>();
        private List<NotebookClue> clues = new List<NotebookClue>();

        private GameObject canvasObject;
        private GameObject notebookContainer;
        private RectTransform notebookRect;
        private Text objectivesText;
        private Text cluesText;
        private AudioSource audioSource;

        private GameObject toastContainer;
        private CanvasGroup toastCanvasGroup;
        private Text toastText;

        public bool IsOpen => isOpen;
        private bool isOpen = false;
        private bool isAnimating = false;
        private Coroutine toastCoroutine;

        private Font defaultFont;

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
            InitializeAudio();
        }

        private void LoadFont()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (defaultFont == null)
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 16);
            }
            if (defaultFont == null)
            {
                defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
        }

        private void InitializeUI()
        {
            // Canvas
            canvasObject = new GameObject("PlayerNotebookCanvas");
            canvasObject.transform.SetParent(transform);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950; // Dưới Testament Menu nhưng trên gameplay bình thường

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            // Dark vignette overlay behind notebook when open
            GameObject overlay = new GameObject("NotebookOverlay");
            overlay.transform.SetParent(canvasObject.transform, false);
            RectTransform overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta = Vector2.zero;
            Image overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.45f);
            overlay.SetActive(false);

            // Notebook Container (Kích thước thực tế phong cách Sons of the Forest: 960 x 620)
            notebookContainer = new GameObject("NotebookContainer");
            notebookContainer.transform.SetParent(canvasObject.transform, false);
            notebookRect = notebookContainer.AddComponent<RectTransform>();
            notebookRect.anchorMin = new Vector2(0.5f, 0.5f);
            notebookRect.anchorMax = new Vector2(0.5f, 0.5f);
            notebookRect.pivot = new Vector2(0.5f, 0.5f);
            notebookRect.sizeDelta = new Vector2(1080, 680);
            notebookRect.anchoredPosition = new Vector2(0, -1000); // Ẩn dưới màn hình

            // Khung bìa da nâu sậm
            Image bookCover = notebookContainer.AddComponent<Image>();
            bookCover.color = new Color(0.18f, 0.12f, 0.08f, 0.98f);

            // Giấy nền bên trong (Màu giấy da ngả vàng cũ kỹ)
            GameObject paperBg = new GameObject("PaperBackground");
            paperBg.transform.SetParent(notebookContainer.transform, false);
            RectTransform paperRt = paperBg.AddComponent<RectTransform>();
            paperRt.anchorMin = Vector2.zero;
            paperRt.anchorMax = Vector2.one;
            paperRt.offsetMin = new Vector2(16, 16);
            paperRt.offsetMax = new Vector2(-16, -16);
            Image paperImg = paperBg.AddComponent<Image>();
            paperImg.color = new Color(0.92f, 0.88f, 0.79f, 1f); // Màu giấy kraft cổ điển

            // Gáy sổ ở giữa (Spine Divider)
            GameObject spine = new GameObject("BookSpine");
            spine.transform.SetParent(paperBg.transform, false);
            RectTransform spineRt = spine.AddComponent<RectTransform>();
            spineRt.anchorMin = new Vector2(0.5f, 0f);
            spineRt.anchorMax = new Vector2(0.5f, 1f);
            spineRt.sizeDelta = new Vector2(10, 0);
            Image spineImg = spine.AddComponent<Image>();
            spineImg.color = new Color(0.68f, 0.62f, 0.52f, 0.8f);

            // Trang Trái: MỤC TIÊU (Objectives Checklist)
            GameObject leftPage = new GameObject("LeftPage");
            leftPage.transform.SetParent(paperBg.transform, false);
            RectTransform leftRect = leftPage.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.485f, 1f);
            leftRect.offsetMin = new Vector2(26, 26);
            leftRect.offsetMax = new Vector2(-18, -26);

            Text leftTitle = CreateText(leftPage, "MỤC TIÊU CẦN LÀM", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            leftTitle.color = new Color(0.28f, 0.20f, 0.12f, 1f);
            leftTitle.rectTransform.anchorMin = new Vector2(0, 0.90f);
            leftTitle.rectTransform.anchorMax = new Vector2(1, 1f);

            // Dòng gạch dưới tiêu đề
            GameObject leftLine = new GameObject("LeftLine");
            leftLine.transform.SetParent(leftPage.transform, false);
            RectTransform leftLineRt = leftLine.AddComponent<RectTransform>();
            leftLineRt.anchorMin = new Vector2(0, 0.89f);
            leftLineRt.anchorMax = new Vector2(1, 0.895f);
            leftLineRt.sizeDelta = new Vector2(0, 2f);
            Image lineImg1 = leftLine.AddComponent<Image>();
            lineImg1.color = new Color(0.6f, 0.5f, 0.4f, 0.7f);

            objectivesText = CreateText(leftPage, "", 18, FontStyle.Normal, TextAnchor.UpperLeft);
            objectivesText.lineSpacing = 1.25f;
            objectivesText.rectTransform.anchorMin = new Vector2(0, 0.05f);
            objectivesText.rectTransform.anchorMax = new Vector2(1, 0.87f);
            objectivesText.color = new Color(0.2f, 0.16f, 0.12f, 1f);

            // Trang Phải: NHẬT KÝ & GHI CHÉP (Journal & Clues)
            GameObject rightPage = new GameObject("RightPage");
            rightPage.transform.SetParent(paperBg.transform, false);
            RectTransform rightRect = rightPage.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.515f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.offsetMin = new Vector2(18, 26);
            rightRect.offsetMax = new Vector2(-26, -26);

            Text rightTitle = CreateText(rightPage, "NHẬT KÝ & MANH MỐI", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            rightTitle.color = new Color(0.28f, 0.20f, 0.12f, 1f);
            rightTitle.rectTransform.anchorMin = new Vector2(0, 0.90f);
            rightTitle.rectTransform.anchorMax = new Vector2(1, 1f);

            GameObject rightLine = new GameObject("RightLine");
            rightLine.transform.SetParent(rightPage.transform, false);
            RectTransform rightLineRt = rightLine.AddComponent<RectTransform>();
            rightLineRt.anchorMin = new Vector2(0, 0.89f);
            rightLineRt.anchorMax = new Vector2(1, 0.895f);
            rightLineRt.sizeDelta = new Vector2(0, 2f);
            Image lineImg2 = rightLine.AddComponent<Image>();
            lineImg2.color = new Color(0.6f, 0.5f, 0.4f, 0.7f);

            cluesText = CreateText(rightPage, "", 18, FontStyle.Normal, TextAnchor.UpperLeft);
            cluesText.lineSpacing = 1.25f;
            cluesText.rectTransform.anchorMin = new Vector2(0, 0.05f);
            cluesText.rectTransform.anchorMax = new Vector2(1, 0.87f);
            cluesText.color = new Color(0.25f, 0.20f, 0.15f, 1f);

            // Gợi ý bấm đóng ở góc dưới
            Text closeHint = CreateText(notebookContainer, "Nhấn [TAB] hoặc [J] để gấp sổ lại", 15, FontStyle.Italic, TextAnchor.LowerCenter);
            closeHint.rectTransform.anchorMin = new Vector2(0, 0);
            closeHint.rectTransform.anchorMax = new Vector2(1, 0.04f);
            closeHint.color = new Color(0.5f, 0.42f, 0.35f, 1f);

            // Ẩn ban đầu
            notebookContainer.SetActive(false);

            // Toast thông báo nhỏ góc trên (Chỉ hiện trong gameplay, TUYỆT ĐỐI không hiện khi đang ở TestamentMenu!)
            toastContainer = new GameObject("ToastContainer");
            toastContainer.transform.SetParent(canvasObject.transform, false);
            RectTransform toastRect = toastContainer.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = new Vector2(380, 44);
            toastRect.anchoredPosition = new Vector2(0, -35);

            Image toastBg = toastContainer.AddComponent<Image>();
            toastBg.color = new Color(0.08f, 0.08f, 0.10f, 0.90f);

            toastCanvasGroup = toastContainer.AddComponent<CanvasGroup>();
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.blocksRaycasts = false;

            toastText = CreateText(toastContainer, "📓 ĐÃ CẬP NHẬT SỔ GHI CHÚ  <color=#FFD700>[TAB]</color>", 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            toastText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            toastText.rectTransform.anchorMin = Vector2.zero;
            toastText.rectTransform.anchorMax = Vector2.one;
            toastContainer.SetActive(false);
        }

        private Text CreateText(GameObject parent, string defaultText, int fontSize, FontStyle style, TextAnchor alignment)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.font = defaultFont;
            txt.text = defaultText;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.supportRichText = true;

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return txt;
        }

        private void InitializeAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.8f;
            audioSource.ignoreListenerPause = true; // Chạy được cả khi Time.timeScale = 0
        }

        private void Update()
        {
            // Không mở sổ khi đang ở giao diện Ký Di Chúc mở đầu
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                if (isOpen) CloseNotebookImmediate();
                return;
            }

            // Không mở sổ khi đang chạy phim cutscene mở đầu
            if (HorrorGame.Story.CinematicIntroManager.Instance != null && HorrorGame.Story.CinematicIntroManager.Instance.IsPlayingIntro)
            {
                if (isOpen) CloseNotebookImmediate();
                return;
            }

            if (Keyboard.current != null && (Keyboard.current.tabKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame))
            {
                ToggleNotebook();
            }
        }

        public void AddObjective(string id, string title, string description)
        {
            var existing = objectives.Find(o => o.id == id);
            if (existing == null)
            {
                objectives.Add(new NotebookObjective { id = id, title = title, description = description, isCompleted = false, progress = "" });
                RefreshUI();
                ShowNotebookUpdateNotification();
            }
            else
            {
                existing.title = title;
                existing.description = description;
                RefreshUI();
            }
        }

        public void UpdateObjectiveProgress(string id, string progress)
        {
            var obj = objectives.Find(o => o.id == id);
            if (obj != null)
            {
                obj.progress = progress;
                RefreshUI();
                ShowNotebookUpdateNotification();
            }
        }

        public void CompleteObjective(string id)
        {
            var obj = objectives.Find(o => o.id == id);
            if (obj != null && !obj.isCompleted)
            {
                obj.isCompleted = true;
                RefreshUI();
                ShowNotebookUpdateNotification();
            }
        }

        public void RemoveObjective(string id)
        {
            objectives.RemoveAll(o => o.id == id);
            RefreshUI();
        }

        public void AddClue(string id, string text)
        {
            if (clues.Find(c => c.id == id) == null)
            {
                clues.Add(new NotebookClue { id = id, text = text, timestamp = DateTime.Now.ToString("HH:mm") });
                RefreshUI();
                ShowNotebookUpdateNotification();
            }
        }

        private void RefreshUI()
        {
            if (objectivesText == null || cluesText == null) return;

            // Trang Trái: Mục tiêu
            string objContent = "";
            foreach (var obj in objectives)
            {
                if (obj.isCompleted)
                {
                    objContent += $"<color=#707070><s>[✓] {obj.title}</s></color>\n";
                }
                else
                {
                    objContent += $"<color=#8B2500><b>[  ] {obj.title}</b></color>\n";
                    if (!string.IsNullOrEmpty(obj.description))
                    {
                        objContent += $"     <color=#3A3228><i>{obj.description}</i></color>\n";
                    }
                    if (!string.IsNullOrEmpty(obj.progress))
                    {
                        objContent += $"     <color=#C85A17><b>Tiến độ: {obj.progress}</b></color>\n";
                    }
                }
                objContent += "\n";
            }
            objectivesText.text = objContent;

            // Trang Phải: Manh mối & nhật ký
            string clueContent = "";
            for (int i = 0; i < clues.Count; i++)
            {
                var clue = clues[i];
                clueContent += $"<color=#6A5A4A><size=14>[{clue.timestamp}]</size></color> ";
                clueContent += $"<color=#2B221B><i>\"{clue.text}\"</i></color>\n\n";
            }
            cluesText.text = clueContent;
        }

        public void ToggleNotebook()
        {
            if (isAnimating) return;
            isOpen = !isOpen;
            StartCoroutine(AnimateNotebook(isOpen));
        }

        private void CloseNotebookImmediate()
        {
            isOpen = false;
            isAnimating = false;
            StopAllCoroutines();
            if (notebookContainer != null) notebookContainer.SetActive(false);
            var overlay = canvasObject != null ? canvasObject.transform.Find("NotebookOverlay") : null;
            if (overlay != null) overlay.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        private IEnumerator AnimateNotebook(bool open)
        {
            isAnimating = true;

            var overlay = canvasObject.transform.Find("NotebookOverlay");
            if (overlay != null) overlay.gameObject.SetActive(open);

            if (open)
            {
                notebookContainer.SetActive(true);
                RefreshUI();
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                PlayPageSound();
            }
            else
            {
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                PlayPageSound();
            }

            float duration = 0.28f;
            float elapsed = 0f;
            float startY = open ? -900f : 0f;
            float endY = open ? 0f : -900f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Mượt mà dạng EaseOutBack nhẹ
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                notebookRect.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, t));
                yield return null;
            }

            notebookRect.anchoredPosition = new Vector2(0, endY);

            if (!open)
            {
                notebookContainer.SetActive(false);
            }

            isAnimating = false;
        }

        private void PlayPageSound()
        {
            if (audioSource == null) return;
#if UNITY_EDITOR
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI/TestamentMenu/Audio/SFX_Paper_Tear.wav");
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 0.65f);
                return;
            }
#endif
            // Fallback âm thanh xột xoạt giấy
            AudioClip proceduralPaper = GeneratePaperSound();
            if (proceduralPaper != null) audioSource.PlayOneShot(proceduralPaper, 0.45f);
        }

        private AudioClip GeneratePaperSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.08f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                samples[i] = noise * Mathf.Exp(-t * 18f);
            }
            AudioClip clip = AudioClip.Create("PaperFlip", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void ShowNotebookUpdateNotification()
        {
            // Đã tắt thông báo "ĐÃ CẬP NHẬT SỔ GHI CHÚ" theo yêu cầu của người chơi
            return;
        }

        private IEnumerator ToastRoutine()
        {
            toastContainer.SetActive(true);

            // Fade in
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.25f);
                yield return null;
            }
            toastCanvasGroup.alpha = 1f;

            // Wait
            yield return new WaitForSecondsRealtime(2.5f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.unscaledDeltaTime;
                toastCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.35f);
                yield return null;
            }
            toastCanvasGroup.alpha = 0f;
            toastContainer.SetActive(false);
        }
    }
}
