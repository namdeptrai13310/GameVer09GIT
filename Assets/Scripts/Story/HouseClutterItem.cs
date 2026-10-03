using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Các đồ đạc hư hỏng bừa bộn trong biệt thự (ghế gãy, bàn vỡ, hộp sắt rỉ, tủ mục) cần dọn dẹp.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HouseClutterItem : MonoBehaviour, IPlayerInteractable
    {
        [Header("Clutter Info")]
        public string clutterName = "Thùng Đồ Cũ Nát";
        public Sprite clutterIcon;
        public bool isPickedUp = false;

        private void Awake()
        {
            if (clutterIcon == null)
            {
                clutterIcon = HorrorGame.Player.QuickSlotSystem.GetDefaultItemIcon(gameObject.name);
            }
        }

        public bool CanInteract()
        {
            if (isPickedUp) return false;
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Đang bê món đồ khác thì không bê thêm được
            return !VillaSurveyAndRepairQuest.Instance.isCarryingClutter;
        }

        public string GetInteractionPrompt()
        {
            if (isPickedUp) return "";
            if (VillaSurveyAndRepairQuest.Instance != null && 
                VillaSurveyAndRepairQuest.Instance.currentPhase < VillaSurveyAndRepairQuest.QuestPhase.CleanVilla)
            {
                return "Chưa Cần Dọn: Khảo sát cầu thang trước";
            }
            return "Bê " + clutterName;
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isPickedUp) return;
            if (VillaSurveyAndRepairQuest.Instance == null) return;

            if (VillaSurveyAndRepairQuest.Instance.currentPhase < VillaSurveyAndRepairQuest.QuestPhase.CleanVilla)
            {
                string pName = PlayerPrefs.GetString("PlayerName", "An");
                StoryObjectiveBanner.ShowObjective(
                    pName.ToUpper(), 
                    "Nhà bừa bộn thật, nhưng hãy đi dọc hành lang tìm cầu thang kiểm tra trước đã!", 
                    4.0f
                );
                return;
            }

            isPickedUp = true;
            VillaSurveyAndRepairQuest.Instance.PickUpClutter(this);
            gameObject.SetActive(false);
        }
    }
}
