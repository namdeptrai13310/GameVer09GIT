using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Chìa Khóa Cửa Hầm Bí Mật đặt trên bàn làm việc trong phòng ngủ/phòng thờ tầng 2.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BasementKeyPickupItem : MonoBehaviour, IPlayerInteractable
    {
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxKeyPickup;

        public bool isPickedUp = false;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.8f;
            }
        }

        public bool CanInteract()
        {
            if (isPickedUp) return false;
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Chỉ có thể nhặt chìa khóa khi đã sửa xong sàn tầng 2 và bước vào phòng
            return VillaSurveyAndRepairQuest.Instance.isFloorRepaired;
        }

        public string GetInteractionPrompt()
        {
            return "Nhặt Chìa Khóa Tầng Hầm";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isPickedUp) return;
            isPickedUp = true;

            if (sfxKeyPickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxKeyPickup);
            }

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ CÓ CHÌA KHÓA TẦNG HẦM",
                "Chiếc chìa khóa sắt rỉ sét nặng trịch. Hãy tìm cửa hầm bí mật ở gian nhà kho sau nhà!",
                5.5f
            );

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnBasementKeyCollected();
            }

            if (HorrorGame.Player.PlayerHandEquipment.Instance != null)
            {
                HorrorGame.Player.PlayerHandEquipment.Instance.PlayPickupAnimation();
            }

            if (HorrorGame.Player.QuickSlotSystem.Instance != null)
            {
                Sprite icon = HorrorGame.Player.QuickSlotSystem.GetDefaultItemIcon("basement_key");
                HorrorGame.Player.QuickSlotSystem.Instance.AddItem("basement_key", "Chìa Khóa Hầm", icon, null);
            }

            gameObject.SetActive(false);
        }
    }
}
