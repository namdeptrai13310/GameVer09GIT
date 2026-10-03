using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Cây gỗ khô ngoài vườn có thể dùng Rìu chặt để lấy ván gỗ sửa sàn nhà.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChoppableTreeInteractable : MonoBehaviour, IPlayerInteractable
    {
        [Header("Tree Settings")]
        public string treeName = "Cây Gỗ Khô";
        public int chopsRequired = 3;
        public int currentChops = 0;
        public bool isChoppedDown = false;

        [Header("Plank Drop")]
        public GameObject woodPlankDropPrefab;
        public Transform dropPoint;

        [Header("Visual Effects")]
        public Transform trunkMeshTransform;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxChopWood;
        public AudioClip sfxTreeFall;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
            }

            if (trunkMeshTransform == null)
            {
                trunkMeshTransform = transform;
            }
        }

        public bool CanInteract()
        {
            if (isChoppedDown) return false;
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            return VillaSurveyAndRepairQuest.Instance.currentPhase == VillaSurveyAndRepairQuest.QuestPhase.GetAxeAndChopTrees && VillaSurveyAndRepairQuest.Instance.hasAxe;
        }

        public string GetInteractionPrompt()
        {
            if (VillaSurveyAndRepairQuest.Instance != null && VillaSurveyAndRepairQuest.Instance.hasAxe)
            {
                return $"Dùng Rìu Chặt Cây ({currentChops}/{chopsRequired})";
            }
            return "Cần Cây Rìu (Lấy trên thùng xe bán tải)";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (isChoppedDown) return;

            if (VillaSurveyAndRepairQuest.Instance == null || !VillaSurveyAndRepairQuest.Instance.hasAxe)
            {
                StoryObjectiveBanner.ShowObjective("CẦN CÂY RÌU", "Hãy ra thùng xe bán tải ngoài cổng để lấy Cây Rìu của cha!", 3.5f);
                return;
            }

            currentChops++;

            // Hoạt ảnh vung rìu bổ trên tay góc nhìn thứ nhất
            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.PlayAxeSwingAnimation();
            }

            // Âm thanh nhát rìu bổ vào thân cây
            if (sfxChopWood != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.92f, 1.08f);
                audioSource.PlayOneShot(sfxChopWood, 0.9f);
            }

            // Rung lắc nhẹ thân cây
            StartCoroutine(RoutineTreeShake());

            if (currentChops < chopsRequired)
            {
                StoryObjectiveBanner.ShowObjective("CHẶT CÂY LẤY GỖ", $"Đang chặt thân cây... ({currentChops}/{chopsRequired})", 2.0f);
            }
            else
            {
                // Cây đổ và văng ván gỗ ra
                isChoppedDown = true;
                StartCoroutine(RoutineTreeChoppedDown());
            }
        }

        private IEnumerator RoutineTreeShake()
        {
            if (trunkMeshTransform == null) yield break;

            Vector3 origPos = trunkMeshTransform.localPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float mag = (1f - (elapsed / duration)) * 0.08f;
                trunkMeshTransform.localPosition = origPos + new Vector3(
                    Random.Range(-mag, mag),
                    0f,
                    Random.Range(-mag, mag)
                );
                yield return null;
            }

            trunkMeshTransform.localPosition = origPos;
        }

        private IEnumerator RoutineTreeChoppedDown()
        {
            if (sfxTreeFall != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxTreeFall, 0.9f);
            }

            // Nghiêng đổ thân cây hoặc thu nhỏ dần
            if (trunkMeshTransform != null)
            {
                Quaternion startRot = trunkMeshTransform.localRotation;
                Quaternion fallRot = startRot * Quaternion.Euler(0f, 0f, 40f);
                float fElapsed = 0f;
                float fDur = 0.8f;
                while (fElapsed < fDur)
                {
                    fElapsed += Time.deltaTime;
                    trunkMeshTransform.localRotation = Quaternion.Slerp(startRot, fallRot, fElapsed / fDur);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.2f);

            // Rơi ván gỗ ra đất
            Vector3 spawnPos = dropPoint != null ? dropPoint.position : (transform.position + Vector3.up * 0.6f + transform.forward * 0.8f);
            if (woodPlankDropPrefab != null)
            {
                Instantiate(woodPlankDropPrefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            }

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ CHẶT ĐỔ CÂY!", 
                "Một tấm ván gỗ lớn đã rơi ra đất! Hãy nhặt tấm ván gỗ [F].", 
                4.5f
            );

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnTreeChopped();
            }
        }
    }
}
