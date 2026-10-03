using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Bãi rác sân sau / Đống thiêu hủy rác phế thải.
    /// Nơi người chơi đem các thùng đồ cũ nát ra quăng.
    /// Khi gom đủ đồ định châm lửa đốt, một ô tủ cũ hé mở làm lộ ra bức di thư manh mối!
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BackyardBurnSite : MonoBehaviour, IPlayerInteractable
    {
        public static BackyardBurnSite Instance { get; private set; }

        [Header("Cabinet / Drawer Clue Event")]
        [Tooltip("Cánh cửa tủ hoặc ngăn kéo sẽ tự động hé mở")]
        public Transform cabinetDoorTransform;
        public Vector3 cabinetOpenRotation = new Vector3(0f, 65f, 0f);
        public float doorOpenSpeed = 1.2f;

        [Tooltip("Vật phẩm lá thư bí mật lộ ra")]
        public GameObject secretLetterObject;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxTrashDrop;
        public AudioClip sfxCreakDrawer;

        [Header("Visual Pile")]
        public Transform droppedItemsContainer;

        private bool eventTriggered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.8f;
            }

            if (secretLetterObject != null)
            {
                secretLetterObject.SetActive(false);
            }
        }

        public bool CanInteract()
        {
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Người chơi đang bê rác mới có thể tương tác để quăng rác vào bãi
            return VillaSurveyAndRepairQuest.Instance.isCarryingClutter;
        }

        public string GetInteractionPrompt()
        {
            return "Vứt Rác Vào Bãi Đốt";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (VillaSurveyAndRepairQuest.Instance == null) return;

            // Phát âm thanh vứt rác bịch nặng
            if (sfxTrashDrop != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxTrashDrop, 0.85f);
            }

            VillaSurveyAndRepairQuest.Instance.DropClutterAtBackyard();
        }

        /// <summary>
        /// Kích hoạt khi người chơi đã dọn sạch tất cả thùng đồ ra bãi sau
        /// </summary>
        public void TriggerSecretCabinetEvent()
        {
            if (eventTriggered) return;
            eventTriggered = true;

            StartCoroutine(RoutineOpenCabinetAndRevealLetter());
        }

        private IEnumerator RoutineOpenCabinetAndRevealLetter()
        {
            yield return new WaitForSeconds(1.0f);

            // Tiếng nhân vật cảm thán
            string playerName = PlayerPrefs.GetString("PlayerName", "An");
            StoryObjectiveBanner.ShowObjective(playerName.ToUpper(), "Phù... Cuối cùng cũng dọn xong đống đồ cũ này ra đây. Giờ chuẩn bị đốt...", 4.0f);

            yield return new WaitForSeconds(4.2f);

            // Phát tiếng cọt kẹt rùng rợn của cánh tủ/ngăn kéo
            if (sfxCreakDrawer != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxCreakDrawer, 0.95f);
            }

            // Xoay từ từ cánh tủ/ngăn kéo hé mở
            if (cabinetDoorTransform != null)
            {
                Quaternion startRot = cabinetDoorTransform.localRotation;
                Quaternion targetRot = Quaternion.Euler(cabinetOpenRotation);

                float elapsed = 0f;
                float duration = 1.8f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    cabinetDoorTransform.localRotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                    yield return null;
                }
                cabinetDoorTransform.localRotation = targetRot;
            }

            yield return new WaitForSeconds(0.3f);

            // Hiện bức thư bí mật
            if (secretLetterObject != null)
            {
                secretLetterObject.SetActive(true);
            }

            // Lời thoại bất ngờ
            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(), 
                "Khoan đã... Cái hộc tủ cũ kia tự mở ra?! Bên trong dường như có một lá thư...", 
                5.0f
            );

            // Cập nhật mục tiêu
            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnCabinetOpened();
            }
        }
    }
}
