using UnityEngine;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Vùng trigger đặt tại mỗi căn phòng để thông báo tên phòng trên HUD và kích hoạt các sự kiện khảo sát.
    /// Giúp người chơi cảm nhận sự khám phá chân thực theo phong cách Fears to Fathom.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomZoneTrigger : MonoBehaviour
    {
        [Header("Room Identity")]
        public string roomName = "Phòng Khách";
        public string roomSubtitle = "Tầng Trệt";

        [Header("Story Triggers")]
        public bool triggerVillaFoyerDarkness = false;
        public bool triggerBlockedStairs = false;

        private void OnTriggerEnter(Collider other)
        {
            // Bỏ qua nếu đang ở menu di chúc hoặc intro cutscene
            if (TestamentMenuController.Instance != null && TestamentMenuController.Instance.isMenuOpen) return;
            if (CinematicIntroManager.Instance != null && CinematicIntroManager.Instance.IsPlayingIntro) return;

            if (IsPlayer(other))
            {
                // 1. Hiển thị banner tên phòng
                if (RoomNotificationUI.Instance != null)
                {
                    RoomNotificationUI.Instance.ShowRoom(roomName, roomSubtitle);
                }

                // 2. Kích hoạt cốt truyện nếu có
                if (VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.OnPlayerEnterRoom(roomName);
                }

                if (triggerVillaFoyerDarkness && VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.OnPlayerEnterVillaFoyer();
                }

                if (triggerBlockedStairs && VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.OnDiscoverBlockedStairs();
                }
            }
        }

        private bool IsPlayer(Collider col)
        {
            if (col.CompareTag("Player")) return true;
            if (col.GetComponent<CharacterController>() != null) return true;
            if (col.GetComponentInParent<CharacterController>() != null) return true;
            return false;
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.25f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.8f);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
    }
}
