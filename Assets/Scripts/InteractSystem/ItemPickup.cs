using UnityEngine;
using HorrorGame.Player;
using HorrorGame.QuestSystem;

namespace HorrorGame.InteractSystem
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IPlayerInteractable
    {
        [Header("Item Info")]
        public string itemId = "flashlight";
        public string itemName = "Đèn Pin";
        public Sprite itemIcon;
        public GameObject dropPrefab;

        [Header("Audio")]
        public AudioClip pickupClip;

        [Header("Effects")]
        public Light subtleLight;

        public bool CanInteract() => true;
        public string GetInteractionPrompt() => GetPrompt();
        public string GetInteractionKey() => "F";
        public void OnInteract() => Pickup();

        private void Start()
        {
            // Tự động gán icon mini 3D sắc nét theo đúng itemId
            if (itemIcon == null)
            {
                itemIcon = QuickSlotSystem.GetDefaultItemIcon(itemId);
            }
        }

        public string GetPrompt()
        {
            return "Nhặt " + itemName;
        }

        public bool Pickup()
        {
            if (QuickSlotSystem.Instance == null) return false;

            if (itemIcon == null)
            {
                itemIcon = QuickSlotSystem.GetDefaultItemIcon(itemId);
            }

            GameObject prefabToPass = dropPrefab;
            if (prefabToPass == null || prefabToPass.scene.IsValid())
            {
                if (itemId == "flashlight" && QuickSlotSystem.Instance != null)
                {
                    prefabToPass = QuickSlotSystem.Instance.defaultFlashlightDropPrefab;
                }
            }

            bool added = QuickSlotSystem.Instance.AddItem(itemId, itemName, itemIcon, prefabToPass);
            if (added)
            {
                // Kích hoạt animation nhân vật cúi người vươn tay nhặt đồ
                if (PlayerHandEquipment.Instance != null)
                {
                    PlayerHandEquipment.Instance.PlayPickupAnimation();
                }

                // Thông báo cho Quest Manager và kích hoạt PlayerFlashlight nếu đây là Đèn Pin
                if (itemId == "flashlight")
                {
                    if (PlayerFlashlight.Instance != null)
                    {
                        PlayerFlashlight.Instance.EquipFlashlight();
                    }
                    if (HorrorQuestManager.Instance != null)
                    {
                        HorrorQuestManager.Instance.OnFlashlightFound();
                    }
                    if (HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                    {
                        HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.OnFlashlightPickedUp();
                    }
                }
                else if (itemId == "toolbox" && HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                {
                    HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.CollectToolbox();
                }
                else if (itemId == "wood_plank" && HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                {
                    HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.CollectWoodPlank();
                }
                else if (itemId == "axe" && HorrorGame.Story.VillaSurveyAndRepairQuest.Instance != null)
                {
                    HorrorGame.Story.VillaSurveyAndRepairQuest.Instance.CollectAxe();
                }

                // Phát âm thanh nhặt đồ
                PlayPickupAudio();

                // Hủy vật thể trên sàn / bàn
                Destroy(gameObject);
                return true;
            }

            return false;
        }

        private void PlayPickupAudio()
        {
            AudioClip clip = pickupClip != null ? pickupClip : GeneratePickupSound();
            AudioSource.PlayClipAtPoint(clip, transform.position, 0.75f);
        }

        private AudioClip GeneratePickupSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.12f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float tone1 = Mathf.Sin(2f * Mathf.PI * 880f * (float)i / sampleRate);
                float tone2 = Mathf.Sin(2f * Mathf.PI * 1320f * (float)i / sampleRate);
                float env = Mathf.Exp(-t * 18f);
                samples[i] = (tone1 * 0.5f + tone2 * 0.5f) * env * 0.5f;
            }
            AudioClip clip = AudioClip.Create("ItemPickupSound", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
