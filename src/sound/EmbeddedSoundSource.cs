using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace SoundMod.src.sound
{
    public class EmbeddedSoundSource : SoundSourceBase
    {
        private readonly string resourcePath;
        private readonly Assembly assembly;

        public EmbeddedSoundSource(string soundId, string displayName, string resourcePath)
            : base(soundId, displayName)
        {
            this.resourcePath = resourcePath;
            this.assembly = Assembly.GetExecutingAssembly();
        }

        public override bool LoadSound()
        {
            try
            {
                Plugin.Log("Attempting to load embedded sound: " + SoundId);
                Plugin.Log("Resource path: " + resourcePath);

                if (assembly == null)
                {
                    Plugin.LogError("Assembly is null in LoadSound!");
                    return false;
                }

                Plugin.Log("Assembly location: " + assembly.Location);

                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null)
                    {
                        Plugin.LogError("Stream is null for resource: " + resourcePath);

                        // Debug: List all available resources
                        var resourceNames = assembly.GetManifestResourceNames();
                        Plugin.Log("Available resources in assembly:");
                        foreach (var name in resourceNames)
                        {
                            Plugin.Log(" - " + name);
                        }

                        return false;
                    }

                    Plugin.Log("Stream found, length: " + stream.Length);

                    byte[] audioData = new byte[stream.Length];
                    stream.Read(audioData, 0, audioData.Length);

                    Plugin.Log("Audio data read, converting to AudioClip...");

                    AudioClip = WavUtility.ToAudioClip(audioData, SoundId);

                    if (AudioClip == null)
                    {
                        Plugin.LogError("WavUtility.ToAudioClip returned null");
                        return false;
                    }

                    Plugin.Log("Successfully loaded embedded sound: " + SoundId);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Exception in LoadSound for " + SoundId + ": " + ex.Message);
                Plugin.LogError("Stack trace: " + ex.StackTrace);
                return false;
            }
        }
    }
}