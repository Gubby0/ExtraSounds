/*
 * Plugin code referenced from Toaster's public github https://github.com/ckhawks/ToasterCameras/blob/main/src/Plugin.cs
 */
using UnityEngine;
using HarmonyLib;
using UnityEngine.Rendering;
using SoundMod.src.sound;
using System;
using System.Linq;
using System.Collections.Generic;
using SoundMod.Core;

namespace SoundMod
{
    public class Plugin : IPuckMod
    {

        public static string MOD_NAME = "XtraSounds";
        public static string MOD_VERSION = "1.0.0";
        public static string MOD_GUID = "com.gubby.xtrasounds";
        public static PlayerManager playerManager;
        static readonly Harmony harmony = new Harmony(MOD_GUID);

        private static bool isInitialized = false;

        public bool OnEnable()
        {
            Plugin.Log("=== XtraSounds OnEnable START ===");
            Plugin.Log("Unity Application.isPlaying: " + Application.isPlaying);
            Plugin.Log("SystemInfo.graphicsDeviceType: " + SystemInfo.graphicsDeviceType);

            LogAllPatchedMethods();

            try
            {
                Plugin.Log("Environment: client - PROCEEDING");

                Plugin.Log("Applying Harmony patches...");
                harmony.PatchAll();
                LogAllPatchedMethods();

                Plugin.Log("Initializing sound system...");
                InitializeSoundSystem();
                Plugin.Log("=== XtraSounds OnEnable SUCCESS ===");
                return true;
            }
            catch (Exception e)
            {
                Plugin.LogError("=== XtraSounds OnEnable FAILED ===");
                Plugin.LogError("Failed to Enable: " + e.Message + "!");
                Plugin.LogError("Stack trace: " + e.StackTrace);
                return false;
            }


        }

        public bool OnDisable()
        {
            try
            {
                Plugin.Log("Disabling...");
                harmony.UnpatchSelf();

                // Cleanup sound system
                if (!IsDedicatedServer() && isInitialized)
                {
                    CleanupSoundSystem();
                }

                Plugin.Log("Disabled! Goodbye!");
                return true;
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to disable: " + e.Message + "!");
                return false;
            }
        }

        private static void InitializeSoundSystem()
        {
            if (isInitialized) return;

            Plugin.Log("Initializing XtraSounds system...");

            try
            {
                // Initialize sound manager with retry logic
                SoundManager soundManager = null;
                int retries = 3;

                for (int i = 0; i < retries; i++)
                {
                    Plugin.Log("Attempting to create SoundManager (attempt " + (i + 1) + "/" + retries + ")");
                    soundManager = SoundManager.Instance;

                    if (soundManager != null)
                    {
                        Plugin.Log("SoundManager created successfully on attempt " + (i + 1));
                        break;
                    }

                    Plugin.LogError("SoundManager creation failed on attempt " + (i + 1));

                    // Wait a frame and try again
                    if (i < retries - 1)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

                if (soundManager == null)
                {
                    Plugin.LogError("Failed to create SoundManager after " + retries + " attempts!");
                    return;
                }

                // Register embedded sounds
                RegisterEmbeddedSounds();

                // Register external sounds
                RegisterExternalSounds();

                // Load all sounds
                soundManager.LoadAllSounds();

                isInitialized = true;
                Plugin.Log("XtraSounds system initialized successfully!");
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to initialize sound system: " + e.Message);
                Plugin.LogError("Stack trace: " + e.StackTrace);
            }
        }

        private static void CleanupSoundSystem()
        {
            try
            {
                Plugin.Log("Cleaning up sound system...");

                // Destroy sound manager if it exists
                if (SoundManager.Instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(SoundManager.Instance.gameObject);
                }

                isInitialized = false;
                Plugin.Log("Sound system cleaned up.");
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to cleanup sound system: " + e.Message);
            }
        }

        private static void RegisterEmbeddedSounds()
        {
            Plugin.Log("Registering embedded sounds...");

            try
            {
                var manager = SoundManager.Instance;
                if (manager == null)
                {
                    Plugin.LogError("SoundManager.Instance is null!");
                    return;
                }

                // Debug: List all embedded resources first
                var assembly = typeof(Plugin).Assembly;
                if (assembly == null)
                {
                    Plugin.LogError("Assembly is null!");
                    return;
                }

                Plugin.Log("Assembly location: " + assembly.Location);

                var resourceNames = assembly.GetManifestResourceNames();
                if (resourceNames == null)
                {
                    Plugin.LogError("ResourceNames is null!");
                    return;
                }

                Plugin.Log("Found " + resourceNames.Length + " embedded resources:");
                foreach (var resourceName in resourceNames)
                {
                    Plugin.Log(" - " + resourceName);
                }

                // Only proceed if we have embedded resources
                if (resourceNames.Length == 0)
                {
                    Plugin.Log("No embedded resources found. Skipping embedded sound registration.");
                    return;
                }

                // Register all your sounds
                Plugin.Log("Registering PuckHitBoards sounds...");
                manager.RegisterSoundSource(SoundType.PuckHitBoards,
                    new EmbeddedSoundSource("boards_hit_1", "Boards Hit 1", "SoundMod.Resources.hockey-puck-hits-board-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitBoardsSlow,
                    new EmbeddedSoundSource("boards_hit_slow_1", "Boards Hit Slow 1", "SoundMod.Resources.hockey-puck-hits-board-slow-1.wav"));

                Plugin.Log("Registering PuckHitGlass sounds...");
                manager.RegisterSoundSource(SoundType.PuckHitGlass,
                    new EmbeddedSoundSource("glass_hit_1", "Glass Hit 1", "SoundMod.Resources.hockey-puck-hits-glass-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlass,
                    new EmbeddedSoundSource("glass_hit_2", "Glass Hit 2", "SoundMod.Resources.hockey-puck-hits-glass-2.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlass,
                    new EmbeddedSoundSource("glass_hit_3", "Glass Hit 3", "SoundMod.Resources.hockey-puck-hits-glass-3.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlass,
                    new EmbeddedSoundSource("glass_hit_4", "Glass Hit 4", "SoundMod.Resources.hockey-puck-hits-glass-4.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlass,
                    new EmbeddedSoundSource("glass_hit_5", "Glass Hit 5", "SoundMod.Resources.hockey-puck-hits-glass-5.wav"));

                Plugin.Log("Registering PuckHitGlassSlow sounds...");
                manager.RegisterSoundSource(SoundType.PuckHitGlassSlow,
                    new EmbeddedSoundSource("glass_hit_slow_1", "Glass Hit Slow 1", "SoundMod.Resources.hockey-puck-hits-glass-slow-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlassSlow,
                    new EmbeddedSoundSource("glass_hit_slow_2", "Glass Hit Slow 2", "SoundMod.Resources.hockey-puck-hits-glass-slow-2.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitGlassSlow,
                    new EmbeddedSoundSource("glass_hit_slow_3", "Glass Hit Slow 3", "SoundMod.Resources.hockey-puck-hits-glass-slow-3.wav"));

                Plugin.Log("Registering PuckHitIce sounds...");
                manager.RegisterSoundSource(SoundType.PuckHitIce,
                    new EmbeddedSoundSource("puck_hit_ice_1", "Ice Hit 1", "SoundMod.Resources.hockey-puck-hits-ice-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitIce,
                    new EmbeddedSoundSource("puck_hit_ice_2", "Ice Hit 2", "SoundMod.Resources.hockey-puck-hits-ice-2.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitIce,
                    new EmbeddedSoundSource("puck_hit_ice_3", "Ice Hit 3", "SoundMod.Resources.hockey-puck-hits-ice-3.wav"));
                manager.RegisterSoundSource(SoundType.PuckHitIce,
                    new EmbeddedSoundSource("puck_hit_ice_4", "Ice Hit 4", "SoundMod.Resources.hockey-puck-hits-ice-4.wav"));

                Plugin.Log("Registering PuckShot sounds...");
                manager.RegisterSoundSource(SoundType.PuckShot,
                    new EmbeddedSoundSource("puck_shot_1", "Puck Shot 1", "SoundMod.Resources.hockey-shot-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckShot,
                    new EmbeddedSoundSource("puck_shot_2", "Puck Shot 2", "SoundMod.Resources.hockey-shot-2.wav"));

                Plugin.Log("Registering PuckStickHandling sounds...");
                manager.RegisterSoundSource(SoundType.PuckStickHandling,
                    new EmbeddedSoundSource("puck_shot_slow_1", "Puck Shot Slow 1", "SoundMod.Resources.hockey-shot-slow-1.wav"));
                manager.RegisterSoundSource(SoundType.PuckStickHandling,
                    new EmbeddedSoundSource("puck_shot_slow_2", "Puck Shot Slow 2", "SoundMod.Resources.hockey-shot-slow-2.wav"));
                manager.RegisterSoundSource(SoundType.PuckStickHandling,
                    new EmbeddedSoundSource("puck_shot_slow_3", "Puck Shot Slow 3", "SoundMod.Resources.hockey-shot-slow-3.wav"));
                manager.RegisterSoundSource(SoundType.PuckStickHandling,
                    new EmbeddedSoundSource("puck_shot_slow_4", "Puck Shot Slow 4", "SoundMod.Resources.hockey-shot-slow-4.wav"));
                manager.RegisterSoundSource(SoundType.PuckStickHandling,
                    new EmbeddedSoundSource("puck_shot_slow_5", "Puck Shot Slow 5", "SoundMod.Resources.hockey-shot-slow-5.wav"));

                Plugin.Log("Embedded sounds registered successfully.");
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to register embedded sounds: " + e.Message);
                Plugin.LogError("Stack trace: " + e.StackTrace);
            }
        }

        private static void RegisterExternalSounds()
        {
            Plugin.Log("Registering external sounds...");

            try
            {
                var manager = SoundManager.Instance;
                if (manager == null)
                {
                    Plugin.LogError("SoundManager.Instance is null in RegisterExternalSounds!");
                    return;
                }

                string modPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(modPath))
                {
                    Plugin.LogError("ModPath is null or empty!");
                    return;
                }

                string soundsPath = System.IO.Path.Combine(modPath, "Sounds");
                Plugin.Log("Looking for external sounds in: " + soundsPath);

                if (System.IO.Directory.Exists(soundsPath))
                {
                    var audioFiles = new List<string>();
                    audioFiles.AddRange(System.IO.Directory.GetFiles(soundsPath, "*.wav"));
                    audioFiles.AddRange(System.IO.Directory.GetFiles(soundsPath, "*.mp3"));

                    Plugin.Log("Found " + audioFiles.Count + " audio files in Sounds directory.");

                    foreach (var file in audioFiles)
                    {
                        Plugin.Log("Processing file: " + file);
                        // External sound processing code can go here if needed
                    }
                }
                else
                {
                    Plugin.Log("Sounds directory not found. Only embedded sounds will be used.");
                }
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to register external sounds: " + e.Message);
                Plugin.LogError("Stack trace: " + e.StackTrace);
            }
        }

        public static bool IsDedicatedServer()
        {
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        }

        public static void LogAllPatchedMethods()
        {
            var allPatchedMethods = harmony.GetPatchedMethods();
            var pluginId = harmony.Id;
            var mine = allPatchedMethods
                .Select(m => new { method = m, info = Harmony.GetPatchInfo(m) })
                .Where(x =>
                    x.info.Prefixes.Any(p => p.owner == pluginId) ||
                    x.info.Postfixes.Any(p => p.owner == pluginId) ||
                    x.info.Transpilers.Any(p => p.owner == pluginId) ||
                    x.info.Finalizers.Any(p => p.owner == pluginId)
                )
                .Select(x => x.method);
            foreach (var m in mine)
                Plugin.Log(" - " + m.DeclaringType.FullName + "." + m.Name);
        }

        public static void Log(string message)
        {
            Debug.Log("[" + MOD_NAME + "] " + message);
        }

        public static void LogError(string message)
        {
            Debug.LogError("[" + MOD_NAME + "] " + message);
        }

        // Public API for other parts of the mod to access sound functionality
        public static void PlayCustomSound(SoundType soundType, Vector3 position, float volumeMultiplier = 1.0f)
        {
            if (!isInitialized || IsDedicatedServer())
                return;

            try
            {
                SoundManager.Instance.PlaySound(soundType, position, volumeMultiplier);
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to play custom sound " + soundType + ": " + e.Message);
            }
        }

        public static AudioClip GetCustomSound(SoundType soundType)
        {
            if (!isInitialized || IsDedicatedServer())
                return null;

            try
            {
                return SoundManager.Instance.GetRandomSound(soundType);
            }
            catch (Exception e)
            {
                Plugin.LogError("Failed to get custom sound " + soundType + ": " + e.Message);
                return null;
            }
        }
    }
}