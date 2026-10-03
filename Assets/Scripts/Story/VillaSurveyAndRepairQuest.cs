using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HorrorGame.InteractSystem;
using HorrorGame.Player;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Quản lý chuỗi nhiệm vụ chính trong trò chơi kinh dị 'Tai Ương':
    /// Trình tự nhiệm vụ:
    /// Phase 0: ExploreVilla           — Vô tham quan biệt thự tự do, nhặt đèn pin trên bàn sảnh, khám phá 3 phòng tầng 1.
    /// Phase 1: DiscoverBlockedStairs  — Định lên tầng 2 thì phát hiện cầu thang bị chặn bởi đống bàn ghế, tủ kệ cũ nát.
    /// Phase 2: CleanVilla             — Dọn dẹp rác & đồ phế liệu ở tầng 1 (bê từng món ra bãi rác sân sau, có thể bấm G đặt xuống đất).
    /// Phase 3: GetAxeAndChopTrees     — Dọn rác xong → ra xe bán tải lấy Cây Rìu của cha.
    /// Phase 4: SmashStairsBlockage    — Quay lại cầu thang, dùng Rìu chém tan đống đổ nát → rơi ra 3 tấm ván gỗ & bức di thư của cha!
    /// Phase 5: RepairFloor            — Nhặt ván gỗ, đi lên tầng 2 đóng vá lại mảng sàn mục nát.
    /// Phase 6: ExploreAncestralRoom   — Bước qua sàn vào phòng thờ tổ tiên, tìm chìa khóa hầm và bùa trấn yểm.
    /// Phase 7: UnlockBasementCellar   — Xuống kho tầng trệt mở nắp hầm bí mật.
    /// Phase 8: DescendIntoBasement    — Khám phá hầm mộ tế lễ, đối mặt bí ẩn cuối cùng.
    /// Phase 9: Completed              — Hết game.
    /// </summary>
    public class VillaSurveyAndRepairQuest : MonoBehaviour
    {
        private static VillaSurveyAndRepairQuest _instance;
        public static VillaSurveyAndRepairQuest Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<VillaSurveyAndRepairQuest>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        public static int lastClutterDropFrame = -1;

        public enum QuestPhase
        {
            ExploreVilla,           // 0. Tham quan biệt thự tự do, nhặt đèn pin, khám phá tầng 1
            DiscoverBlockedStairs,  // 1. Phát hiện cầu thang bị chặn bởi đồ cũ
            CleanVilla,             // 2. Dọn đồ phế liệu tầng 1 ra bãi rác sân sau
            ReadSecretLetter,       // 3. (Phụ trợ) Đọc di thư
            GetAxeAndChopTrees,     // 4. Lấy rìu trên xe bán tải
            RepairFloor,            // 5. Phá cầu thang, nhặt ván gỗ lên tầng 2 sửa sàn
            ExploreAncestralRoom,   // 6. Khám phá phòng thờ tầng 2
            UnlockBasementCellar,   // 7. Xuống mở cửa hầm bí mật
            DescendIntoBasement,    // 8. Khám phá tầng hầm
            Completed               // 9. Hoàn thành
        }

        [Header("Current Quest State")]
        public QuestPhase currentPhase = QuestPhase.ExploreVilla;

        [Header("Phase 0: Exploration & Flashlight")]
        public bool hasFlashlight = false;
        public bool hasEnteredVilla = false;
        public QuestWaypointMarker flashlightWaypoint;

        [Header("Floor 1 Room Exploration")]
        public bool hasVisitedFoyer = false;
        public bool hasVisitedLivingRoom = false;
        public bool hasVisitedKitchen = false;
        public int GetFloor1RoomsExploredCount()
        {
            int count = 0;
            if (hasVisitedFoyer) count++;
            if (hasVisitedLivingRoom) count++;
            if (hasVisitedKitchen) count++;
            return count;
        }
        public bool AreAllFloor1RoomsExplored() => hasVisitedFoyer && hasVisitedLivingRoom && hasVisitedKitchen;

        [Header("Staircase Blockage")]
        public GameObject stairsBlockageObject;

        [Header("Phase 2: Cleanup")]
        public int clutterCleanedCount = 0;
        public int clutterRequired = 4;
        public bool isCarryingClutter = false;
        public HouseClutterItem currentCarriedClutterItem;
        public GameObject carriedClutterVisual;
        private Transform carriedClutterHolder;
        private MeshFilter carriedMeshFilter;
        private MeshRenderer carriedMeshRenderer;
        public Light carriedClutterLight;
        private float clutterBobTimer = 0f;
        private Vector3 clutterBasePos = new Vector3(0.04f, -0.08f, 0.44f);
        private Vector3 currentClutterTargetOffset = new Vector3(0.04f, -0.08f, 0.44f);

        [Header("Phase 3: Secret Letter")]
        public bool hasReadSecretLetter = false;

        [Header("Phase 4: Axe")]
        public bool hasAxe = false;
        public int woodPlanksCollected = 0;
        public int woodPlanksRequired = 3;
        public GameObject inHandAxeVisual;
        private Coroutine axeSwingRoutine;
        private Vector3 axeDefaultPos = new Vector3(0.26f, -0.24f, 0.46f);
        private Quaternion axeDefaultRot = Quaternion.Euler(15f, -10f, -5f);

        [Header("Phase 5: Floor Repair")]
        public bool hasToolbox = true;
        public bool isFloorRepaired = false;
        public int repairedFloorSpots = 0;
        public int totalFloorSpots = 1;

        [Header("Phase 6, 7 & 8: Basement Exploration")]
        public bool hasBasementKey = false;
        public bool hasTalisman = false;
        public bool hasEnteredBasement = false;
        public QuestWaypointMarker basementKeyWaypoint;
        public QuestWaypointMarker talismanWaypoint;
        public QuestWaypointMarker cellarTrapdoorWaypoint;
        public QuestWaypointMarker basementAltarWaypoint;
        public QuestWaypointMarker truckEscapeWaypoint;

        [Header("Quest Waypoints (Yellow Guide Dots)")]
        public QuestWaypointMarker burnSiteWaypoint;
        public QuestWaypointMarker secretLetterWaypoint;
        public QuestWaypointMarker axeWaypoint;
        public QuestWaypointMarker floorRepairWaypoint;
        public QuestWaypointMarker stairsWaypoint;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxHammerRepair;
        public AudioClip sfxWoodPickup;
        public AudioClip sfxAxePickup;
        public AudioClip sfxClutterPickup;
        public AudioClip sfxClutterDrop;

        private string playerName;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            playerName = PlayerPrefs.GetString("PlayerName", "An");
            clutterRequired = 4;
            LoadAudioClips();
            SetupCarriedClutterVisual();
            SetupInHandAxeVisual();
            FindBlockageAndWaypoints();

            // Chờ cho đến khi menu Ký Di Chúc đóng và hết phim mở đầu mới hiển thị thông báo
            StartCoroutine(RoutineWaitForIntroFinish());
        }

        private void Update()
        {
            UpdateCarriedClutterBobbing();

            // Lưu ý: Phím G/Q vứt phế liệu được xử lý tập trung trong QuickSlotSystem.HandleDropInput để triệt tiêu race condition vứt nhầm đèn pin

            // Chuột Trái: Chém rìu khi đang cầm rìu
            if (hasAxe && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Không chém nếu đang mở menu sổ tay hoặc di chúc
                if ((PlayerNotebook.Instance == null || !PlayerNotebook.Instance.IsOpen) &&
                    (TestamentMenuController.Instance == null || !TestamentMenuController.Instance.isMenuOpen))
                {
                    AttackWithAxe();
                }
            }
        }

        private IEnumerator RoutineWaitForIntroFinish()
        {
            // Chờ đến khi người chơi đã ký di chúc xong và phim intro kết thúc
            while ((TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) ||
                   (CinematicIntroManager.Instance != null && CinematicIntroManager.Instance.IsPlayingIntro))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.6f);

            // Ghi chú đầu tiên vào sổ
            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddObjective("explore_villa",
                    "Khảo sát biệt thự",
                    "Đi vào bên trong căn biệt thự để kiểm tra tình trạng ngôi nhà.");

                PlayerNotebook.Instance.AddClue("arrival",
                    "Mình vừa đến biệt thự cũ của gia đình. Ngôi nhà bỏ hoang lâu năm quá rồi... Phải vào kiểm tra xem thế nào.");
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void FindBlockageAndWaypoints()
        {
            if (stairsBlockageObject == null)
            {
                stairsBlockageObject = GameObject.Find("Stairs_Blockage_Rubble");
            }

            if (flashlightWaypoint == null)
            {
                var fl = GameObject.Find("Flashlight_Pickup");
                if (fl != null)
                {
                    flashlightWaypoint = fl.GetComponent<QuestWaypointMarker>();
                    var beacon = fl.transform.Find("BeaconLight");
                    if (beacon != null) beacon.gameObject.SetActive(false);
                }
            }

            if (burnSiteWaypoint == null)
            {
                var bs = GameObject.Find("Backyard_BurnSite");
                if (bs != null) burnSiteWaypoint = bs.GetComponent<QuestWaypointMarker>();
            }

            if (secretLetterWaypoint == null)
            {
                var sl = GameObject.Find("Secret_Letter_Item");
                if (sl == null && burnSiteWaypoint != null)
                {
                    var ch = burnSiteWaypoint.transform.Find("Secret_Letter_Item");
                    if (ch != null) sl = ch.gameObject;
                }
                if (sl != null) secretLetterWaypoint = sl.GetComponent<QuestWaypointMarker>();
            }

            if (axeWaypoint == null)
            {
                var ax = GameObject.Find("Axe_Pickup");
                if (ax != null) axeWaypoint = ax.GetComponent<QuestWaypointMarker>();
            }

            if (floorRepairWaypoint == null)
            {
                var fs = GameObject.Find("BrokenFloorSpot_1");
                if (fs != null) floorRepairWaypoint = fs.GetComponent<QuestWaypointMarker>();
            }

            if (basementKeyWaypoint == null)
            {
                var bk = GameObject.Find("Basement_Key_Pickup");
                if (bk != null) basementKeyWaypoint = bk.GetComponent<QuestWaypointMarker>();
            }

            if (talismanWaypoint == null)
            {
                var tl = GameObject.Find("Ancestral_Talisman_Pickup");
                if (tl != null) talismanWaypoint = tl.GetComponent<QuestWaypointMarker>();
            }

            if (cellarTrapdoorWaypoint == null)
            {
                var ct = GameObject.Find("Cellar_Trapdoor");
                if (ct != null) cellarTrapdoorWaypoint = ct.GetComponent<QuestWaypointMarker>();
            }

            if (basementAltarWaypoint == null)
            {
                var ba = GameObject.Find("Basement_Ritual_Altar");
                if (ba != null) basementAltarWaypoint = ba.GetComponent<QuestWaypointMarker>();
            }

            if (truckEscapeWaypoint == null)
            {
                var trk = GameObject.Find("Truck_Escape_Trigger");
                if (trk != null) truckEscapeWaypoint = trk.GetComponent<QuestWaypointMarker>();
            }
        }

        private void SetupCarriedClutterVisual()
        {
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player == null) return;
            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam == null) return;

            Transform existing = cam.transform.Find("Carried_Clutter_Holder");
            if (existing != null)
            {
                carriedClutterHolder = existing;
                carriedClutterHolder.localPosition = currentClutterTargetOffset;
                carriedClutterHolder.localRotation = Quaternion.Euler(10f, 18f, -4f);
                carriedMeshFilter = carriedClutterHolder.GetComponentInChildren<MeshFilter>();
                carriedMeshRenderer = carriedClutterHolder.GetComponentInChildren<MeshRenderer>();
            }
            else
            {
                GameObject holder = new GameObject("Carried_Clutter_Holder");
                holder.transform.SetParent(cam.transform, false);
                holder.transform.localPosition = currentClutterTargetOffset;
                holder.transform.localRotation = Quaternion.Euler(10f, 18f, -4f);
                carriedClutterHolder = holder.transform;

                GameObject visualChild = new GameObject("HeldObjectVisual");
                visualChild.transform.SetParent(carriedClutterHolder, false);
                carriedMeshFilter = visualChild.AddComponent<MeshFilter>();
                carriedMeshRenderer = visualChild.AddComponent<MeshRenderer>();
                carriedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (carriedMeshRenderer != null)
            {
                carriedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Đèn chiếu sáng phụ (viewmodel fill light) gắn trên holder để rọi sáng phế liệu bê trên tay
            // Giúp vật phẩm hiển thị rõ ràng, chân thực trong bóng tối đồng bộ với đèn pin của người chơi
            carriedClutterLight = carriedClutterHolder.GetComponent<Light>();
            if (carriedClutterLight == null)
            {
                carriedClutterLight = carriedClutterHolder.gameObject.AddComponent<Light>();
            }
            carriedClutterLight.type = LightType.Point;
            carriedClutterLight.range = 1.6f;
            carriedClutterLight.intensity = 0.85f;
            carriedClutterLight.color = new Color(1f, 0.96f, 0.88f);
            carriedClutterLight.shadows = LightShadows.None;
            carriedClutterLight.enabled = (PlayerFlashlight.Instance != null && PlayerFlashlight.Instance.IsOn);

            carriedClutterHolder.gameObject.SetActive(false);
            carriedClutterVisual = carriedClutterHolder.gameObject;
        }

        private void UpdateCarriedClutterBobbing()
        {
            if (!isCarryingClutter || carriedClutterHolder == null) return;

            if (carriedClutterLight != null)
            {
                bool flashlightOn = (PlayerFlashlight.Instance != null && PlayerFlashlight.Instance.IsOn);
                if (carriedClutterLight.enabled != flashlightOn)
                {
                    carriedClutterLight.enabled = flashlightOn;
                }
            }

            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;

            float speed = (cc != null && cc.isGrounded) ? cc.velocity.magnitude : 0f;
            if (speed > 0.2f)
            {
                clutterBobTimer += Time.deltaTime * 7.5f;
                float waveY = Mathf.Sin(clutterBobTimer) * 0.005f;
                float waveX = Mathf.Cos(clutterBobTimer * 0.5f) * 0.004f;
                carriedClutterHolder.localPosition = currentClutterTargetOffset + new Vector3(waveX, waveY, 0f);
            }
            else
            {
                clutterBobTimer = 0f;
                carriedClutterHolder.localPosition = Vector3.Lerp(carriedClutterHolder.localPosition, currentClutterTargetOffset, Time.deltaTime * 6f);
            }
        }

        private void SetupInHandAxeVisual()
        {
            if (inHandAxeVisual != null) return;

            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    Transform existing = cam.transform.Find("InHand_Axe_Model");
                    if (existing != null)
                    {
                        inHandAxeVisual = existing.gameObject;
                    }
                    else
                    {
#if UNITY_EDITOR
                        var axePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Axe/Axe_Pickup.prefab");
                        if (axePrefab != null)
                        {
                            GameObject axe = Instantiate(axePrefab, cam.transform);
                            axe.name = "InHand_Axe_Model";
                            axe.transform.localPosition = axeDefaultPos;
                            axe.transform.localRotation = axeDefaultRot;
                            axe.transform.localScale = Vector3.one * 0.45f;

                            foreach (var c in axe.GetComponentsInChildren<Collider>()) Destroy(c);
                            foreach (var s in axe.GetComponentsInChildren<ItemPickup>()) Destroy(s);

                            inHandAxeVisual = axe;
                        }
#endif
                    }

                    if (inHandAxeVisual != null) inHandAxeVisual.SetActive(false);
                }
            }
        }

        #region Phase 0: Exploration, Flashlight & Floor 1 Rooms

        public void OnPlayerEnterRoom(string roomName)
        {
            if (string.IsNullOrEmpty(roomName)) return;

            if (roomName.Contains("Sảnh"))
            {
                hasVisitedFoyer = true;
                OnPlayerEnterVillaFoyer();
            }
            else if (roomName.Contains("Khách"))
            {
                if (!hasVisitedLivingRoom)
                {
                    hasVisitedLivingRoom = true;
                    OnFloor1RoomExplored("Phòng Khách");
                }
            }
            else if (roomName.Contains("Bếp") || roomName.Contains("Ăn"))
            {
                if (!hasVisitedKitchen)
                {
                    hasVisitedKitchen = true;
                    OnFloor1RoomExplored("Phòng Ăn & Bếp");
                }
            }
            else if (roomName.Contains("Cầu Thang"))
            {
                OnPlayerApproachStairs();
            }
        }

        private void OnFloor1RoomExplored(string roomName)
        {
            int count = GetFloor1RoomsExploredCount();

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.UpdateObjectiveProgress("explore_floor1", $"{count}/3");
                PlayerNotebook.Instance.AddClue("explored_" + roomName, $"Đã khảo sát {roomName}.");
            }

            if (AreAllFloor1RoomsExplored())
            {
                StoryObjectiveBanner.ShowObjective(
                    playerName.ToUpper(),
                    "Đã kiểm tra xong các phòng tầng 1. Giờ mình hãy tìm cầu thang lên tầng 2 xem sao...",
                    5.0f
                );
            }
            else
            {
                StoryObjectiveBanner.ShowObjective(
                    "KHẢO SÁT TẦNG 1",
                    $"Đã vào {roomName} ({count}/3). Hãy đi kiểm tra nốt các phòng còn lại.",
                    3.5f
                );
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnPlayerApproachStairs()
        {
            if (currentPhase > QuestPhase.ExploreVilla) return;

            if (!hasFlashlight)
            {
                StoryObjectiveBanner.ShowObjective(
                    playerName.ToUpper(),
                    "Tối quá, một bước cũng không thấy rõ... Phải tìm gấp đèn pin soi sáng trước đã!",
                    4.5f
                );
                return;
            }

            if (!AreAllFloor1RoomsExplored())
            {
                StoryObjectiveBanner.ShowObjective(
                    playerName.ToUpper(),
                    $"Mình nên đi kiểm tra hết các phòng ở tầng trệt trước khi lên lầu ({GetFloor1RoomsExploredCount()}/3 phòng)... Nhỡ đâu có điều gì bất thường!",
                    4.8f
                );
                return;
            }

            OnDiscoverBlockedStairs();
        }

        public void OnPlayerEnterVillaFoyer()
        {
            if (hasEnteredVilla) return;
            hasEnteredVilla = true;
            hasVisitedFoyer = true;

            if (currentPhase == QuestPhase.ExploreVilla && !hasFlashlight)
            {
                StartCoroutine(RoutineFoyerDarkness());
            }
        }

        private IEnumerator RoutineFoyerDarkness()
        {
            yield return new WaitForSeconds(0.5f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Trời đất ơi, sao trong nhà lại tối om như mực thế này?! Không có một chút ánh sáng nào... Phải tìm gấp cái gì đó để soi sáng trước đã!",
                5.5f
            );

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("dark_house",
                    "Nhà tối om, không có điện. Hình như có cái Đèn Pin ở đâu đó trên chiếc bàn ở sảnh chính...");
            }

            yield return new WaitForSeconds(3.0f);

            if (!hasFlashlight)
            {
                UpdateWaypointVisibility();
            }
        }

        public void OnFlashlightPickedUp()
        {
            hasFlashlight = true;
            hasVisitedFoyer = true;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("find_flashlight");
                PlayerNotebook.Instance.AddClue("flashlight_found",
                    "Đã tìm được Đèn Pin trên bàn sảnh. Giờ có thể soi sáng mà đi khảo sát.");

                PlayerNotebook.Instance.AddObjective("explore_floor1",
                    "Khảo sát các phòng tầng 1",
                    "Đi kiểm tra các phòng ở tầng trệt (Sảnh Chính, Phòng Khách, Phòng Ăn & Bếp).");
                PlayerNotebook.Instance.UpdateObjectiveProgress("explore_floor1", $"{GetFloor1RoomsExploredCount()}/3");
            }

            StartCoroutine(RoutineAfterFlashlightPickedUp());
        }

        private IEnumerator RoutineAfterFlashlightPickedUp()
        {
            yield return new WaitForSeconds(1.0f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Ơn trời, tìm thấy đèn pin rồi! May quá nó vẫn còn dùng được... Giờ phải đi kiểm tra từng góc của tầng 1 xem sao.",
                5.0f
            );

            yield return new WaitForSeconds(5.2f);
            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        #endregion

        #region Phase 1: Discover Blocked Stairs

        public void OnDiscoverBlockedStairs()
        {
            if (currentPhase >= QuestPhase.DiscoverBlockedStairs) return;
            currentPhase = QuestPhase.DiscoverBlockedStairs;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("explore_upstairs");
                PlayerNotebook.Instance.AddClue("blocked_stairs",
                    "Cầu thang lên tầng 2 bị chặn kín bởi đống bàn ghế và cây đàn cũ nát! Phải dọn hết đồ phế liệu tầng 1 trước.");
            }

            StartCoroutine(RoutineBlockedStairsDiscovery());
        }

        private IEnumerator RoutineBlockedStairsDiscovery()
        {
            yield return new WaitForSeconds(0.4f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Cái quái gì thế này?! Đống bàn ghế với cây đàn mục nát này chặn đứng cả lối lên cầu thang rồi!",
                5.5f
            );

            yield return new WaitForSeconds(5.7f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Chật chội thế này không lách qua nổi... Phải dọn đống phế liệu bừa bộn ở tầng 1 đem ra bãi rác sân sau trước đã rồi tính tiếp!",
                5.5f
            );

            yield return new WaitForSeconds(5.7f);
            StartCleanupQuest();
        }

        public void StartCleanupQuest()
        {
            if (currentPhase < QuestPhase.CleanVilla)
            {
                currentPhase = QuestPhase.CleanVilla;

                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.AddObjective("clean_villa",
                        "Dọn dẹp phế liệu tầng 1",
                        "Bê các đồ đạc hư hỏng bừa bộn ở tầng 1 đem ra bãi rác sân sau để vứt.");
                    PlayerNotebook.Instance.UpdateObjectiveProgress("clean_villa", $"0/{clutterRequired}");
                }

                UpdateObjectiveDisplay();
                UpdateWaypointVisibility();
            }
        }

        #endregion

        #region Phase 2: Cleanup

        public void PickUpClutter(HouseClutterItem item)
        {
            if (isCarryingClutter) return;
            isCarryingClutter = true;
            currentCarriedClutterItem = item;

            if (carriedClutterHolder == null)
            {
                SetupCarriedClutterVisual();
            }

            if (carriedClutterHolder != null)
            {
                MeshFilter srcMf = item.GetComponent<MeshFilter>() ?? item.GetComponentInChildren<MeshFilter>();
                MeshRenderer srcMr = item.GetComponent<MeshRenderer>() ?? item.GetComponentInChildren<MeshRenderer>();

                if (srcMf != null && srcMf.sharedMesh != null)
                {
                    if (carriedMeshFilter == null) carriedMeshFilter = carriedClutterHolder.GetComponentInChildren<MeshFilter>();
                    if (carriedMeshRenderer == null) carriedMeshRenderer = carriedClutterHolder.GetComponentInChildren<MeshRenderer>();

                    carriedMeshFilter.sharedMesh = srcMf.sharedMesh;
                    carriedMeshRenderer.sharedMaterials = srcMr.sharedMaterials;
                    carriedMeshRenderer.enabled = true;
                    carriedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    Bounds b = srcMf.sharedMesh.bounds;
                    float maxDim = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    float targetSize = 0.38f;
                    Vector3 holderOffset = new Vector3(0.04f, -0.08f, 0.44f);
                    Quaternion holderRot = Quaternion.Euler(10f, 18f, -4f);

                    if (item.name.Contains("Heater") || item.name.Contains("Suoi"))
                    {
                        targetSize = 0.38f;
                        holderOffset = new Vector3(0.04f, -0.08f, 0.44f);
                        holderRot = Quaternion.Euler(10f, 25f, -3f);
                    }
                    else if (item.name.Contains("Table"))
                    {
                        targetSize = 0.40f;
                        holderOffset = new Vector3(0.04f, -0.08f, 0.44f);
                        holderRot = Quaternion.Euler(-18f, 15f, 0f);
                    }
                    else if (item.name.Contains("Drawer") || item.name.Contains("Box"))
                    {
                        targetSize = 0.36f;
                        holderOffset = new Vector3(0.04f, -0.08f, 0.44f);
                        holderRot = Quaternion.Euler(12f, -15f, 4f);
                    }
                    else if (item.name.Contains("Chair"))
                    {
                        targetSize = 0.35f;
                        holderOffset = new Vector3(0.04f, -0.08f, 0.44f);
                        holderRot = Quaternion.Euler(10f, 20f, -4f);
                    }

                    currentClutterTargetOffset = holderOffset;
                    clutterBasePos = holderOffset;

                    float s = (maxDim > 0.01f) ? (targetSize / maxDim) : 0.38f;
                    carriedMeshFilter.transform.localScale = Vector3.one * s;
                    // Đưa tâm hình học của mesh về đúng gốc (0,0,0) của holder với góc xoay identity để tránh lệch tâm
                    carriedMeshFilter.transform.localPosition = -b.center * s;
                    carriedMeshFilter.transform.localRotation = Quaternion.identity;

                    carriedClutterHolder.localPosition = holderOffset;
                    carriedClutterHolder.localRotation = holderRot;
                }

                if (carriedClutterLight != null)
                {
                    carriedClutterLight.enabled = (PlayerFlashlight.Instance != null && PlayerFlashlight.Instance.IsOn);
                }

                carriedClutterHolder.gameObject.SetActive(true);
            }

            // Tắt waypoint marker của vật phẩm khi đang được bê trên tay
            var itemWp = item.GetComponent<QuestWaypointMarker>();
            if (itemWp != null) itemWp.SetVisible(false);

            // Đồng bộ thêm vào thanh QuickSlot để hiển thị icon và tên rõ ràng
            if (QuickSlotSystem.Instance != null)
            {
                string clutterId = "clutter_" + item.name.ToLower();
                Sprite clutterIcon = item.clutterIcon != null ? item.clutterIcon : QuickSlotSystem.GetDefaultItemIcon(clutterId);
                if (clutterIcon == null) clutterIcon = QuickSlotSystem.GetDefaultItemIcon("clutter");
                QuickSlotSystem.Instance.AddClutterItem(clutterId, item.clutterName, clutterIcon);
            }

            // Tạm thời ẩn các vật phẩm trên tay (đèn pin, rìu) để 2 tay bê phế liệu
            if (PlayerHandEquipment.Instance != null)
            {
                PlayerHandEquipment.Instance.EquipItem("");
            }
            if (inHandAxeVisual != null)
            {
                inHandAxeVisual.SetActive(false);
            }

            // Giữ đèn pin rọi sáng phía trước ngực khi đang bê phế liệu (nếu người chơi có đèn)
            if (PlayerFlashlight.Instance != null)
            {
                if (PlayerFlashlight.Instance.HasFlashlightInInventory())
                {
                    // Tự động bật đèn rọi đường đi cho người chơi thấy rõ
                    PlayerFlashlight.Instance.SetFlashlight(true, false);
                }
                else
                {
                    PlayerFlashlight.Instance.RefreshLightState();
                }
            }

            if (sfxClutterPickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxClutterPickup, 0.85f);
            }

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("carrying_clutter",
                    $"Đang bê {item.clutterName}. Phải đem ra bãi rác sân sau để vứt.");
            }

            // Dọa sợ Bước 1: Khi đang dọn dẹp (đã dọn xong 1-2 món), bức ảnh gia đình trên tường rớt đùng lộ manh mối
            if (clutterCleanedCount >= 1 && FallingPictureScare.Instance != null && !FallingPictureScare.Instance.hasTriggered)
            {
                FallingPictureScare.Instance.TriggerScare();
            }

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ BÊ " + item.clutterName.ToUpper(),
                $"Nặng thật đấy... Mau đem ra bãi rác sân sau vứt thôi! [F] hoặc bấm [G] để đặt tạm xuống đất.",
                4.8f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void DropCurrentClutterOnGround()
        {
            if (!isCarryingClutter) return;

            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            Vector3 playerPos = player != null ? player.transform.position : transform.position;

            // Nếu người chơi đang ở ngoài bãi rác sân sau (gần bãi đốt Backyard_BurnSite trong bán kính 7m)
            // mà bấm G/Q vứt rác, tự động chuyển thành vứt vào bãi rác để tính tiến trình nhiệm vụ!
            var burnSite = BackyardBurnSite.Instance != null ? BackyardBurnSite.Instance.gameObject : GameObject.Find("Backyard_BurnSite");
            if (burnSite != null && Vector3.Distance(playerPos, burnSite.transform.position) < 7.0f)
            {
                DropClutterAtBackyard();
                return;
            }

            isCarryingClutter = false;
            lastClutterDropFrame = Time.frameCount;

            if (carriedClutterLight != null)
            {
                carriedClutterLight.enabled = false;
            }

            if (carriedClutterHolder != null)
            {
                carriedClutterHolder.gameObject.SetActive(false);
            }

            if (currentCarriedClutterItem != null)
            {
                Camera cam = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;

                // Bắn tia raycast từ camera theo hướng nhìn của người chơi (tối đa 2.5m) để đặt chính xác nơi người chơi đang nhìn
                Vector3 targetSpot;
                float groundY = playerPos.y;

                Ray camRay = (cam != null) ? new Ray(cam.transform.position, cam.transform.forward) : new Ray(playerPos + Vector3.up * 1.5f, player != null ? player.transform.forward : Vector3.forward);
                if (Physics.Raycast(camRay, out RaycastHit camHit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    targetSpot = camHit.point;
                    groundY = camHit.point.y;
                }
                else
                {
                    // Nếu nhìn lên trời hoặc quá xa, hạ xuống mặt sàn phía trước mặt 1m
                    Vector3 forwardFlat = (cam != null ? cam.transform.forward : (player != null ? player.transform.forward : Vector3.forward));
                    forwardFlat.y = 0f;
                    if (forwardFlat.sqrMagnitude < 0.01f) forwardFlat = (player != null ? player.transform.forward : Vector3.forward);
                    forwardFlat.Normalize();
                    targetSpot = playerPos + forwardFlat * 1.05f;

                    if (Physics.Raycast(targetSpot + Vector3.up * 1.5f, Vector3.down, out RaycastHit floorHit, 4.0f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        groundY = floorHit.point.y;
                    }
                }

                // Căn chỉnh đáy vật thể dựa trên Bounds để vật phẩm đứng vững trên sàn, không lún hay rơi xuyên sàn (+0.03m clearance)
                float bottomOffset = 0.03f;
                var mf = currentCarriedClutterItem.GetComponent<MeshFilter>() ?? currentCarriedClutterItem.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    bottomOffset = -mf.sharedMesh.bounds.min.y * currentCarriedClutterItem.transform.lossyScale.y + 0.03f;
                }

                Vector3 finalPos = new Vector3(targetSpot.x, groundY + bottomOffset, targetSpot.z);
                currentCarriedClutterItem.transform.position = finalPos;

                float yaw = (cam != null ? cam.transform.eulerAngles.y : (player != null ? player.transform.eulerAngles.y : 0f));
                currentCarriedClutterItem.transform.rotation = Quaternion.Euler(0f, yaw + 15f, 0f);

                currentCarriedClutterItem.gameObject.SetActive(true);
                currentCarriedClutterItem.isPickedUp = false;

                // Bật lại waypoint marker của item để người chơi thấy lại chấm vàng chỉ đường
                var wp = currentCarriedClutterItem.GetComponent<QuestWaypointMarker>();
                if (wp != null) wp.SetVisible(true);

                // Đảm bảo hiển thị đầy đủ MeshRenderer
                var renderers = currentCarriedClutterItem.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    mr.enabled = true;
                    mr.gameObject.SetActive(true);
                }

                // KHÓA CỨNG KINEMATIC TUYỆT ĐỐI - KHÔNG BAO GIỜ dùng Rigidbody rơi tự do để tránh rớt xuyên sàn xuống hầm
                var rbs = currentCarriedClutterItem.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // Đảm bảo bật Collider để người chơi có thể chỉa tâm vào và bấm F nhặt lại bình thường
                var cols = currentCarriedClutterItem.GetComponentsInChildren<Collider>(true);
                foreach (var col in cols)
                {
                    col.enabled = true;
                }

                if (sfxClutterDrop != null && audioSource != null)
                {
                    audioSource.PlayOneShot(sfxClutterDrop, 0.8f);
                }

                StoryObjectiveBanner.ShowObjective(
                    "ĐÃ ĐẶT " + currentCarriedClutterItem.clutterName.ToUpper() + " XUỐNG ĐẤT",
                    $"Đã tạm gác món đồ xuống đất [G]. Có thể quay lại nhặt bất cứ lúc nào [F].",
                    3.8f
                );

                currentCarriedClutterItem = null;
            }

            // Xóa phế liệu khỏi QuickSlot khi đặt xuống đất
            if (QuickSlotSystem.Instance != null)
            {
                QuickSlotSystem.Instance.RemoveItemByPrefix("clutter");
            }

            // Khôi phục trang bị vũ khí/công cụ trên tay từ thanh QuickSlot (ưu tiên đèn pin nếu có)
            if (QuickSlotSystem.Instance != null)
            {
                int flSlot = -1;
                for (int i = 0; i < QuickSlotSystem.Instance.Slots.Count; i++)
                {
                    if (QuickSlotSystem.Instance.Slots[i].itemId == "flashlight")
                    {
                        flSlot = i;
                        break;
                    }
                }
                if (flSlot >= 0)
                {
                    QuickSlotSystem.Instance.SelectSlot(flSlot, true);
                }
                else if (QuickSlotSystem.Instance.CurrentSelectedSlot >= 0)
                {
                    QuickSlotSystem.Instance.SelectSlot(QuickSlotSystem.Instance.CurrentSelectedSlot, false);
                }
            }

            if (PlayerFlashlight.Instance != null)
            {
                PlayerFlashlight.Instance.RefreshLightState();
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void DropClutterAtBackyard()
        {
            if (!isCarryingClutter) return;
            isCarryingClutter = false;
            lastClutterDropFrame = Time.frameCount;

            if (carriedClutterLight != null)
            {
                carriedClutterLight.enabled = false;
            }

            if (carriedClutterHolder != null)
            {
                carriedClutterHolder.gameObject.SetActive(false);
            }

            // Đưa vật thể thực tế vào bãi rác sân sau với tọa độ cố định, vĩnh viễn không bị biến mất
            if (currentCarriedClutterItem != null)
            {
                // Mảng các vị trí cố định trên mặt đất thoáng trước bãi rác sân sau (Ground Y = 10.04f, ngay trước mặt tủ di thư)
                Vector3[] trashPositions = new Vector3[]
                {
                    new Vector3(80.5f, 10.04f, 38.6f), // Món 1 (bàn vỡ - góc thoáng trước bên trái tủ)
                    new Vector3(81.2f, 10.04f, 38.9f), // Món 2 (ghế da gãy - chính diện trước tủ)
                    new Vector3(81.8f, 10.04f, 38.8f), // Món 3 (hộp sắt hoen rỉ - góc phải)
                    new Vector3(82.4f, 10.04f, 39.2f)  // Món 4 (bàn gỗ cũ / tủ mục - góc phía sau)
                };

                int posIdx = Mathf.Clamp(clutterCleanedCount, 0, trashPositions.Length - 1);
                Vector3 finalDropPos = trashPositions[posIdx];
                float groundY = 10.04f;
                // Raycast bắt đầu từ Y=13.5f (bên dưới mái nhà kính greenhouse Y=16.5m) để bắn trúng mặt đất bãi rác
                if (Physics.Raycast(new Vector3(finalDropPos.x, 13.5f, finalDropPos.z), Vector3.down, out RaycastHit groundHit, 5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    groundY = groundHit.point.y;
                }

                // Tính toán độ cao đáy vật thể để vật phẩm đứng vững trên mặt sàn, không bị lún hay chìm xuống đất (+0.03m clearance)
                float bottomOffset = 0.03f;
                var mf = currentCarriedClutterItem.GetComponent<MeshFilter>() ?? currentCarriedClutterItem.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    bottomOffset = -mf.sharedMesh.bounds.min.y * currentCarriedClutterItem.transform.lossyScale.y + 0.03f;
                }
                finalDropPos.y = groundY + bottomOffset;

                currentCarriedClutterItem.transform.position = finalDropPos;
                currentCarriedClutterItem.transform.rotation = Quaternion.Euler(0f, 15f + posIdx * 45f, 0f);
                currentCarriedClutterItem.gameObject.SetActive(true);
                currentCarriedClutterItem.isPickedUp = true;

                // Đảm bảo hiển thị đầy đủ MeshRenderer
                var renderers = currentCarriedClutterItem.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in renderers)
                {
                    mr.enabled = true;
                    mr.gameObject.SetActive(true);
                }

                // Khóa cứng vật lý để vật phẩm nằm vững chắc trên bãi rác vĩnh viễn, không bao giờ rơi qua sàn hay biến mất
                var rbs = currentCarriedClutterItem.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // TẮT COLLIDER trên món rác đã vứt để người chơi không bị vướng chân và không che khuất tia raycast tương tác vào tủ/bãi đốt
                var cols = currentCarriedClutterItem.GetComponentsInChildren<Collider>(true);
                foreach (var col in cols)
                {
                    col.enabled = false;
                }

                // Tắt và hủy QuestWaypointMarker trên rác đã vứt để tránh chấm vàng mồ côi
                var wp = currentCarriedClutterItem.GetComponent<QuestWaypointMarker>();
                if (wp != null)
                {
                    wp.SetVisible(false);
                    if (Application.isPlaying) Destroy(wp);
                    else DestroyImmediate(wp);
                }

                var clutterComp = currentCarriedClutterItem;
                currentCarriedClutterItem = null;
                if (Application.isPlaying) Destroy(clutterComp);
                else DestroyImmediate(clutterComp);
            }

            if (sfxClutterDrop != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxClutterDrop, 0.9f);
            }

            clutterCleanedCount++;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.UpdateObjectiveProgress("clean_villa", $"{clutterCleanedCount}/{clutterRequired}");
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();

            // Xóa phế liệu khỏi QuickSlot khi vứt vào bãi rác
            if (QuickSlotSystem.Instance != null)
            {
                QuickSlotSystem.Instance.RemoveItemByPrefix("clutter");
            }

            // Khôi phục trang bị vũ khí/công cụ trên tay từ thanh QuickSlot (ưu tiên đèn pin nếu có)
            if (QuickSlotSystem.Instance != null)
            {
                int flSlot = -1;
                for (int i = 0; i < QuickSlotSystem.Instance.Slots.Count; i++)
                {
                    if (QuickSlotSystem.Instance.Slots[i].itemId == "flashlight")
                    {
                        flSlot = i;
                        break;
                    }
                }
                if (flSlot >= 0)
                {
                    QuickSlotSystem.Instance.SelectSlot(flSlot, true);
                }
                else if (QuickSlotSystem.Instance.CurrentSelectedSlot >= 0)
                {
                    QuickSlotSystem.Instance.SelectSlot(QuickSlotSystem.Instance.CurrentSelectedSlot, false);
                }
            }

            if (PlayerFlashlight.Instance != null)
            {
                PlayerFlashlight.Instance.RefreshLightState();
            }

            if (clutterCleanedCount < clutterRequired)
            {
                StoryObjectiveBanner.ShowObjective(
                    "DỌN DẸP NHÀ CỬA",
                    $"Phù, vứt được một món rồi... ({clutterCleanedCount}/{clutterRequired}). Mau quay lại dọn nốt cho xong!",
                    4.5f
                );
            }
            else
            {
                // Dọn sạch hết tầng 1!
                // Dọa sợ Bước 2: Cửa sổ tầng 2 đóng sầm & bóng đen nhìn xuống
                if (BackyardWindowScare.Instance != null && !BackyardWindowScare.Instance.hasTriggered)
                {
                    BackyardWindowScare.Instance.TriggerScare();
                }

                if (BackyardBurnSite.Instance != null)
                {
                    BackyardBurnSite.Instance.TriggerSecretCabinetEvent();
                }

                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.CompleteObjective("clean_villa");
                    PlayerNotebook.Instance.AddClue("clutter_cleaned",
                        "Đã dọn sạch các đồ phế liệu ở tầng 1! Giờ phải tìm công cụ để phá đống chướng ngại vật chắn cầu thang.");
                }

                StartCoroutine(RoutineAfterClutterCleaned());
            }
        }

        private IEnumerator RoutineAfterClutterCleaned()
        {
            yield return new WaitForSeconds(0.8f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Phù... Mệt đứt cả hơi! Cuối cùng tầng 1 cũng sạch sẽ rồi. Nhưng đống bàn ghế chắn cầu thang này kẹt chặt quá, tay không thì chịu thua...",
                5.5f
            );

            yield return new WaitForSeconds(5.7f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Khoan đã! Bác quản gia từng dặn là trên thùng chiếc xe bán tải ngoài cổng có để sẵn cây rìu và đồ làm vườn... Mau ra lấy mới được!",
                5.8f
            );

            yield return new WaitForSeconds(6.0f);
            TransitionToFetchAxe();
        }

        public void TransitionToFetchAxe()
        {
            if (currentPhase >= QuestPhase.GetAxeAndChopTrees) return;
            currentPhase = QuestPhase.GetAxeAndChopTrees;

            if (hasAxe)
            {
                // Người chơi đã nhặt rìu sớm từ trước
                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.CompleteObjective("get_axe");
                    PlayerNotebook.Instance.AddObjective("smash_stairs",
                        "Dùng Rìu phá chướng ngại vật cầu thang",
                        "Quay lại chân cầu thang và dùng cây rìu chém nát đống bàn ghế cây đàn [Chuột Trái hoặc F].");
                    PlayerNotebook.Instance.AddClue("axe_ready",
                        "Đã có sẵn Cây Rìu trên tay! Mau quay lại chân cầu thang phá tan đống chướng ngại vật!");
                }

                StoryObjectiveBanner.ShowObjective(
                    "ĐÃ CÓ SẴN CÂY RÌU",
                    "Cây rìu làm vườn đã sẵn sàng trên tay! Mau quay lại chân cầu thang bổ nát đống bàn ghế chắn đường [Chuột Trái / F].",
                    5.2f
                );
            }
            else
            {
                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.AddObjective("get_axe",
                        "Lấy Cây Rìu trên xe bán tải",
                        "Ra thùng chiếc xe bán tải ngoài cổng lấy cây rìu mà bác quản gia đã chuẩn bị sẵn.");
                    PlayerNotebook.Instance.AddClue("fetch_axe",
                        "Tầng 1 đã dọn sạch. Bác quản gia có dặn trên thùng xe bán tải có để sẵn cây rìu làm vườn, phải ra lấy để phá đống đồ chắn cầu thang.");
                }
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        #endregion

        #region Phase 3: Axe & Smashing Stairs Blockage

        public void CollectAxe()
        {
            if (hasAxe) return;
            hasAxe = true;

            if (inHandAxeVisual != null)
            {
                inHandAxeVisual.SetActive(true);
            }

            if (sfxAxePickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxAxePickup, 0.85f);
            }

            // CHỈ thêm vào QuickSlot nếu chưa có (tránh bị duplicate 2 cây rìu do ItemPickup đã add)
            if (QuickSlotSystem.Instance != null && !QuickSlotSystem.Instance.HasItem("axe"))
            {
                QuickSlotSystem.Instance.AddItem("axe", "Cây Rìu", null, null);
            }

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("get_axe");
                PlayerNotebook.Instance.AddObjective("smash_stairs",
                    "Dùng Rìu phá chướng ngại vật cầu thang",
                    "Quay lại chân cầu thang và dùng cây rìu chém nát đống bàn ghế cây đàn [Chuột Trái hoặc F].");
                PlayerNotebook.Instance.AddClue("axe_collected",
                    "Đã lấy được Cây Rìu làm vườn trên thùng xe. Giờ quay lại cầu thang đập tan đống chướng ngại vật!");
            }

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ CÓ CÂY RÌU LÀM VƯỜN",
                "Rìu bén ngót! Mau quay lại chân cầu thang bổ nát đống bàn ghế chắn đường [Chuột Trái / F].",
                5.5f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void AttackWithAxe()
        {
            if (!hasAxe) return;
            PlayAxeSwingAnimation();

            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            Camera cam = player != null ? player.GetComponentInChildren<Camera>() : Camera.main;
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                var blockage = hit.collider.GetComponentInParent<StairsBlockageInteractable>() ??
                               hit.collider.GetComponentInChildren<StairsBlockageInteractable>();

                if (blockage == null && stairsBlockageObject != null && hit.collider.transform.IsChildOf(stairsBlockageObject.transform))
                {
                    blockage = stairsBlockageObject.GetComponentInChildren<StairsBlockageInteractable>();
                }

                if (blockage != null && !blockage.isDestroyed)
                {
                    SpawnWoodChopEffect(hit.point, hit.normal);
                    blockage.DoSmashBlockage();
                }
            }
        }

        public void SpawnWoodChopEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (sfxHammerRepair != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxHammerRepair, 1.0f);
            }

            for (int i = 0; i < 8; i++)
            {
                GameObject splinter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                splinter.name = "WoodSplinter";
                splinter.transform.position = hitPoint + Random.insideUnitSphere * 0.15f;
                splinter.transform.localScale = new Vector3(Random.Range(0.02f, 0.05f), Random.Range(0.08f, 0.18f), Random.Range(0.02f, 0.05f));
                splinter.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));

                var ren = splinter.GetComponent<Renderer>();
                if (ren != null)
                {
                    ren.material.color = new Color(0.42f, 0.28f, 0.15f);
                }

                var col = splinter.GetComponent<Collider>();
                if (col != null) col.enabled = false;

                Rigidbody rb = splinter.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                Vector3 force = (hitNormal + Random.insideUnitSphere * 0.8f).normalized * Random.Range(2.5f, 5.0f);
                rb.linearVelocity = force;

                Destroy(splinter, 1.8f);
            }
        }

        public void PlayAxeSwingAnimation()
        {
            if (HorrorGame.Player.PlayerHandEquipment.Instance != null)
            {
                HorrorGame.Player.PlayerHandEquipment.Instance.PlayAttackAnimation();
            }

            if (inHandAxeVisual == null || !hasAxe) return;
            if (axeSwingRoutine != null) StopCoroutine(axeSwingRoutine);
            axeSwingRoutine = StartCoroutine(RoutineAxeSwing());
        }

        private IEnumerator RoutineAxeSwing()
        {
            Transform t = inHandAxeVisual.transform;
            Quaternion startRot = axeDefaultRot;
            Quaternion backRot = Quaternion.Euler(-25f, 20f, -15f);
            Quaternion strikeRot = Quaternion.Euler(60f, -30f, 25f);
            Vector3 startPos = axeDefaultPos;
            Vector3 strikePos = axeDefaultPos + new Vector3(-0.08f, -0.05f, 0.12f);

            float elapsed = 0f;
            float windupDur = 0.08f;
            while (elapsed < windupDur)
            {
                elapsed += Time.deltaTime;
                float frac = elapsed / windupDur;
                t.localRotation = Quaternion.Slerp(startRot, backRot, frac);
                yield return null;
            }

            elapsed = 0f;
            float strikeDur = 0.09f;
            while (elapsed < strikeDur)
            {
                elapsed += Time.deltaTime;
                float frac = elapsed / strikeDur;
                t.localRotation = Quaternion.Slerp(backRot, strikeRot, frac);
                t.localPosition = Vector3.Lerp(startPos, strikePos, frac);
                yield return null;
            }

            elapsed = 0f;
            float recoverDur = 0.22f;
            while (elapsed < recoverDur)
            {
                elapsed += Time.deltaTime;
                float frac = Mathf.SmoothStep(0f, 1f, elapsed / recoverDur);
                t.localRotation = Quaternion.Slerp(strikeRot, startRot, frac);
                t.localPosition = Vector3.Lerp(strikePos, startPos, frac);
                yield return null;
            }

            t.localPosition = startPos;
            t.localRotation = startRot;
            axeSwingRoutine = null;
        }

        public void SmashStairsBlockage(StairsBlockageInteractable blockage)
        {
            StartCoroutine(RoutineSmashStairsBlockage(blockage));
        }

        private IEnumerator RoutineSmashStairsBlockage(StairsBlockageInteractable blockage)
        {
            PlayAxeSwingAnimation();

            yield return new WaitForSeconds(0.12f);

            if (sfxHammerRepair != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxHammerRepair, 1.0f);
            }

            if (stairsBlockageObject != null)
            {
                stairsBlockageObject.SetActive(false);
            }
            if (blockage != null)
            {
                blockage.gameObject.SetActive(false);
            }

            // Sinh ra 3 tấm ván gỗ rơi dưới chân cầu thang
            SpawnWoodPlanksAtStairs();

            // Sinh ra bức di thư bí mật của cha rơi ra sau cây đàn
            SpawnSecretLetterAtStairs();

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("smash_stairs");
                PlayerNotebook.Instance.AddClue("stairs_smashed",
                    "Đã dùng rìu đập tan đống bàn ghế cây đàn chắn cầu thang! " +
                    "Những tấm ván gỗ rơi ra còn rất tốt, và có một phong thư của cha rơi ra sau cây đàn.");

                PlayerNotebook.Instance.AddObjective("repair_floor",
                    "Sửa sàn gỗ mục tầng 2",
                    "Nhặt 3 tấm ván gỗ dưới chân cầu thang rồi đi lên tầng 2 đóng vá lại mảng sàn mục nát.");
            }

            currentPhase = QuestPhase.RepairFloor;

            StoryObjectiveBanner.ShowObjective(
                "ẦM MỘT PHÁT! ĐÃ PHÁ NÁT CHƯỚNG NGẠI VẬT!",
                "Lối lên lầu 2 đã thông! Có mấy tấm Ván Gỗ và một bức Thư Manh Mối của cha rơi ra kìa... Mau nhặt lấy!",
                6.0f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        private void SpawnWoodPlanksAtStairs()
        {
            Vector3 basePos = new Vector3(102.8f, 11.35f, 22.8f);
#if UNITY_EDITOR
            var plankPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WoodPlank_Pickup.prefab");
#else
            GameObject plankPrefab = null;
#endif

            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnPos = basePos + new Vector3((i - 1) * 0.45f, 0.05f, (i % 2 == 0 ? 0.15f : -0.15f));
                GameObject plank = null;
                if (plankPrefab != null)
                {
                    plank = Instantiate(plankPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 180), 0));
                }
                else
                {
                    plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plank.transform.position = spawnPos;
                    plank.transform.localScale = new Vector3(0.2f, 0.04f, 0.9f);
                }

                plank.name = "WoodPlank_Pickup_" + (i + 1);
                var itemPickup = plank.GetComponent<ItemPickup>();
                if (itemPickup == null) itemPickup = plank.AddComponent<ItemPickup>();
                itemPickup.itemId = "wood_plank";
                itemPickup.itemName = "Ván Gỗ Chắc Chắn";

                var col = plank.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;
            }
        }

        private void SpawnSecretLetterAtStairs()
        {
            Vector3 letterPos = new Vector3(103.2f, 11.38f, 22.6f);
            var letter = GameObject.Find("Secret_Letter_Item");
            if (letter != null)
            {
                letter.transform.position = letterPos;
                letter.SetActive(true);
            }
            else
            {
                letter = GameObject.CreatePrimitive(PrimitiveType.Quad);
                letter.name = "Secret_Letter_Item";
                letter.transform.position = letterPos;
                letter.transform.rotation = Quaternion.Euler(90f, 25f, 0f);
                letter.transform.localScale = new Vector3(0.25f, 0.35f, 1f);
                letter.AddComponent<SecretLetterClueInteractable>();
            }

            if (secretLetterWaypoint != null)
            {
                secretLetterWaypoint.transform.position = letterPos + Vector3.up * 0.3f;
                secretLetterWaypoint.gameObject.SetActive(true);
            }
        }

        private void LoadAudioClips()
        {
#if UNITY_EDITOR
            if (sfxClutterDrop == null)
                sfxClutterDrop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Trash_Drop.wav");
            if (sfxHammerRepair == null)
                sfxHammerRepair = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Wood_Chop.wav");
            if (sfxWoodPickup == null)
                sfxWoodPickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Trash_Drop.wav");
            if (sfxAxePickup == null)
                sfxAxePickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Wood_Chop.wav");
#endif
            if (sfxClutterPickup == null)
            {
                sfxClutterPickup = GenerateTactilePickupSound();
            }
        }

        private AudioClip GenerateTactilePickupSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.14f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float tone = Mathf.Sin(2f * Mathf.PI * 520f * (float)i / sampleRate);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 780f * (float)i / sampleRate);
                float noise = (Random.value * 2f - 1f) * 0.3f;
                float env = Mathf.Exp(-t * 22f);
                samples[i] = (tone * 0.4f + tone2 * 0.3f + noise) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("TactilePickupSound", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void CollectToolbox()
        {
            hasToolbox = true;
        }

        public void OnCabinetOpened()
        {
            currentPhase = QuestPhase.ReadSecretLetter;
            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnSecretLetterRead()
        {
            hasReadSecretLetter = true;
            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("father_secret", "Di thư của cha cảnh báo về căn phòng thờ và căn hầm bí mật...");
            }
        }

        public void OnTreeChopped()
        {
        }

        public void OnPlayerEnterVilla()
        {
            OnPlayerEnterVillaFoyer();
        }

        public void StartRepairQuest()
        {
            currentPhase = QuestPhase.RepairFloor;
            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void CollectWoodPlank()
        {
            woodPlanksCollected++;

            if (sfxWoodPickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxWoodPickup, 0.8f);
            }

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.UpdateObjectiveProgress("repair_floor", $"{woodPlanksCollected}/{woodPlanksRequired} Ván Gỗ");
            }

            if (woodPlanksCollected < woodPlanksRequired)
            {
                StoryObjectiveBanner.ShowObjective(
                    "THU THẬP VÁN GỖ",
                    $"Tấm ván này còn chắc lắm ({woodPlanksCollected}/{woodPlanksRequired}). Nhặt đủ 3 tấm rồi đem lên vá lại sàn lầu 2!",
                    4.5f
                );
            }
            else
            {
                StoryObjectiveBanner.ShowObjective(
                    "ĐÃ ĐỦ 3 TẤM VÁN GỖ!",
                    "Đủ ván rồi! Nhanh chân leo lên lầu 2 gia cố lại chỗ sàn bị mục sập thôi [F].",
                    5.5f
                );
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        #endregion

        #region Phase 4: Floor Repair

        public bool CanRepairFloor()
        {
            return (woodPlanksCollected >= woodPlanksRequired);
        }

        public void OnFloorRepaired()
        {
            if (isFloorRepaired) return;
            isFloorRepaired = true;
            repairedFloorSpots = 1;
            currentPhase = QuestPhase.ExploreAncestralRoom;

            if (sfxHammerRepair != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxHammerRepair, 0.95f);
            }

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("repair_floor");
                PlayerNotebook.Instance.AddClue("floor_fixed",
                    "Đã đóng kín hố sàn mục nát! Sàn nhà chắc chắn rồi. " +
                    "Phía cuối hành lang có phòng thờ tổ tiên... Phải vào xem.");

                PlayerNotebook.Instance.AddObjective("explore_ancestor_room",
                    "Khám phá phòng thờ tổ tiên",
                    "Bước qua sàn gỗ vào căn phòng cuối hành lang tầng 2.");
            }

            StartCoroutine(RoutineFloorRepairedVictory());
        }

        private IEnumerator RoutineFloorRepairedVictory()
        {
            StoryObjectiveBanner.ShowObjective(
                "ĐÃ VÁ XONG SÀN NHÀ!",
                "Sàn đã được đóng ván khít rịt và chắc nịch, không sợ bị sụp chân nữa!",
                5.5f
            );

            yield return new WaitForSeconds(5.8f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "Khoan... Không khí phía phòng thờ cuối hành lang sao lạnh buốt xương thế này... Nổi hết cả da gà. Phải vào kiểm tra xem sao!",
                6.0f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        #endregion

        #region Phase 5 & 6: Ancestral Room & Basement

        public void OnBasementKeyCollected()
        {
            hasBasementKey = true;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("basement_key_found",
                    "Tìm thấy Chìa Khóa Tầng Hầm trên bàn thờ! Có một tầng hầm bí mật dưới ngôi nhà này?!");
            }

            if (currentPhase < QuestPhase.UnlockBasementCellar)
            {
                currentPhase = QuestPhase.UnlockBasementCellar;

                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.CompleteObjective("explore_ancestor_room");
                    PlayerNotebook.Instance.AddObjective("unlock_cellar",
                        "Mở cửa hầm bí mật",
                        "Xuống gian nhà kho tầng trệt mở cửa hầm bí mật.");
                }
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnTalismanCollected()
        {
            hasTalisman = true;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("talisman_found",
                    "Đã tìm được Bùa Trấn Yểm cổ xưa! Có vẻ như đây là vật phẩm quan trọng để hóa giải lời nguyền...");
            }

            // Dọa sợ Bước 2: Đài radio quỷ ám tự bật phát thông điệp của cha
            if (HauntedRadioScare.Instance != null && !HauntedRadioScare.Instance.hasTriggered)
            {
                HauntedRadioScare.Instance.TriggerScare();
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void TransitionToVilla2Search()
        {
            StoryObjectiveBanner.ShowObjective(
                "MỤC TIÊU MỚI: SANG BIỆT THỰ 2",
                "Theo lời cha trên đài, hãy sang Biệt Thự số 2 tìm Chìa Khóa Tầng Hầm!",
                6.0f
            );
            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnEnteredBasement()
        {
            hasEnteredBasement = true;
            currentPhase = QuestPhase.DescendIntoBasement;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("unlock_cellar");
                PlayerNotebook.Instance.AddClue("entered_basement",
                    "Đã xuống tầng hầm bí mật! Nơi đây tối om và rùng rợn... " +
                    "Có một bàn tế lễ ở sâu bên trong. Phải quyết định: đặt bùa lên bàn thờ hay tẩu thoát?");

                PlayerNotebook.Instance.AddObjective("basement_choice",
                    "Đối mặt bí mật tầng hầm",
                    hasTalisman
                        ? "Đặt Bùa Trấn Yểm lên bàn tế lễ để hóa giải lời nguyền, hoặc tẩu thoát bằng xe."
                        : "CẢNH BÁO: Chưa có bùa! Mau quay xe chạy trốn!");
            }

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnBasementRitualCompleted()
        {
            currentPhase = QuestPhase.Completed;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.CompleteObjective("basement_choice");
                PlayerNotebook.Instance.AddClue("game_ending_good",
                    "Bùa Trấn Yểm đã phát huy tác dụng. Ngọn lửa thanh tẩy rực sáng, giải thoát linh hồn gia tộc. Cơn ác mộng đã chấm dứt.");
            }

            StoryObjectiveBanner.ShowObjective(
                "LỜI NGUYỀN ĐÃ HÓA GIẢI!",
                "Ánh sáng bừng nở xóa tan bóng tối ngàn năm. Bạn đã kế thừa di sản bình an.",
                8.0f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        public void OnTruckEscape()
        {
            currentPhase = QuestPhase.Completed;

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("game_ending_escape",
                    "Tiếng động cơ gầm vang trong đêm. Bạn đạp hết ga chạy trốn khỏi căn biệt thự bị nguyền rủa.");
            }

            StoryObjectiveBanner.ShowObjective(
                "TẨU THOÁT THÀNH CÔNG!",
                "Bạn đã quay xe chạy trốn trong màn đêm... Căn biệt thự và bí mật chôn giấu vẫn ngủ yên.",
                8.0f
            );

            UpdateObjectiveDisplay();
            UpdateWaypointVisibility();
        }

        #endregion

        #region Waypoints & Tracker

        public void UpdateWaypointVisibility()
        {
            // 0. Đèn Pin: CHỈ hiện khi người chơi đã bước vào sảnh, nghe lời thoại bóng tối và chưa nhặt đèn pin!
            if (flashlightWaypoint != null)
            {
                bool showFlashlight = (hasEnteredVilla && !hasFlashlight);
                flashlightWaypoint.SetVisible(showFlashlight);
                var beacon = flashlightWaypoint.transform.Find("BeaconLight");
                if (beacon != null) beacon.gameObject.SetActive(showFlashlight);
            }

            // 1. Cầu thang: chỉ hiện khi có đèn pin, ĐÃ KHÁM PHÁ HẾT CÁC PHÒNG TẦNG 1, và chưa phát hiện bị chặn (hoặc khi đã có rìu)
            if (stairsWaypoint != null)
            {
                stairsWaypoint.SetVisible((hasFlashlight && AreAllFloor1RoomsExplored() && currentPhase == QuestPhase.ExploreVilla) ||
                                          (currentPhase == QuestPhase.GetAxeAndChopTrees && hasAxe));
            }

            // 2. Clutter items: chỉ hiện khi ở phase CleanVilla và KHÔNG đang bê đồ
            bool showClutter = (currentPhase == QuestPhase.CleanVilla && !isCarryingClutter);
            var clutterItems = Object.FindObjectsByType<HouseClutterItem>(FindObjectsSortMode.None);
            foreach (var c in clutterItems)
            {
                var wp = c.GetComponent<QuestWaypointMarker>();
                if (wp != null) wp.SetVisible(showClutter && !c.isPickedUp);
            }

            // 3. Bãi rác sân sau: chỉ hiện khi đang bê rác
            if (burnSiteWaypoint != null)
            {
                burnSiteWaypoint.SetVisible(currentPhase == QuestPhase.CleanVilla && isCarryingClutter);
            }

            // 4. Rìu trên xe bán tải
            if (axeWaypoint != null)
            {
                axeWaypoint.SetVisible(currentPhase == QuestPhase.GetAxeAndChopTrees && !hasAxe);
            }

            // 5. Di thư bí mật
            if (secretLetterWaypoint != null)
            {
                secretLetterWaypoint.SetVisible(currentPhase >= QuestPhase.RepairFloor && !hasReadSecretLetter);
            }

            // 6. Mảng sàn mục trên tầng 2
            if (floorRepairWaypoint != null)
            {
                floorRepairWaypoint.SetVisible(currentPhase == QuestPhase.RepairFloor && !isFloorRepaired && woodPlanksCollected >= woodPlanksRequired);
            }

            // 7. Chìa khóa tầng hầm
            if (basementKeyWaypoint != null)
            {
                basementKeyWaypoint.SetVisible(currentPhase == QuestPhase.ExploreAncestralRoom && !hasBasementKey);
            }

            // 8. Bùa trấn yểm
            if (talismanWaypoint != null)
            {
                talismanWaypoint.SetVisible(currentPhase >= QuestPhase.ExploreAncestralRoom && !hasTalisman);
            }

            // 9. Cửa hầm bí mật
            if (cellarTrapdoorWaypoint != null)
            {
                cellarTrapdoorWaypoint.SetVisible((currentPhase == QuestPhase.UnlockBasementCellar || hasBasementKey) && !hasEnteredBasement);
            }

            // 10. Bàn tế lễ tầng hầm
            if (basementAltarWaypoint != null)
            {
                basementAltarWaypoint.SetVisible(currentPhase == QuestPhase.DescendIntoBasement);
            }

            // 11. Xe bán tải tẩu thoát
            if (truckEscapeWaypoint != null)
            {
                truckEscapeWaypoint.SetVisible(hasReadSecretLetter || isFloorRepaired);
            }
        }

        public void UpdateObjectiveDisplay()
        {
            switch (currentPhase)
            {
                case QuestPhase.ExploreVilla:
                    if (!hasFlashlight)
                    {
                        if (hasEnteredVilla)
                        {
                            if (QuestTrackerHUD.Instance != null)
                                QuestTrackerHUD.Instance.UpdateTracker("TÌM ĐÈN PIN", "Nhặt Đèn Pin trên chiếc bàn ở sảnh chính [F]");

                            if (PlayerNotebook.Instance != null)
                            {
                                PlayerNotebook.Instance.AddObjective("find_flashlight",
                                    "Tìm Đèn Pin",
                                    "Nhà tối om, tìm Đèn Pin trên bàn sảnh chính.");
                            }
                        }
                        else
                        {
                            if (QuestTrackerHUD.Instance != null)
                                QuestTrackerHUD.Instance.UpdateTracker("KHẢO SÁT BIỆT THỰ", "Đi vào bên trong biệt thự để kiểm tra tình trạng ngôi nhà");
                        }
                    }
                    else
                    {
                        if (!AreAllFloor1RoomsExplored())
                        {
                            if (QuestTrackerHUD.Instance != null)
                                QuestTrackerHUD.Instance.UpdateTracker("KHẢO SÁT CÁC PHÒNG TẦNG 1",
                                    "Kiểm tra các phòng tầng trệt (Sảnh Chính, Phòng Khách, Phòng Ăn & Bếp)",
                                    GetFloor1RoomsExploredCount(), 3);
                        }
                        else
                        {
                            if (QuestTrackerHUD.Instance != null)
                                QuestTrackerHUD.Instance.UpdateTracker("LÊN TẦNG 2", "Đi dọc hành lang tìm cầu thang dẫn lên tầng 2 để khảo sát");
                        }
                    }
                    break;

                case QuestPhase.DiscoverBlockedStairs:
                    if (QuestTrackerHUD.Instance != null)
                        QuestTrackerHUD.Instance.UpdateTracker("CẦU THANG BỊ CHẶN!", "Cầu thang bị chặn bởi đống bàn ghế cây đàn cũ. Phải dọn tầng 1 trước!");
                    break;

                case QuestPhase.CleanVilla:
                    string cleanDesc = isCarryingClutter
                        ? "Đang bê đồ! Đi ra cửa sau vứt vào bãi rác [F]"
                        : "Bê các đồ đạc hư hỏng ở tầng 1 ra bãi rác sân sau [F]";
                    if (QuestTrackerHUD.Instance != null)
                    {
                        QuestTrackerHUD.Instance.UpdateTracker("DỌN DẸP NHÀ CỬA", cleanDesc, clutterCleanedCount, clutterRequired);
                    }
                    break;

                case QuestPhase.GetAxeAndChopTrees:
                    if (!hasAxe)
                    {
                        if (QuestTrackerHUD.Instance != null)
                            QuestTrackerHUD.Instance.UpdateTracker("LẤY RÌU TRÊN XE", "Ra thùng xe bán tải ngoài cổng lấy Cây Rìu của cha [F]");
                    }
                    else
                    {
                        if (QuestTrackerHUD.Instance != null)
                            QuestTrackerHUD.Instance.UpdateTracker("PHÁ ĐỐNG CHẶN CẦU THANG", "Quay lại chân cầu thang dùng Rìu chém nát đống chướng ngại vật [F]");
                    }
                    break;

                case QuestPhase.RepairFloor:
                    if (woodPlanksCollected < woodPlanksRequired)
                    {
                        if (QuestTrackerHUD.Instance != null)
                            QuestTrackerHUD.Instance.UpdateTracker("NHẶT VÁN GỖ", $"Nhặt 3 tấm ván gỗ rơi dưới chân cầu thang ({woodPlanksCollected}/{woodPlanksRequired}) [F]");
                    }
                    else
                    {
                        if (QuestTrackerHUD.Instance != null)
                            QuestTrackerHUD.Instance.UpdateTracker("GIA CỐ SÀN TẦNG 2", "Leo lên tầng 2 đóng ván gỗ vào hố sàn mục [F]");
                    }
                    break;

                case QuestPhase.ExploreAncestralRoom:
                    if (QuestTrackerHUD.Instance != null)
                        QuestTrackerHUD.Instance.UpdateTracker("KHÁM PHÁ TẦNG 2", "Tìm Chìa Khóa Hầm trong Phòng Ngủ và Bùa Trấn Yểm trên Bàn Thờ [F]");
                    break;

                case QuestPhase.UnlockBasementCellar:
                    if (QuestTrackerHUD.Instance != null)
                        QuestTrackerHUD.Instance.UpdateTracker("MỞ CỬA HẦM BÍ MẬT", "Xuống gian nhà kho mở cửa hầm bí mật [F]");
                    break;

                case QuestPhase.DescendIntoBasement:
                    if (QuestTrackerHUD.Instance != null)
                    {
                        QuestTrackerHUD.Instance.UpdateTracker("BÍ MẬT DƯỚI HẦM SÂU",
                            hasTalisman
                                ? "Tiến đến bàn tế lễ đặt Bùa Trấn Yểm để hóa giải lời nguyền [F]"
                                : "CẢNH BÁO: Chưa có Bùa! Có thể quay xe tẩu thoát hoặc liều mạng!");
                    }
                    break;

                case QuestPhase.Completed:
                    if (QuestTrackerHUD.Instance != null)
                        QuestTrackerHUD.Instance.UpdateTracker("CÂU CHUYỆN KẾT THÚC", "Bạn đã khám phá toàn bộ bí mật của biệt thự.");
                    break;
            }
        }

        #endregion
    }
}
