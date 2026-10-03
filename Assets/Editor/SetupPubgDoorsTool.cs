using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HorrorGame.DoorSystem;

namespace HorrorGame.EditorTools
{
    public class SetupPubgDoorsTool : EditorWindow
    {
        [MenuItem("Tools/PUBG Door System/Setup All Doors in Scene")]
        public static void SetupAllDoors()
        {
            int configuredCount = 0;
            int duplicatesRemoved = 0;
            int misalignedFixed = 0;

            Scene scene = SceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Setup PUBG Door System");
            int undoGroup = Undo.GetCurrentGroup();

            Debug.Log($"[PUBG Door Setup] Bắt đầu thiết lập hệ thống cửa trong Scene: {scene.name} (Unity 6 LTS)...");

            // 1. Cấu hình hoặc dọn dẹp các cửa trong root GameObject "Door"
            GameObject rootDoor = null;
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "Door")
                {
                    rootDoor = r;
                    break;
                }
            }

            if (rootDoor != null)
            {
                List<Transform> uniqueDoors = new List<Transform>();
                List<GameObject> toDestroy = new List<GameObject>();

                for (int i = 0; i < rootDoor.transform.childCount; i++)
                {
                    Transform t = rootDoor.transform.GetChild(i);
                    bool isDuplicate = false;

                    foreach (var u in uniqueDoors)
                    {
                        if (Vector3.Distance(u.position, t.position) < 0.05f)
                        {
                            isDuplicate = true;
                            toDestroy.Add(t.gameObject);
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        uniqueDoors.Add(t);
                    }
                }

                foreach (var dup in toDestroy)
                {
                    Undo.DestroyObjectImmediate(dup);
                    duplicatesRemoved++;
                }

                foreach (var doorTrans in uniqueDoors)
                {
                    if (doorTrans == null) continue;

                    // Sửa các cửa bị lệch góc so với tường
                    if (Vector3.Distance(doorTrans.position, new Vector3(78.83f, 11.32f, 22.33f)) < 0.3f)
                    {
                        doorTrans.eulerAngles = new Vector3(0f, 270.46f, 0f);
                        misalignedFixed++;
                    }
                    else if (Vector3.Distance(doorTrans.position, new Vector3(85.61f, 11.32f, 27.27f)) < 0.3f)
                    {
                        doorTrans.eulerAngles = new Vector3(0f, 270.00f, 0f);
                        misalignedFixed++;
                    }
                    else if (Vector3.Distance(doorTrans.position, new Vector3(94.61f, 11.32f, 22.34f)) < 0.3f)
                    {
                        doorTrans.eulerAngles = new Vector3(0f, 90.00f, 0f);
                        misalignedFixed++;
                    }
                    else if (Vector3.Distance(doorTrans.position, new Vector3(95.90f, 11.32f, 15.60f)) < 0.3f)
                    {
                        doorTrans.eulerAngles = new Vector3(0f, 90.00f, 0f);
                        misalignedFixed++;
                    }
                    else if (Vector3.Distance(doorTrans.position, new Vector3(102.90f, 10.51f, -74.94f)) < 0.3f)
                    {
                        doorTrans.eulerAngles = new Vector3(0f, 358.81f, 0f);
                        misalignedFixed++;
                    }

                    DoorInteractable di = doorTrans.GetComponent<DoorInteractable>();
                    if (di == null) di = Undo.AddComponent<DoorInteractable>(doorTrans.gameObject);

                    di.mainDoorLeaf = doorTrans;
                    di.secondDoorLeaf = null;
                    di.masterDoor = null;
                    di.openAngle = 90f;
                    di.animationDuration = 0.35f;
                    di.pushAwayFromPlayer = false; // Luôn mở theo hướng chuẩn, không lật mở ngược vào phòng
                    di.InitializeClosedRotations();

                    // Đảm bảo có BoxCollider
                    BoxCollider col = doorTrans.GetComponent<BoxCollider>();
                    if (col == null)
                    {
                        col = Undo.AddComponent<BoxCollider>(doorTrans.gameObject);
                    }

                    configuredCount++;
                }

                // Cấu hình CỬA ĐÔI CHÍNH Biệt Thự 1 (Villa1_Door_B_1 và Villa1_Door_B (2))
                Transform v1_b1 = rootDoor.transform.Find("Villa1_Door_B_1");
                Transform v1_b2 = rootDoor.transform.Find("Villa1_Door_B (2)");
                if (v1_b1 != null && v1_b2 != null)
                {
                    var di1 = v1_b1.GetComponent<DoorInteractable>();
                    var di2 = v1_b2.GetComponent<DoorInteractable>();
                    if (di1 != null && di2 != null)
                    {
                        di1.mainDoorLeaf = v1_b1;
                        di1.secondDoorLeaf = v1_b2;
                        di1.masterDoor = null;
                        di1.pushAwayFromPlayer = false;
                        di1.openAngle = 90f;
                        di1.invertDirection = false;
                        di1.InitializeClosedRotations();

                        di2.mainDoorLeaf = null;
                        di2.secondDoorLeaf = null;
                        di2.masterDoor = di1; // Bấm vào cánh nào cũng mở cả 2 cánh đồng bộ
                        di2.pushAwayFromPlayer = false;
                        Debug.Log("[PUBG Door Setup] Đã ghép nối Cửa Đôi chính Villa1_Door_B (mở đồng bộ ra ngoài).");
                    }
                }

                // Cấu hình CỬA ĐÔI Biệt Thự 2 (Villa1_Door_B (1) và Villa1_Door_B_2)
                Transform v2_b1 = rootDoor.transform.Find("Villa1_Door_B (1)");
                Transform v2_b2 = rootDoor.transform.Find("Villa1_Door_B_2");
                if (v2_b1 != null && v2_b2 != null)
                {
                    var di1 = v2_b1.GetComponent<DoorInteractable>();
                    var di2 = v2_b2.GetComponent<DoorInteractable>();
                    if (di1 != null && di2 != null)
                    {
                        di1.mainDoorLeaf = v2_b1;
                        di1.secondDoorLeaf = v2_b2;
                        di1.masterDoor = null;
                        di1.pushAwayFromPlayer = false;
                        di1.openAngle = 90f;
                        di1.invertDirection = false;
                        di1.InitializeClosedRotations();

                        di2.mainDoorLeaf = null;
                        di2.secondDoorLeaf = null;
                        di2.masterDoor = di1;
                        di2.pushAwayFromPlayer = false;
                        Debug.Log("[PUBG Door Setup] Đã ghép nối Cửa Đôi chính Villa 2 (mở đồng bộ ra ngoài).");
                    }
                }
            }

            // 2. Cấu hình các cửa trong Map/MapDuoiLongDat
            GameObject mapObj = GameObject.Find("Map");
            if (mapObj != null)
            {
                // Sửa cửa KitchenA Door_V3 (3) bị xoay lệch trong prefab
                GameObject dV3 = GameObject.Find("Map/MapDuoiLongDat/1stFloor/KitchenA/Door_V3 (3)");
                if (dV3 != null)
                {
                    Transform leaf = dV3.transform.Find("Door");
                    if (leaf != null && leaf.localEulerAngles != Vector3.zero)
                    {
                        leaf.localEulerAngles = Vector3.zero;
                        misalignedFixed++;
                    }
                }

                // Xóa cánh cửa trùng lặp trong Morgue
                GameObject dMorgue = GameObject.Find("Map/MapDuoiLongDat/Basement/Morgue/DoorD_V1 (1)");
                if (dMorgue != null)
                {
                    Transform dupLeaf = dMorgue.transform.Find("DoorD_Right (1)");
                    if (dupLeaf != null)
                    {
                        Undo.DestroyObjectImmediate(dupLeaf.gameObject);
                        duplicatesRemoved++;
                    }
                }

                List<Transform> singleDoors = new List<Transform>();
                List<Transform> doubleDoors = new List<Transform>();

                FindDoorsRecursive(mapObj.transform, singleDoors, doubleDoors);

                // Cửa đơn: Door_V1, Door_V2, Door_V3
                foreach (var sd in singleDoors)
                {
                    Transform doorLeaf = sd.Find("Door");
                    if (doorLeaf != null)
                    {
                        DoorInteractable di = sd.GetComponent<DoorInteractable>();
                        if (di == null) di = Undo.AddComponent<DoorInteractable>(sd.gameObject);

                        di.mainDoorLeaf = doorLeaf;
                        di.secondDoorLeaf = null;
                        di.masterDoor = null;
                        di.openAngle = 90f;
                        di.animationDuration = 0.35f;
                        di.pushAwayFromPlayer = false;
                        di.InitializeClosedRotations();

                        MeshCollider mc = doorLeaf.GetComponent<MeshCollider>();
                        if (mc != null) mc.convex = true;

                        configuredCount++;
                    }
                }

                // Cửa đôi: DoorD_V1, DoorD_V2
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
                        DoorInteractable di = dd.GetComponent<DoorInteractable>();
                        if (di == null) di = Undo.AddComponent<DoorInteractable>(dd.gameObject);

                        di.mainDoorLeaf = leftLeaf;
                        di.secondDoorLeaf = rightLeaf;
                        di.masterDoor = null;
                        di.openAngle = 90f;
                        di.animationDuration = 0.35f;
                        di.pushAwayFromPlayer = false;
                        di.InitializeClosedRotations();

                        MeshCollider mcL = leftLeaf.GetComponent<MeshCollider>();
                        if (mcL != null) mcL.convex = true;
                        MeshCollider mcR = rightLeaf.GetComponent<MeshCollider>();
                        if (mcR != null) mcR.convex = true;

                        configuredCount++;
                    }
                }
            }

            // 3. Cấu hình Player
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player != null)
            {
                PlayerDoorInteraction pdi = player.GetComponent<PlayerDoorInteraction>();
                if (pdi == null) pdi = Undo.AddComponent<PlayerDoorInteraction>(player);

                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null) pdi.playerCamera = cam.transform;

                pdi.interactDistance = 2.8f;
                Debug.Log($"[PUBG Door Setup] Đã gắn PlayerDoorInteraction vào {player.name}.");
            }
            else
            {
                Debug.LogWarning("[PUBG Door Setup] Không tìm thấy Player trong Scene!");
            }

            // 4. Cấu hình UI Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                PubgDoorUI ui = canvas.GetComponentInChildren<PubgDoorUI>(true);
                if (ui == null)
                {
                    GameObject uiObj = new GameObject("PubgDoorUI");
                    Undo.RegisterCreatedObjectUndo(uiObj, "Create PubgDoorUI");
                    uiObj.transform.SetParent(canvas.transform, false);
                    ui = Undo.AddComponent<PubgDoorUI>(uiObj);
                }

                if (ui.promptContainer != null)
                {
                    Undo.DestroyObjectImmediate(ui.promptContainer);
                }
                ui.BuildDynamicUI();
                Debug.Log("[PUBG Door Setup] Đã tạo PubgDoorUI bán trong suốt trên Canvas.");
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log($"[PUBG Door Setup] HOÀN TẤT! Đã cấu hình {configuredCount} cửa, sửa {misalignedFixed} cửa lệch góc, dọn dẹp {duplicatesRemoved} cửa trùng lặp.");
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
    }
}
