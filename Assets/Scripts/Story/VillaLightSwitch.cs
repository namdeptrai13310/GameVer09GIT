using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Công tắc đèn tường tại sảnh biệt thự 1.
    /// Ban đầu khi mới vào nhà, bấm công tắc thì điện chưa sáng liền, phát hiện đường dây bị chuột cắn/hỏng,
    /// cần gọi thợ điện Duy tới sửa. Sau khi Duy sửa xong thì điện sáng trưng.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VillaLightSwitch : MonoBehaviour, IPlayerInteractable
    {
        public static VillaLightSwitch Instance { get; private set; }

        [Header("State")]
        public bool isLightOn = false;
        public bool isPowerCut = false;
        public bool isPowerRepaired = false;
        public bool hasDiscoveredPowerFailure = false;

        [Header("Light Fixtures")]
        public List<Light> ceilingLights = new List<Light>();
        public float normalIntensity = 0.85f;
        public Color vintageAmberColor = new Color(1.0f, 0.80f, 0.45f, 1.0f);

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip sfxSwitchClick;

        private Coroutine flickerRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.9f;
            }

            if (sfxSwitchClick == null)
            {
                sfxSwitchClick = GenerateSwitchSound();
            }

            SetupCeilingLights();
        }

        private void SetupCeilingLights()
        {
            if (ceilingLights.Count > 0) return;

            var allLamps = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allLamps)
            {
                if (go.name == "Prop_Lamp_D" && go.transform.position.z > 8f && go.transform.position.z < 28f && go.transform.position.y < 20f)
                {
                    Transform bulb = go.transform.Find("Vintage_Bulb_Light");
                    Light l = null;
                    if (bulb != null)
                    {
                        l = bulb.GetComponent<Light>();
                    }
                    else
                    {
                        GameObject bulbGo = new GameObject("Vintage_Bulb_Light");
                        bulbGo.transform.SetParent(go.transform, false);
                        bulbGo.transform.localPosition = new Vector3(0f, -0.65f, 0f);
                        l = bulbGo.AddComponent<Light>();
                        l.type = LightType.Point;
                        l.range = 8.5f;
                        l.color = vintageAmberColor;
                        l.intensity = normalIntensity;
                        l.shadows = LightShadows.Soft;
                    }

                    if (l != null)
                    {
                        l.enabled = isLightOn;
                        ceilingLights.Add(l);
                    }
                }
            }
        }

        public bool CanInteract() => !isPowerCut;

        public string GetInteractionPrompt()
        {
            if (isPowerCut) return "Công Tắc Đèn (Mất Điện)";
            if (!isPowerRepaired) return "Bật Công Tắc Đèn";
            return isLightOn ? "Tắt Đèn Nhà" : "Bật Đèn Nhà";
        }

        public string GetInteractionKey() => "F";

        public void OnInteract()
        {
            if (isPowerCut) return;

            // Nếu điện chưa được thợ điện Duy sửa chữa
            if (!isPowerRepaired)
            {
                if (audioSource != null && sfxSwitchClick != null)
                {
                    audioSource.PlayOneShot(sfxSwitchClick, 0.85f);
                }

                hasDiscoveredPowerFailure = true;
                string pName = PlayerPrefs.GetString("PlayerName", "An").ToUpper();

                StoryObjectiveBanner.ShowObjective(
                    pName,
                    "Ủa? Sao bật không lên vậy nè... Chắc đường dây điện lâu ngày bị chuột cắn đứt rồi! Phải ra điện thoại bàn gọi cho thằng Duy thợ điện bảo nó chạy qua kiểm tra sửa giúp mới được.",
                    6.0f
                );

                if (QuestTrackerHUD.Instance != null)
                {
                    QuestTrackerHUD.Instance.UpdateTracker("GỌI THỢ ĐIỆN DUY", "Đến chiếc điện thoại bàn ở phòng khách gọi cho Duy thợ điện [F]");
                }
                return;
            }

            ToggleSwitch();
        }

        public void ToggleSwitch()
        {
            ToggleSwitchExplicit(!isLightOn);
        }

        public void ToggleSwitchExplicit(bool state)
        {
            isLightOn = state;

            if (audioSource != null && sfxSwitchClick != null)
            {
                audioSource.PlayOneShot(sfxSwitchClick, 0.85f);
            }

            ApplyLightState();

            if (isLightOn)
            {
                if (flickerRoutine != null) StopCoroutine(flickerRoutine);
                flickerRoutine = StartCoroutine(RoutineVintageFlicker());
            }
            else
            {
                if (flickerRoutine != null)
                {
                    StopCoroutine(flickerRoutine);
                    flickerRoutine = null;
                }
            }
        }

        public void CutPower()
        {
            isPowerCut = true;
            isLightOn = false;

            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }

            ApplyLightState();
        }

        private void ApplyLightState()
        {
            foreach (var l in ceilingLights)
            {
                if (l != null)
                {
                    l.enabled = isLightOn && !isPowerCut;
                    l.intensity = normalIntensity;
                }
            }
        }

        private IEnumerator RoutineVintageFlicker()
        {
            while (isLightOn && !isPowerCut)
            {
                yield return new WaitForSeconds(Random.Range(3.5f, 9.0f));

                if (!isLightOn || isPowerCut) yield break;

                int flickers = Random.Range(1, 4);
                for (int i = 0; i < flickers; i++)
                {
                    float dip = Random.Range(0.15f, 0.45f);
                    foreach (var l in ceilingLights)
                    {
                        if (l != null) l.intensity = dip;
                    }
                    yield return new WaitForSeconds(Random.Range(0.04f, 0.09f));

                    foreach (var l in ceilingLights)
                    {
                        if (l != null) l.intensity = normalIntensity;
                    }
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
                }
            }
        }

        private AudioClip GenerateSwitchSound()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.08f);
            float[] samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / length;
                float click = Mathf.Sin(2f * Mathf.PI * 2200f * (float)i / sampleRate) * Mathf.Exp(-t * 60f);
                float snap = Mathf.Sin(2f * Mathf.PI * 950f * (float)i / sampleRate) * Mathf.Exp(-t * 40f) * 0.6f;
                float hum = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f) * 0.25f;
                samples[i] = Mathf.Clamp((click + snap + hum) * 0.9f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("SFX_Switch_Click", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
