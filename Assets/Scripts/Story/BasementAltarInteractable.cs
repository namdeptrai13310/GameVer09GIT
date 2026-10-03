using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Bàn tế lễ tà thuật cổ xưa tại phòng xác / nhà tang lễ dưới Tầng Hầm (Morgue).
    /// Đây là nơi diễn ra phân nhánh kết thúc chính:
    /// - Không có Bùa Trấn Yểm: Ma quỷ thức tỉnh -> Bad Ending.
    /// - Có Bùa Trấn Yểm: Đặt bùa lên bàn thờ -> Hóa giải lời nguyền -> True Ending.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BasementAltarInteractable : MonoBehaviour, IPlayerInteractable
    {
        [Header("State")]
        public bool isPurified = false;

        [Header("Altar Visuals")]
        public GameObject cursedCandlesObj;
        public GameObject holyLightObj;

        private void Awake()
        {
            if (holyLightObj != null) holyLightObj.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isPurified) return;

            // Kiểm tra nếu là người chơi bước vào phòng tế lễ
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                if (VillaSurveyAndRepairQuest.Instance != null && !VillaSurveyAndRepairQuest.Instance.hasTalisman)
                {
                    // Người chơi KHÔNG có bùa trấn yểm mà bước vào phòng tế -> BAD ENDING!
                    if (HorrorEndingManager.Instance != null && !HorrorEndingManager.Instance.isEndingTriggered)
                    {
                        HorrorEndingManager.Instance.TriggerCurseBadEnding();
                    }
                }
            }
        }

        public bool CanInteract()
        {
            if (isPurified) return false;
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Chỉ tương tác khi có bùa trấn yểm
            return VillaSurveyAndRepairQuest.Instance.hasTalisman;
        }

        public string GetInteractionPrompt()
        {
            return "Đặt Bùa Trấn Yểm Lên Bàn Thờ (Hóa Giải Lời Nguyền)";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isPurified) return;
            isPurified = true;

            if (holyLightObj != null) holyLightObj.SetActive(true);
            if (cursedCandlesObj != null) cursedCandlesObj.SetActive(false);

            if (HorrorEndingManager.Instance != null)
            {
                HorrorEndingManager.Instance.TriggerTruePurificationEnding();
            }
        }
    }
}
