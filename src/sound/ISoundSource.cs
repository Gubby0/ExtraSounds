using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SoundMod.src.sound
{
    public interface ISoundSource
    {
        string SoundId { get; }
        string DisplayName { get; }
        AudioClip AudioClip { get; }
        float Volume { get; set; }
        float Pitch { get; set; }
        bool IsLoaded { get; }

        bool LoadSound();
        void UnloadSound();
        AudioClip GetProcessedClip(); // For any audio processing
    }
}
