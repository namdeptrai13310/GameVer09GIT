using System.Collections;
using UnityEngine;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Hiệu ứng dọa Bước 2: Khi người chơi ở bãi rác sân sau,
    /// cửa sổ tầng 2 biệt thự bất ngờ đóng sầm lại, một bóng đen ma quái
    /// đứng nhìn trừng trừng xuống người chơi rồi lướt mất vào bóng tối.
    /// </summary>
    public class BackyardWindowScare : MonoBehaviour
    {
        public static BackyardWindowScare Instance { get; private set; }

        [Header("Window References")]
        public Transform windowTransform;
        public GameObject shadowFigureObject;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxWindGust;
        public AudioClip sfxWindowSlam;

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
                audioSource.spatialBlend = 0.95f; // 3D sound from 2nd floor
            }

            if (sfxWindowSlam == null)
            {
                sfxWindowSlam = GenerateWindowSlamSound();
            }

#if UNITY_EDITOR
            if (sfxWindGust == null)
            {
                sfxWindGust = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
            }
#endif
        }

        public void TriggerScare()
        {
            if (hasTriggered) return;
            hasTriggered = true;
            StartCoroutine(RoutineExecuteScare());
        }

        private IEnumerator RoutineExecuteScare()
        {
            // 1. Tiếng gió rít rợn người
            if (sfxWindGust != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxWindGust, 0.85f);
            }

            yield return new WaitForSeconds(0.6f);

            // 2. Vị trí cửa sổ tầng 2 quay mặt ra sân sau: khoảng (82.75, 20.80, 27.5)
            Vector3 windowPos = new Vector3(82.75f, 20.80f, 27.5f);
            transform.position = windowPos;

            // 3. Tiếng đóng sầm cửa sổ dữ dội
            if (sfxWindowSlam != null && audioSource != null)
            {
                audioSource.PlayOneShot(sfxWindowSlam, 1.0f);
            }

            // 4. Ánh sáng le lói hắt ngược từ bên trong phòng tầng 2 để làm nổi bật bóng đen
            GameObject backlightGo = new GameObject("Window_Backlight");
            backlightGo.transform.position = windowPos + new Vector3(0f, 0.5f, -1.8f);
            Light backlight = backlightGo.AddComponent<Light>();
            backlight.type = LightType.Point;
            backlight.range = 6.0f;
            backlight.color = new Color(0.95f, 0.75f, 0.45f);
            backlight.intensity = 1.6f;
            backlight.shadows = LightShadows.Hard;

            // 5. Tạo bóng đen ma quái đứng sau cửa sổ
            CreateShadowFigure(windowPos);

            // Cho người chơi đủ thời gian (3.2 giây) quay đầu nhìn lên thấy rõ bóng đen
            yield return new WaitForSeconds(3.2f);

            // 6. Bóng đen lùi dần vào bóng tối rồi biến mất, đèn phòng phụt tắt
            if (shadowFigureObject != null)
            {
                float elapsed = 0f;
                Vector3 startPos = shadowFigureObject.transform.position;
                Vector3 endPos = startPos - Vector3.forward * 2.5f;

                var ren = shadowFigureObject.GetComponent<Renderer>();
                Color startCol = ren != null ? ren.material.color : Color.black;

                while (elapsed < 1.0f)
                {
                    elapsed += Time.deltaTime;
                    float frac = elapsed / 1.0f;
                    shadowFigureObject.transform.position = Vector3.Lerp(startPos, endPos, frac);
                    if (backlight != null)
                    {
                        backlight.intensity = Mathf.Lerp(1.6f, 0f, frac);
                    }
                    if (ren != null)
                    {
                        ren.material.color = Color.Lerp(startCol, new Color(0, 0, 0, 0), frac);
                    }
                    yield return null;
                }

                Destroy(shadowFigureObject);
            }

            if (backlightGo != null)
            {
                Destroy(backlightGo);
            }

            // 7. Lời thoại hoảng hốt của nhân vật ngay sau khi tận mắt thấy bóng đen
            string playerName = PlayerPrefs.GetString("PlayerName", "An");
            StoryObjectiveBanner.ShowObjective(
                playerName.ToUpper(),
                "AI ĐÓ?! ... Vừa có bóng người trên cửa sổ tầng 2 nhìn xuống mình?! Nhưng trong nhà làm gì có ai?!",
                6.0f
            );

            if (PlayerNotebook.Instance != null)
            {
                PlayerNotebook.Instance.AddClue("window_shadow",
                    "Khi đang ở bãi rác sân sau, cửa sổ tầng 2 đóng sầm lại. " +
                    "Tôi thề là đã thấy một bóng đen đang nhìn xuống! Ngôi nhà này có điều gì đó rất bất thường...");
            }
        }

        private void CreateShadowFigure(Vector3 windowPos)
        {
            if (shadowFigureObject != null) return;

            shadowFigureObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            shadowFigureObject.name = "Backyard_ShadowFigure";
            shadowFigureObject.transform.position = windowPos + new Vector3(0f, -0.2f, -0.6f);
            shadowFigureObject.transform.localScale = new Vector3(0.55f, 1.0f, 0.35f);

            var col = shadowFigureObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var ren = shadowFigureObject.GetComponent<Renderer>();
            if (ren != null)
            {
                Material shadowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                shadowMat.color = new Color(0.02f, 0.02f, 0.03f, 0.95f);
                ren.material = shadowMat;
            }
        }

        private AudioClip GenerateWindowSlamSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.8f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float thud = Mathf.Sin(2f * Mathf.PI * 75f * (float)i / sampleRate) * Mathf.Exp(-t * 16f) * 0.9f;
                float woodImpact = (Random.value * 2f - 1f) * Mathf.Exp(-t * 28f) * 0.7f;
                float glassRattle = Mathf.Sin(2f * Mathf.PI * 1850f * (float)i / sampleRate) * Mathf.Exp(-t * 8f) * 0.35f;

                samples[i] = Mathf.Clamp(thud + woodImpact + glassRattle, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Window_Slam", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
