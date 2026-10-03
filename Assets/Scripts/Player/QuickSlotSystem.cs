using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HorrorGame.Player
{
    [System.Serializable]
    public class QuickSlotItem
    {
        public string itemId = "";
        public string itemName = "";
        public Sprite icon = null;
        public GameObject dropPrefab = null;

        public bool IsEmpty => string.IsNullOrEmpty(itemId);

        public void Clear()
        {
            itemId = "";
            itemName = "";
            icon = null;
            dropPrefab = null;
        }
    }

    public class QuickSlotSystem : MonoBehaviour
    {

        [Header("References")]
        public PlayerFlashlight flashlight;
        public Transform playerCamera;

        [Header("Settings")]
        [Range(3, 6)]
        public int totalSlots = 4;

        [Header("Drop Prefabs")]
        public GameObject defaultFlashlightDropPrefab;

        [Header("Default Item Icons (Direct Serialized Sprites)")]
        public Sprite defaultFlashlightIcon;
        public Sprite defaultAxeIcon;
        public Sprite defaultWoodPlankIcon;
        public Sprite defaultToolboxIcon;
        public Sprite defaultKeyIcon;
        public Sprite defaultTalismanIcon;
        public Sprite defaultClutterIcon;
        public Sprite defaultChairIcon;
        public Sprite defaultTableIcon;
        public Sprite defaultScrapBoxIcon;

        [Header("Colors (Needle-Sharp Pixel-Perfect UI)")]
        public Color idleBorderColor = new Color(0.35f, 0.40f, 0.48f, 0.85f); // Viền thép xám
        public Color activeBorderColor = new Color(1.0f, 0.85f, 0.15f, 1.0f); // Vàng rực rỡ, phát sáng
        public Color idleFillColor = new Color(0.06f, 0.07f, 0.09f, 0.96f);   // Đen sâu thẳm
        public Color activeFillColor = new Color(0.28f, 0.22f, 0.06f, 0.98f); // Nền ấm hổ phách sáng rõ

        [Header("Runtime Inventory")]
        [SerializeField] private List<QuickSlotItem> slots = new List<QuickSlotItem>();
        [SerializeField] private int currentSelectedSlot = -1; // -1 = Tay không
        public int CurrentSelectedSlot => currentSelectedSlot;
        public List<QuickSlotItem> Slots
        {
            get
            {
                EnsureSlotsInitialized();
                return slots;
            }
        }

        private static QuickSlotSystem _instance;
        public static QuickSlotSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<QuickSlotSystem>();
                }
                return _instance;
            }
            set => _instance = value;
        }

        public static Canvas GetMainHUDCanvas()
        {
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c.transform.parent == null && c.gameObject.name == "Canvas")
                    return c;
            }
            foreach (var c in allCanvases)
            {
                if (c.transform.parent == null && !c.name.Contains("Menu") && !c.name.Contains("Letter") && !c.name.Contains("Clue"))
                    return c;
            }
            return allCanvases.Length > 0 ? allCanvases[0] : null;
        }

        // UI References
        private GameObject hotbarRoot;
        private RectTransform[] slotRects;
        private Outline[] slotOutlines;
        private Image[] slotOuterBorders;
        private Image[] slotInnerFills;
        private Image[] slotIcons;
        private GameObject[] slotActiveIndicators;
        private Text[] slotNumberTexts;
        private Text[] slotNameTexts;
        private Text keyHintText;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            if (flashlight == null)
            {
                flashlight = FindAnyObjectByType<PlayerFlashlight>();
            }

            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
                else if (Camera.main != null) playerCamera = Camera.main.transform;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            LoadAllDefaultIcons();
            ConfigureCanvasClarity();
            InitializeSlots();
            BuildHotbarUI();
            AdjustStaminaUIPosition();
        }

        public void ConfigureCanvasClarity()
        {
            Canvas canvas = GetMainHUDCanvas();
            if (canvas != null)
            {
                canvas.pixelPerfect = true;

                CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
                if (cs != null)
                {
                    // Thiết lập chuẩn Full HD 1920x1080
                    cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    cs.referenceResolution = new Vector2(1920f, 1080f);
                    cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    cs.matchWidthOrHeight = 0.5f;
                    cs.dynamicPixelsPerUnit = 3f; // Siêu lấy mẫu x3 nét căng
                    cs.referencePixelsPerUnit = 100f;
                }
            }
        }

        private void Start()
        {
            // Mặc định vào game nhân vật tay không (chưa có đèn pin, phải làm nhiệm vụ nhặt)
            SelectSlot(-1, false);

            if (flashlight != null)
            {
                flashlight.SetFlashlight(false, false);
                flashlight.SetEquipped(false, false);
            }

            AdjustStaminaUIPosition();
        }

        public void EnsureSlotsInitialized()
        {
            if (slots == null) slots = new List<QuickSlotItem>();
            while (slots.Count < totalSlots)
            {
                slots.Add(new QuickSlotItem());
            }
        }

        private void InitializeSlots()
        {
            slots = new List<QuickSlotItem>();
            for (int i = 0; i < totalSlots; i++)
            {
                slots.Add(new QuickSlotItem());
            }
        }

        private void Update()
        {
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                return;
            }

            HandleSlotInputs();
            HandleDropInput();
        }

        private void HandleSlotInputs()
        {
            int slotPressed = -1;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) slotPressed = 0;
                else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) slotPressed = 1;
                else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) slotPressed = 2;
                else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame) slotPressed = 3;
            }

            // Mouse Scroll Wheel
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (scroll > 0.1f)
                {
                    int nextSlot = (currentSelectedSlot <= 0) ? totalSlots - 1 : currentSelectedSlot - 1;
                    SelectSlot(nextSlot, true);
                    return;
                }
                else if (scroll < -0.1f)
                {
                    int nextSlot = (currentSelectedSlot < 0 || currentSelectedSlot >= totalSlots - 1) ? 0 : currentSelectedSlot + 1;
                    SelectSlot(nextSlot, true);
                    return;
                }
            }

            if (slotPressed >= 0 && slotPressed < totalSlots)
            {
                if (currentSelectedSlot == slotPressed)
                {
                    // Bấm lại chính ô đang chọn:
                    // Nếu là ô Đèn Pin thì bật/tắt đèn!
                    if (currentSelectedSlot < slots.Count && slots[currentSelectedSlot].itemId == "flashlight")
                    {
                        if (flashlight != null) flashlight.Toggle();
                    }
                    else
                    {
                        // Cất đồ (tay không)
                        SelectSlot(-1, true);
                    }
                }
                else
                {
                    SelectSlot(slotPressed, true);
                }
            }
        }

        private void HandleDropInput()
        {
            if (Keyboard.current == null) return;

            // Kiểm tra phím G hoặc phím Q
            bool dropPressed = Keyboard.current.gKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame;
            if (!dropPressed) return;

            // NẾU ĐANG BÊ PHẾ LIỆU (bàn, ghế, thùng...):
            // Phím G / Q chỉ được phép ĐẶT PHẾ LIỆU XUỐNG ĐẤT, TUYỆT ĐỐI KHÔNG BAO GIỜ được vứt đồ trên người (đèn pin...)!
            if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null &&
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.isCarryingClutter)
            {
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.DropCurrentClutterOnGround();
                return;
            }

            // Bảo vệ chống race condition: nếu vừa đặt phế liệu trong frame này, cấm vứt đồ khác
            if (HorrorGame.Story.VillaSurveyAndRepairQuest.lastClutterDropFrame == Time.frameCount)
            {
                return;
            }

            DropCurrentItem();
        }

        public void SelectSlot(int slotIndex, bool playSound)
        {
            // Không đổi vũ khí/đồ cầm nếu đang bê phế liệu bằng 2 tay
            if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null &&
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.isCarryingClutter)
            {
                if (slotIndex >= 0 && slotIndex < slots.Count && !slots[slotIndex].IsEmpty && slots[slotIndex].itemId.StartsWith("clutter"))
                {
                    currentSelectedSlot = slotIndex;
                    UpdateUI();
                }
                return;
            }

            EnsureSlotsInitialized();
            currentSelectedSlot = slotIndex;

            bool isHoldingFlashlight = false;
            bool isHoldingAxe = false;
            string heldItemId = "";

            if (currentSelectedSlot >= 0 && currentSelectedSlot < slots.Count)
            {
                QuickSlotItem item = slots[currentSelectedSlot];
                if (!item.IsEmpty)
                {
                    heldItemId = item.itemId;
                    if (item.itemId == "flashlight") isHoldingFlashlight = true;
                    else if (item.itemId == "axe") isHoldingAxe = true;
                }
            }

            if (flashlight != null)
            {
                flashlight.SetEquipped(isHoldingFlashlight, playSound);
                // Khi vừa rút đèn pin ra lần đầu, tự động bật đèn nếu chưa bật
                if (isHoldingFlashlight && !flashlight.IsOn)
                {
                    flashlight.SetFlashlight(true, playSound);
                }
            }

            if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null && 
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.inHandAxeVisual != null)
            {
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.inHandAxeVisual.SetActive(isHoldingAxe);
            }

            // Đồng bộ trực tiếp hiển thị mô hình 3D trên tay nhân vật (RightHand_ItemSocket)
            if (PlayerHandEquipment.Instance != null)
            {
                PlayerHandEquipment.Instance.EquipItem(heldItemId);
                if (isHoldingFlashlight && flashlight != null)
                {
                    PlayerHandEquipment.Instance.SetFlashlightLight(flashlight.IsOn);
                }
            }

            UpdateUI();

            if (playSound && audioSource != null)
            {
                PlaySwitchSound();
            }
        }

        public void LoadAllDefaultIcons()
        {
#if UNITY_EDITOR
            if (defaultFlashlightIcon == null)
                defaultFlashlightIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Flashlight.png");
            if (defaultAxeIcon == null)
                defaultAxeIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Axe.png");
            if (defaultWoodPlankIcon == null)
                defaultWoodPlankIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_WoodPlank.png");
            if (defaultToolboxIcon == null)
                defaultToolboxIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Toolbox.png");
            if (defaultKeyIcon == null)
                defaultKeyIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Basement_Key.png");
            if (defaultTalismanIcon == null)
                defaultTalismanIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Ancestral_Talisman.png");
            if (defaultClutterIcon == null)
                defaultClutterIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Clutter.png");
            if (defaultChairIcon == null)
                defaultChairIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Clutter_Chair.png");
            if (defaultTableIcon == null)
                defaultTableIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Clutter_Table.png");
            if (defaultScrapBoxIcon == null)
                defaultScrapBoxIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Icon_Mini_Clutter_Box.png");
#endif
        }

        private static Dictionary<string, Sprite> generatedIconCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Tải icon mini 3D render sắc nét thực tế của từng vật phẩm
        /// </summary>
        public static Sprite GetDefaultItemIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            string id = itemId.ToLower();

            // 1. Kiểm tra trực tiếp các Sprite serialized trên Instance
            if (Instance != null)
            {
                if (id == "flashlight" || id.Contains("den") || id.Contains("light"))
                {
                    if (Instance.defaultFlashlightIcon != null) return Instance.defaultFlashlightIcon;
                }
                else if (id == "axe" || id.Contains("riu"))
                {
                    if (Instance.defaultAxeIcon != null) return Instance.defaultAxeIcon;
                }
                else if (id.Contains("chair") || id.Contains("ghe"))
                {
                    if (Instance.defaultChairIcon != null) return Instance.defaultChairIcon;
                }
                else if (id.Contains("table") || id.Contains("ban"))
                {
                    if (Instance.defaultTableIcon != null) return Instance.defaultTableIcon;
                }
                else if (id.Contains("drawer") || id.Contains("scrap") || id.Contains("iron") || id.Contains("sat") || id.Contains("heater") || id.Contains("suoi"))
                {
                    if (Instance.defaultScrapBoxIcon != null) return Instance.defaultScrapBoxIcon;
                }
                else if (id.Contains("plank") || id.Contains("wood") || id.Contains("van"))
                {
                    if (Instance.defaultWoodPlankIcon != null) return Instance.defaultWoodPlankIcon;
                }
                else if (id.Contains("toolbox") || id.Contains("dung_cu"))
                {
                    if (Instance.defaultToolboxIcon != null) return Instance.defaultToolboxIcon;
                }
                else if (id.Contains("key") || id.Contains("chia"))
                {
                    if (Instance.defaultKeyIcon != null) return Instance.defaultKeyIcon;
                }
                else if (id.Contains("talisman") || id.Contains("bua"))
                {
                    if (Instance.defaultTalismanIcon != null) return Instance.defaultTalismanIcon;
                }
                else if (id.Contains("clutter") || id.Contains("box") || id.Contains("thung") || id.Contains("rac") || id.Contains("phe_lieu"))
                {
                    if (Instance.defaultClutterIcon != null) return Instance.defaultClutterIcon;
                }
            }

            if (generatedIconCache.TryGetValue(id, out var cached) && cached != null)
            {
                return cached;
            }

            Sprite sp = null;
#if UNITY_EDITOR
            string iconPath = null;
            if (id == "flashlight" || id.Contains("den") || id.Contains("light"))
                iconPath = "Assets/UI/Icons/Icon_Flashlight.png";
            else if (id == "axe" || id.Contains("riu"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Axe.png";
            else if (id.Contains("chair") || id.Contains("ghe"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Clutter_Chair.png";
            else if (id.Contains("table") || id.Contains("ban"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Clutter_Table.png";
            else if (id.Contains("drawer") || id.Contains("scrap") || id.Contains("iron") || id.Contains("sat"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Clutter_Box.png";
            else if (id.Contains("plank") || id.Contains("wood") || id.Contains("van"))
                iconPath = "Assets/UI/Icons/Icon_Mini_WoodPlank.png";
            else if (id.Contains("toolbox") || id.Contains("dung_cu"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Toolbox.png";
            else if (id.Contains("key") || id.Contains("chia"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Basement_Key.png";
            else if (id.Contains("talisman") || id.Contains("bua"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Ancestral_Talisman.png";
            else if (id.Contains("clutter") || id.Contains("box") || id.Contains("thung") || id.Contains("rac") || id.Contains("phe_lieu"))
                iconPath = "Assets/UI/Icons/Icon_Mini_Clutter.png";

            if (!string.IsNullOrEmpty(iconPath))
            {
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sp != null)
                {
                    generatedIconCache[id] = sp;
                    return sp;
                }
            }
#endif
            sp = Resources.Load<Sprite>("Icons/Icon_Mini_" + id);
            if (sp != null)
            {
                generatedIconCache[id] = sp;
                return sp;
            }

            // Fallback an toàn tuyệt đối: không bao giờ trả về null nếu còn bất kỳ icon nào
            if (Instance != null)
            {
                return Instance.defaultClutterIcon ?? Instance.defaultFlashlightIcon;
            }

            return null;
        }

        /// <summary>
        /// Thêm đồ vào ô trống đầu tiên trong Hotbar
        /// </summary>
        public bool AddItem(string itemId, string itemName, Sprite icon, GameObject dropPrefab)
        {
            EnsureSlotsInitialized();
            if (icon == null)
            {
                icon = GetDefaultItemIcon(itemId);
            }
            if (icon == null && Instance != null)
            {
                icon = Instance.defaultClutterIcon ?? Instance.defaultFlashlightIcon;
            }

            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].itemId = itemId;
                    slots[i].itemName = itemName;
                    slots[i].icon = icon;
                    slots[i].dropPrefab = dropPrefab;
                    if (slots[i].dropPrefab == null && itemId == "flashlight")
                    {
                        slots[i].dropPrefab = defaultFlashlightDropPrefab;
                    }

                    // Tự động cầm luôn nếu đang tay không hoặc nếu đang ở đúng ô này
                    if (currentSelectedSlot == -1 || currentSelectedSlot == i)
                    {
                        SelectSlot(i, true);
                    }
                    else
                    {
                        UpdateUI();
                    }
                    return true;
                }
            }

            return false; // Hết chỗ
        }

        /// <summary>
        /// Thêm phế liệu đang bê vào Hotbar với icon đầy đủ
        /// </summary>
        public bool AddClutterItem(string clutterId, string clutterName, Sprite clutterIcon)
        {
            if (clutterIcon == null)
            {
                clutterIcon = GetDefaultItemIcon(clutterId);
            }
            bool added = AddItem(clutterId, clutterName, clutterIcon, null);
            if (added)
            {
                for (int s = 0; s < slots.Count; s++)
                {
                    if (slots[s].itemId == clutterId)
                    {
                        currentSelectedSlot = s;
                        break;
                    }
                }
                UpdateUI();
            }
            return added;
        }

        /// <summary>
        /// Xóa vật phẩm theo tiền tố itemId (ví dụ: "clutter")
        /// </summary>
        public bool RemoveItemByPrefix(string prefix)
        {
            EnsureSlotsInitialized();
            bool removed = false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].itemId.StartsWith(prefix))
                {
                    slots[i].Clear();
                    if (currentSelectedSlot == i)
                    {
                        currentSelectedSlot = -1;
                    }
                    removed = true;
                }
            }
            if (removed) UpdateUI();
            return removed;
        }

        /// <summary>
        /// Xóa chính xác vật phẩm theo itemId
        /// </summary>
        public bool RemoveItem(string itemId)
        {
            EnsureSlotsInitialized();
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].itemId == itemId)
                {
                    slots[i].Clear();
                    if (currentSelectedSlot == i)
                    {
                        currentSelectedSlot = -1;
                    }
                    UpdateUI();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Vứt món đồ trong ô đang chọn ra mặt đất
        /// </summary>
        public void DropCurrentItem()
        {
            // Bảo vệ an toàn tuyệt đối: nếu vừa đặt phế liệu trong frame này, cấm vứt đồ khác
            if (HorrorGame.Story.VillaSurveyAndRepairQuest.lastClutterDropFrame == Time.frameCount) return;
            if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null &&
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.isCarryingClutter)
            {
                HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.DropCurrentClutterOnGround();
                return;
            }

            if (currentSelectedSlot < 0 || currentSelectedSlot >= slots.Count) return;

            QuickSlotItem item = slots[currentSelectedSlot];
            if (item.IsEmpty) return;

            // Nếu ô hiện tại là phế liệu, đặt phế liệu xuống chứ không vứt các đồ khác
            if (item.itemId.StartsWith("clutter"))
            {
                if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                {
                    HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.DropCurrentClutterOnGround();
                }
                else
                {
                    item.Clear();
                    SelectSlot(-1, false);
                    UpdateUI();
                }
                return;
            }

            // 1. Tọa độ ném đồ ra trước mặt nhân vật
            Vector3 spawnPos = transform.position + Vector3.up * 1.0f + transform.forward * 0.7f;
            if (playerCamera != null)
            {
                spawnPos = playerCamera.position + playerCamera.forward * 0.75f - Vector3.up * 0.15f;
            }

            GameObject prefabToSpawn = item.dropPrefab != null ? item.dropPrefab : (item.itemId == "flashlight" ? defaultFlashlightDropPrefab : null);
            if (prefabToSpawn != null && Application.isPlaying)
            {
                GameObject droppedObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                droppedObj.name = prefabToSpawn.name;

                Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedObj.AddComponent<Rigidbody>();

                rb.isKinematic = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                Vector3 throwDir = playerCamera != null ? playerCamera.forward : transform.forward;
                rb.linearVelocity = throwDir * 2.8f + Vector3.up * 1.2f;
                rb.angularVelocity = Random.insideUnitSphere * 4f;
            }

            // 2. Nếu là Đèn pin, tắt đèn và xóa hoàn toàn quyền sở hữu
            if (item.itemId == "flashlight")
            {
                if (flashlight != null)
                {
                    flashlight.SetFlashlight(false, false);
                    flashlight.SetEquipped(false, false);
                }
                if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                {
                    HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.hasFlashlight = false;
                }
            }

            // 3. Xóa đồ khỏi ô và chuyển về tay không
            item.Clear();
            SelectSlot(-1, false);

            if (flashlight != null)
            {
                flashlight.RefreshLightState();
            }

            // 4. Phát âm thanh ném/rơi đồ
            PlayDropSound();

            // 5. Cập nhật UI
            UpdateUI();
        }

        public bool HasItem(string itemId)
        {
            foreach (var s in slots)
            {
                if (s.itemId == itemId) return true;
            }
            return false;
        }

        private void UpdateUI()
        {
            EnsureSlotsInitialized();
            if (slotOuterBorders == null || slotOuterBorders.Length == 0)
            {
                BuildHotbarUI();
            }
            if (slotOuterBorders == null) return;

            int count = Mathf.Min(totalSlots, slotOuterBorders.Length);
            for (int i = 0; i < count; i++)
            {
                if (slotOuterBorders[i] == null) continue;

                bool isActive = (i == currentSelectedSlot);
                QuickSlotItem item = (i < slots.Count) ? slots[i] : null;
                bool hasItem = item != null && !item.IsEmpty;

                // 1. Phóng to 1.15x ô đang chọn và nhô lên cao để người chơi thấy rõ 100% đang chọn ô nào
                if (slotRects != null && i < slotRects.Length && slotRects[i] != null)
                {
                    slotRects[i].localScale = isActive ? new Vector3(1.15f, 1.15f, 1.0f) : Vector3.one;
                }

                // 2. Viền ngoài sắc nét (Outer Border)
                slotOuterBorders[i].color = isActive ? activeBorderColor : idleBorderColor;

                // 3. Viền hào quang vàng phát sáng (Glow Outline)
                if (slotOutlines != null && i < slotOutlines.Length && slotOutlines[i] != null)
                {
                    slotOutlines[i].enabled = isActive;
                }

                // 4. Ruột nền trong (Inner Fill)
                if (slotInnerFills != null && i < slotInnerFills.Length && slotInnerFills[i] != null)
                {
                    slotInnerFills[i].color = isActive ? activeFillColor : idleFillColor;
                }

                // 5. Vạch chỉ báo Active phía trên (Sharp Top Indicator)
                if (slotActiveIndicators != null && i < slotActiveIndicators.Length && slotActiveIndicators[i] != null)
                {
                    slotActiveIndicators[i].SetActive(isActive);
                }

                // 6. Số thứ tự ô (1, 2, 3, 4) - Font to, rõ nét
                if (slotNumberTexts != null && i < slotNumberTexts.Length && slotNumberTexts[i] != null)
                {
                    slotNumberTexts[i].color = isActive ? activeBorderColor : new Color(0.70f, 0.75f, 0.82f, 0.85f);
                    slotNumberTexts[i].fontSize = isActive ? 20 : 18;
                }

                // 7. Icon vật phẩm (Uncompressed TrueColor) - Ràng buộc chặt chẽ không bao giờ mất icon
                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    if (hasItem)
                    {
                        if (item.icon == null)
                        {
                            item.icon = GetDefaultItemIcon(item.itemId);
                        }
                        if (item.icon != null)
                        {
                            slotIcons[i].sprite = item.icon;
                            slotIcons[i].enabled = true;
                            slotIcons[i].color = Color.white;
                        }
                        else
                        {
                            slotIcons[i].enabled = false;
                        }
                    }
                    else
                    {
                        slotIcons[i].enabled = false;
                    }
                }

                // 8. Tên vật phẩm
                if (slotNameTexts != null && i < slotNameTexts.Length && slotNameTexts[i] != null)
                {
                    if (hasItem)
                    {
                        slotNameTexts[i].text = item.itemName;
                        slotNameTexts[i].color = isActive ? new Color(1f, 0.95f, 0.40f, 1f) : new Color(0.80f, 0.85f, 0.90f, 0.85f);
                    }
                    else
                    {
                        slotNameTexts[i].text = isActive ? "(Trống)" : "";
                        slotNameTexts[i].color = new Color(0.6f, 0.65f, 0.7f, 0.7f);
                    }
                }
            }
        }

        private void PlaySwitchSound()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            if (audioSource == null) return;

            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.04f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1400f * (float)i / sampleRate) * Mathf.Exp(-t * 35f) * 0.4f;
            }
            AudioClip clip = AudioClip.Create("SlotClick", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, 0.45f);
        }

        private void PlayDropSound()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            if (audioSource == null) return;

            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.08f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float tone = Mathf.Sin(2f * Mathf.PI * 320f * (float)i / sampleRate);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                samples[i] = (tone * 0.5f + noise * 0.5f) * Mathf.Exp(-t * 22f) * 0.5f;
            }
            AudioClip clip = AudioClip.Create("DropClatter", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, 0.55f);
        }

        private Font GetCrispFont()
        {
            // 1. Ưu tiên font Inter-SemiBold chuẩn đồ họa cao cấp
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
        /// Đảm bảo thanh thể lực (StaminaUI) luôn nằm chính xác ở ĐÁY MÀN HÌNH (ngay trên Hotbar),
        /// không bao giờ bị nhảy lên giữa màn hình trên mọi độ phân giải (đặc biệt là Full HD 1920x1080).
        /// </summary>
        public void AdjustStaminaUIPosition()
        {
            GameObject staminaUI = GameObject.Find("StaminaUI");
            GameObject border = GameObject.Find("Border");
            GameObject fill = GameObject.Find("FillBar");

            // 1. Căn lại GameObject cha StaminaUI về mép đáy màn hình (Bottom-Center)
            if (staminaUI != null)
            {
                RectTransform rt = staminaUI.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 132f); // Cách đáy 132px (nằm ngay trên Hotbar ~26px)
                    rt.sizeDelta = new Vector2(240f, 10f);
                }
            }

            // 2. Con Border nằm đúng tâm cha StaminaUI
            if (border != null)
            {
                RectTransform rt = border.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(240f, 10f);
                }
            }

            // 3. Con FillBar nằm đúng tâm cha StaminaUI
            if (fill != null)
            {
                RectTransform rt = fill.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(240f, 10f);
                }
            }
        }

        private bool TryBindExistingHotbar(GameObject root)
        {
            if (root == null) return false;
            Transform slotsRow = root.transform.Find("SlotsRow");
            if (slotsRow == null || slotsRow.childCount < totalSlots) return false;

            slotOuterBorders = new Image[totalSlots];
            slotInnerFills = new Image[totalSlots];
            slotIcons = new Image[totalSlots];
            slotActiveIndicators = new GameObject[totalSlots];
            slotNumberTexts = new Text[totalSlots];
            slotNameTexts = new Text[totalSlots];
            slotRects = new RectTransform[totalSlots];
            slotOutlines = new Outline[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                Transform slot = slotsRow.GetChild(i);
                if (slot == null) return false;

                slotRects[i] = slot.GetComponent<RectTransform>();
                slotOuterBorders[i] = slot.GetComponent<Image>();
                slotOutlines[i] = slot.GetComponent<Outline>();

                Transform inner = slot.Find("InnerFill");
                if (inner == null) return false;

                slotInnerFills[i] = inner.GetComponent<Image>();
                slotActiveIndicators[i] = inner.Find("ActiveIndicator")?.gameObject;
                slotNumberTexts[i] = inner.Find("NumBadge")?.GetComponent<Text>();
                slotIcons[i] = inner.Find("ItemIcon")?.GetComponent<Image>();
                slotNameTexts[i] = inner.Find("ItemName")?.GetComponent<Text>();

                if (slotIcons[i] == null || slotInnerFills[i] == null) return false;
            }

            hotbarRoot = root;
            return true;
        }

        /// <summary>
        /// Tạo giao diện Hotbar chuẩn Full HD 1920x1080 cực kỳ sắc nét, pixel-perfect
        /// </summary>
        public void BuildHotbarUI()
        {
            ConfigureCanvasClarity();

            Canvas canvas = GetMainHUDCanvas();
            if (canvas == null) return;

            Transform existing = canvas.transform.Find("Hotbar_QuickSlots");
            if (existing != null && TryBindExistingHotbar(existing.gameObject))
            {
                UpdateUI();
                return;
            }

            // Xóa triệt để mọi bản Hotbar_QuickSlots cũ trên MỌI Canvas để không bị bóng ma UI
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                for (int ci = c.transform.childCount - 1; ci >= 0; ci--)
                {
                    var child = c.transform.GetChild(ci);
                    if (child.name == "Hotbar_QuickSlots")
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(child.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }

            Font font = GetCrispFont();

            // Root Container (Dưới đáy màn hình, pos.y = 26px) trên đúng Canvas chính
            hotbarRoot = new GameObject("Hotbar_QuickSlots");
            hotbarRoot.transform.SetParent(canvas.transform, false);
            hotbarRoot.transform.SetAsLastSibling();

            RectTransform rootRect = hotbarRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 26f);

            VerticalLayoutGroup vLayout = hotbarRoot.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 8f;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;

            ContentSizeFitter csf = hotbarRoot.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Hàng 4 ô Slot (Kích thước chuẩn Full HD: 68x68)
            GameObject slotsRow = new GameObject("SlotsRow");
            slotsRow.transform.SetParent(hotbarRoot.transform, false);

            RectTransform rowRt = slotsRow.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(320f, 68f);

            HorizontalLayoutGroup hLayout = slotsRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 12f;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;

            slotOuterBorders = new Image[totalSlots];
            slotInnerFills = new Image[totalSlots];
            slotIcons = new Image[totalSlots];
            slotActiveIndicators = new GameObject[totalSlots];
            slotNumberTexts = new Text[totalSlots];
            slotNameTexts = new Text[totalSlots];
            slotRects = new RectTransform[totalSlots];
            slotOutlines = new Outline[totalSlots];

            for (int i = 0; i < totalSlots; i++)
            {
                // 1. Khung viền ngoài (Outer Border 68x68) - Chuẩn Full HD
                GameObject outerGo = new GameObject("Slot_" + (i + 1));
                outerGo.transform.SetParent(slotsRow.transform, false);

                RectTransform outerRt = outerGo.AddComponent<RectTransform>();
                outerRt.sizeDelta = new Vector2(68f, 68f);
                outerRt.pivot = new Vector2(0.5f, 0.5f);
                slotRects[i] = outerRt;

                Image outerImg = outerGo.AddComponent<Image>();
                outerImg.color = idleBorderColor;
                slotOuterBorders[i] = outerImg;

                Outline outline = outerGo.AddComponent<Outline>();
                outline.effectColor = new Color(1.0f, 0.85f, 0.15f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.enabled = false;
                slotOutlines[i] = outline;

                // 2. Ruột nền trong (Inner Fill - lùi chính xác đúng 2 pixel nguyên để tạo viền 2px sắc bén)
                GameObject innerGo = new GameObject("InnerFill");
                innerGo.transform.SetParent(outerGo.transform, false);

                RectTransform innerRt = innerGo.AddComponent<RectTransform>();
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.offsetMin = new Vector2(2f, 2f);
                innerRt.offsetMax = new Vector2(-2f, -2f);

                Image innerImg = innerGo.AddComponent<Image>();
                innerImg.color = idleFillColor;
                slotInnerFills[i] = innerImg;

                // 3. Vạch viền vàng trên cùng khi Active (Top Accent Line 4px)
                GameObject topBarGo = new GameObject("ActiveIndicator");
                topBarGo.transform.SetParent(innerGo.transform, false);
                RectTransform topBarRt = topBarGo.AddComponent<RectTransform>();
                topBarRt.anchorMin = new Vector2(0f, 1f);
                topBarRt.anchorMax = new Vector2(1f, 1f);
                topBarRt.pivot = new Vector2(0.5f, 1f);
                topBarRt.anchoredPosition = Vector2.zero;
                topBarRt.sizeDelta = new Vector2(0f, 4f);
                Image topBarImg = topBarGo.AddComponent<Image>();
                topBarImg.color = activeBorderColor;
                topBarGo.SetActive(false);
                slotActiveIndicators[i] = topBarGo;

                // 4. Số thứ tự (Góc trên trái: 1, 2, 3, 4) - Font to 18, cực nét
                GameObject numGo = new GameObject("NumBadge");
                numGo.transform.SetParent(innerGo.transform, false);

                RectTransform numRt = numGo.AddComponent<RectTransform>();
                numRt.anchorMin = new Vector2(0f, 1f);
                numRt.anchorMax = new Vector2(0f, 1f);
                numRt.pivot = new Vector2(0f, 1f);
                numRt.anchoredPosition = new Vector2(6f, -4f);
                numRt.sizeDelta = new Vector2(24f, 22f);

                Text numTxt = numGo.AddComponent<Text>();
                numTxt.font = font;
                numTxt.fontSize = 18;
                numTxt.fontStyle = FontStyle.Bold;
                numTxt.alignment = TextAnchor.UpperLeft;
                numTxt.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
                numTxt.text = (i + 1).ToString();
                slotNumberTexts[i] = numTxt;

                // 5. Icon ảnh vật phẩm (Ở chính giữa ô, 50x50 sắc nét)
                GameObject iconGo = new GameObject("ItemIcon");
                iconGo.transform.SetParent(innerGo.transform, false);

                RectTransform iconRt = iconGo.AddComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 0.52f);
                iconRt.anchorMax = new Vector2(0.5f, 0.52f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(50f, 50f);

                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.enabled = false;
                slotIcons[i] = iconImg;

                // 6. Tên nhãn vật phẩm (Dưới đáy ô, font 11 có đổ bóng)
                GameObject labelGo = new GameObject("ItemName");
                labelGo.transform.SetParent(innerGo.transform, false);

                RectTransform labelRt = labelGo.AddComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 0f);
                labelRt.anchoredPosition = new Vector2(0f, 3f);
                labelRt.sizeDelta = new Vector2(0f, 16f);

                Text labelTxt = labelGo.AddComponent<Text>();
                labelTxt.font = font;
                labelTxt.fontSize = 11;
                labelTxt.fontStyle = FontStyle.Bold;
                labelTxt.alignment = TextAnchor.MiddleCenter;
                labelTxt.color = new Color(0.88f, 0.92f, 0.98f, 0.95f);
                labelTxt.text = "";

                Shadow lblShadow = labelGo.AddComponent<Shadow>();
                lblShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
                lblShadow.effectDistance = new Vector2(1f, -1f);

                slotNameTexts[i] = labelTxt;
            }

            UpdateUI();

            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                if (hotbarRoot != null) hotbarRoot.SetActive(false);
            }
        }
    }
}
