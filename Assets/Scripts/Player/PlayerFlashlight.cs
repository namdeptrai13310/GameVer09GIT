using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Player
{
    public class PlayerFlashlight : MonoBehaviour
    {
        [Header("Flashlight References")]
        [Tooltip("Transform chứa Model 3D đèn pin")]
        public Transform flashlightModel;

        [Tooltip("SpotLight chiếu sáng")]
        public Light flashlightLight;

        [Tooltip("Renderer chứa mặt kính đèn để bật/tắt hiệu ứng phát sáng")]
        public MeshRenderer lensRenderer;
        public int lensMaterialIndex = 1;

        [Header("Settings")]
        public bool startsOn = false;
        public float intensity = 2.4f;
        public float range = 25f;
        public float spotAngle = 55f;
        public Color lightColor = new Color(1f, 0.96f, 0.88f);

        [Header("Viewmodel Sway (Độ trễ rê chuột)")]
        public bool enableSway = true;
        public float swayAmount = 1.5f;
        public float maxSwayAngle = 5f;
        public float swaySmoothness = 6f;

        [Header("Walking Bobbing (Đung đưa bước chân)")]
        public bool enableBobbing = true;
        public float bobbingSpeed = 8f;
        public float bobbingAmount = 0.015f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip clickClip;

        private static PlayerFlashlight _instance;
        public static PlayerFlashlight Instance
        {
            get
            {
                if (_instance == null) _instance = Object.FindFirstObjectByType<PlayerFlashlight>();
                return _instance;
            }
            private set => _instance = value;
        }

        public bool IsOn { get; private set; }

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private float bobTimer = 0f;
        private CharacterController characterController;
        private Material dynamicLensMat;
        private Color originalLensEmission = new Color(1f, 0.95f, 0.85f) * 2.5f;
        private Coroutine flickerCoroutine;

        private void Awake()
        {
            _instance = this;
            // Force flashlight OFF at game start regardless of serialized scene value
            // Player must find and pick up the flashlight during gameplay
            startsOn = false;
            
            characterController = GetComponentInParent<CharacterController>();

            if (flashlightModel == null)
            {
                var found = transform.Find("Flashlight_Model");
                if (found != null) flashlightModel = found;
            }

            if (flashlightLight == null)
            {
                flashlightLight = GetComponentInChildren<Light>();
            }

            // Đảm bảo flashlightLight là con trực tiếp của Camera để chùm sáng luôn hoạt động
            // ngay cả khi ẩn mô hình đèn pin trên tay (ví dụ: khi đang 2 tay bê đồ)
            if (flashlightLight != null && flashlightLight.transform.parent != transform)
            {
                flashlightLight.transform.SetParent(transform, true);
            }

            if (lensRenderer == null && flashlightModel != null)
            {
                lensRenderer = flashlightModel.GetComponent<MeshRenderer>();
            }

            if (lensRenderer != null && lensRenderer.materials.Length > lensMaterialIndex)
            {
                dynamicLensMat = lensRenderer.materials[lensMaterialIndex];
                if (dynamicLensMat.HasProperty("_EmissionColor"))
                {
                    originalLensEmission = dynamicLensMat.GetColor("_EmissionColor");
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D sound for player
                }
            }

            if (clickClip == null || clickClip.name.Contains("SFX_Flashlight_Click"))
            {
#if UNITY_EDITOR
                clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Flashlight_RealClick.mp3");
#endif
            }

            SetupLight();

            if (flashlightModel != null)
            {
                initialLocalPos = flashlightModel.localPosition;
                initialLocalRot = flashlightModel.localRotation;
            }
        }

        private void Start()
        {
            SetEquipped(startsOn, false);
            SetFlashlight(startsOn, false);
        }

        private void SetupLight()
        {
            if (flashlightLight != null)
            {
                flashlightLight.type = LightType.Spot;
                flashlightLight.range = range;
                flashlightLight.spotAngle = spotAngle;
                flashlightLight.innerSpotAngle = spotAngle * 0.65f;
                flashlightLight.color = lightColor;
                flashlightLight.shadows = LightShadows.Soft;
                flashlightLight.intensity = intensity;
                flashlightLight.enabled = false; // Always off until picked up and turned on!
            }
        }

        public bool IsEquipped { get; private set; } = false;

        public bool HasFlashlightInInventory()
        {
            if (QuickSlotSystem.Instance != null)
            {
                return QuickSlotSystem.Instance.HasItem("flashlight");
            }
            return false;
        }

        public bool IsCarryingClutter()
        {
            return HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null &&
                   HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.isCarryingClutter;
        }

        public void SetModelVisible(bool visible)
        {
            if (flashlightModel != null)
            {
                if (!flashlightModel.gameObject.activeSelf)
                {
                    flashlightModel.gameObject.SetActive(true);
                }
                if (lensRenderer == null)
                {
                    lensRenderer = flashlightModel.GetComponent<MeshRenderer>();
                }
                if (lensRenderer != null)
                {
                    lensRenderer.enabled = visible;
                }
            }
        }

        public void RefreshLightState()
        {
            bool hasFlashlight = HasFlashlightInInventory();
            if (!hasFlashlight)
            {
                IsOn = false;
                IsEquipped = false;
                SetModelVisible(false);
                if (flashlightLight != null) flashlightLight.enabled = false;
                if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(false);
                UpdateLensEmission();
                return;
            }

            bool carrying = IsCarryingClutter();
            if (carrying)
            {
                // Khi đang 2 tay bê phế liệu: ẩn mô hình đèn pin trên tay nhưng GIỮ NGUYÊN chùm sáng rọi thẳng phía trước!
                SetModelVisible(false);
                if (flashlightLight != null) flashlightLight.enabled = IsOn;
                if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(false);
            }
            else
            {
                SetModelVisible(IsEquipped);
                if (flashlightLight != null) flashlightLight.enabled = IsEquipped && IsOn;
                if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(IsEquipped && IsOn);
            }

            UpdateLensEmission();
        }

        public void EquipFlashlight()
        {
            if (!HasFlashlightInInventory()) return;
            SetEquipped(true, true);
            SetFlashlight(true, true);
        }

        private void Update()
        {
            HandleInput();
            HandleSwayAndBobbing();
        }

        private void LateUpdate()
        {
            UpdateLightPosition();
        }

        private void UpdateLightPosition()
        {
            if (flashlightLight == null) return;

            bool carrying = IsCarryingClutter();
            if (carrying)
            {
                // Khi đang bê phế liệu (2 tay ôm bàn/ghế/thùng):
                // Chùm sáng đóng vai trò đèn gắn ngực/trán, chiếu thẳng về phía trước theo hướng nhìn của Camera
                if (flashlightLight.transform.parent == transform)
                {
                    flashlightLight.transform.localPosition = new Vector3(0f, 0.15f, 0.15f);
                    flashlightLight.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
                }
                else if (flashlightModel != null)
                {
                    flashlightLight.transform.localPosition = new Vector3(0f, 0.15f, 0.15f);
                    flashlightLight.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
                }
            }
            else
            {
                // Khi cầm đèn trên tay: chùm sáng bám theo đầu đèn pin
                if (flashlightLight.transform.parent == transform && flashlightModel != null)
                {
                    flashlightLight.transform.position = flashlightModel.TransformPoint(new Vector3(0f, 0f, 0.12f));
                    flashlightLight.transform.rotation = flashlightModel.rotation;
                }
                else
                {
                    flashlightLight.transform.localPosition = new Vector3(0f, 0f, 0.08f);
                    flashlightLight.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void HandleInput()
        {
            // Không nhận phím khi đang ở menu Di chúc hoặc đang mở Sổ tay
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) return;
            if (HorrorGame.UI.PlayerNotebook.Instance != null && HorrorGame.UI.PlayerNotebook.Instance.IsOpen) return;

            // Nếu người chơi KHÔNG có đèn pin trong túi đồ -> Bỏ qua mọi phím
            if (!HasFlashlightInInventory()) return;

            bool togglePressed = false;

            // 1. Phím T - Phím tắt độc lập chuyên dụng bật/tắt đèn
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                togglePressed = true;
            }
            // 2. Chuột phải (Right Click) - Bật/tắt đèn trực quan, không xung đột bất kỳ phím nào
            else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                togglePressed = true;
            }

            // TUYỆT ĐỐI KHÔNG DÙNG PHÍM F ĐỂ BẬT ĐÈN PIN (F dành riêng cho mở cửa, nhặt đồ)

            if (togglePressed)
            {
                if (IsCarryingClutter())
                {
                    // Đang bê phế liệu vẫn bật/tắt được đèn gắn ngực để người chơi không bị mù trong bóng tối!
                    SetFlashlight(!IsOn, true);
                }
                else
                {
                    TryToggleOrEquip();
                }
            }
        }

        /// <summary>
        /// Bật/tắt đèn hoặc tự động rút đèn ra cầm nếu người chơi đã sở hữu đèn pin
        /// </summary>
        public void TryToggleOrEquip()
        {
            if (!HasFlashlightInInventory())
            {
                SetFlashlight(false, false);
                SetEquipped(false, false);
                return;
            }

            if (IsCarryingClutter())
            {
                SetFlashlight(!IsOn, true);
                return;
            }

            // Nếu đang cầm đèn trên tay -> Bật / Tắt trực tiếp
            if (IsEquipped)
            {
                Toggle();
                return;
            }

            int flashlightSlot = -1;
            if (QuickSlotSystem.Instance != null)
            {
                for (int i = 0; i < QuickSlotSystem.Instance.Slots.Count; i++)
                {
                    if (QuickSlotSystem.Instance.Slots[i].itemId == "flashlight")
                    {
                        flashlightSlot = i;
                        break;
                    }
                }
            }

            // Tự động rút đèn pin ra và bật sáng
            if (flashlightSlot >= 0 && QuickSlotSystem.Instance != null)
            {
                QuickSlotSystem.Instance.SelectSlot(flashlightSlot, true);
                SetFlashlight(true, true);
            }
            else
            {
                EquipFlashlight();
            }
        }

        private void HandleSwayAndBobbing()
        {
            if (flashlightModel == null) return;

            // 1. Mouse Sway
            Quaternion targetSwayRot = initialLocalRot;
            if (enableSway && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.05f;
                float mouseX = Mathf.Clamp(-mouseDelta.x * swayAmount, -maxSwayAngle, maxSwayAngle);
                float mouseY = Mathf.Clamp(mouseDelta.y * swayAmount, -maxSwayAngle, maxSwayAngle);

                Quaternion swayRotation = Quaternion.Euler(mouseY, mouseX, -mouseX * 0.5f);
                targetSwayRot = initialLocalRot * swayRotation;
            }

            flashlightModel.localRotation = Quaternion.Slerp(flashlightModel.localRotation, targetSwayRot, Time.deltaTime * swaySmoothness);

            // 2. Walking Bobbing
            Vector3 targetBobPos = initialLocalPos;
            if (enableBobbing && characterController != null)
            {
                Vector3 horizontalVel = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
                float speed = horizontalVel.magnitude;

                if (speed > 0.1f && characterController.isGrounded)
                {
                    bobTimer += Time.deltaTime * (bobbingSpeed * (speed / 3.5f));
                    float wavesliceX = Mathf.Cos(bobTimer * 0.5f);
                    float wavesliceY = Mathf.Sin(bobTimer);

                    float totalTranslateX = wavesliceX * bobbingAmount * 0.5f;
                    float totalTranslateY = Mathf.Abs(wavesliceY) * bobbingAmount;

                    targetBobPos += new Vector3(totalTranslateX, totalTranslateY, 0f);
                }
                else
                {
                    bobTimer = 0f;
                }
            }

            flashlightModel.localPosition = Vector3.Lerp(flashlightModel.localPosition, targetBobPos, Time.deltaTime * 8f);
        }

        public void SetEquipped(bool equip, bool playSound = true)
        {
            if (equip && !HasFlashlightInInventory())
            {
                equip = false;
            }

            IsEquipped = equip;
            RefreshLightState();

            if (playSound && HasFlashlightInInventory())
            {
                PlayClickSound();
            }
        }

        public void Toggle()
        {
            if (!HasFlashlightInInventory())
            {
                SetFlashlight(false, false);
                SetEquipped(false, false);
                return;
            }

            if (!IsEquipped && !IsCarryingClutter())
            {
                TryToggleOrEquip();
                return;
            }

            SetFlashlight(!IsOn, true);
        }

        public void SetFlashlight(bool turnOn, bool playSound = true)
        {
            if (turnOn && !HasFlashlightInInventory())
            {
                turnOn = false;
            }

            IsOn = turnOn;
            RefreshLightState();

            if (playSound && HasFlashlightInInventory())
            {
                PlayClickSound();
            }
        }

        private void UpdateLensEmission()
        {
            if (dynamicLensMat != null && dynamicLensMat.HasProperty("_EmissionColor"))
            {
                bool active = HasFlashlightInInventory() && IsOn && (IsEquipped || IsCarryingClutter());
                if (active)
                {
                    dynamicLensMat.EnableKeyword("_EMISSION");
                    dynamicLensMat.SetColor("_EmissionColor", originalLensEmission);
                }
                else
                {
                    dynamicLensMat.DisableKeyword("_EMISSION");
                    dynamicLensMat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private void PlayClickSound()
        {
            if (audioSource == null) return;

            if (clickClip != null)
            {
                audioSource.PlayOneShot(clickClip, 0.9f);
            }
        }

        public void TriggerFlicker(float duration = 1.2f, int blinks = 4)
        {
            if (!IsEquipped || !IsOn) return;
            if (flickerCoroutine != null) StopCoroutine(flickerCoroutine);
            flickerCoroutine = StartCoroutine(RoutineFlicker(duration, blinks));
        }

        private System.Collections.IEnumerator RoutineFlicker(float duration, int blinks)
        {
            float blinkInterval = duration / (blinks * 2);
            for (int i = 0; i < blinks; i++)
            {
                if (flashlightLight != null) flashlightLight.enabled = false;
                if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(false);
                yield return new WaitForSeconds(blinkInterval * Random.Range(0.6f, 1.2f));

                if (flashlightLight != null) flashlightLight.enabled = true;
                if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(true);
                yield return new WaitForSeconds(blinkInterval * Random.Range(0.6f, 1.2f));
            }

            if (flashlightLight != null) flashlightLight.enabled = IsOn;
            if (PlayerHandEquipment.Instance != null) PlayerHandEquipment.Instance.SetFlashlightLight(IsOn);
            flickerCoroutine = null;
        }
    }
}
