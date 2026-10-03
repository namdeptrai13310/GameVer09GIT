using System.Collections;
using UnityEngine;

namespace HorrorGame.Lighting
{
    public class FlickeringLight : MonoBehaviour
    {
        [Header("Light Target")]
        public Light targetLight;
        public MeshRenderer bulbRenderer;
        public int materialIndex = 0;

        [Header("Flicker Intensity")]
        public float minIntensity = 0.05f;
        public float maxIntensity = 1.2f;

        [Header("Timing")]
        public float minFlickerInterval = 0.03f;
        public float maxFlickerInterval = 0.15f;

        [Header("Blackout Event (Chớp tắt ngúm)")]
        [Range(0f, 1f)] public float blackoutChance = 0.08f;
        public float minBlackoutDuration = 0.3f;
        public float maxBlackoutDuration = 0.8f;

        [Header("Audio (Tùy chọn)")]
        public AudioSource audioSource;

        private Material dynamicMat;
        private Color originalEmission;
        private Coroutine flickerCoroutine;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>() ?? GetComponentInChildren<Light>();
            }

            if (bulbRenderer == null)
            {
                bulbRenderer = GetComponent<MeshRenderer>() ?? GetComponentInParent<MeshRenderer>();
            }

            if (bulbRenderer != null && bulbRenderer.materials.Length > materialIndex)
            {
                dynamicMat = bulbRenderer.materials[materialIndex];
                if (dynamicMat.HasProperty("_EmissionColor"))
                {
                    originalEmission = dynamicMat.GetColor("_EmissionColor");
                }
            }
        }

        private void OnEnable()
        {
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        private void OnDisable()
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (true)
            {
                // Thỉnh thoảng tắt ngấm hoàn toàn tạo cảm giác nghẹt thở
                if (Random.value < blackoutChance)
                {
                    SetLightState(0f);
                    float blackoutTime = Random.Range(minBlackoutDuration, maxBlackoutDuration);
                    yield return new WaitForSeconds(blackoutTime);
                }

                // Nhấp nháy liên hồi
                float targetIntensity = Random.Range(minIntensity, maxIntensity);
                SetLightState(targetIntensity);

                float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
                yield return new WaitForSeconds(waitTime);
            }
        }

        private void SetLightState(float intensity)
        {
            if (targetLight != null)
            {
                targetLight.intensity = intensity;
                targetLight.enabled = intensity > 0.01f;
            }

            if (dynamicMat != null && dynamicMat.HasProperty("_EmissionColor"))
            {
                float factor = Mathf.Clamp01(intensity / maxIntensity);
                dynamicMat.SetColor("_EmissionColor", originalEmission * factor);
            }
        }
    }
}
