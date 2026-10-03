using System.Collections;
using UnityEngine;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Hiệu ứng dọa Bước 2 kết thúc: Chiếc đài radio cũ trên tầng 2 tự bật,
    /// phát ra sóng nhiễu rè rè và đoạn băng ghi âm méo mó đầy kinh hãi của người cha,
    /// tiết lộ bí mật về con quái vật dưới hầm và chỉ dẫn sang Biệt thự số 2 tìm chìa khóa.
    /// </summary>
    public class HauntedRadioScare : MonoBehaviour
    {
        public static HauntedRadioScare Instance { get; private set; }

        [Header("Radio Components")]
        public AudioSource audioSource;
        public Light radioIndicatorLight;
        public AudioClip sfxRadioStatic;

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

            if (radioIndicatorLight == null)
            {
                radioIndicatorLight = GetComponentInChildren<Light>();
                if (radioIndicatorLight == null)
                {
                    GameObject lightObj = new GameObject("Radio_RedLight");
                    lightObj.transform.SetParent(transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                    radioIndicatorLight = lightObj.AddComponent<Light>();
                    radioIndicatorLight.type = LightType.Point;
                    radioIndicatorLight.color = Color.red;
                    radioIndicatorLight.range = 3.5f;
                    radioIndicatorLight.intensity = 1.8f;
                }
            }

            if (radioIndicatorLight != null) radioIndicatorLight.enabled = false;

            if (sfxRadioStatic == null)
            {
                sfxRadioStatic = GenerateRadioStaticSound();
            }
        }

        public void TriggerScare()
        {
            if (hasTriggered) return;
            hasTriggered = true;
            StartCoroutine(RoutineExecuteRadioBroadcast());
        }

        private IEnumerator RoutineExecuteRadioBroadcast()
        {
            yield return new WaitForSeconds(0.8f);

            // 1. Đài bật sáng đèn đỏ ma quái
            if (radioIndicatorLight != null) radioIndicatorLight.enabled = true;

            // 2. Tiếng nhiễu sóng rè rè
            if (audioSource != null && sfxRadioStatic != null)
            {
                audioSource.clip = sfxRadioStatic;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return new WaitForSeconds(1.0f);

            // 3. Chuỗi lời thoại / thông điệp kinh hoàng của người cha
            StoryObjectiveBanner.ShowObjective(
                "ĐÀI RADIO CŨ TỰ BẬT!",
                "[RÈ RÈ... TÍCH TẮC...] 'An à... nếu con nghe được đoạn băng này... thì cha đã không còn nữa...'",
                5.0f
            );

            yield return new WaitForSeconds(5.2f);

            StoryObjectiveBanner.ShowObjective(
                "GIỌNG NÓI CỦA CHA TRÊN ĐÀI",
                "'Thứ bị nhốt dưới tầng hầm... nó sắp thức tỉnh rồi... Nắp hầm đã bị phong ấn nhiều lớp khóa...'",
                5.0f
            );

            yield return new WaitForSeconds(5.2f);

            StoryObjectiveBanner.ShowObjective(
                "GIỌNG NÓI CỦA CHA TRÊN ĐÀI",
                "'Chìa khóa mở tầng hầm... cha đã giấu ở BIỆT THỰ SỐ 2... Hãy cẩn thận... ĐỪNG ĐỂ NÓ BẮT ĐƯỢC CON!'",
                6.0f
            );

            yield return new WaitForSeconds(6.2f);

            // 4. Tiếng nổ bụp của bóng đèn radio, đài tắt phụt
            if (radioIndicatorLight != null) radioIndicatorLight.enabled = false;
            if (audioSource != null) audioSource.Stop();

            // 5. Cập nhật Sổ tay và Chuyển giai đoạn sang Biệt thự 2
            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("radio_father_message",
                    "Đài radio cũ phát đoạn ghi âm của cha: Ngôi nhà bị nguyền rủa! " +
                    "Chìa khóa mở nắp hầm bí mật đã được giấu ở BIỆT THỰ SỐ 2. Phải sang đó tìm!");

                PlayerNotebook.Instance.AddObjective("explore_villa_2",
                    "Khám phá Biệt thự số 2",
                    "Rời Biệt thự 1, đi sang Biệt thự số 2 để tìm Chìa Khóa Tầng Hầm.");
            }

            // Kích hoạt Waypoint chỉ sang Biệt thự 2
            var quest = VillaSurveyAndRepairQuest.Instance;
            if (quest != null)
            {
                quest.TransitionToVilla2Search();
            }
        }

        private AudioClip GenerateRadioStaticSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 2.5f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float noise = (Random.value * 2f - 1f) * 0.45f;
                // High frequency heterodyne whistle
                float whine = Mathf.Sin(2f * Mathf.PI * (1200f + Mathf.Sin(t * 15f) * 400f) * (float)i / sampleRate) * 0.15f;
                // 50Hz hum
                float hum = Mathf.Sin(2f * Mathf.PI * 50f * (float)i / sampleRate) * 0.25f;

                samples[i] = Mathf.Clamp(noise + whine + hum, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Radio_Static_Loop", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
