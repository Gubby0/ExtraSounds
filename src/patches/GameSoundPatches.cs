// HarmonyPatches.cs - Harmony patches to override game sounds
using HarmonyLib;
using UnityEngine;
using SoundMod.src.sound;
using System.Collections;
using SoundMod.Core;

namespace SoundMod.Patches
{
    [HarmonyPatch]
    public class GameSoundPatches
    {
        // Slapshot detection variables
        private static float stickIceContactTime = 0f;
        private static bool stickOnIce = false;
        private static Vector3 lastStickPosition = Vector3.zero;
        private static float totalDragDistance = 0f;

        private static float minimumDragTime = 0.1f;      // Reduce from 0.2f 
        private static float minimumDragDistance = 0f;  // Reduce from 0.5f (was too high)
        private static float maximumTimeBetween = 1.0f;   // Increase from 0.8f
        private static float minimumShotForce = 8f;       // Reduce from 15f (you had shots with 12+ force)

        private static Vector3 lastStickVelocity = Vector3.zero;
        private static float maxStickSpeed = 0f;

        // Patch stick hitting ice
        [HarmonyPatch(typeof(StickPositioner), "OnGrounded", new System.Type[] { typeof(GameObject) })]
        [HarmonyPrefix]
        public static bool StickGroundedOverride(GameObject ground, StickPositioner __instance)
        {
            try
            {
                if (__instance.IsGrounded)
                {
                    return true;
                }

                if (ground.layer == LayerMask.NameToLayer("Ice"))
                {
                    // Start tracking stick on ice
                    stickIceContactTime = Time.time;
                    stickOnIce = true;
                    lastStickPosition = __instance.transform.position;
                    totalDragDistance = 0f;

                    Plugin.Log("Stick contacted ice - starting slapshot tracking");

                    // Start coroutine to track stick movement
                    __instance.StartCoroutine(TrackStickMovement(__instance));
                }

                return true; // Let original method handle the sound
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error in stick grounded patch: " + ex.Message);
                return true;
            }
        }

        // Coroutine to track stick movement while on ice
        private static IEnumerator TrackStickMovement(StickPositioner stick)
        {
            maxStickSpeed = 0f; // Reset max speed tracking
            Vector3 previousPosition = stick.transform.position;

            while (stickOnIce && stick.IsGrounded)
            {
                Vector3 currentPosition = stick.transform.position;
                float distance = Vector3.Distance(lastStickPosition, currentPosition);
                totalDragDistance += distance;

                // Calculate stick speed
                Vector3 velocity = (currentPosition - previousPosition) / Time.fixedDeltaTime;
                float speed = velocity.magnitude;

                // Track maximum speed during the drag
                if (speed > maxStickSpeed)
                {
                    maxStickSpeed = speed;
                }

                lastStickPosition = currentPosition;
                previousPosition = currentPosition;

                yield return new WaitForFixedUpdate();
            }

            float dragTime = Time.time - stickIceContactTime;
            Plugin.Log("Stick left ice - Drag time: " + dragTime.ToString("F2") +
                      "s, Distance: " + totalDragDistance.ToString("F2") +
                      ", Max Speed: " + maxStickSpeed.ToString("F2"));

            stickOnIce = false;
        }


        // Patch to detect when stick leaves ice (if there's a method for this)
        [HarmonyPatch(typeof(StickPositioner), "OnUngrounded")] // Replace with actual method if it exists
        [HarmonyPrefix]
        public static bool StickUngroundedOverride(StickPositioner __instance)
        {
            try
            {
                if (stickOnIce)
                {
                    float dragTime = Time.time - stickIceContactTime;
                    Plugin.Log("Stick ungrounded - Drag time: " + dragTime.ToString("F2") +
                              "s, Distance: " + totalDragDistance.ToString("F2"));
                    stickOnIce = false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error in stick ungrounded patch: " + ex.Message);
                return true;
            }
        }

        [HarmonyPatch(typeof(Puck), "OnCollisionEnter")]
        [HarmonyPrefix]
        public static bool PuckCollisionEnterOverride(Collision collision, Puck __instance)
        {
            try
            {
                Plugin.Log("PUCK OnCollisionEnter - Object: " + collision.gameObject.name +
                          ", Velocity: " + collision.relativeVelocity.magnitude.ToString("F2"));

                if (!collision.gameObject)
                {
                    return true;
                }

                string layerName = LayerMask.LayerToName(collision.gameObject.layer);
                float force = collision.relativeVelocity.magnitude; // Use velocity magnitude as force

                Plugin.Log("Collision details - Layer: " + layerName + ", Force: " + force.ToString("F2"));

                // Check if this is a stick hitting the puck (potential slapshot)
                if (collision.gameObject.name.Contains("Stick") || layerName == "Stick")
                {
                    float timeSinceIceContact = Time.time - stickIceContactTime;
                    float dragTime = stickOnIce ? timeSinceIceContact : (Time.time - stickIceContactTime);

                    Plugin.Log("Stick-Puck collision - Force: " + force.ToString("F2") +
                              ", Time since ice: " + timeSinceIceContact.ToString("F2") +
                              ", Drag time: " + dragTime.ToString("F2") +
                              ", Drag distance: " + totalDragDistance.ToString("F2"));

                    // Check if this qualifies as a slapshot
                    bool isDragTimeGood = dragTime >= minimumDragTime;
                    bool isDragDistanceGood = totalDragDistance >= minimumDragDistance;
                    bool isTimingGood = timeSinceIceContact <= maximumTimeBetween;
                    bool isForceGood = force >= minimumShotForce;

                    Plugin.Log("Slapshot check - DragTime: " + isDragTimeGood +
                              " (" + dragTime.ToString("F2") + "s >= " + minimumDragTime + "), DragDist: " + isDragDistanceGood +
                              " (" + totalDragDistance.ToString("F2") + " >= " + minimumDragDistance + "), Timing: " + isTimingGood +
                              " (" + timeSinceIceContact.ToString("F2") + " <= " + maximumTimeBetween + "), Force: " + isForceGood +
                              " (" + force.ToString("F2") + " >= " + minimumShotForce + ")");

                    if (isDragTimeGood && isDragDistanceGood && isTimingGood && isForceGood)
                    {
                        var slapshotClip = SoundManager.Instance.GetRandomSound(SoundType.PuckShot);
                        if (slapshotClip != null)
                        {
                            float forceVolume = Mathf.Clamp01(force / 30f);
                            float speedVolume = Mathf.Clamp01(maxStickSpeed / 10f);
                            float volume = (forceVolume * 0.6f) + (speedVolume * 0.4f);
                            volume = Mathf.Clamp01(volume);
                            volume = Mathf.Max(volume, 0.3f);

                            AudioSource.PlayClipAtPoint(slapshotClip, __instance.transform.position, volume);
                            Plugin.Log("SLAPSHOT detected! (force: " + force +
                                      ", max speed: " + maxStickSpeed.ToString("F2") +
                                      ", volume: " + volume.ToString("F2") + ")");

                            stickOnIce = false;
                            totalDragDistance = 0f;
                            maxStickSpeed = 0f;
                        }
                    }
                    else
                    {
                        var shotClip = SoundManager.Instance.GetRandomSound(SoundType.PuckStickHandling);
                        if (shotClip != null)
                        {
                            float volume = Mathf.Clamp01(force / 20f);
                            volume = Mathf.Max(volume, 0.2f);

                            AudioSource.PlayClipAtPoint(shotClip, __instance.transform.position, volume);
                            Plugin.Log("Regular shot detected (force: " + force + ", volume: " + volume.ToString("F2") + ")");
                        }
                    }
                }

                // Handle Goal Post
                if (layerName == "Goal Post")
                {
                    Plugin.Log("Goal post collision detected");
                    return true; // Let original handle
                }

                // Handle Ice collision
                if (layerName == "Ice")
                {
                    float minimumForce = 2f;
                    if (force < minimumForce)
                    {
                        Plugin.Log("Ice collision too light (force: " + force.ToString("F2") + "), skipping custom sound");
                        return true;
                    }

                    var customClip = SoundManager.Instance.GetRandomSound(SoundType.PuckHitIce);
                    if (customClip != null)
                    {
                        float volume = Mathf.Clamp01(force / 25f);
                        volume = Mathf.Max(volume, 0.1f);

                        AudioSource.PlayClipAtPoint(customClip, __instance.transform.position, volume);
                        Plugin.Log("Played custom ice sound (force: " + force + ", volume: " + volume.ToString("F2") + ")");
                    }
                }

                // Handle Boards collision
                if (layerName == "Boards")
                {
                    float minimumForce = 1f;
                    if (force < minimumForce)
                    {
                        Plugin.Log("Boards collision too light (force: " + force.ToString("F2") + "), skipping custom sound");
                        return true;
                    }

                    float collisionY = collision.gameObject.transform.position.y;
                    float puckY = __instance.transform.position.y;
                    float glassHeightThreshold = 2f;
                    bool isGlass = puckY > glassHeightThreshold;

                    Plugin.Log("Boards collision - Y: " + collisionY.ToString("F2") +
                              ", Puck Y: " + puckY.ToString("F2") +
                              ", Force: " + force.ToString("F2") +
                              ", IsGlass: " + isGlass);

                    if (isGlass)
                    {
                        float slowHitThreshold = 8f;
                        bool isSlowHit = force < slowHitThreshold;
                        var soundType = isSlowHit ? SoundType.PuckHitGlassSlow : SoundType.PuckHitGlass;
                        var customClip = SoundManager.Instance.GetRandomSound(soundType);

                        if (customClip != null)
                        {
                            float volume = Mathf.Clamp01(force / 15f);
                            volume = Mathf.Max(volume, 0.1f);

                            AudioSource.PlayClipAtPoint(customClip, __instance.transform.position, volume);
                            Plugin.Log("Played custom glass sound (" + (isSlowHit ? "slow" : "fast") +
                                      ") (force: " + force + ", volume: " + volume.ToString("F2") + ")");
                        }
                    }
                    else
                    {
                        float slowHitThreshold = 10f;
                        bool isSlowHit = force < slowHitThreshold;
                        var soundType = isSlowHit ? SoundType.PuckHitBoardsSlow : SoundType.PuckHitBoards;
                        var customClip = SoundManager.Instance.GetRandomSound(soundType);

                        if (customClip != null)
                        {
                            float volume = Mathf.Clamp01(force / 20f);
                            volume = Mathf.Max(volume, 0.15f);

                            AudioSource.PlayClipAtPoint(customClip, __instance.transform.position, volume);
                            Plugin.Log("Played custom boards sound (" + (isSlowHit ? "slow" : "fast") +
                                      ") (force: " + force + ", volume: " + volume.ToString("F2") + ")");
                        }
                    }
                }

                return true; // Always let original method continue
            }
            catch (System.Exception ex)
            {
                Plugin.LogError("Error in puck collision enter patch: " + ex.Message);
                return true;
            }
        }
    }
}