using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HorrorGame.DoorSystem;
using HorrorGame.Story;

namespace HorrorGame.EditorTools
{
    public class HorrorMapSanitizer : EditorWindow
    {
        [MenuItem("Tools/Horror Game/1. Rà Soát & Sửa Toàn Diện Map (Cửa, Đồ Lơ Lửng, Tủ Hầm, Âm Thanh)")]
        public static void SanitizeMap()
        {
            Scene scene = SceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Sanitize Horror Map");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log($"<color=#00FF7F><b>[Horror Map Sanitizer]</b></color> Bắt đầu rà soát toàn diện Scene: {scene.name}...");

            int doorsFixed = FixAllDoors();
            int propsFixed = FixFloatingAndClippingProps();
            int lockersAdded = SetupBasementLockers();
            int audioFixed = SetupHorrorAudioEmitters();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"<color=#00FF7F><b>[Horror Map Sanitizer] HOÀN TẤT RÀ SOÁT MAP!</b></color>\n" +
                      $"- Cửa đã sửa góc chuẩn & chống ngược: {doorsFixed}\n" +
                      $"- Đồ vật lơ lửng / dính xuyên đã xử lý: {propsFixed}\n" +
                      $"- Tủ sắt trốn quái vật dưới hầm đã bố trí: {lockersAdded}\n" +
                      $"- Hệ thống âm thanh rùng rợn 3D đã kích hoạt: {audioFixed}");

            EditorUtility.DisplayDialog(
                "Rà Soát Map Hoàn Tất!",
                $"Đã hoàn tất dọn dẹp và nâng cấp Map:\n\n" +
                $"✔ Sửa {doorsFixed} cửa mở sẵn / xoay lệch góc\n" +
                $"✔ Xử lý {propsFixed} đồ vật lơ lửng / dính xuyên sàn\n" +
                $"✔ Bố trí {lockersAdded} tủ sắt trốn quái vật dưới hầm ngầm\n" +
                $"✔ Thiết lập hệ thống âm thanh rùng rợn 3D\n\n" +
                $"Bây giờ bản đồ đã sạch sẽ, chuẩn xác để sẵn sàng làm Gameplay!",
                "Tuyệt vời"
            );
        }

        // =====================================================================
        // 1. SỬA TOÀN BỘ CỬA TRONG MAP (CHUẨN HÓA GÓC ĐÓNG, TRÁNH NGƯỢC TRẠNG THÁI)
        // =====================================================================
        private static int FixAllDoors()
        {
            int count = 0;
            AudioClip defaultCreak = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Creak_Drawer.wav");
            AudioClip defaultShut = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI/TestamentMenu/Audio/SFX_Car_Door_Shut.wav");

            // A. Cửa trong root "Door" (Biệt thự 1 & Biệt thự 2)
            GameObject rootDoor = GameObject.Find("Door");
            if (rootDoor != null)
            {
                for (int i = 0; i < rootDoor.transform.childCount; i++)
                {
                    Transform t = rootDoor.transform.GetChild(i);
                    DoorInteractable di = t.GetComponent<DoorInteractable>();
                    if (di == null) di = Undo.AddComponent<DoorInteractable>(t.gameObject);

                    di.pushAwayFromPlayer = true;
                    if (di.openSound == null) di.openSound = defaultCreak;
                    if (di.closeSound == null) di.closeSound = defaultShut;

                    di.InitializeClosedRotations();
                    count++;
                }
            }

            // B. Cửa trong Map/MapDuoiLongDat (Tầng hầm ngầm & Nhà xác Morgue)
            GameObject mapObj = GameObject.Find("Map");
            if (mapObj != null)
            {
                List<Transform> singleDoors = new List<Transform>();
                List<Transform> doubleDoors = new List<Transform>();
                FindDoorsRecursive(mapObj.transform, singleDoors, doubleDoors);

                // Cửa đơn (Door_V1, Door_V2, Door_V3)
                foreach (var sd in singleDoors)
                {
                    Transform leaf = sd.Find("Door");
                    if (leaf != null)
                    {
                        Undo.RecordObject(leaf, "Reset Door Leaf Rotation");
                        // Reset góc mở sẵn trong 3D model về đúng 0 độ chuẩn khép kín khung cửa
                        leaf.localEulerAngles = Vector3.zero;

                        DoorInteractable di = sd.GetComponent<DoorInteractable>();
                        if (di == null) di = Undo.AddComponent<DoorInteractable>(sd.gameObject);

                        di.mainDoorLeaf = leaf;
                        di.pushAwayFromPlayer = true;
                        di.openAngle = 90f;
                        di.animationDuration = 0.35f;
                        if (di.openSound == null) di.openSound = defaultCreak;
                        if (di.closeSound == null) di.closeSound = defaultShut;
                        di.InitializeClosedRotations();

                        count++;
                    }
                }

                // Cửa đôi (DoorD_V1, DoorD_V2)
                foreach (var dd in doubleDoors)
                {
                    Transform leftLeaf = null;
                    Transform rightLeaf = null;
                    for (int i = 0; i < dd.childCount; i++)
                    {
                        Transform c = dd.GetChild(i);
                        if (c.name.ToLower().Contains("left")) leftLeaf = c;
                        else if (c.name.ToLower().Contains("right")) rightLeaf = c;
                    }

                    if (leftLeaf != null && rightLeaf != null)
                    {
                        Undo.RecordObject(leftLeaf, "Reset Door Left Leaf Rotation");
                        Undo.RecordObject(rightLeaf, "Reset Door Right Leaf Rotation");
                        leftLeaf.localEulerAngles = Vector3.zero;
                        rightLeaf.localEulerAngles = Vector3.zero;

                        DoorInteractable di = dd.GetComponent<DoorInteractable>();
                        if (di == null) di = Undo.AddComponent<DoorInteractable>(dd.gameObject);

                        di.mainDoorLeaf = leftLeaf;
                        di.secondDoorLeaf = rightLeaf;
                        di.pushAwayFromPlayer = true;
                        di.openAngle = 90f;
                        di.animationDuration = 0.35f;
                        if (di.openSound == null) di.openSound = defaultCreak;
                        if (di.closeSound == null) di.closeSound = defaultShut;
                        di.InitializeClosedRotations();

                        count++;
                    }
                }
            }

            return count;
        }

        private static void FindDoorsRecursive(Transform parent, List<Transform> singleDoors, List<Transform> doubleDoors)
        {
            if (parent.name.StartsWith("Door_V"))
            {
                singleDoors.Add(parent);
                return;
            }
            if (parent.name.StartsWith("DoorD_V"))
            {
                doubleDoors.Add(parent);
                return;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                FindDoorsRecursive(parent.GetChild(i), singleDoors, doubleDoors);
            }
        }

        // =====================================================================
        // 2. SỬA ĐỒ VẬT LƠ LỬNG & DÍNH XUYÊN NHAU
        // =====================================================================
        private static int FixFloatingAndClippingProps()
        {
            int fixedCount = 0;

            // 1. Hạ ghế đá ParkBench lơ lửng ở Y=7.64m
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            foreach (var r in allRenderers)
            {
                if (r.gameObject.name.Contains("ParkBench") && r.transform.position.y > 4.0f)
                {
                    Undo.RecordObject(r.transform, "Lower Floating ParkBench");
                    Vector3 pos = r.transform.position;
                    pos.y = 0.69f;
                    r.transform.position = pos;
                    fixedCount++;
                    Debug.Log($"[Horror Map Sanitizer] Đã hạ ghế đá lơ lửng tại {pos} xuống mặt đất.");
                }
            }

            // 2. Sửa thảm Rug clipping bàn ăn tại (88.5, 11.36, 17.0)
            foreach (var r in allRenderers)
            {
                if (r.gameObject.name.Contains("Rug") && Vector3.Distance(r.transform.position, new Vector3(88.5f, 11.36f, 17.0f)) < 0.5f)
                {
                    Undo.RecordObject(r.transform, "Lower Clipping Rug");
                    Vector3 pos = r.transform.position;
                    pos.y = 11.31f; // Nằm sát sàn gạch, không dính xuyên mặt bàn
                    r.transform.position = pos;
                    fixedCount++;
                    Debug.Log($"[Horror Map Sanitizer] Đã hạ thảm phòng khách xuống {pos.y} để tránh z-fighting với bàn.");
                }
            }

            // 3. Sửa đèn Prop_Lamp_A trùng lặp tại (0, 0, 0)
            GameObject[] rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            List<GameObject> lampsAtZero = new List<GameObject>();
            foreach (var root in rootObjs)
            {
                if (root.name.Contains("Lamp") && root.transform.position == Vector3.zero)
                {
                    lampsAtZero.Add(root);
                }
            }
            if (lampsAtZero.Count > 1)
            {
                // Chuyển 1 chiếc lên bàn học tầng 2
                Undo.RecordObject(lampsAtZero[0].transform, "Relocate Duplicate Lamp");
                lampsAtZero[0].transform.position = new Vector3(74.6f, 19.35f, 17.5f);
                fixedCount++;
                Debug.Log("[Horror Map Sanitizer] Đã chuyển đèn bàn trùng lặp lên bàn làm việc tầng 2.");
            }

            return fixedCount;
        }

        // =====================================================================
        // 3. BỐ TRÍ TỦ SẮT TRỐN QUÁI VẬT DƯỚI HẦM NGẦM
        // =====================================================================
        private static int SetupBasementLockers()
        {
            int created = 0;
            GameObject cupboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Abandoned_Asylum/Prefabs/Cupboard.prefab");
            if (cupboardPrefab == null)
            {
                Debug.LogWarning("[Horror Map Sanitizer] Không tìm thấy Cupboard.prefab!");
                return 0;
            }

            AudioClip sfxCreak = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Creak_Drawer.wav");
            AudioClip sfxBreathing = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Exhausted_Breathing.wav");

            // Tìm hoặc tạo nhóm cha "Basement_Hiding_Spots"
            GameObject lockerGroup = GameObject.Find("Basement_Hiding_Spots");
            if (lockerGroup == null)
            {
                lockerGroup = new GameObject("Basement_Hiding_Spots");
                Undo.RegisterCreatedObjectUndo(lockerGroup, "Create Basement Hiding Spots Group");
                lockerGroup.transform.position = new Vector3(90f, -2.15f, -100f);
            }

            // Tủ 1: Góc phòng tế lễ / nhà xác
            Transform locker1Trans = lockerGroup.transform.Find("Basement_Hiding_Locker_1");
            if (locker1Trans == null)
            {
                GameObject l1 = (GameObject)PrefabUtility.InstantiatePrefab(cupboardPrefab, lockerGroup.transform);
                l1.name = "Basement_Hiding_Locker_1";
                l1.transform.position = new Vector3(88.5f, -2.15f, -101.5f);
                l1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                var spot = l1.GetComponent<HidingSpot>() ?? l1.AddComponent<HidingSpot>();
                spot.spotName = "Tủ Sắt Nhà Tang Lễ";
                spot.spotType = HidingSpot.SpotType.MetalLocker;
                spot.sfxEnter = sfxCreak;
                spot.sfxExit = sfxCreak;
                spot.sfxHeartbeat = sfxBreathing;

                Undo.RegisterCreatedObjectUndo(l1, "Create Basement Locker 1");
                created++;
            }

            // Tủ 2: Góc hành lang sâu dưới hầm
            Transform locker2Trans = lockerGroup.transform.Find("Basement_Hiding_Locker_2");
            if (locker2Trans == null)
            {
                GameObject l2 = (GameObject)PrefabUtility.InstantiatePrefab(cupboardPrefab, lockerGroup.transform);
                l2.name = "Basement_Hiding_Locker_2";
                l2.transform.position = new Vector3(97.5f, -2.15f, -107.5f);
                l2.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

                var spot = l2.GetComponent<HidingSpot>() ?? l2.AddComponent<HidingSpot>();
                spot.spotName = "Tủ Sắt Góc Hầm";
                spot.spotType = HidingSpot.SpotType.MetalLocker;
                spot.sfxEnter = sfxCreak;
                spot.sfxExit = sfxCreak;
                spot.sfxHeartbeat = sfxBreathing;

                Undo.RegisterCreatedObjectUndo(l2, "Create Basement Locker 2");
                created++;
            }

            return created;
        }

        // =====================================================================
        // 4. THIẾT LẬP HỆ THỐNG ÂM THANH RÙNG RỢN 3D
        // =====================================================================
        private static int SetupHorrorAudioEmitters()
        {
            int configured = 0;

            // Đảm bảo HorrorAmbienceManager có mặt và đầy đủ audio
            HorrorAmbienceManager ham = Object.FindAnyObjectByType<HorrorAmbienceManager>();
            if (ham == null)
            {
                GameObject hamObj = new GameObject("HorrorAmbienceManager");
                Undo.RegisterCreatedObjectUndo(hamObj, "Create HorrorAmbienceManager");
                ham = hamObj.AddComponent<HorrorAmbienceManager>();
                configured++;
            }

            // Tạo Emitter 3D rên rỉ dưới hầm (DeepRattle)
            GameObject basementAudio = GameObject.Find("Basement_Horror_Audio_Emitter");
            if (basementAudio == null)
            {
                basementAudio = new GameObject("Basement_Horror_Audio_Emitter");
                Undo.RegisterCreatedObjectUndo(basementAudio, "Create Basement Audio Emitter");
                basementAudio.transform.position = new Vector3(93.29f, -1.0f, -106.53f);

                AudioSource asrc = basementAudio.AddComponent<AudioSource>();
                asrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
                asrc.loop = true;
                asrc.playOnAwake = true;
                asrc.spatialBlend = 1.0f; // Full 3D
                asrc.minDistance = 4f;
                asrc.maxDistance = 25f;
                asrc.volume = 0.65f;
                asrc.rolloffMode = AudioRolloffMode.Linear;
                configured++;
            }

            // Tạo Emitter 3D gió hú tại Biệt Thự 2 (WindHowl)
            GameObject villa2Audio = GameObject.Find("Villa2_Horror_Audio_Emitter");
            if (villa2Audio == null)
            {
                villa2Audio = new GameObject("Villa2_Horror_Audio_Emitter");
                Undo.RegisterCreatedObjectUndo(villa2Audio, "Create Villa 2 Audio Emitter");
                villa2Audio.transform.position = new Vector3(109.5f, 6.5f, -54.6f);

                AudioSource asrc = villa2Audio.AddComponent<AudioSource>();
                asrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
                asrc.loop = true;
                asrc.playOnAwake = true;
                asrc.spatialBlend = 0.9f;
                asrc.minDistance = 6f;
                asrc.maxDistance = 35f;
                asrc.volume = 0.5f;
                configured++;
            }

            return configured;
        }
    }
}
