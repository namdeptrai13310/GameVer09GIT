using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;

namespace HorrorGame.Story
{
    /// <summary>
    /// Cánh cửa hầm bí mật (Cellar Trapdoor) nằm ở gian nhà kho/bếp phía sau biệt thự.
    /// Dẫn thẳng xuống khu hầm ngầm cổ xưa bên dưới căn nhà.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CellarTrapdoorInteractable : MonoBehaviour, IPlayerInteractable
    {
        [Header("State")]
        public bool isUnlocked = false;

        [Header("Destination")]
        public Vector3 basementLandingPos = new Vector3(85.8f, 1.5f, -92.5f);
        public float destinationYRotation = 180f;

        [Header("Visual")]
        public Transform trapdoorLidTransform;
        public Vector3 openRotation = new Vector3(-85f, 0f, 0f);

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxUnlock;
        public AudioClip sfxHeavyCreak;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
            }
        }

        public bool CanInteract()
        {
            if (VillaSurveyAndRepairQuest.Instance == null) return false;

            // Chỉ tương tác khi đã sửa xong sàn tầng 2 hoặc đang tìm lối xuống hầm
            return VillaSurveyAndRepairQuest.Instance.isFloorRepaired;
        }

        public string GetInteractionPrompt()
        {
            if (isUnlocked)
            {
                return "Bước Xuống Tầng Hầm";
            }

            if (VillaSurveyAndRepairQuest.Instance != null && VillaSurveyAndRepairQuest.Instance.hasBasementKey)
            {
                return "Dùng Chìa Khóa Mở Cửa Hầm";
            }

            return "Cửa Hầm Bị Khóa (Cần Chìa Khóa Trên Tầng 2)";
        }

        public string GetInteractionKey()
        {
            return "F";
        }

        public void OnInteract()
        {
            if (VillaSurveyAndRepairQuest.Instance == null) return;

            if (!isUnlocked)
            {
                if (!VillaSurveyAndRepairQuest.Instance.hasBasementKey)
                {
                    StoryObjectiveBanner.ShowObjective(
                        "CỬA HẦM BỊ KHÓA SẮT", 
                        "Cửa hầm bí mật đã bị xích chặt bằng ổ khóa sắt cũ. Hãy lên tầng 2 tìm Chìa Khóa!", 
                        4.5f
                    );
                    return;
                }

                // Mở khóa
                isUnlocked = true;
                if (sfxUnlock != null && audioSource != null)
                {
                    audioSource.PlayOneShot(sfxUnlock);
                }

                StartCoroutine(RoutineOpenTrapdoorAndDescend());
            }
            else
            {
                // Đã mở rồi, chỉ việc bước xuống
                StartCoroutine(RoutineDescendImmediate());
            }
        }

        private IEnumerator RoutineOpenTrapdoorAndDescend()
        {
            if (sfxHeavyCreak != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxHeavyCreak, 0.95f);
            }

            // Xoay nắp hầm mở lên
            if (trapdoorLidTransform != null)
            {
                Quaternion startRot = trapdoorLidTransform.localRotation;
                Quaternion endRot = Quaternion.Euler(openRotation);
                float elapsed = 0f;
                float dur = 1.4f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    trapdoorLidTransform.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / dur);
                    yield return null;
                }
                trapdoorLidTransform.localRotation = endRot;
            }

            yield return new WaitForSeconds(0.4f);

            StoryObjectiveBanner.ShowObjective(
                "ĐÃ MỞ CỬA HẦM BÍ MẬT!",
                "Làn gió lạnh buốt từ dưới lòng đất ùa lên... Đang bước xuống tầng hầm...",
                3.5f
            );

            yield return StartCoroutine(RoutineDescendImmediate());
        }

        private IEnumerator RoutineDescendImmediate()
        {
            var player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = basementLandingPos;
                player.transform.rotation = Quaternion.Euler(0f, destinationYRotation, 0f);

                yield return new WaitForSeconds(0.05f);
                if (cc != null) cc.enabled = true;
            }

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.OnEnteredBasement();
            }
        }
    }
}
