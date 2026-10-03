using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HorrorGame.EditorTools
{
    public static class HorrorAudioGenerator
    {
        public static void GenerateAllHorrorAudio()
        {
            string dir = Path.Combine(Application.dataPath, "Audio/SFX");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            GenerateWoodenDoorCreak(Path.Combine(dir, "SFX_WoodDoor_Creak.wav"));
            GenerateWoodenDoorClose(Path.Combine(dir, "SFX_WoodDoor_Close.wav"));
            GenerateHumanBreathing(Path.Combine(dir, "SFX_Human_Heavy_Breathing.wav"));
            GenerateRoomTone(Path.Combine(dir, "SFX_RoomTone_Horror.wav"));
            GeneratePeacefulTestamentAmbience(Path.Combine(dir, "BGM_Testament_Peaceful.wav"));

            AssetDatabase.Refresh();
            Debug.Log("[HorrorAudioGenerator] All audio assets synthesized successfully!");
        }

        private static void GenerateWoodenDoorCreak(string path)
        {
            int sampleRate = 44100;
            float duration = 1.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Mô phỏng tiếng bản lề gỗ cũ cọt kẹt: dao động ma sát ngắt quãng (stick-slip friction)
            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Tần số dao động ma sát gỗ: biến thiên từ 160Hz đến 380Hz
                float baseFreq = 180f + 140f * Mathf.Sin(t * 8f) + 60f * Mathf.Sin(t * 23f);
                // Hiệu ứng stick-slip: xung ma sát ngắt quãng theo chu kỳ quay bản lề
                float stickSlip = Mathf.Pow(Mathf.Sin(2f * Mathf.PI * 18f * t), 4f);

                phase += 2f * Mathf.PI * baseFreq / sampleRate;
                float tone = Mathf.Sin(phase);
                float harmonic = Mathf.Sin(phase * 2.1f) * 0.35f + Mathf.Sin(phase * 3.05f) * 0.15f;

                // Tiếng xước thớ gỗ (wood grain friction)
                float woodFriction = (UnityEngine.Random.value * 2f - 1f) * 0.18f * stickSlip;

                // Acoustic envelope: tăng dần và thoái trào nhẹ nhàng
                float env = Mathf.Sin(progress * Mathf.PI);

                samples[i] = Mathf.Clamp((tone + harmonic + woodFriction) * env * 0.75f, -1f, 1f);
            }

            WriteWavFile(path, samples, sampleRate);
        }

        private static void GenerateWoodenDoorClose(string path)
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Mô phỏng cánh cửa gỗ đóng vào khung gỗ: tiếng "cộp" đầm ấm + tiếng lẫy chốt kim loại nhẹ
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Tiếng cộp khung gỗ (Wood frame thud) ~ 110Hz decay nhanh
                float woodThud = Mathf.Sin(2f * Mathf.PI * 115f * t) * Mathf.Exp(-t * 22f) * 0.85f;
                float subBass = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 16f) * 0.6f;

                // 2. Tiếng lẫy cửa gỗ khớp chốt (latch click) tại thời điểm 0.08s
                float latch = 0f;
                if (t >= 0.07f && t <= 0.14f)
                {
                    float lt = t - 0.07f;
                    latch = Mathf.Sin(2f * Mathf.PI * 1400f * lt) * Mathf.Exp(-lt * 80f) * 0.35f;
                }

                // 3. Tiếng dội nhẹ của cánh cửa gỗ
                float woodResonance = (UnityEngine.Random.value * 2f - 1f) * 0.08f * Mathf.Exp(-t * 28f);

                samples[i] = Mathf.Clamp(woodThud + subBass + latch + woodResonance, -1f, 1f);
            }

            WriteWavFile(path, samples, sampleRate);
        }

        private static void GenerateHumanBreathing(string path)
        {
            int sampleRate = 44100;
            float cycleDuration = 2.4f; // 1 chu kỳ hít vào - thở ra mệt mỏi
            int totalSamples = (int)(sampleRate * cycleDuration);
            float[] samples = new float[totalSamples];

            // Mô phỏng hơi thở dốc con người tự nhiên:
            // 0.0s - 0.9s: Hít vào (Inhale qua miệng/họng, tăng dần tần số ~400-900Hz)
            // 0.9s - 1.1s: Khoảng dừng ngắn
            // 1.1s - 2.3s: Thở ra mệt mỏi (Exhale hơi ấm, giảm dần tần số, êm dịu không máy móc)
            float filterState = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float rawNoise = UnityEngine.Random.value * 2f - 1f;

                float envelope = 0f;
                float cutoff = 500f;

                if (t < 0.95f) // Hít vào
                {
                    float progress = t / 0.95f;
                    envelope = Mathf.Sin(progress * Mathf.PI * 0.5f);
                    cutoff = 400f + progress * 550f; // 400Hz -> 950Hz
                }
                else if (t < 1.1f) // Khoảng nghỉ
                {
                    envelope = 0.02f;
                    cutoff = 300f;
                }
                else // Thở ra
                {
                    float progress = (t - 1.1f) / 1.3f;
                    envelope = Mathf.Pow(1f - progress, 1.8f);
                    cutoff = 750f - progress * 400f; // 750Hz -> 350Hz
                }

                // Low-pass filter một cực (one-pole filter) để loại bỏ hoàn toàn tiếng rít chói tai như máy chà
                float alpha = 2f * Mathf.PI * cutoff / sampleRate;
                alpha = Mathf.Clamp01(alpha);
                filterState += alpha * (rawNoise - filterState);

                // Thêm độ rung nhẹ của cơ vòm họng
                float throatMod = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 7.5f * t);

                samples[i] = Mathf.Clamp(filterState * envelope * throatMod * 0.55f, -1f, 1f);
            }

            WriteWavFile(path, samples, sampleRate);
        }

        private static void GenerateRoomTone(string path)
        {
            int sampleRate = 44100;
            float duration = 8.0f; // Loop 8 giây
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Mô phỏng tiếng ồn trắng tâm lý (Psychological Room Tone):
            // Tần số cực trầm (40Hz - 90Hz) tạo áp lực không gian u ám, kết hợp luồng khí nhẹ
            float filterLow = 0f;
            float filterMid = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float rawNoise = UnityEngine.Random.value * 2f - 1f;

                // Sub-bass drone trầm lắng (tạo cảm giác ngột ngạt tâm lý)
                float sub = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.15f;

                // Air flow room tone (lọc dải 80 - 250Hz)
                float alphaLow = 2f * Mathf.PI * 180f / sampleRate;
                filterLow += alphaLow * (rawNoise - filterLow);

                float alphaMid = 2f * Mathf.PI * 60f / sampleRate;
                filterMid += alphaMid * (filterLow - filterMid);

                float roomAir = (filterLow - filterMid) * 0.35f;

                // Biến thiên chậm rãi tạo nhịp thở của ngôi nhà
                float swell = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);

                samples[i] = Mathf.Clamp((sub + roomAir) * swell * 0.25f, -1f, 1f);
            }

            WriteWavFile(path, samples, sampleRate);
        }

        private static void GeneratePeacefulTestamentAmbience(string path)
        {
            int sampleRate = 44100;
            float duration = 12.0f; // Loop 12 giây
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Âm thanh nền êm dịu, trang nghiêm, ấm áp trước bi kịch:
            // Hợp âm nhẹ nhàng (D minor ấm áp: D3 = 146.8Hz, F3 = 174.6Hz, A3 = 220Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // Hợp âm phong cầm / piano điện êm dịu
                float d = Mathf.Sin(2f * Mathf.PI * 146.8f * t);
                float f = Mathf.Sin(2f * Mathf.PI * 174.6f * t);
                float a = Mathf.Sin(2f * Mathf.PI * 220.0f * t);
                float chord = (d * 0.4f + f * 0.3f + a * 0.3f);

                // Shimmer nhẹ nhàng
                float shimmer = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.08f * (0.5f + 0.5f * Mathf.Sin(t * 1.5f));

                // Fade in và Fade out mượt mà để loop không giật
                float loopWindow = Mathf.Sin(t / duration * Mathf.PI);

                samples[i] = Mathf.Clamp((chord + shimmer) * loopWindow * 0.22f, -1f, 1f);
            }

            WriteWavFile(path, samples, sampleRate);
        }

        private static void WriteWavFile(string filePath, float[] samples, int sampleRate)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                int channels = 1;
                int bitsPerSample = 16;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                int subChunk2Size = samples.Length * channels * (bitsPerSample / 8);
                int chunkSize = 36 + subChunk2Size;

                // RIFF header
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(chunkSize);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt subchunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // SubChunk1Size (PCM = 16)
                writer.Write((short)1); // AudioFormat (1 = PCM)
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write((short)(channels * (bitsPerSample / 8))); // BlockAlign
                writer.Write((short)bitsPerSample);

                // data subchunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(subChunk2Size);

                for (int i = 0; i < samples.Length; i++)
                {
                    short sampleInt = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                    writer.Write(sampleInt);
                }
            }
        }
    }
}
