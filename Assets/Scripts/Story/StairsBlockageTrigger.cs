using System.Collections;
using UnityEngine;

namespace HorrorGame.Story
{
    /// <summary>
    /// Kích hoạt khi người chơi đi đến chân cầu thang và THỰC SỰ NHÌN THẤY lối lên tầng 2 bị phế liệu chặn cứng.
    /// Có kiểm tra hướng nhìn (look angle) và tầm nhìn trực diện (line of sight), tránh kích hoạt sớm khi đi mép hành lang.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StairsBlockageTrigger : MonoBehaviour
    {
        [Header("Target & View Settings")]
        public Transform rubbleTarget;
        public float maxTriggerDistance = 5.2f;
        [Range(0.4f, 0.9f)]
        public float lookDotThreshold = 0.65f; // Góc nhìn ~40 độ quanh tâm nhìn camera

        private bool hasTriggered = false;
        private bool playerInside = false;
        private Transform playerCamera;

        private void Start()
        {
            if (rubbleTarget == null)
            {
                var r = GameObject.Find("Stairs_Blockage_Rubble");
                if (r != null) rubbleTarget = r.transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                playerInside = true;
                FindCamera();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                playerInside = false;
            }
        }

        private void Update()
        {
            if (hasTriggered || !playerInside) return;

            if (VillaSurveyAndRepairQuest.Instance != null && 
                VillaSurveyAndRepairQuest.Instance.currentPhase <= VillaSurveyAndRepairQuest.QuestPhase.DiscoverBlockedStairs)
            {
                if (CheckPlayerActuallySeesStairs())
                {
                    hasTriggered = true;
                    StartCoroutine(RoutineStairsBlocked());
                }
            }
        }

        private void FindCamera()
        {
            if (playerCamera != null) return;
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var cam = player.GetComponentInChildren<Camera>();
                if (cam != null) playerCamera = cam.transform;
            }
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }

        private bool CheckPlayerActuallySeesStairs()
        {
            if (rubbleTarget == null)
            {
                var r = GameObject.Find("Stairs_Blockage_Rubble");
                if (r != null) rubbleTarget = r.transform;
                else return false;
            }

            FindCamera();
            if (playerCamera == null) return false;

            Vector3 targetPos = rubbleTarget.position + Vector3.up * 1.0f;
            Vector3 camPos = playerCamera.position;
            Vector3 toTarget = targetPos - camPos;
            float dist = toTarget.magnitude;

            // Phải đứng trong khoảng cách nhìn thấy rõ đống đồ chắn
            if (dist > maxTriggerDistance) return false;

            // Camera phải nhìn về phía đống đổ nát (không kích hoạt nếu quay lưng hoặc nhìn hướng khác)
            float dot = Vector3.Dot(playerCamera.forward, toTarget.normalized);
            if (dot < lookDotThreshold) return false;

            // Kiểm tra Line of Sight: Raycast không bị tường chắn
            if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit hit, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == rubbleTarget || hit.transform.IsChildOf(rubbleTarget) || 
                    hit.transform.name.Contains("Stair") || hit.transform.name.Contains("Rubble") ||
                    hit.distance >= dist - 0.6f)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator RoutineStairsBlocked()
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "An");

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(), 
                "Cái quái gì thế này?! Đống bàn ghế với cây đàn mục nát này chặn đứng cả lối lên cầu thang rồi!", 
                5.5f
            );

            yield return new WaitForSeconds(5.7f);

            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(), 
                "Chật chội thế này không lách qua nổi... Phải dọn đống phế liệu bừa bộn ở tầng 1 đem ra bãi rác sân sau trước đã rồi tính tiếp!", 
                5.5f
            );

            yield return new WaitForSeconds(5.2f);

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.StartCleanupQuest();
            }
        }
    }
}
