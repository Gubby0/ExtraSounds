using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SoundMod.src
{
    public static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] wavFileBytes, string clipName = "AudioClip")
        {
            if (wavFileBytes == null || wavFileBytes.Length < 44)
            {
                Debug.LogError("Invalid WAV file data");
                return null;
            }

            // Parse header
            int channels = ToInt16(wavFileBytes, 22);
            int sampleRate = ToInt32(wavFileBytes, 24);
            int bitsPerSample = ToInt16(wavFileBytes, 34);

            Debug.Log("Loading WAV: " + sampleRate + "Hz, " + channels + "ch, " + bitsPerSample + "-bit");

            // Find the data chunk
            int pos = 12;
            while (!(wavFileBytes[pos] == 'd' && wavFileBytes[pos + 1] == 'a' && wavFileBytes[pos + 2] == 't' && wavFileBytes[pos + 3] == 'a'))
            {
                pos += 4;
                int chunkSize = BitConverter.ToInt32(wavFileBytes, pos);
                pos += 4 + chunkSize;
                if (pos >= wavFileBytes.Length)
                {
                    Debug.LogError("DATA chunk not found");
                    return null;
                }
            }

            pos += 4; // Skip 'data'
            int dataSize = BitConverter.ToInt32(wavFileBytes, pos);
            pos += 4;

            // Handle different bit depths
            float[] samples = null;
            int totalSamples = 0;

            if (bitsPerSample == 16)
            {
                int sampleCount = dataSize / 2; // 16 bits = 2 bytes
                samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(wavFileBytes, pos);
                    samples[i] = sample / 32768.0f; // Convert to float -1 to 1
                    pos += 2;
                }
                totalSamples = sampleCount / channels;
            }
            else if (bitsPerSample == 24)
            {
                int sampleCount = dataSize / 3; // 24 bits = 3 bytes
                samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    // Read 24-bit sample (little endian)
                    int sample = wavFileBytes[pos] |
                               (wavFileBytes[pos + 1] << 8) |
                               (wavFileBytes[pos + 2] << 16);

                    // Sign extend if negative
                    if ((sample & 0x800000) != 0)
                        sample |= unchecked((int)0xFF000000);

                    samples[i] = sample / 8388608.0f; // Convert to float -1 to 1 (2^23)
                    pos += 3;
                }
                totalSamples = sampleCount / channels;
            }
            else if (bitsPerSample == 32)
            {
                int sampleCount = dataSize / 4; // 32 bits = 4 bytes
                samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    int sample = BitConverter.ToInt32(wavFileBytes, pos);
                    samples[i] = sample / 2147483648.0f; // Convert to float -1 to 1 (2^31)
                    pos += 4;
                }
                totalSamples = sampleCount / channels;
            }
            else if (bitsPerSample == 8)
            {
                int sampleCount = dataSize; // 8 bits = 1 byte
                samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    byte sample = wavFileBytes[pos];
                    samples[i] = (sample - 128) / 128.0f; // Convert unsigned to signed float
                    pos += 1;
                }
                totalSamples = sampleCount / channels;
            }
            else
            {
                Debug.LogError("Unsupported bit depth: " + bitsPerSample + "-bit. Supported: 8, 16, 24, 32-bit");
                return null;
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, channels, sampleRate, false);
            clip.SetData(samples, 0);

            Debug.Log("Successfully loaded WAV: " + clipName);
            return clip;
        }

        public static short ToInt16(byte[] bytes, int offset)
        {
            return (short)(bytes[offset] | (bytes[offset + 1] << 8));
        }

        public static int ToInt32(byte[] bytes, int offset)
        {
            return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
        }
    }
}