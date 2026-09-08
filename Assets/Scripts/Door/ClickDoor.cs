using UnityEngine;
using UnityEngine.InputSystem;

// Gan script nay vao tung cua.
// Cua CHI mo/dong khi click chuot trai truc tiep vao cua.
// KHONG tu dong them/sua Collider - dung nguyen Collider co san cua ban.
public class ClickDoor : MonoBehaviour
{
    public enum DoorSide { Left, Right }
    public enum OpenAngleOption { Angle65 = 65, Angle90 = 90 }

    [Header("Door Side (chon tay cho dung)")]
    public DoorSide side = DoorSide.Left;

    [Header("Door Settings")]
    [Tooltip("Chon goc mo: 65 do hoac 90 do")]
    public OpenAngleOption openAngle = OpenAngleOption.Angle90;

    [Tooltip("Toc do xoay (do/giay)")]
    public float rotateSpeed = 180f;

    [Tooltip("Truc xoay cua: thuong la Y")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Interaction")]
    [Tooltip("Khoang cach toi da de click mo duoc cua")]
    public float interactDistance = 3f;

    [Tooltip("Camera dung de raycast, de trong se tu lay Camera.main")]
    public Camera playerCamera;

    [Header("Optional Sound")]
    public AudioSource doorSound;

    [Header("Debug")]
    [Tooltip("Bat de xem log kiem tra trong Console khi click chuot")]
    public bool debugLog = true;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Quaternion targetRotation;
    private bool isOpen = false;

    private const float SNAP_THRESHOLD = 0.05f;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Chi kiem tra va bao loi, KHONG tu dong them/sua Collider
        Collider col = GetComponent<Collider>();
        if (debugLog)
        {
            if (col == null)
                Debug.LogWarning($"[{name}] KHONG CO COLLIDER - Raycast se khong bao gio trung cua nay. Can tu them Collider (Box/Mesh) trong Inspector.");
            else if (col.isTrigger)
                Debug.LogWarning($"[{name}] Collider dang la Trigger (Is Trigger = true) - can bo tick de Raycast/va cham hoat dong dung.");
            else if (col is MeshCollider mc && mc.convex == false && mc.sharedMesh == null)
                Debug.LogWarning($"[{name}] MeshCollider khong co Mesh duoc gan (Shared Mesh = None).");

            if (playerCamera == null)
                Debug.LogWarning($"[{name}] Khong tim thay Camera (Camera.main = null). Hay gan tag 'MainCamera' cho camera, hoac keo tay vao truong Player Camera.");
        }

        closedRotation = transform.localRotation;

        float directionSign = (side == DoorSide.Left) ? -1f : 1f;
        float angle = (float)openAngle;
        openRotation = closedRotation * Quaternion.Euler(rotationAxis * (angle * directionSign));

        targetRotation = closedRotation;
    }

    void Update()
    {
        HandleClickInput();
        RotateSmoothly();
    }

    private void HandleClickInput()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (playerCamera == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            if (debugLog) Debug.Log($"[{name}] Raycast trung: {hit.collider.gameObject.name}");

            if (hit.collider.gameObject == gameObject || hit.transform.IsChildOf(transform))
            {
                ToggleDoor();
            }
        }
        else
        {
            if (debugLog) Debug.Log($"[{name}] Raycast khong trung gi trong pham vi {interactDistance}m - co the click khong nham dung cua, hoac cua qua xa.");
        }
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;

        if (doorSound != null) doorSound.Play();
    }

    private void RotateSmoothly()
    {
        if (Quaternion.Angle(transform.localRotation, targetRotation) < SNAP_THRESHOLD)
        {
            transform.localRotation = targetRotation;
            return;
        }

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    public void ForceOpen()
    {
        isOpen = true;
        targetRotation = openRotation;
    }

    public void ForceClose()
    {
        isOpen = false;
        targetRotation = closedRotation;
    }
}