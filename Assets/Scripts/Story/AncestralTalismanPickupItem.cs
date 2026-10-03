using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Lá Bùa Trấn Yểm Gia Tộc được phong ấn trên bàn thờ tổ tiên ở tầng 2.
    /// Mang lá bùa này khi xuống tầng hầm sẽ hóa giải lời nguyền và mở ra True Ending!
    /// Nếu xuống tầng hầm mà KHÔNG mang bùa, ma quỷ sẽ thức tỉnh dẫn đến Bad Ending!
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AncestralTalismanPickupItem : MonoBehaviour, IPlayerInteractable
    {
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxTalismanPickup;

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

            return VillaSurveyAndRepairQuest.Instance.isFloorRepaired;
        }

        public string GetInteractionPrompt()
        {
            return "Nhặt Lá Bùa Trấn Yểm Gia Tộc";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isPickedUp) return;
            isPickedUp = true;

            if (sfxTalismanPickup != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxTalismanPickup);
            }

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ CẦM BÙA TRẤN YỂM!",
                "Lá bùa phát ra hơi ấm kỳ lạ xua tan chướng khí. Giờ bạn có thể an toàn bước xuống Tầng Hầm!",
                6.0f
            );

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnTalismanCollected();
            }

            if (HorrorGame.Player.PlayerHandEquipment.Instance != null)
            {
                HorrorGame.Player.PlayerHandEquipment.Instance.PlayPickupAnimation();
            }

            if (HorrorGame.Player.QuickSlotSystem.Instance != null)
            {
                Sprite icon = HorrorGame.Player.QuickSlotSystem.GetDefaultItemIcon("talisman");
                HorrorGame.Player.QuickSlotSystem.Instance.AddItem("talisman", "Bùa Trấn Yểm", icon, null);
            }

            gameObject.SetActive(false);
        }
    }
}
