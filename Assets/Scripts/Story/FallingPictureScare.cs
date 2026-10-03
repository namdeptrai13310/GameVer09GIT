using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.Player;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Hiệu ứng dọa Bước 1: Khi người chơi đang dọn dẹp nhà cửa ở tầng 1,
    /// bức ảnh gia đình trên tường đột ngột rơi cái ĐÙNG, tiếng kính vỡ tan,
    /// đèn pin chớp tắt, lộ ra mẩu di thư bí mật của người cha chỉ chỗ cất Cây Rìu trên xe bán tải.
    /// </summary>
    public class FallingPictureScare : MonoBehaviour
    {
        public static FallingPictureScare Instance { get; private set; }

        [Header("Target Picture")]
        public Transform pictureTransform;
        public GameObject clueNoteObject;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxGlassCrash;

        [Header("State")]
        public bool hasTriggered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.85f;
            }

            if (pictureTransform == null)
            {
                var pt = GameObject.Find("Prop_Painting_C");
                if (pt != null) pictureTransform = pt.transform;
            }

            if (sfxGlassCrash == null)
            {
                sfxGlassCrash = GenerateGlassCrashSound();
            }
        }

        public void TriggerScare()
        {
            if (hasTriggered) return;
            hasTriggered = true;
            StartCoroutine(RoutineExecuteScare());
        }

        private IEnumerator RoutineExecuteScare()
        {
            // 1. Rung chấn và âm thanh rơi vỡ đột ngột
            if (pictureTransform != null)
            {
                var rb = pictureTransform.GetComponent<Rigidbody>();
                if (rb == null) rb = pictureTransform.gameObject.AddComponent<Rigidbody>();
                rb.mass = 3.5f;
                rb.isKinematic = false;
                rb.useGravity = true;

                // Thêm lực đẩy nhẹ vào giữa phòng khách (theo hướng tường -X) để rơi thẳng xuống sàn, không vướng lưng ghế
                Vector3 pushDir = pictureTransform.forward * 1.2f + Vector3.down * 1.8f;
                rb.AddForce(pushDir, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(5f, 15f), Random.Range(-5f, 5f), Random.Range(10f, 20f)), ForceMode.Impulse);

                var col = pictureTransform.GetComponent<Collider>();
                if (col == null)
                {
                    var mc = pictureTransform.gameObject.AddComponent<BoxCollider>();
                    mc.size = new Vector3(1.2f, 1.6f, 0.15f);
                }
            }

            if (audioSource != null && sfxGlassCrash != null)
            {
                audioSource.PlayOneShot(sfxGlassCrash, 1.0f);
            }

            // 2. Đèn pin chớp tắt hỏng hóc trong 1.5 giây
            if (PlayerFlashlight.Instance != null)
            {
                PlayerFlashlight.Instance.TriggerFlicker(1.6f, 6);
            }

            yield return new WaitForSeconds(0.6f);

            // Cố định bức tranh tiếp đất vững chắc trên sàn nhà phòng khách (Y = 11.32m), tuyệt đối không để lơ lửng trên ghế
            if (pictureTransform != null)
            {
                var rb = pictureTransform.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                Vector3 p = pictureTransform.position;
                pictureTransform.position = new Vector3(Mathf.Clamp(p.x, 81.2f, 82.1f), 11.32f, Mathf.Clamp(p.z, 16.8f, 17.8f));
                pictureTransform.rotation = Quaternion.Euler(88f, 35f, 0f);
            }

            // 3. Sinh ra / Hiển thị mẩu giấy manh mối của người cha ngay tại chỗ bức tranh rơi
            Vector3 notePos = (pictureTransform != null) 
                ? (pictureTransform.position + Vector3.up * 0.05f) 
                : new Vector3(81.8f, 11.35f, 17.4f);

            SpawnFatherAxeClueNote(notePos);

            // 4. Lời thoại giật mình của nhân vật
            string playerName = PlayerPrefs.GetString("PlayerName", "An");
            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "TRỜI ĐẤT ƠI! Cái quái gì vậy?! ... Bức ảnh gia đình đột nhiên rơi xuống sàn vỡ tan... Khoan đã, có mẩu giấy kẹp phía sau khung ảnh!",
                6.0f
            );

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("fallen_portrait",
                    "Bức ảnh trên tường phòng khách bất ngờ rơi xuống vỡ vụn. " +
                    "Phía sau khung ảnh có một mẩu giấy dặn dò của bác quản gia chỉ về chiếc xe bán tải!");
            }
        }

        private void SpawnFatherAxeClueNote(Vector3 position)
        {
            if (clueNoteObject != null)
            {
                clueNoteObject.transform.position = position;
                clueNoteObject.SetActive(true);
                return;
            }

            GameObject note = GameObject.CreatePrimitive(PrimitiveType.Quad);
            note.name = "Clue_CaretakerNote_Axe";
            note.transform.position = position + new Vector3(0.2f, 0.05f, 0.1f);
            note.transform.rotation = Quaternion.Euler(90f, 35f, 0f);
            note.transform.localScale = new Vector3(0.35f, 0.45f, 1f);

            note.AddComponent<FatherAxeNoteInteractable>();

            var col = note.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;

            clueNoteObject = note;
        }

        private AudioClip GenerateGlassCrashSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.95f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                // High frequency glass burst
                float burst = (Random.value * 2f - 1f) * Mathf.Exp(-t * 12f);
                // Glass chimes/shards ringing frequencies
                float ring1 = Mathf.Sin(2f * Mathf.PI * 3200f * (float)i / sampleRate) * Mathf.Exp(-t * 7f) * 0.4f;
                float ring2 = Mathf.Sin(2f * Mathf.PI * 4800f * (float)i / sampleRate) * Mathf.Exp(-t * 9f) * 0.35f;
                float ring3 = Mathf.Sin(2f * Mathf.PI * 6100f * (float)i / sampleRate) * Mathf.Exp(-t * 14f) * 0.25f;
                // Heavy wood frame thud impact
                float thud = Mathf.Sin(2f * Mathf.PI * 65f * (float)i / sampleRate) * Mathf.Exp(-t * 18f) * 0.8f;

                samples[i] = Mathf.Clamp((burst * 0.7f + ring1 + ring2 + ring3 + thud) * 0.95f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_GlassCrash_Impact", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>
    /// Vật phẩm tương tác mẩu giấy của bác quản gia rơi ra sau bức ảnh
    /// </summary>
    public class FatherAxeNoteInteractable : MonoBehaviour, IPlayerInteractable
    {
        private bool hasBeenRead = false;

        public bool CanInteract() => !hasBeenRead;
        public string GetInteractionPrompt() => "Đọc Mẩu Giấy Kẹp Sau Bức Ảnh";
        public string GetInteractionKey() => "F";

        public void OnInteract()
        {
            hasBeenRead = true;

            StoryObjectiveBanner.ShowObjective(
                "MẨU GIẤY CỦA BÁC QUẢN GIA",
                "'Kính gửi cậu chủ: Đồ làm vườn và cây rìu tôi để sẵn ở thùng chiếc xe bán tải ngoài cổng, phòng khi cậu cần dùng dọn dẹp...'",
                6.5f
            );

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("note_axe_in_truck",
                    "Bác quản gia đã để sẵn cây Rìu trong thùng xe bán tải ngoài cổng. Cần ra lấy để phá đống chướng ngại vật!");
            }

            if (VillaSurveyAndRepairQuest.Instance != null)
            {
                VillaSurveyAndRepairQuest.Instance.TransitionToFetchAxe();
            }

            gameObject.SetActive(false);
        }
    }
}
