using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Điểm sàn gỗ bị mục nát trên hành lang tầng 2.
    /// Người chơi cần thu thập đủ 3 tấm ván gỗ (và hộp dụng cụ) để đóng đinh gia cố lại mảng sàn thành mặt phẳng an toàn.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BrokenFloorRepairInteractable : MonoBehaviour, IPlayerInteractable
    {
        [Header("State")]
        public bool isRepaired = false;

        [Header("Visual Meshes")]
        public GameObject brokenHoleVisual;
        public GameObject repairedWoodVisual;
        public Collider blockingCollider;

        [Header("Prompt Settings")]
        public string actionName = "Đóng Đinh & Sửa Sàn Gỗ";

        private void Start()
        {
            UpdateVisuals();
        }

        public bool CanInteract()
        {
            if (isRepaired) return false;
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Cho phép tương tác khi ở phase sửa sàn hoặc khi đã có đủ ván gỗ
            return VillaSurveyAndRepairQuest.Instance.currentPhase == VillaSurveyAndRepairQuest.QuestPhase.RepairFloor ||
                   VillaSurveyAndRepairQuest.Instance.woodPlanksCollected >= VillaSurveyAndRepairQuest.Instance.woodPlanksRequired;
        }

        public string GetInteractionPrompt()
        {
            if (isRepaired) return "";

            if (VillaSurveyAndRepairQuest.Instance != null && VillaSurveyAndRepairQuest.Instance.woodPlanksCollected >= VillaSurveyAndRepairQuest.Instance.woodPlanksRequired)
            {
                return "Đóng Đinh Gia Cố Sàn Gỗ (3/3 Ván)";
            }
            int collected = VillaSurveyAndRepairQuest.Instance != null ? VillaSurveyAndRepairQuest.Instance.woodPlanksCollected : 0;
            return $"Cần Ván Gỗ Để Vá Sàn ({collected}/3 Ván)";
        }

        public string GetActionText() => GetInteractionPrompt();

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isRepaired) return;

            if (VillaSurveyAndRepairQuest.Instance != null && VillaSurveyAndRepairQuest.Instance.CanRepairFloor())
            {
                isRepaired = true;
                UpdateVisuals();
                VillaSurveyAndRepairQuest.Instance.OnFloorRepaired();
            }
            else
            {
                int collected = VillaSurveyAndRepairQuest.Instance != null ? VillaSurveyAndRepairQuest.Instance.woodPlanksCollected : 0;
                StoryObjectiveBanner.ShowObjective("CHƯA ĐỦ VÁN GỖ", $"Bạn cần đủ 3 tấm Ván Gỗ để đóng vá sàn! Hiện tại: {collected}/3 ván.", 4.0f);
            }
        }

        public void Interact() => OnInteract();

        public void UpdateVisuals()
        {
            if (brokenHoleVisual != null) brokenHoleVisual.SetActive(!isRepaired);
            if (repairedWoodVisual != null) repairedWoodVisual.SetActive(isRepaired);
            if (blockingCollider != null) blockingCollider.enabled = !isRepaired;
        }
    }
}
