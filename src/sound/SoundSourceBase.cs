using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SoundMod.src.sound
{
    public abstract class SoundSourceBase : ISoundSource
    {
        public string SoundId { get; protected set; }
        public string DisplayName { get; protected set; }
        public AudioClip AudioClip { get; protected set; }
        public float Volume { get; set; } = 1.0f;
        public float Pitch { get; set; } = 1.0f;
        public bool IsLoaded => AudioClip != null;

        protected SoundSourceBase(string soundId, string displayName)
        {
            SoundId = soundId;
            DisplayName = displayName;
        }

        public abstract bool LoadSound();

        public virtual void UnloadSound()
        {
            if (AudioClip != null)
            {
                UnityEngine.Object.DestroyImmediate(AudioClip);
                AudioClip = null;
            }
        }

        public virtual AudioClip GetProcessedClip()
        {
            return AudioClip;
        }
    }
}
