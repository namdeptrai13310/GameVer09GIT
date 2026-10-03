using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Story
{
    /// <summary>
    /// Quản lý âm thanh bầu không khí kinh dị tâm lý theo phong cách "Fears to Fathom":
    /// - Nhạc nền u ám, tiếng gió rít qua khe cửa, tiếng nhà cũ cọt kẹt.
    /// - Các sự kiện âm thanh bất ngờ (tiếng gõ cửa sổ, tiếng ván sàn trên lầu kêu, tiếng thở dài).
    /// - Tăng độ căng thẳng khi tiếp cận các khu vực nguy hiểm (chân cầu thang bị chặn, hố sàn mục, cửa hầm).
    /// </summary>
    public class HorrorAmbienceManager : MonoBehaviour
    {
        public static HorrorAmbienceManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource ambientDroneSource;
        public AudioSource windSource;
        public AudioSource randomCreakSource;
        public AudioSource tensionStingerSource;

        [Header("Audio Clips")]
        public AudioClip bgmAmbience;
        public AudioClip sfxWindHowl;
        public AudioClip[] sfxCreaks;
        public AudioClip[] sfxTaps;

        [Header("Settings")]
        public float baseAmbienceVolume = 0.35f;
        public float baseWindVolume = 0.25f;
        public float minEventInterval = 18f;
        public float maxEventInterval = 35f;

        private Transform playerTransform;
        private Coroutine randomEventRoutine;

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

            SetupAudioSources();
            LoadAudioClips();
        }

        private void Start()
        {
            var p = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;

            StartAmbience();
            randomEventRoutine = StartCoroutine(RoutinePsychologicalSoundEvents());
        }

        private void SetupAudioSources()
        {
            if (ambientDroneSource == null)
            {
                ambientDroneSource = gameObject.AddComponent<AudioSource>();
                ambientDroneSource.loop = true;
                ambientDroneSource.playOnAwake = false;
                ambientDroneSource.spatialBlend = 0f;
                ambientDroneSource.volume = baseAmbienceVolume;
            }

            if (windSource == null)
            {
                windSource = gameObject.AddComponent<AudioSource>();
                windSource.loop = true;
                windSource.playOnAwake = false;
                windSource.spatialBlend = 0f;
                windSource.volume = baseWindVolume;
            }

            if (randomCreakSource == null)
            {
                randomCreakSource = gameObject.AddComponent<AudioSource>();
                randomCreakSource.loop = false;
                randomCreakSource.playOnAwake = false;
                randomCreakSource.spatialBlend = 0.4f; // Semi-spatial for creepy location feeling
                randomCreakSource.volume = 0.6f;
            }

            if (tensionStingerSource == null)
            {
                tensionStingerSource = gameObject.AddComponent<AudioSource>();
                tensionStingerSource.loop = false;
                tensionStingerSource.playOnAwake = false;
                tensionStingerSource.spatialBlend = 0f;
                tensionStingerSource.volume = 0.7f;
            }
        }

        private void LoadAudioClips()
        {
#if UNITY_EDITOR
            if (bgmAmbience == null)
            {
                bgmAmbience = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/UI/TestamentMenu/Audio/BGM_Testament_Ambience.wav");
                if (bgmAmbience == null)
                    bgmAmbience = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Background.mp3");
            }

            if (sfxWindHowl == null)
            {
                sfxWindHowl = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/WindHowl.mp3");
            }

            var creakList = new List<AudioClip>();
            var c1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Creak_Drawer.wav");
            var c2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/DeepRattle.mp3");
            if (c1 != null) creakList.Add(c1);
            if (c2 != null) creakList.Add(c2);
            sfxCreaks = creakList.ToArray();

            var tapList = new List<AudioClip>();
            var t1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Flooded_Grounds/Content/Sounds/Taps.mp3");
            if (t1 != null) tapList.Add(t1);
            sfxTaps = tapList.ToArray();
#endif
        }

        public void StartAmbience()
        {
            if (ambientDroneSource != null && bgmAmbience != null)
            {
                ambientDroneSource.clip = bgmAmbience;
                ambientDroneSource.Play();
            }

            if (windSource != null && sfxWindHowl != null)
            {
                windSource.clip = sfxWindHowl;
                windSource.Play();
            }
        }

        private IEnumerator RoutinePsychologicalSoundEvents()
        {
            while (true)
            {
                float waitTime = Random.Range(minEventInterval, maxEventInterval);
                yield return new WaitForSeconds(waitTime);

                // Play a random creepy house sound
                int choice = Random.Range(0, 3);
                if (choice == 0 && sfxCreaks != null && sfxCreaks.Length > 0)
                {
                    AudioClip clip = sfxCreaks[Random.Range(0, sfxCreaks.Length)];
                    if (randomCreakSource != null && clip != null)
                    {
                        if (playerTransform != null)
                        {
                            Vector3 offset = Random.insideUnitSphere * 8f;
                            offset.y = Mathf.Abs(offset.y) + 2f; // Always above or level
                            randomCreakSource.transform.position = playerTransform.position + offset;
                        }
                        randomCreakSource.PlayOneShot(clip, Random.Range(0.4f, 0.7f));
                    }
                }
                else if (choice == 1 && sfxTaps != null && sfxTaps.Length > 0)
                {
                    AudioClip clip = sfxTaps[Random.Range(0, sfxTaps.Length)];
                    if (randomCreakSource != null && clip != null)
                    {
                        if (playerTransform != null)
                        {
                            randomCreakSource.transform.position = playerTransform.position + Random.onUnitSphere * 6f;
                        }
                        randomCreakSource.PlayOneShot(clip, 0.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Tăng cường độ âm thanh khi đến gần các điểm kinh dị then chốt
        /// </summary>
        public void TriggerTensionSpike(float duration = 4.0f)
        {
            StartCoroutine(RoutineTensionSpike(duration));
        }

        private IEnumerator RoutineTensionSpike(float duration)
        {
            if (ambientDroneSource == null) yield break;

            float originalVol = ambientDroneSource.volume;
            float targetVol = Mathf.Min(originalVol * 1.8f, 0.85f);

            float elapsed = 0f;
            float half = duration * 0.5f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                ambientDroneSource.volume = Mathf.Lerp(originalVol, targetVol, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                ambientDroneSource.volume = Mathf.Lerp(targetVol, originalVol, elapsed / half);
                yield return null;
            }

            ambientDroneSource.volume = originalVol;
        }
    }
}
