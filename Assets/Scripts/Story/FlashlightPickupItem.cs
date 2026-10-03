using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.Player;

namespace HorrorGame.Story
{
    /// <summary>
    /// Vật phẩm Đèn Pin nằm trên bàn sảnh chính.
    /// Người chơi bước vào căn nhà tối om phải nhặt đèn pin để bật sáng sảnh trước khi khảo sát.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FlashlightPickupItem : MonoBehaviour, IPlayerInteractable
    {
        [Header("Item Info")]
        public Sprite itemIcon;
        public GameObject dropPrefab;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxPickup;

        private bool isPickedUp = false;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.8f;
            }

            if (itemIcon == null)
            {
                itemIcon = QuickSlotSystem.GetDefaultItemIcon("flashlight");
            }
        }

        public bool CanInteract()
        {
            if (isPickedUp) return false;
            var playerFlashlight = Object.FindFirstObjectByType<PlayerFlashlight>();
            if (playerFlashlight != null && playerFlashlight.IsEquipped) return false;

            return true;
        }

        public string GetInteractionPrompt()
        {
            return "Nhặt Đèn Pin";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isPickedUp) return;
            isPickedUp = true;

            if (PlayerHandEquipment.Instance != null)
            {
                PlayerHandEquipment.Instance.PlayPickupAnimation();
            }

            var playerFlashlight = Object.FindFirstObjectByType<PlayerFlashlight>();
            if (playerFlashlight != null)
            {
                playerFlashlight.EquipFlashlight();
            }

            if (QuickSlotSystem.Instance != null)
            {
                if (itemIcon == null)
                {
                    itemIcon = QuickSlotSystem.GetDefaultItemIcon("flashlight");
                }
                QuickSlotSystem.Instance.AddItem("flashlight", "Đèn Pin", itemIcon, dropPrefab);
            }

            if (sfxPickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxPickup);
            }

            // Thông báo trên HUD
            StoryObjectiveBanner.ShowObjective(
                "ĐÃ CÓ ĐÈN PIN!",
                "Ánh sáng xé toạc bóng tối. Nhấn [T] hoặc [Chuột Phải] để bật/tắt đèn pin.",
                5.0f
            );

            // Báo cho Quest Manager
            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnFlashlightPickedUp();
            }

            // Ẩn vật thể trên bàn
            gameObject.SetActive(false);
        }
    }
}
