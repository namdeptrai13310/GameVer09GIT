using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using HorrorGame.InteractSystem;
using HorrorGame.Player;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Điểm trốn (Tủ sắt / Tủ gỗ / Thùng phi) để người chơi ẩn nấp khi bị quái vật truy đuổi.
    /// Cho phép nhìn qua khe cửa/lỗ nhìn, nín thở và hỗ trợ minigame giữ bình tĩnh.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HidingSpot : MonoBehaviour, IPlayerInteractable
    {
        public enum SpotType
        {
            MetalLocker,    // Tủ sắt hai cánh
            WoodenWardrobe, // Tủ gỗ lớn
            BarrelOrBox     // Thùng gỗ / Thùng phi
        }

        [Header("Spot Settings")]
        public SpotType spotType = SpotType.MetalLocker;
        public string spotName = "Tủ Sắt Cũ";
        public Transform peepCamPoint;    // Điểm đặt camera nhìn qua khe cửa khi trốn
        public Transform exitPoint;       // Điểm bước ra ngoài khi rời tủ

        [Header("Door Animation / Mesh")]
        public Transform leftDoor;
        public Transform rightDoor;
        public float doorOpenAngle = 80f;
        public float animSpeed = 4f;

        [Header("Peep Look Clamping")]
        public float maxYaw = 40f;
        public float maxPitch = 25f;
        private float currentYaw = 0f;
        private float currentPitch = 0f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxEnter;
        public AudioClip sfxExit;
        public AudioClip sfxHeartbeat;

        [Header("State")]
        public bool isOccupied = false;
        private bool isTransitioning = false;

        private GameObject playerObj;
        private CharacterController characterController;
        private PlayerController playerController;
        private Camera playerCamera;
        private Vector3 originalCamLocalPos;
        private Quaternion originalCamLocalRot;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
            }

            if (peepCamPoint == null)
            {
                var existing = transform.Find("PeepCamPoint");
                if (existing != null) peepCamPoint = existing;
                else
                {
                    GameObject peep = new GameObject("PeepCamPoint");
                    peep.transform.SetParent(transform, false);
                    peep.transform.localPosition = new Vector3(0f, 1.4f, 0.18f);
                    peepCamPoint = peep.transform;
                }
            }

            if (exitPoint == null)
            {
                var existing = transform.Find("ExitPoint");
                if (existing != null) exitPoint = existing;
                else
                {
                    GameObject exit = new GameObject("ExitPoint");
                    exit.transform.SetParent(transform, false);
                    exit.transform.localPosition = new Vector3(0f, 0f, 0.95f);
                    exitPoint = exit.transform;
                }
            }
        }

        private void Update()
        {
            if (!isOccupied || isTransitioning) return;

            // Xử lý góc nhìn liếc nhẹ qua khe cửa khi đang trốn
            if (Mouse.current != null && playerCamera != null && peepCamPoint != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.1f;
                currentYaw = Mathf.Clamp(currentYaw + mouseDelta.x, -maxYaw, maxYaw);
                currentPitch = Mathf.Clamp(currentPitch - mouseDelta.y, -maxPitch, maxPitch);

                playerCamera.transform.position = peepCamPoint.position;
                playerCamera.transform.rotation = peepCamPoint.rotation * Quaternion.Euler(currentPitch, currentYaw, 0f);
            }

            // Nhấn phím F hoặc E để rời khỏi chỗ trốn
            if (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                ExitSpot();
            }
        }

        public bool CanInteract()
        {
            return !isTransitioning;
        }

        public string GetInteractionPrompt()
        {
            return isOccupied ? "Rời Khỏi Chỗ Trốn" : $"Trốn Vào {spotName}";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (!isOccupied)
            {
                EnterSpot();
            }
            else
            {
                ExitSpot();
            }
        }

        public void EnterSpot()
        {
            if (isOccupied || isTransitioning) return;

            playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc != null) playerObj = pc.gameObject;
            }

            if (playerObj == null) return;

            characterController = playerObj.GetComponent<CharacterController>();
            playerController = playerObj.GetComponent<PlayerController>();
            playerCamera = playerObj.GetComponentInChildren<Camera>();

            StartCoroutine(RoutineEnter());
        }

        public void ExitSpot()
        {
            if (!isOccupied || isTransitioning) return;
            StartCoroutine(RoutineExit());
        }

        private IEnumerator RoutineEnter()
        {
            isTransitioning = true;

            if (sfxEnter != null && audioSource != null)
                audioSource.PlayOneShot(sfxEnter, 0.9f);

            // Lưu lại vị trí & góc xoay camera ban đầu
            if (playerCamera != null)
            {
                originalCamLocalPos = playerCamera.transform.localPosition;
                originalCamLocalRot = playerCamera.transform.localRotation;
            }

            // Tắt điều khiển người chơi & CharacterController
            if (playerController != null) playerController.enabled = false;
            if (characterController != null) characterController.enabled = false;

            // Đưa người chơi vào vị trí trong tủ
            playerObj.transform.position = transform.position;
            currentYaw = 0f;
            currentPitch = 0f;

            if (playerCamera != null && peepCamPoint != null)
            {
                playerCamera.transform.position = peepCamPoint.position;
                playerCamera.transform.rotation = peepCamPoint.rotation;
            }

            yield return new WaitForSeconds(0.2f);

            isOccupied = true;
            isTransitioning = false;

            StoryObjectiveBanner.ShowObjective(
                "ĐANG ẨN NẤP",
                $"Đã trốn vào {spotName}. Giữ bình tĩnh... Di chuột để nhìn qua khe cửa | Nhấn [F] để bước ra.",
                3.5f
            );
        }

        private IEnumerator RoutineExit()
        {
            isTransitioning = true;

            if (sfxExit != null && audioSource != null)
                audioSource.PlayOneShot(sfxExit, 0.9f);

            yield return new WaitForSeconds(0.15f);

            // Bước ra ngoài vị trí thoát
            if (playerObj != null && exitPoint != null)
            {
                playerObj.transform.position = exitPoint.position;
                playerObj.transform.rotation = exitPoint.rotation;
            }

            // Phục hồi lại camera theo firstPersonCamOffset
            if (playerCamera != null)
            {
                if (playerController != null && playerController.firstPersonCamOffset != Vector3.zero)
                {
                    playerCamera.transform.localPosition = playerController.firstPersonCamOffset;
                }
                else
                {
                    playerCamera.transform.localPosition = originalCamLocalPos != Vector3.zero ? originalCamLocalPos : new Vector3(0f, 1.819f, 0.254f);
                }
                playerCamera.transform.localRotation = Quaternion.identity;
            }

            // Bật lại CharacterController và PlayerController
            if (characterController != null) characterController.enabled = true;
            if (playerController != null) playerController.enabled = true;

            isOccupied = false;
            isTransitioning = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            if (exitPoint != null) Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
            Gizmos.color = Color.cyan;
            if (peepCamPoint != null) Gizmos.DrawWireSphere(peepCamPoint.position, 0.15f);
        }
    }
}
