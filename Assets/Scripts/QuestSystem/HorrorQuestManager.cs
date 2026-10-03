using UnityEngine;

namespace HorrorGame.QuestSystem
{
    public class HorrorQuestManager : MonoBehaviour
    {
        public static HorrorQuestManager Instance { get; private set; }

        [Header("State")]
        public bool hasFlashlight = false;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            // Gỡ bỏ hoàn toàn QuestHUD trên Canvas theo yêu cầu người dùng
            RemoveQuestUI();
        }

        public void RemoveQuestUI()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform old = canvas.transform.Find("QuestHUD");
                if (old != null)
                {
                    DestroyImmediate(old.gameObject);
                }
            }
        }

        public void BuildQuestUI()
        {
            RemoveQuestUI();
        }

        public void OnFlashlightFound()
        {
            hasFlashlight = true;
            PlayQuestChime();
        }

        private void PlayQuestChime()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.45f);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float note = (t < 0.15f) ? 587.33f : (t < 0.30f ? 739.99f : 880f);
                float decay = Mathf.Exp(-((t % 0.15f) * 16f));
                samples[i] = Mathf.Sin(2f * Mathf.PI * note * t) * decay * 0.45f;
            }
            AudioClip chime = AudioClip.Create("QuestCompleteChime", length, 1, sampleRate, false);
            chime.SetData(samples, 0);
            if (audioSource != null)
            {
                audioSource.PlayOneShot(chime, 0.6f);
            }
        }
    }
}
