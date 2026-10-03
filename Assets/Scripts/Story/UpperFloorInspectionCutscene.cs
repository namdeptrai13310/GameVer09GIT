using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Kích hoạt khi người chơi đi lên tầng lầu và tiếp cận vị trí sàn gỗ bị mục nát.
    /// Khóa điều khiển người chơi, lia camera nhìn thẳng xuống hố sàn mục, chạy lời thoại cảm thán,
    /// hiển thị nhiệm vụ sửa sàn và trả lại quyền điều khiển cho người chơi.
    /// </summary>
    public class UpperFloorInspectionCutscene : MonoBehaviour
    {
        public static UpperFloorInspectionCutscene Instance { get; private set; }

        [Header("Target & Focus")]
        [Tooltip("Vị trí hố sàn mục để camera lia vào")]
        public Transform holeTarget;
        public Vector3 holeFocusOffset = new Vector3(0f, -0.2f, 0f);

        [Header("Dialogue Lines")]
        [TextArea(2, 4)]
        public string line1 = "Trời đất... Sàn gỗ trên lầu bị mục nát hẳn một mảng lớn thế này sao mà bước qua được?!";
        [TextArea(2, 4)]
        public string line2 = "Căn biệt thự này đúng là bỏ hoang quá lâu rồi. Nếu cứ thế bước qua là sập xuống tầng dưới ngay...";
        [TextArea(2, 4)]
        public string line3 = "Mình nhớ trên thùng xe bán tải có Hộp Dụng Cụ, và quanh nhà chắc có ván gỗ... Phải ra xe lấy đồ nghề và nhặt đủ ván gỗ về đây gia cố lại sàn mới đi tiếp được!";

        [Header("State")]
        public bool hasInspected = false;
        private bool isInspecting = false;

        private PlayerController playerController;
        private Transform playerCameraTransform;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasInspected || isInspecting) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                StartInspectionCutscene();
            }
        }

        public void StartInspectionCutscene()
        {
            if (hasInspected || isInspecting) return;
            hasInspected = true;
            isInspecting = true;
            StartCoroutine(RoutineInspectionSequence());
        }

        private IEnumerator RoutineInspectionSequence()
        {
            // 1. Tìm Player & Camera
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
                Camera cam = player.GetComponentInChildren<Camera>();
                if (cam != null) playerCameraTransform = cam.transform;
            }

            // Khóa di chuyển & xoay chuột của người chơi
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 2. Kéo vệt đen điện ảnh letterbox vào
            if (CinematicIntroManager.Instance != null)
            {
                StartCoroutine(CinematicIntroManager.Instance.AnimateLetterbox(true, 0.8f));
            }

            // 3. Lia camera nhìn thẳng xuống vị trí hố sàn mục
            Vector3 focusWorldPos = holeTarget != null ? (holeTarget.position + holeFocusOffset) : new Vector3(88.0f, 18.25f, 23.49f);

            if (playerController != null && playerCameraTransform != null)
            {
                Vector3 startPlayerForward = playerController.transform.forward;
                Quaternion startCamLocal = playerCameraTransform.localRotation;

                Vector3 toHole = (focusWorldPos - playerCameraTransform.position).normalized;
                Vector3 flatToHole = new Vector3(toHole.x, 0f, toHole.z).normalized;
                Quaternion targetBodyRot = Quaternion.LookRotation(flatToHole, Vector3.up);

                float targetPitch = -Mathf.Asin(Mathf.Clamp(toHole.y, -1f, 1f)) * Mathf.Rad2Deg;
                Quaternion targetCamLocal = Quaternion.Euler(targetPitch, 0f, 0f);

                float panDuration = 1.3f;
                float pElapsed = 0f;
                while (pElapsed < panDuration)
                {
                    pElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, pElapsed / panDuration);

                    playerController.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(startPlayerForward, Vector3.up), targetBodyRot, t);
                    playerCameraTransform.localRotation = Quaternion.Slerp(startCamLocal, targetCamLocal, t);
                    yield return null;
                }

                playerController.SetLookDirection(toHole);
            }

            yield return new WaitForSeconds(0.4f);

            // 4. Đọc các câu thoại cảm thán
            string playerName = PlayerPrefs.GetString("PlayerName", "An");
            if (VillaSurveyAndRepairQuest.Instance != null && !VillaSurveyAndRepairQuest.Instance.CanRepairFloor())
            {
                yield return StartCoroutine(DisplayDialogueLine(playerName, "Trời đất... Sàn gỗ trên lầu bị mục nát hẳn một mảng lớn thế này sao mà bước qua được?!", 3.8f));
                yield return StartCoroutine(DisplayDialogueLine(playerName, "Nếu cứ thế bước qua là sập xuống tầng dưới ngay... Nguy hiểm quá!", 3.5f));
                yield return StartCoroutine(DisplayDialogueLine(playerName, "Mình phải ra thùng xe bán tải lấy cây Rìu của cha và chặt cây khô ngoài vườn để lấy ván gỗ về đây vá sàn!", 4.5f));

                if (PlayerNotebook.Instance != null)
                {
                    PlayerNotebook.Instance.AddClue("rotten_floor", "Sàn gỗ hành lang tầng 2 bị mục nát một khoảng lớn, rất nguy hiểm. Cần ván gỗ và dụng cụ để vá lại.");
                    PlayerNotebook.Instance.AddObjective("get_wood_planks", "Thu thập ván gỗ vá sàn", "Ra xe bán tải lấy Cây Rìu của cha và chặt 3 cây gỗ khô ngoài vườn sau lấy ván.");
                }

                if (VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.UpdateObjectiveDisplay();
                    VillaSurveyAndRepairQuest.Instance.UpdateWaypointVisibility();
                }
            }
            else
            {
                yield return StartCoroutine(DisplayDialogueLine(playerName, "Đã có đủ ván gỗ và đồ nghề mang lên đây rồi!", 3.5f));
                yield return StartCoroutine(DisplayDialogueLine(playerName, "Hãy đóng đinh các tấm ván gỗ để gia cố lại sàn nhà và bước qua!", 3.8f));

                if (VillaSurveyAndRepairQuest.Instance != null)
                {
                    VillaSurveyAndRepairQuest.Instance.StartRepairQuest();
                }
            }

            // 6. Trả góc nhìn camera về thẳng phía trước
            if (playerController != null && playerCameraTransform != null)
            {
                Quaternion startCamLocal = playerCameraTransform.localRotation;
                Quaternion targetLevelLocal = Quaternion.Euler(0f, 0f, 0f);

                float returnDuration = 0.8f;
                float rElapsed = 0f;
                while (rElapsed < returnDuration)
                {
                    rElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, rElapsed / returnDuration);
                    playerCameraTransform.localRotation = Quaternion.Slerp(startCamLocal, targetLevelLocal, t);
                    yield return null;
                }

                playerController.SetLookAngles(playerController.transform.eulerAngles.y, 0f);
            }

            // 7. Thu thanh letterbox điện ảnh lại
            if (CinematicIntroManager.Instance != null)
            {
                yield return StartCoroutine(CinematicIntroManager.Instance.AnimateLetterbox(false, 0.6f));
            }

            // 8. Trả quyền điều khiển cho người chơi
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            isInspecting = false;
        }

        private IEnumerator DisplayDialogueLine(string speaker, string line, float duration)
        {
            if (CinematicIntroManager.Instance != null && CinematicIntroManager.Instance.subtitleText != null)
            {
                if (CinematicIntroManager.Instance.speakerNameText != null)
                    CinematicIntroManager.Instance.speakerNameText.text = speaker;

                yield return StartCoroutine(CinematicIntroManager.Instance.DisplaySubtitleRoutine(line, duration));
            }
            else
            {
                StoryObjectiveBanner.ShowObjective(speaker.ToUpper(), line, duration);
                yield return new WaitForSeconds(duration + 0.6f);
            }
        }
    }
}
