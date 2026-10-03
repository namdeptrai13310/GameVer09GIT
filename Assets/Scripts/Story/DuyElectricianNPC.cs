using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.Story;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// NPC Thợ điện Duy: Đến sửa chữa đường dây điện và hộp cầu dao cho biệt thự theo phong cách Fear to Fathom.
    /// </summary>
    public class DuyElectricianNPC : MonoBehaviour, IPlayerInteractable
    {
        public static DuyElectricianNPC Instance { get; private set; }

        [Header("State")]
        public bool hasArrived = false;
        public bool isRepairing = false;
        public bool hasRepaired = false;
        public bool hasFinished = false;

        [Header("Waypoints & Transforms")]
        public Transform repairSpot; // Vị trí đứng sửa cầu dao
        public Transform exitSpot;

        [Header("Effects & Audio")]
        public AudioSource audioSource;
        public AudioClip sfxSparks;
        public AudioClip sfxTools;

        private Animator animator;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
            }

            animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // Ban đầu ẩn NPC cho đến khi người chơi gọi điện
            if (!hasArrived)
            {
                gameObject.SetActive(false);
            }
        }

        public void SpawnDuyAtGate()
        {
            gameObject.SetActive(true);
            hasArrived = true;

            StartCoroutine(RoutineDuyArrival());
        }

        private IEnumerator RoutineDuyArrival()
        {
            string pName = PlayerPrefs.GetString("PlayerName", "An").ToUpper();

            yield return new WaitForSeconds(1.5f);

            StoryObjectiveBanner.ShowObjective(
                "DUY THỢ ĐIỆN",
                "Em tới rồi đây anh! Em đang đứng kiểm tra hộp cầu dao tổng ở bên hông sảnh trước.",
                5.0f
            );

            if (QuestTrackerHUD.Instance != null)
            {
                QuestTrackerHUD.Instance.UpdateTracker("GẶP THỢ ĐIỆN DUY", "Ra chỗ hộp cầu dao nói chuyện với Duy thợ điện [F]");
            }
        }

        public bool CanInteract() => hasArrived && !isRepairing && !hasFinished;

        public string GetInteractionPrompt()
        {
            if (!hasRepaired) return "Nói chuyện với Duy (Nhờ sửa điện)";
            return "Cảm ơn Duy";
        }

        public string GetInteractionKey() => "F";

        public void OnInteract()
        {
            if (!hasRepaired)
            {
                StartCoroutine(RoutineRepairElectricity());
            }
        }

        private IEnumerator RoutineRepairElectricity()
        {
            isRepairing = true;

            StoryObjectiveBanner.ShowObjective(
                "DUY THỢ ĐIỆN",
                "Để em kiểm tra xem... À đây rồi! Cầu dao bị cháy cầu chì với chuột cắn đứt dây tiếp địa! Đợi em 10 giây em nối lại dây mới nhé.",
                5.0f
            );

            yield return new WaitForSeconds(2.0f);

            // Phát âm thanh tiếng đồ nghề kim loại lách cách
            PlayToolClankSFX();
            yield return new WaitForSeconds(2.5f);

            // Tiếng điện giật xẹt xẹt và chớp nháy
            PlaySparkSFX();
            yield return new WaitForSeconds(2.5f);

            PlayToolClankSFX();
            yield return new WaitForSeconds(2.0f);

            // ĐÓNG CẦU DAO - ĐIỆN SÁNG TRƯNG!
            hasRepaired = true;
            isRepairing = false;

            if (VillaLightSwitch.Instance != null)
            {
                VillaLightSwitch.Instance.isPowerRepaired = true;
                VillaLightSwitch.Instance.ToggleSwitchExplicit(true);
            }

            StoryObjectiveBanner.ShowObjective(
                "DUY THỢ ĐIỆN",
                "Xong rồi đó anh! Đèn trong nhà sáng trưng rồi nhé! Nhưng mà công nhận căn nhà này âm u ớn lạnh thật đấy... Anh ở lại cẩn thận nha, em xin phép về trước!",
                6.5f
            );

            if (QuestTrackerHUD.Instance != null)
            {
                QuestTrackerHUD.Instance.UpdateTracker("KHẢO SÁT CÁC PHÒNG TẦNG 1", "Điện đã có! Khảo sát Sảnh chính, Phòng khách và Bếp tầng 1");
            }

            yield return new WaitForSeconds(6.5f);

            hasFinished = true;
            // Duy đi về phía cổng và biến mất
            StartCoroutine(RoutineDuyLeave());
        }

        private IEnumerator RoutineDuyLeave()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * 8f - Vector3.right * 5f;

            while (elapsed < 4f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, endPos, elapsed / 4f);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private void PlayToolClankSFX()
        {
            if (audioSource == null) return;
            AudioClip clip = sfxTools != null ? sfxTools : GenerateToolSound();
            audioSource.pitch = Random.Range(0.92f, 1.08f);
            audioSource.PlayOneShot(clip, 0.8f);
        }

        private void PlaySparkSFX()
        {
            if (audioSource == null) return;
            AudioClip clip = sfxSparks != null ? sfxSparks : GenerateSparkSound();
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, 0.85f);
        }

        private AudioClip GenerateToolSound()
        {
            int sr = 44100;
            int len = (int)(sr * 0.25f);
            float[] samples = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float tone = Mathf.Sin(2f * Mathf.PI * 1800f * (float)i / sr) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 2400f * (float)i / sr) * 0.3f;
                float noise = (Random.value * 2f - 1f) * 0.2f;
                samples[i] = (tone + noise) * Mathf.Exp(-t * 14f) * 0.7f;
            }
            AudioClip clip = AudioClip.Create("ToolClank", len, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSparkSound()
        {
            int sr = 44100;
            int len = (int)(sr * 0.45f);
            float[] samples = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float buzz = Mathf.Sin(2f * Mathf.PI * 60f * (float)i / sr) * 0.4f;
                float crackle = (Random.value > 0.85f) ? (Random.value * 2f - 1f) * 0.8f : 0f;
                samples[i] = (buzz + crackle) * Mathf.Exp(-t * 5f) * 0.75f;
            }
            AudioClip clip = AudioClip.Create("ElectricSpark", len, 1, sr, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
