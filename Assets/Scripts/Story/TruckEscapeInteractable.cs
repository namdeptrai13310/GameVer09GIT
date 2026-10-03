using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Điểm tương tác trên chiếc xe bán tải ngoài cổng để kích hoạt Kết thúc 1: Tẩu Thoát Trong Sợ Hãi.
    /// Khả dụng sau khi người chơi phát hiện ra những bí ẩn kinh hoàng trong biệt thự (đọc di thư hoặc mở cửa hầm).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TruckEscapeInteractable : MonoBehaviour, IPlayerInteractable
    {
        public bool CanInteract()
        {
            if (HorrorEndingManager.Instance != null && HorrorEndingManager.Instance.isEndingTriggered)
            {
                return false;
            }

            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Người chơi chỉ có thể chọn tẩu thoát khi đã đọc di thư hoặc đã sửa xong sàn/mở cửa hầm
            return VillaSurveyAndRepairQuest.Instance.hasReadSecretLetter || VillaSurveyAndRepairQuest.Instance.isFloorRepaired;
        }

        public string GetInteractionPrompt()
        {
            return "Nổ Máy Xe Tẩu Thoát Khỏi Biệt Thự";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (HorrorEndingManager.Instance != null)
            {
                HorrorEndingManager.Instance.TriggerEscapeEnding();
            }
        }
    }
}
