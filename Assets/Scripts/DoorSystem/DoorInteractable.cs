using System.Collections;
using UnityEngine;

namespace HorrorGame.DoorSystem
{
    public class DoorInteractable : MonoBehaviour
    {
        [Header("Door Leaves")]
        [Tooltip("Cánh cửa chính (nếu để trống sẽ tự lấy Transform hiện tại)")]
        public Transform mainDoorLeaf;

        [Tooltip("Cánh cửa thứ 2 (nếu là cửa đôi)")]
        public Transform secondDoorLeaf;

        [Tooltip("Nếu là cánh phụ của cửa đôi, liên kết tới cánh chính")]
        public DoorInteractable masterDoor;

        [Header("Door Settings")]
        [Tooltip("Góc mở tiêu chuẩn (độ)")]
        public float openAngle = 90f;

        [Tooltip("Thời gian mở/đóng (giây)")]
        public float animationDuration = 0.85f;

        [Tooltip("Tự động mở đẩy về phía trước nhân vật")]
        public bool pushAwayFromPlayer = false;

        [Tooltip("Đảo chiều xoay nếu cần")]
        public bool invertDirection = false;

        [Tooltip("Nếu cửa ở trạng thái mở sẵn khi bắt đầu màn chơi")]
        public bool startsOpen = false;

        [Header("State")]
        [SerializeField] private bool isOpen = false;
        private bool isAnimating = false;
        private bool isInitialized = false;

        // Lưu trữ góc đóng chuẩn ban đầu (World Rotation)
        private Quaternion closedRotMain;
        private Quaternion closedRotSecond;
        private Quaternion targetRotMain;
        private Quaternion targetRotSecond;

        [Header("Audio (Tùy chọn)")]
        public AudioSource audioSource;
        public AudioClip openSound;
        public AudioClip closeSound;

        public bool IsOpen => masterDoor != null ? masterDoor.isOpen : isOpen;
        public bool IsAnimating => masterDoor != null ? masterDoor.isAnimating : isAnimating;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
                audioSource.minDistance = 1.5f;
                audioSource.maxDistance = 18f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }

            LoadDefaultSounds();
            InitializeClosedRotations();
        }

        public void LoadDefaultSounds()
        {
#if UNITY_EDITOR
            if (openSound == null)
            {
                openSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Granny_Door_Creak.mp3");
                if (openSound == null)
                    openSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_WoodDoor_Creak.wav");
            }
            if (closeSound == null)
            {
                closeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Granny_Door_Close.mp3");
                if (closeSound == null)
                    closeSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_WoodDoor_Close.wav");
            }
#endif
        }

        public void InitializeClosedRotations(bool force = false)
        {
            if (masterDoor != null) return;
            if (isInitialized && !force) return;

            if (mainDoorLeaf == null)
            {
                mainDoorLeaf = transform;
            }

            if (startsOpen)
            {
                isOpen = true;
                // Nếu cửa startsOpen nhưng scene lưu ở trạng thái đóng:
                closedRotMain = mainDoorLeaf.rotation;
                if (secondDoorLeaf != null) closedRotSecond = secondDoorLeaf.rotation;
                
                ComputeTargetOpenRotations(transform.position + transform.forward);
                mainDoorLeaf.rotation = targetRotMain;
                if (secondDoorLeaf != null) secondDoorLeaf.rotation = targetRotSecond;
            }
            else
            {
                isOpen = false;
                closedRotMain = mainDoorLeaf.rotation;
                targetRotMain = closedRotMain;

                if (secondDoorLeaf != null)
                {
                    closedRotSecond = secondDoorLeaf.rotation;
                    targetRotSecond = closedRotSecond;
                }
            }

            isInitialized = true;
        }

        public string GetActionText()
        {
            if (masterDoor != null) return masterDoor.GetActionText();
            return isOpen ? "Đóng Cửa" : "Mở Cửa";
        }

        /// <summary>
        /// Gọi khi người chơi bấm nút F tương tác
        /// </summary>
        /// <param name="playerPos">Vị trí của người chơi</param>
        public void ToggleDoor(Vector3 playerPos)
        {
            if (masterDoor != null)
            {
                masterDoor.ToggleDoor(playerPos);
                return;
            }

            if (isAnimating) return;

            if (!isOpen)
            {
                OpenDoor(playerPos);
            }
            else
            {
                CloseDoor();
            }
        }

        private float GetDirectionSign(Vector3 playerPos)
        {
            if (!pushAwayFromPlayer) return 1f;

            Vector3 toPlayer = (playerPos - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toPlayer);
            return dot >= 0 ? -1f : 1f;
        }

        private void ComputeTargetOpenRotations(Vector3 playerPos)
        {
            float directionSign = GetDirectionSign(playerPos);
            if (invertDirection)
            {
                directionSign *= -1f;
            }

            Vector3 upAxis = transform.up;

            if (secondDoorLeaf != null)
            {
                // CỬA ĐÔI: Tự động phân biệt cánh Trái / Phải theo trục transform.right
                bool mainIsRight = Vector3.Dot(mainDoorLeaf.position - secondDoorLeaf.position, transform.right) >= 0;
                float mainSign = mainIsRight ? 1f : -1f;
                float secondSign = -mainSign;

                targetRotMain = Quaternion.AngleAxis(openAngle * directionSign * mainSign, upAxis) * closedRotMain;
                targetRotSecond = Quaternion.AngleAxis(openAngle * directionSign * secondSign, upAxis) * closedRotSecond;
            }
            else
            {
                // CỬA ĐƠN: Hỗ trợ cả trường hợp mesh bị Scale âm (mirrored)
                float scaleSign = Mathf.Sign(mainDoorLeaf.lossyScale.x);
                float finalAngle = openAngle * directionSign * scaleSign;
                targetRotMain = Quaternion.AngleAxis(finalAngle, upAxis) * closedRotMain;
            }
        }

        public void OpenDoor(Vector3 playerPos)
        {
            if (isAnimating) return;

            ComputeTargetOpenRotations(playerPos);

            if (audioSource != null && openSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(openSound, 0.90f);
            }

            StartCoroutine(AnimateDoor(true));
        }

        public void CloseDoor()
        {
            if (isAnimating) return;

            targetRotMain = closedRotMain;
            if (secondDoorLeaf != null)
            {
                targetRotSecond = closedRotSecond;
            }

            if (audioSource != null && closeSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(closeSound, 0.85f);
            }

            StartCoroutine(AnimateDoor(false));
        }

        private IEnumerator AnimateDoor(bool opening)
        {
            isAnimating = true;
            isOpen = opening; // Cập nhật ngay trạng thái để UI phản hồi tức thì

            Quaternion startRotMain = mainDoorLeaf.rotation;
            Quaternion startRotSecond = secondDoorLeaf != null ? secondDoorLeaf.rotation : Quaternion.identity;

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                mainDoorLeaf.rotation = Quaternion.Slerp(startRotMain, targetRotMain, smoothT);

                if (secondDoorLeaf != null)
                {
                    secondDoorLeaf.rotation = Quaternion.Slerp(startRotSecond, targetRotSecond, smoothT);
                }

                yield return null;
            }

            mainDoorLeaf.rotation = targetRotMain;
            if (secondDoorLeaf != null)
            {
                secondDoorLeaf.rotation = targetRotSecond;
            }

            isOpen = opening;
            isAnimating = false;
        }

        /// <summary>
        /// Đưa cửa về trạng thái đóng chuẩn ngay lập tức (dùng cho Editor hoặc Script reset)
        /// </summary>
        public void SnapToClosed()
        {
            if (masterDoor != null)
            {
                masterDoor.SnapToClosed();
                return;
            }

            if (mainDoorLeaf != null) mainDoorLeaf.rotation = closedRotMain;
            if (secondDoorLeaf != null) secondDoorLeaf.rotation = closedRotSecond;
            isOpen = false;
            isAnimating = false;
        }
    }
}
