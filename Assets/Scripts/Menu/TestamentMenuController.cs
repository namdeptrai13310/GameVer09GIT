using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestamentMenuController : MonoBehaviour
{
    public static TestamentMenuController Instance { get; private set; }

    [Header("Menu State")]
    public bool isMenuOpen = true;
    public bool startOnAwake = true;

    [Header("UI Panels")]
    public GameObject menuRoot;
    public Canvas menuCanvas;
    public CanvasGroup menuCanvasGroup;
    public RectTransform testamentDocument;
    public GameObject tornContainer;
    public RectTransform tornTop;
    public RectTransform tornBottom;

    [Header("Player Name & Signature Customization")]
    public InputField playerNameInput;
    public Text bodyText;
    public Text signatureCustomText;
    public Image signatureCustomUnderline;
    public GameObject nameEditHint;
    public Image anSignatureImg;
    public Image stampCertifiedImg;
    public string currentPlayerName = "Nguyễn An";

    [Header("Buttons")]
    public CanvasGroup buttonsGroup;
    public Button btnSignAccept;
    public Button btnTerms;
    public Button btnTearQuit;

    [Header("Terms / Settings Modal")]
    public GameObject termsModal;
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider ambientVolumeSlider;
    public Toggle fullscreenToggle;
    public Button btnCloseTerms;

    [Header("Graphics Quality Buttons (Độ Nét)")]
    public Button[] qualityButtons;
    public Image[] qualityButtonImages;
    public Text[] qualityButtonTexts;
    public int currentQualityLevel = 2; // 0=Thấp, 1=Vừa, 2=Cao, 3=Siêu Nét
    private readonly string[] qualityNames = new string[] { "Thấp", "Vừa", "Cao", "Siêu Nét" };

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip clipPenSign;
    public AudioClip clipPaperTear;
    public AudioClip clipPaperHover;
    public AudioClip clipStampThud;
    public AudioClip clipAmbienceBGM;

    [Header("Gameplay & HUD References")]
    public PlayerController playerController;
    public GameObject staminaUI;
    public GameObject hotbarUI;
    public GameObject crosshairDot;
    public GameObject doorUI;

    [Header("Screen Fade")]
    public Image screenFadeOverlay;

    private bool isTransitioning = false;
    private float currentMasterVol = 1f;
    private float currentSfxVol = 1f;
    private float currentAmbientVol = 0.8f;

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

        // Initialize audio sources if missing
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Dedicated Canvas sorting order 500
        if (menuCanvas == null)
        {
            menuCanvas = GetComponent<Canvas>();
            if (menuCanvas == null) menuCanvas = gameObject.AddComponent<Canvas>();
        }
        menuCanvas.overrideSorting = true;
        menuCanvas.sortingOrder = 500;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (clipAmbienceBGM == null)
        {
            clipAmbienceBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/BGM_Testament_Peaceful.wav");
        }
#endif
        LoadSavedSettings();
        LoadSavedPlayerName();
        SetupButtonListeners();

        if (startOnAwake)
        {
            OpenTestamentMenu();
        }
        else
        {
            CloseMenuInstant();
        }
    }

    private void Update()
    {
        if (isMenuOpen)
        {
            EnsureHUDHidden();
        }

        // ESC key toggle or back out of Terms modal
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (termsModal != null && termsModal.activeSelf)
            {
                CloseTermsModal();
            }
            else if (!isTransitioning)
            {
                if (!isMenuOpen)
                {
                    OpenTestamentMenu();
                }
            }
        }
    }

    // ========================================================
    // PLAYER NAME CUSTOMIZATION
    // ========================================================

    private void LoadSavedPlayerName()
    {
        currentPlayerName = PlayerPrefs.GetString("PlayerName", "Nguyễn An");
        if (string.IsNullOrWhiteSpace(currentPlayerName))
        {
            currentPlayerName = "Nguyễn An";
        }

        if (playerNameInput != null)
        {
            playerNameInput.text = currentPlayerName;
            playerNameInput.onValueChanged.RemoveAllListeners();
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
        }

        UpdateTestamentBodyText();
        UpdateSignatureDisplay();
    }

    public void OnPlayerNameChanged(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            currentPlayerName = "Nguyễn An";
        }
        else
        {
            currentPlayerName = newName.Trim();
        }

        PlayerPrefs.SetString("PlayerName", currentPlayerName);
        UpdateTestamentBodyText();
        UpdateSignatureDisplay();
    }

    public void UpdateTestamentBodyText()
    {
        if (bodyText == null) return;

        string nameUpper = string.IsNullOrWhiteSpace(currentPlayerName) ? "NGUYỄN AN" : currentPlayerName.ToUpper();

        bodyText.text =
            "Tôi là: TRẦN VIẾT NGHIÊM (Sinh năm 1952).\n" +
            "Nay tuổi cao sức yếu, biết ngày ra đi không còn xa. Trong lúc tinh thần còn hoàn toàn minh mẫn, tôi lập bản di chúc này để lại toàn bộ quyền sở hữu cụm bất động sản gồm 02 CĂN BIỆT THỰ ĐỘC LẬP tại vùng ngoại ô đồi thông cho người con gái ruột thất lạc năm xưa:\n\n" +
            $"• NGƯỜI THỪA KẾ DUY NHẤT: {nameUpper} (Sinh năm 2002).\n\n" +
            "Bao năm qua cha chưa từng một lần hoàn thành trách nhiệm, đây là tất cả những gì người cha này có thể để lại bù đắp cho con.\n\n" +
            "ĐIỀU KIỆN TIẾP QUẢN:\n" +
            "Căn biệt thự đã bị bỏ hoang lâu ngày, sàn nhà nhiều nơi mục nát và bừa bộn. Con phải đích thân đến dọn dẹp, tìm gỗ và dụng cụ để gia cố lại sàn nhà trước khi được công nhận quyền sở hữu chính thức.\n\n" +
            "Nếu con đồng ý đón nhận, hãy ký tên xác nhận vào bản cam kết thừa kế này.";
    }

    private void UpdateSignatureDisplay()
    {
        bool isDefaultName = string.Equals(currentPlayerName, "Nguyễn An", StringComparison.OrdinalIgnoreCase);

        if (isDefaultName)
        {
            if (anSignatureImg != null) anSignatureImg.gameObject.SetActive(true);
            if (signatureCustomText != null) signatureCustomText.gameObject.SetActive(false);
            if (signatureCustomUnderline != null) signatureCustomUnderline.gameObject.SetActive(false);
        }
        else
        {
            if (anSignatureImg != null) anSignatureImg.gameObject.SetActive(false);
            if (signatureCustomText != null)
            {
                signatureCustomText.gameObject.SetActive(true);
                signatureCustomText.text = currentPlayerName;
            }
            if (signatureCustomUnderline != null) signatureCustomUnderline.gameObject.SetActive(true);
        }
    }

    // ========================================================
    // SETUP & LISTENERS
    // ========================================================

    private void SetupButtonListeners()
    {
        if (btnSignAccept != null)
        {
            btnSignAccept.onClick.RemoveAllListeners();
            btnSignAccept.onClick.AddListener(OnSignAcceptClicked);
        }

        if (btnTerms != null)
        {
            btnTerms.onClick.RemoveAllListeners();
            btnTerms.onClick.AddListener(OnTermsClicked);
        }

        if (btnTearQuit != null)
        {
            btnTearQuit.onClick.RemoveAllListeners();
            btnTearQuit.onClick.AddListener(OnTearQuitClicked);
        }

        if (btnCloseTerms != null)
        {
            btnCloseTerms.onClick.RemoveAllListeners();
            btnCloseTerms.onClick.AddListener(CloseTermsModal);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.onValueChanged.RemoveAllListeners();
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        if (qualityButtons != null)
        {
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] != null)
                {
                    qualityButtons[i].transition = Selectable.Transition.None;
                    int index = i;
                    qualityButtons[i].onClick.RemoveAllListeners();
                    qualityButtons[i].onClick.AddListener(() => SetGraphicsQuality(index));
                }
            }
        }
    }

    private void LoadSavedSettings()
    {
        currentMasterVol = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        currentSfxVol = PlayerPrefs.GetFloat("SfxVolume", 1.0f);
        currentAmbientVol = PlayerPrefs.GetFloat("AmbientVolume", 0.8f);
        currentQualityLevel = PlayerPrefs.GetInt("GraphicsQuality", 2); // Default Cao

        AudioListener.volume = currentMasterVol;

        if (masterVolumeSlider != null) masterVolumeSlider.value = currentMasterVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSfxVol;
        if (ambientVolumeSlider != null) ambientVolumeSlider.value = currentAmbientVol;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }

        if (bgmSource != null) bgmSource.volume = currentAmbientVol;
        if (sfxSource != null) sfxSource.volume = currentSfxVol;

        ApplyGraphicsQuality(currentQualityLevel);
        UpdateQualityButtonsVisual();
    }

    // ========================================================
    // MENU OPEN / CLOSE
    // ========================================================

    public void OpenTestamentMenu()
    {
        isMenuOpen = true;
        isTransitioning = false;

        if (menuRoot != null) menuRoot.SetActive(true);
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        if (testamentDocument != null) testamentDocument.gameObject.SetActive(true);
        if (tornContainer != null) tornContainer.SetActive(false);
        if (termsModal != null) termsModal.SetActive(false);
        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 1f;
            buttonsGroup.interactable = true;
        }

        // Reset signature and stamp state
        if (anSignatureImg != null)
        {
            Color c = anSignatureImg.color;
            c.a = 0f;
            anSignatureImg.color = c;
            anSignatureImg.fillAmount = 0f;
        }
        if (signatureCustomText != null)
        {
            Color c = signatureCustomText.color;
            c.a = 0f;
            signatureCustomText.color = c;
        }
        if (signatureCustomUnderline != null)
        {
            signatureCustomUnderline.fillAmount = 0f;
        }

        if (stampCertifiedImg != null)
        {
            Color c = stampCertifiedImg.color;
            c.a = 0f;
            stampCertifiedImg.color = c;
            stampCertifiedImg.transform.localScale = Vector3.one * 1.5f;
        }

        // Play Ambience
        if (bgmSource != null && clipAmbienceBGM != null)
        {
            bgmSource.clip = clipAmbienceBGM;
            bgmSource.Play();
        }

        SetPlayerControl(false);
        EnsureHUDHidden();
    }

    public void CloseMenuInstant()
    {
        isMenuOpen = false;
        if (menuRoot != null) menuRoot.SetActive(false);

        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }

        SetPlayerControl(true);
        RestoreHUD();
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null)
            {
                var p = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
                if (p != null) playerController = p.GetComponent<PlayerController>();
            }
        }

        if (playerController != null)
        {
            if (!playerController.gameObject.activeSelf)
                playerController.gameObject.SetActive(true);

            playerController.enabled = enabled;
        }

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private GameObject FindCanvasChild(string childName)
    {
        Canvas mainCanvas = null;
        if (transform.parent != null) mainCanvas = transform.parent.GetComponent<Canvas>();
        if (mainCanvas == null) mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            Transform t = mainCanvas.transform.Find(childName);
            if (t != null) return t.gameObject;
        }
        return GameObject.Find(childName);
    }

    public void EnsureHUDHidden()
    {
        if (staminaUI == null) staminaUI = FindCanvasChild("StaminaUI");
        if (staminaUI != null && staminaUI.activeSelf) staminaUI.SetActive(false);

        if (hotbarUI == null) hotbarUI = FindCanvasChild("Hotbar_QuickSlots");
        if (hotbarUI != null && hotbarUI.activeSelf) hotbarUI.SetActive(false);

        if (crosshairDot == null) crosshairDot = FindCanvasChild("CrosshairDot");
        if (crosshairDot != null && crosshairDot.activeSelf) crosshairDot.SetActive(false);

        if (doorUI == null) doorUI = FindCanvasChild("PubgDoorUI");
        if (doorUI != null && doorUI.activeSelf) doorUI.SetActive(false);
    }

    public void RestoreHUD()
    {
        if (staminaUI == null) staminaUI = FindCanvasChild("StaminaUI");
        if (staminaUI != null) staminaUI.SetActive(true);

        if (hotbarUI == null) hotbarUI = FindCanvasChild("Hotbar_QuickSlots");
        if (hotbarUI != null) hotbarUI.SetActive(true);

        if (crosshairDot == null) crosshairDot = FindCanvasChild("CrosshairDot");
        if (crosshairDot != null) crosshairDot.SetActive(true);

        if (doorUI == null) doorUI = FindCanvasChild("PubgDoorUI");
        if (doorUI != null) doorUI.SetActive(true);
    }

    // ========================================================
    // BUTTON INTERACTIONS
    // ========================================================

    public void OnSignAcceptClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(RoutineSignAndAccept());
    }

    private IEnumerator RoutineSignAndAccept()
    {
        isTransitioning = true;

        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
        }

        if (playerNameInput != null)
        {
            playerNameInput.interactable = false;
        }
        if (nameEditHint != null)
        {
            nameEditHint.SetActive(false);
        }

        // 1. Play Pen Sign Sound
        PlaySFX(clipPenSign);

        // 2. Animate Signature
        bool isDefaultName = string.Equals(currentPlayerName, "Nguyễn An", StringComparison.OrdinalIgnoreCase);
        float signDuration = 1.8f;
        float elapsed = 0f;

        if (isDefaultName && anSignatureImg != null)
        {
            Color c = anSignatureImg.color;
            c.a = 1f;
            anSignatureImg.color = c;

            while (elapsed < signDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / signDuration);
                anSignatureImg.fillAmount = progress;
                yield return null;
            }
            anSignatureImg.fillAmount = 1f;
        }
        else
        {
            if (signatureCustomText != null)
            {
                signatureCustomText.text = currentPlayerName;
                signatureCustomText.gameObject.SetActive(true);
            }
            if (signatureCustomUnderline != null)
            {
                signatureCustomUnderline.gameObject.SetActive(true);
            }

            while (elapsed < signDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / signDuration);

                if (signatureCustomText != null)
                {
                    Color tc = signatureCustomText.color;
                    tc.a = t;
                    signatureCustomText.color = tc;
                }

                if (signatureCustomUnderline != null)
                {
                    signatureCustomUnderline.fillAmount = t;
                }

                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.25f);

        // 3. Stamp "CHỨNG THỰC / ĐÃ KÝ" Thud
        PlaySFX(clipStampThud);
        if (stampCertifiedImg != null)
        {
            stampCertifiedImg.gameObject.SetActive(true);
            Color sc = stampCertifiedImg.color;
            sc.a = 1f;
            stampCertifiedImg.color = sc;

            float stampDuration = 0.25f;
            float sElapsed = 0f;
            while (sElapsed < stampDuration)
            {
                sElapsed += Time.unscaledDeltaTime;
                float t = sElapsed / stampDuration;
                float scale = Mathf.Lerp(1.6f, 1.0f, t);
                stampCertifiedImg.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            stampCertifiedImg.transform.localScale = Vector3.one;
        }

        yield return new WaitForSecondsRealtime(0.8f);

        // 4. Smooth Fade Screen to Black
        if (screenFadeOverlay != null)
        {
            screenFadeOverlay.gameObject.SetActive(true);
            float fadeDur = 1.0f;
            float fElapsed = 0f;
            while (fElapsed < fadeDur)
            {
                fElapsed += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(fElapsed / fadeDur);
                screenFadeOverlay.color = new Color(0, 0, 0, a);
                if (bgmSource != null) bgmSource.volume = Mathf.Lerp(currentAmbientVol, 0f, a);
                yield return null;
            }
        }

        // Hide Menu UI
        isMenuOpen = false;
        if (menuRoot != null) menuRoot.SetActive(false);
        if (bgmSource != null) bgmSource.Stop();

        // 5. Trigger Cinematic Intro Cutscene or direct start
        if (HorrorGame.Story.CinematicIntroManager.Instance != null)
        {
            if (screenFadeOverlay != null) screenFadeOverlay.gameObject.SetActive(false);
            HorrorGame.Story.CinematicIntroManager.Instance.PlayIntroCutscene(() => OnIntroFinished());
        }
        else
        {
            OnIntroFinished();
        }
    }

    private void OnIntroFinished()
    {
        SetPlayerControl(true);
        RestoreHUD();

        // Fade screen from black to clear if overlay is still active
        if (screenFadeOverlay != null && screenFadeOverlay.gameObject.activeSelf)
        {
            StartCoroutine(RoutineUnfadeScreen());
        }
        else
        {
            isTransitioning = false;
        }

        Debug.Log($"[TestamentMenu] '{currentPlayerName}' đã nhận nhà thành công! Bắt đầu khám phá.");
    }

    private IEnumerator RoutineUnfadeScreen()
    {
        float unfadeDur = 0.8f;
        float ufElapsed = 0f;
        while (ufElapsed < unfadeDur)
        {
            ufElapsed += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(ufElapsed / unfadeDur);
            screenFadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }
        screenFadeOverlay.gameObject.SetActive(false);
        isTransitioning = false;
    }

    public void OnTearQuitClicked()
    {
        if (isTransitioning) return;
        StartCoroutine(RoutineTearAndQuit());
    }

    private IEnumerator RoutineTearAndQuit()
    {
        isTransitioning = true;

        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
        }

        PlaySFX(clipPaperTear);

        if (testamentDocument != null) testamentDocument.gameObject.SetActive(false);
        if (tornContainer != null) tornContainer.SetActive(true);

        float tearDuration = 1.2f;
        float elapsed = 0f;

        Vector2 topStartPos = tornTop != null ? tornTop.anchoredPosition : Vector2.zero;
        Vector2 botStartPos = tornBottom != null ? tornBottom.anchoredPosition : Vector2.zero;

        Vector2 topTargetPos = topStartPos + new Vector2(-45f, 60f);
        Vector2 botTargetPos = botStartPos + new Vector2(55f, -90f);

        while (elapsed < tearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / tearDuration);
            float smoothT = Mathf.SmoothStep(0, 1, t);

            if (tornTop != null)
            {
                tornTop.anchoredPosition = Vector2.Lerp(topStartPos, topTargetPos, smoothT);
                tornTop.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, 6.5f, smoothT));
            }
            if (tornBottom != null)
            {
                tornBottom.anchoredPosition = Vector2.Lerp(botStartPos, botTargetPos, smoothT);
                tornBottom.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -8.5f, smoothT));
            }

            if (menuCanvasGroup != null && t > 0.4f)
            {
                menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.3f);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnTermsClicked()
    {
        PlaySFX(clipPaperHover);
        if (termsModal != null)
        {
            termsModal.SetActive(true);
            UpdateQualityButtonsVisual();
        }
    }

    public void CloseTermsModal()
    {
        PlaySFX(clipPaperHover);
        if (termsModal != null)
        {
            termsModal.SetActive(false);
        }
        PlayerPrefs.Save();
    }

    // ========================================================
    // SETTINGS HANDLERS
    // ========================================================

    private void OnMasterVolumeChanged(float val)
    {
        currentMasterVol = val;
        AudioListener.volume = val;
        PlayerPrefs.SetFloat("MasterVolume", val);
    }

    private void OnSfxVolumeChanged(float val)
    {
        currentSfxVol = val;
        if (sfxSource != null) sfxSource.volume = val;
        PlayerPrefs.SetFloat("SfxVolume", val);
        PlaySFX(clipPaperHover); // Audio feedback
    }

    private void OnAmbientVolumeChanged(float val)
    {
        currentAmbientVol = val;
        if (bgmSource != null) bgmSource.volume = val;
        PlayerPrefs.SetFloat("AmbientVolume", val);
    }

    public void SetGraphicsQuality(int level)
    {
        currentQualityLevel = Mathf.Clamp(level, 0, 3);
        PlayerPrefs.SetInt("GraphicsQuality", currentQualityLevel);
        ApplyGraphicsQuality(currentQualityLevel);
        UpdateQualityButtonsVisual();
        PlaySFX(clipPaperHover);
    }

    public void ApplyGraphicsQuality(int level)
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
            urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

        switch (level)
        {
            case 0: // Thấp
                if (urpAsset != null)
                {
                    urpAsset.renderScale = 0.65f;
                    urpAsset.shadowDistance = 20f;
                    urpAsset.msaaSampleCount = 1;
                }
                QualitySettings.shadowDistance = 20f;
                QualitySettings.globalTextureMipmapLimit = 1;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                break;
            case 1: // Vừa
                if (urpAsset != null)
                {
                    urpAsset.renderScale = 0.85f;
                    urpAsset.shadowDistance = 40f;
                    urpAsset.msaaSampleCount = 2;
                }
                QualitySettings.shadowDistance = 40f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                break;
            case 2: // Cao
                if (urpAsset != null)
                {
                    urpAsset.renderScale = 1.0f;
                    urpAsset.shadowDistance = 65f;
                    urpAsset.msaaSampleCount = 4;
                }
                QualitySettings.shadowDistance = 65f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                break;
            case 3: // Siêu Nét
                if (urpAsset != null)
                {
                    urpAsset.renderScale = 1.30f;
                    urpAsset.shadowDistance = 100f;
                    urpAsset.msaaSampleCount = 8;
                }
                QualitySettings.shadowDistance = 100f;
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                break;
        }
        Debug.Log($"[Settings] Thiết lập đồ họa: {qualityNames[level]} (renderScale: {(urpAsset != null ? urpAsset.renderScale : 1f)})");
    }

    public void UpdateQualityButtonsVisual()
    {
        if (qualityButtonImages == null || qualityButtonTexts == null) return;

        for (int i = 0; i < qualityButtonImages.Length; i++)
        {
            if (qualityButtons != null && i < qualityButtons.Length && qualityButtons[i] != null)
            {
                qualityButtons[i].transition = Selectable.Transition.None;
            }

            if (qualityButtonImages[i] == null) continue;

            bool isSelected = (i == currentQualityLevel);
            if (isSelected)
            {
                // Radiant Amber Gold background with active checkmark
                qualityButtonImages[i].color = new Color(0.88f, 0.62f, 0.18f, 0.95f);
                if (i < qualityButtonTexts.Length && qualityButtonTexts[i] != null)
                {
                    qualityButtonTexts[i].color = new Color(0.12f, 0.08f, 0.04f, 1f);
                    qualityButtonTexts[i].text = "✓ " + qualityNames[i];
                    qualityButtonTexts[i].fontStyle = FontStyle.Bold;
                }
            }
            else
            {
                // Unselected dark parchment/bronze
                qualityButtonImages[i].color = new Color(0.25f, 0.16f, 0.11f, 0.80f);
                if (i < qualityButtonTexts.Length && qualityButtonTexts[i] != null)
                {
                    qualityButtonTexts[i].color = new Color(0.88f, 0.82f, 0.74f, 0.90f);
                    qualityButtonTexts[i].text = qualityNames[i];
                    qualityButtonTexts[i].fontStyle = FontStyle.Normal;
                }
            }
        }
    }

    private void OnFullscreenToggled(bool isFull)
    {
        Screen.fullScreen = isFull;
        Screen.fullScreenMode = isFull ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);
        PlaySFX(clipPaperHover);
    }

    public void PlayHoverSound()
    {
        PlaySFX(clipPaperHover);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, currentSfxVol);
        }
    }
}
