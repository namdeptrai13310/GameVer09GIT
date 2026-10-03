using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Camera & Perspective Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 15f;
    [Tooltip("Vị trí camera góc nhìn thứ nhất (FPS)")]
    public Vector3 firstPersonCamOffset = new Vector3(0f, 1.819f, 0.254f);

    [Header("Movement Settings")]
    public float walkSpeed = 3.2f;
    public float runSpeed = 6.2f;
    [Tooltip("Tốc độ khi kiệt sức - Chậm hơn cả đi bộ bình thường")]
    public float exhaustedSpeed = 1.5f;
    [Tooltip("Tốc độ khi bê thùng phế liệu/đồ nặng")]
    public float carryingClutterWalkSpeed = 2.4f;
    public float gravity = -18f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate = 18f; // Tiêu hao 18/giây (~5.5 giây chạy nước rút liên tục)
    public float regenRateStanding = 16f; // Hồi phục nhanh khi đứng nghỉ (~6 giây đầy bình)
    public float regenRateWalking = 9f; // Hồi phục chậm hơn khi vừa đi vừa thở
    public float regenDelay = 1.2f; // Thời gian chờ hồi phục sau khi ngừng chạy
    public float exhaustedRegenDelay = 2.2f; // Thời gian chờ lâu hơn khi bị kiệt sức hoàn toàn
    public float recoveryThreshold = 30f; // Cần hồi tối thiểu 30% thể lực mới được phép chạy tiếp

    [Header("Exhaustion State")]
    public bool isExhausted = false;
    public bool isRunning = false;
    private float staminaRegenCooldown = 0f;

    [Header("Breathing & Heartbeat Audio")]
    public AudioSource breathingAudioSource;
    public AudioClip breathingClip;
    [Range(0f, 1f)] public float maxBreathingVolume = 0.85f;

    [Header("Animation")]
    public Animator animator;

    [Header("Stamina UI")]
    public CanvasGroup staminaCanvas;
    public Image staminaFillBar;
    public Outline borderOutline;
    public Color normalBorderColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public Color normalFillColor = new Color(0.95f, 0.95f, 0.92f, 0.95f);
    public Color recoveringColor = new Color(1.0f, 0.45f, 0.12f, 0.95f); // Cam đỏ cảnh báo
    public Color blinkColor = Color.red; // Đỏ máu khi kiệt sức

    private CharacterController controller;
    private float currentStamina;
    private float verticalVelocity;
    private float xRotation = 0f;

    [Header("Performance")]
    public int targetFPS = 60;

    [Header("Step Climbing (Tự động bước qua gờ/ngưỡng thấp)")]
    public float maxStepHeight = 0.45f;
    public float stepSmoothness = 6f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.stepOffset = maxStepHeight;
            controller.slopeLimit = 60f;
        }

        currentStamina = maxStamina;
        isExhausted = false;
        isRunning = false;

        Application.targetFrameRate = targetFPS;

        if (TestamentMenuController.Instance == null || !TestamentMenuController.Instance.isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetupBreathingAudio();

        Transform girl = transform.Find("civilian_girl");
        if (girl != null)
        {
            girl.gameObject.SetActive(true);
            if (animator == null) animator = girl.GetComponent<Animator>();
        }
    }

    private void SetupBreathingAudio()
    {
        if (breathingAudioSource == null)
        {
            breathingAudioSource = gameObject.AddComponent<AudioSource>();
            breathingAudioSource.playOnAwake = false;
            breathingAudioSource.loop = true;
            breathingAudioSource.spatialBlend = 0f; // 2D âm thanh nội tâm người chơi
            breathingAudioSource.volume = 0f;
        }

        if (breathingClip == null)
        {
#if UNITY_EDITOR
            breathingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Human_Heavy_Breathing.wav");
            if (breathingClip == null)
                breathingClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Exhausted_Breathing.wav");
#endif
        }

        if (breathingClip != null)
        {
            breathingAudioSource.clip = breathingClip;
            breathingAudioSource.Play();
        }
    }

    void Update()
    {
        HandleCameraLook();
        HandleMovement();
        UpdateBreathingAudio();
        UpdateStaminaUI();
    }

    void HandleCameraLook()
    {
        Vector2 mouseDelta = (Mouse.current != null) ? Mouse.current.delta.ReadValue() * mouseSensitivity * Time.deltaTime : Vector2.zero;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.Rotate(Vector3.up * mouseDelta.x);

        // Hiệu ứng thở dốc mệt mỏi rung giật góc nhìn khi kiệt sức
        float exhaustedPitchBob = 0f;
        float exhaustedRollBob = 0f;
        if (isExhausted)
        {
            exhaustedPitchBob = Mathf.Sin(Time.time * 3.5f) * 0.45f;
            exhaustedRollBob = Mathf.Cos(Time.time * 1.75f) * 0.35f;
        }

        if (playerCamera != null)
        {
            playerCamera.localPosition = firstPersonCamOffset;
            playerCamera.localRotation = Quaternion.Euler(xRotation + exhaustedPitchBob, 0f, exhaustedRollBob);
        }
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float y = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * y;
        bool isMoving = move.sqrMagnitude > 0.01f;

        // Kiểm tra xem người chơi có đang bê thùng rác cồng kềnh không
        bool isCarryingHeavyObject = (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null && 
                                      HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.isCarryingClutter);

        bool wantsToRun = Keyboard.current.leftShiftKey.isPressed && isMoving && !isCarryingHeavyObject;

        // Nếu đang kiệt sức (isExhausted), mở lại chạy khi thể lực vượt ngưỡng recoveryThreshold
        if (isExhausted)
        {
            if (currentStamina >= recoveryThreshold)
            {
                isExhausted = false;
            }
        }

        // Xử lý tiêu hao và hồi phục thể lực
        isRunning = false;
        if (wantsToRun && !isExhausted && currentStamina > 0f)
        {
            currentStamina -= drainRate * Time.deltaTime;
            staminaRegenCooldown = regenDelay;
            isRunning = true;

            // Nếu thể lực chạm đáy 0 -> Rơi vào trạng thái kiệt sức
            if (currentStamina <= 0.05f)
            {
                currentStamina = 0f;
                isExhausted = true;
                staminaRegenCooldown = exhaustedRegenDelay;
                isRunning = false;
            }
        }
        else
        {
            // Đếm ngược thời gian chờ hồi phục
            if (staminaRegenCooldown > 0f)
            {
                staminaRegenCooldown -= Time.deltaTime;
            }
            else if (currentStamina < maxStamina)
            {
                float currentRegenRate = isMoving ? regenRateWalking : regenRateStanding;
                currentStamina += currentRegenRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
        }

        // Xác định tốc độ di chuyển hiện tại
        float currentSpeed;
        if (isCarryingHeavyObject)
        {
            currentSpeed = carryingClutterWalkSpeed; // Bê đồ nặng: 2.4 m/s
        }
        else if (isExhausted)
        {
            currentSpeed = exhaustedSpeed; // Kiệt sức: 1.5 m/s (chậm hơn cả đi bộ bình thường)
        }
        else if (isRunning)
        {
            currentSpeed = runSpeed; // Chạy nước rút: 6.2 m/s
        }
        else
        {
            currentSpeed = walkSpeed; // Đi bộ bình thường: 3.2 m/s
        }

        Vector3 moveDir = move.normalized;

        // 1. Tự động hỗ trợ nhấc chân bước qua các gờ/ngưỡng cửa/bậc thềm thấp
        if (moveDir.sqrMagnitude > 0.01f)
        {
            HandleStepClimb(moveDir);
        }

        // 2. Di chuyển ngang
        Vector3 horizontalMove = moveDir * currentSpeed * Time.deltaTime;
        controller.Move(horizontalMove);

        // 3. Trọng lực & Di chuyển dọc
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -4f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        Vector3 targetVelocity = moveDir * currentSpeed;
        if (animator != null)
        {
            animator.SetFloat("Speed", targetVelocity.magnitude);
        }

        if (HorrorGame.Player.PlayerHandEquipment.Instance != null)
        {
            HorrorGame.Player.PlayerHandEquipment.Instance.SetExhausted(isExhausted);
            HorrorGame.Player.PlayerHandEquipment.Instance.SetCarrying(isCarryingHeavyObject);
        }
    }

    void HandleStepClimb(Vector3 moveDir)
    {
        Vector3 footPos = transform.position + Vector3.up * 0.08f;
        float checkDist = controller.radius + 0.25f;

        if (Physics.Raycast(footPos, moveDir, out RaycastHit lowerHit, checkDist, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 upperCheckPos = footPos + Vector3.up * (maxStepHeight + 0.05f);
            if (!Physics.Raycast(upperCheckPos, moveDir, checkDist + 0.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 downRayPos = footPos + moveDir * (controller.radius + 0.18f) + Vector3.up * (maxStepHeight + 0.05f);
                if (Physics.Raycast(downRayPos, Vector3.down, out RaycastHit stepSurfaceHit, maxStepHeight + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    float stepHeight = stepSurfaceHit.point.y - transform.position.y;
                    if (stepHeight > 0.02f && stepHeight <= maxStepHeight && stepSurfaceHit.normal.y > 0.55f)
                    {
                        controller.Move(Vector3.up * (stepHeight * 1.5f) * Time.deltaTime * stepSmoothness);
                    }
                }
            }
        }
    }

    private void UpdateBreathingAudio()
    {
        if (breathingAudioSource == null) return;

        float targetVol = 0f;
        float pct = currentStamina / maxStamina;

        if (isExhausted)
        {
            targetVol = maxBreathingVolume; // Thở dốc hụt hơi tối đa khi kiệt sức
        }
        else if (pct < (recoveryThreshold / maxStamina))
        {
            float f = 1f - (pct / (recoveryThreshold / maxStamina));
            targetVol = f * (maxBreathingVolume * 0.65f);
        }
        else if (pct < 0.5f)
        {
            float f = 1f - ((pct - 0.3f) / 0.2f);
            targetVol = Mathf.Clamp01(f) * (maxBreathingVolume * 0.25f);
        }

        breathingAudioSource.volume = Mathf.MoveTowards(breathingAudioSource.volume, targetVol, Time.deltaTime * 1.6f);
    }

    void UpdateStaminaUI()
    {
        if (staminaFillBar == null || staminaCanvas == null) return;

        float pct = Mathf.Clamp01(currentStamina / maxStamina);

        // Thu gọn ruột thanh thể lực
        staminaFillBar.rectTransform.localScale = new Vector3(pct, 1f, 1f);

        // Tự động mờ ẩn khi thể lực đầy 100% và hiện lên khi chạy hoặc kiệt sức
        bool shouldShow = (pct < 0.99f || isExhausted || isRunning);
        float targetAlpha = shouldShow ? 1f : 0f;
        staminaCanvas.alpha = Mathf.MoveTowards(staminaCanvas.alpha, targetAlpha, Time.deltaTime * (shouldShow ? 5f : 1.8f));

        // Màu sắc cảnh báo chuẩn game kinh dị
        if (isExhausted)
        {
            Color pulseRed = Color.Lerp(Color.red, new Color(0.45f, 0.05f, 0.05f), Mathf.PingPong(Time.time * 7f, 1f));
            staminaFillBar.color = pulseRed;
            if (borderOutline != null)
            {
                borderOutline.effectColor = pulseRed;
            }
        }
        else if (currentStamina < recoveryThreshold)
        {
            staminaFillBar.color = recoveringColor;
            if (borderOutline != null)
            {
                borderOutline.effectColor = Color.Lerp(normalBorderColor, recoveringColor, 0.6f);
            }
        }
        else
        {
            staminaFillBar.color = normalFillColor;
            if (borderOutline != null)
            {
                borderOutline.effectColor = normalBorderColor;
            }
        }
    }

    public void SetLookDirection(Vector3 lookDirection)
    {
        lookDirection.Normalize();
        Vector3 flatDir = new Vector3(lookDirection.x, 0f, lookDirection.z).normalized;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(flatDir, Vector3.up);
        }

        float pitch = -Mathf.Asin(Mathf.Clamp(lookDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    public void SetLookAngles(float yaw, float pitch)
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        xRotation = Mathf.Clamp(pitch, -90f, 90f);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}