// SoundManager.cs - Central manager for all sound operations
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SoundMod.src.sound;

namespace SoundMod.Core
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    try
                    {
                        GameObject go = new GameObject("XtraSoundManager");
                        if (go == null)
                        {
                            Debug.LogError("[XtraSounds] Failed to create GameObject");
                            return null;
                        }

                        instance = go.AddComponent<SoundManager>();
                        if (instance == null)
                        {
                            Debug.LogError("[XtraSounds] Failed to add SoundManager component");
                            Object.DestroyImmediate(go);
                            return null;
                        }

                        DontDestroyOnLoad(go);
                        Debug.Log("[XtraSounds] SoundManager created successfully");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("[XtraSounds] Exception creating SoundManager: " + ex.Message);
                        Debug.LogError("[XtraSounds] Stack trace: " + ex.StackTrace);
                        return null;
                    }
                }
                return instance;
            }
        }

        private Dictionary<SoundType, List<ISoundSource>> soundSources = new Dictionary<SoundType, List<ISoundSource>>();
        private Dictionary<string, AudioSource> audioSourcePool = new Dictionary<string, AudioSource>();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[XtraSounds] SoundManager Awake called");
            }
            else if (instance != this)
            {
                Debug.Log("[XtraSounds] Duplicate SoundManager destroyed");
                DestroyImmediate(gameObject);
            }
        }

        public void RegisterSoundSource(SoundType type, ISoundSource soundSource)
        {
            try
            {
                if (!soundSources.ContainsKey(type))
                {
                    soundSources[type] = new List<ISoundSource>();
                }

                soundSources[type].Add(soundSource);
                Debug.Log("[XtraSounds] Registered sound source: " + soundSource.DisplayName + " for " + type);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error registering sound source: " + ex.Message);
            }
        }

        public void LoadAllSounds()
        {
            try
            {
                int totalSounds = 0;
                int loadedSounds = 0;

                foreach (var category in soundSources)
                {
                    foreach (var sound in category.Value)
                    {
                        totalSounds++;
                        if (!sound.IsLoaded)
                        {
                            if (sound.LoadSound())
                            {
                                loadedSounds++;
                            }
                        }
                        else
                        {
                            loadedSounds++;
                        }
                    }
                }

                Debug.Log("[XtraSounds] Loaded " + loadedSounds + "/" + totalSounds + " sounds successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error loading sounds: " + ex.Message);
            }
        }

        public AudioClip GetRandomSound(SoundType type)
        {
            try
            {
                if (!soundSources.ContainsKey(type) || soundSources[type].Count == 0)
                    return null;

                var availableSounds = soundSources[type].Where(s => s.IsLoaded).ToList();
                if (availableSounds.Count == 0)
                    return null;

                var randomSource = availableSounds[Random.Range(0, availableSounds.Count)];
                return randomSource.GetProcessedClip();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error getting random sound: " + ex.Message);
                return null;
            }
        }

        public void PlaySound(SoundType type, Vector3 position, float volumeMultiplier = 1.0f)
        {
            try
            {
                AudioClip clip = GetRandomSound(type);
                if (clip == null) return;

                AudioSource.PlayClipAtPoint(clip, position, volumeMultiplier);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error playing sound: " + ex.Message);
            }
        }

        public int GetSoundCount(SoundType type)
        {
            try
            {
                if (!soundSources.ContainsKey(type))
                    return 0;

                return soundSources[type].Count(s => s.IsLoaded);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error getting sound count: " + ex.Message);
                return 0;
            }
        }

        public void UnloadAllSounds()
        {
            try
            {
                foreach (var category in soundSources)
                {
                    foreach (var sound in category.Value)
                    {
                        sound.UnloadSound();
                    }
                }
                Debug.Log("[XtraSounds] All sounds unloaded.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[XtraSounds] Error unloading sounds: " + ex.Message);
            }
        }
    }
}