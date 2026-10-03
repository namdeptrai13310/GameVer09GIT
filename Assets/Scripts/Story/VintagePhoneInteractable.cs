using System.Collections;
using UnityEngine;
using HorrorGame.InteractSystem;
using HorrorGame.Story;
using HorrorGame.UI;

namespace HorrorGame.Story
{
    /// <summary>
    /// Điện thoại bàn cổ điển: Cho phép người chơi gọi điện thoại cho Thợ điện Duy đến sửa chữa đường dây điện biệt thự.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VintagePhoneInteractable : MonoBehaviour, IPlayerInteractable
    {
        public static VintagePhoneInteractable Instance { get; private set; }

        public bool hasCalledDuy = false;
        private bool isCalling = false;

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f;
        }

        public bool CanInteract()
        {
            // Chỉ tương tác được khi công tắc đèn đã được bấm và phát hiện hỏng điện, và chưa gọi Duy
            if (VillaLightSwitch.Instance != null && !VillaLightSwitch.Instance.isPowerRepaired && !hasCalledDuy && !isCalling)
            {
                return true;
            }
            return false;
        }

        public string GetInteractionPrompt()
        {
            return "Gọi điện cho Thợ Điện Duy";
        }

        public string GetInteractionKey() => "F";

        public void OnInteract()
        {
            if (isCalling || hasCalledDuy) return;
            StartCoroutine(RoutineCallDuy());
        }

        private IEnumerator RoutineCallDuy()
        {
            isCalling = true;
            string pName = PlayerPrefs.GetString("PlayerName", "An").ToUpper();

            // 1. Tiếng chuông reo đổ máy
            PlayPhoneRingSFX();

            StoryObjectiveBanner.ShowObjective(
                pName,
                "Đang gọi cho thằng Duy thợ điện... *Tút... Tút...*",
                3.5f
            );

            yield return new WaitForSeconds(3.5f);

            // 2. Duy nhấc máy
            StoryObjectiveBanner.ShowObjective(
                "DUY THỢ ĐIỆN",
                "Alo! Em nghe đây anh! Sao thế anh, căn biệt thự cũ của gia đình bị mất điện hả?",
                4.5f
            );

            yield return new WaitForSeconds(4.5f);

            // 3. Nhân vật trả lời
            StoryObjectiveBanner.ShowObjective(
                pName,
                "Ừ Duy ơi, anh vừa tới bật công tắc mà không sáng đèn! Chắc dây điện bị đứt hay chập cầu dao rồi. Em chạy qua ngó giúp anh với!",
                5.0f
            );

            yield return new WaitForSeconds(5.0f);

            // 4. Duy đồng ý và chạy qua
            StoryObjectiveBanner.ShowObjective(
                "DUY THỢ ĐIỆN",
                "Ok anh luôn! Em đang chở đồ nghề gần khu này, 2 phút nữa em có mặt ngoài cổng sảnh trước nha anh!",
                5.0f
            );

            yield return new WaitForSeconds(4.5f);

            hasCalledDuy = true;
            isCalling = false;

            if (QuestTrackerHUD.Instance != null)
            {
                QuestTrackerHUD.Instance.UpdateTracker("CHỜ THỢ ĐIỆN DUY", "Duy đang chạy xe qua. Ra sảnh trước đón Duy [F]");
            }

            // Gọi Duy xuất hiện
            if (DuyElectricianNPC.Instance != null)
            {
                DuyElectricianNPC.Instance.SpawnDuyAtGate();
            }
        }

        private void PlayPhoneRingSFX()
        {
            if (audioSource == null) return;
            int sr = 44100;
            int len = (int)(sr * 3.0f);
            float[] samples = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sr;
                float cycle = t % 1.5f;
                if (cycle < 0.8f)
                {
                    float tone = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 480f * t) * 0.4f;
                    samples[i] = tone * 0.35f;
                }
                else
                {
                    samples[i] = 0f;
                }
            }
            AudioClip clip = AudioClip.Create("PhoneRing", len, 1, sr, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, 0.75f);
        }
    }
}
