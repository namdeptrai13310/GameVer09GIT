using UnityEngine;
using UnityEngine.InputSystem;

using HorrorGame.InteractSystem;

namespace HorrorGame.DoorSystem
{
    public class PlayerDoorInteraction : MonoBehaviour
    {
        [Header("Raycast Settings")]
        public Transform playerCamera;
        public float interactDistance = 2.8f;
        public LayerMask interactLayer = ~0; // Mặc định quét mọi Layer

        [Header("Debug")]
        [SerializeField] private DoorInteractable currentTargetDoor;
        [SerializeField] private ItemPickup currentTargetPickup;
        [SerializeField] private HorrorGame.Story.BrokenFloorRepairInteractable currentTargetRepair;
        private HorrorGame.InteractSystem.IPlayerInteractable currentTargetInteractable;

        [Header("Crosshair Dot (Tâm ngắm chấm trắng)")]
        public bool showCrosshairDot = true;
        private GameObject crosshairDotObj;
        private UnityEngine.UI.Image crosshairDotImg;
        private RectTransform crosshairDotRt;
        private Color dotNormalColor = new Color(1f, 1f, 1f, 0.85f);
        private Color dotHoverColor = new Color(1.0f, 0.82f, 0.18f, 1.0f); // Amber gold khi chỉa vào đồ/cửa

        private void Start()
        {
            if (playerCamera == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
                else if (Camera.main != null) playerCamera = Camera.main.transform;
            }

            if (showCrosshairDot)
            {
                BuildCrosshairDot();
            }
        }

        private void BuildCrosshairDot()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform oldDot = canvas.transform.Find("CrosshairDot");
            if (oldDot != null)
            {
                DestroyImmediate(oldDot.gameObject);
            }

            crosshairDotObj = new GameObject("CrosshairDot");
            crosshairDotObj.transform.SetParent(canvas.transform, false);

            crosshairDotRt = crosshairDotObj.AddComponent<RectTransform>();
            crosshairDotRt.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairDotRt.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairDotRt.pivot = new Vector2(0.5f, 0.5f);
            crosshairDotRt.anchoredPosition = Vector2.zero;
            crosshairDotRt.sizeDelta = new Vector2(6f, 6f);

            crosshairDotImg = crosshairDotObj.AddComponent<UnityEngine.UI.Image>();
            crosshairDotImg.color = dotNormalColor;

            Sprite dotSprite = null;
#if UNITY_EDITOR
            dotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/Crosshair_Dot.png");
#endif
            if (dotSprite != null)
            {
                crosshairDotImg.sprite = dotSprite;
            }

            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                crosshairDotObj.SetActive(false);
            }
        }

        private void Update()
        {
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen)
            {
                if (PubgDoorUI.Instance != null) PubgDoorUI.Instance.Hide();
                return;
            }

            CheckForInteraction();
            HandleInput();
        }

        private void CheckForInteraction()
        {
            if (playerCamera == null) return;

            DoorInteractable detectedDoor = null;
            ItemPickup detectedPickup = null;
            HorrorGame.InteractSystem.IPlayerInteractable detectedInteractable = null;

            // 1. Raycast từ tâm Camera
            Ray ray = new Ray(playerCamera.position, playerCamera.forward);
            float actualDistance = interactDistance + 2.5f;
            if (Physics.Raycast(ray, out RaycastHit hit, actualDistance, interactLayer, QueryTriggerInteraction.Ignore))
            {
                detectedDoor = hit.collider.GetComponentInParent<DoorInteractable>() ?? hit.collider.GetComponentInChildren<DoorInteractable>();
                if (detectedDoor != null)
                {
                    string dName = detectedDoor.gameObject.name.ToLower();
                    if (dName.Contains("zone") || dName.Contains("clutter") || dName.Contains("panel") || dName.Contains("slot") || dName.Contains("headlight") || dName.Contains("hint"))
                    {
                        detectedDoor = null;
                    }
                }

                if (detectedDoor == null)
                {
                    detectedPickup = hit.collider.GetComponentInParent<ItemPickup>() ?? hit.collider.GetComponentInChildren<ItemPickup>();
                    if (detectedPickup == null)
                    {
                        currentTargetRepair = hit.collider.GetComponentInParent<HorrorGame.Story.BrokenFloorRepairInteractable>() ?? hit.collider.GetComponentInChildren<HorrorGame.Story.BrokenFloorRepairInteractable>();
                        if (currentTargetRepair == null)
                        {
                            var interactable = hit.collider.GetComponentInParent<HorrorGame.InteractSystem.IPlayerInteractable>() ?? hit.collider.GetComponentInChildren<HorrorGame.InteractSystem.IPlayerInteractable>();
                            if (interactable != null && interactable.CanInteract())
                            {
                                detectedInteractable = interactable;
                            }
                        }
                    }
                    else
                    {
                        currentTargetRepair = null;
                    }
                }
                else
                {
                    currentTargetRepair = null;
                }
            }
            else
            {
                currentTargetRepair = null;
            }

            // 2. Fallback cho Cửa: Nếu tâm camera hơi lệch nhưng người chơi đứng rất gần cửa
            if (detectedDoor == null && detectedPickup == null && currentTargetRepair == null)
            {
                Collider[] closeColliders = Physics.OverlapSphere(transform.position + Vector3.up, 1.6f, interactLayer, QueryTriggerInteraction.Ignore);
                float bestDot = 0.5f; // Chỉ nhận nếu người chơi nhìn về phía cửa
                foreach (var col in closeColliders)
                {
                    DoorInteractable door = col.GetComponentInParent<DoorInteractable>() ?? col.GetComponentInChildren<DoorInteractable>();
                    if (door != null)
                    {
                        Vector3 dirToDoor = (door.transform.position - transform.position).normalized;
                        float dot = Vector3.Dot(playerCamera.forward, dirToDoor);
                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            detectedDoor = door;
                        }
                    }
                }
            }

            currentTargetDoor = detectedDoor;
            currentTargetPickup = detectedPickup;
            currentTargetInteractable = detectedInteractable;

            bool isHovering = (currentTargetDoor != null || currentTargetPickup != null || currentTargetRepair != null || currentTargetInteractable != null);
            if (crosshairDotImg != null && crosshairDotRt != null)
            {
                crosshairDotImg.color = isHovering ? dotHoverColor : dotNormalColor;
                crosshairDotRt.sizeDelta = isHovering ? new Vector2(8f, 8f) : new Vector2(5f, 5f);
            }

            // Hiển thị HUD PUBG
            if (PubgDoorUI.Instance != null)
            {
                if (currentTargetInteractable != null)
                {
                    PubgDoorUI.Instance.Show(currentTargetInteractable.GetInteractionPrompt(), currentTargetInteractable.GetInteractionKey());
                }
                else if (currentTargetRepair != null)
                {
                    PubgDoorUI.Instance.Show(currentTargetRepair.GetActionText(), "F");
                }
                else if (currentTargetPickup != null)
                {
                    PubgDoorUI.Instance.Show(currentTargetPickup.GetPrompt(), "F");
                }
                else if (currentTargetDoor != null)
                {
                    PubgDoorUI.Instance.Show(currentTargetDoor.GetActionText(), "F");
                }
                else
                {
                    PubgDoorUI.Instance.Hide();
                }
            }
        }

        public bool HasActiveTarget()
        {
            return (currentTargetDoor != null || currentTargetPickup != null || currentTargetRepair != null || currentTargetInteractable != null);
        }

        private void HandleInput()
        {
            if (currentTargetDoor == null && currentTargetPickup == null && currentTargetRepair == null && currentTargetInteractable == null) return;

            bool fPressed = false;

            // New Input System: Phím F trên bàn phím
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                fPressed = true;
            }
            // Hỗ trợ nút tương tác trên Gamepad (nút X trên Xbox / nút Vuông trên PS)
            else if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                fPressed = true;
            }

            if (fPressed)
            {
                if (currentTargetInteractable != null)
                {
                    var interactable = currentTargetInteractable;
                    currentTargetInteractable = null;
                    if (PubgDoorUI.Instance != null)
                    {
                        PubgDoorUI.Instance.Hide();
                    }
                    interactable.OnInteract();
                }
                else if (currentTargetRepair != null)
                {
                    currentTargetRepair.Interact();
                    if (PubgDoorUI.Instance != null)
                    {
                        PubgDoorUI.Instance.Show(currentTargetRepair.GetActionText(), "F");
                    }
                }
                else if (currentTargetPickup != null)
                {
                    ItemPickup pickup = currentTargetPickup;
                    currentTargetPickup = null;
                    if (PubgDoorUI.Instance != null)
                    {
                        PubgDoorUI.Instance.Hide();
                    }
                    pickup.Pickup();
                }
                else if (currentTargetDoor != null)
                {
                    currentTargetDoor.ToggleDoor(transform.position);

                    // Cập nhật ngay lập tức text gợi ý trên HUD sau khi bấm
                    if (PubgDoorUI.Instance != null)
                    {
                        PubgDoorUI.Instance.Show(currentTargetDoor.GetActionText(), "F");
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactDistance);
            }
        }
    }
}
