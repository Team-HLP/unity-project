﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VSX.Rumbles
{
    /// <summary>
    /// This Unity Event class passes a rumble event to functions on other scripts.
    /// </summary>
    [System.Serializable]
    public class OnRumbleEventHandler : UnityEvent<float> { }

    /// <summary>
    /// This class is used to store information about a single instance of a rumble.
    /// </summary>
    [System.Serializable]
    public class Rumble
    {

        public bool distanceBased = true;

        // The position of the rumble
        public Vector3 position;

        // The strength of the rumble
        public float maxLevel;

        // How long the rumble lasts
        public float duration;

        // Rumble curve
        public AnimationCurve rumbleCurve;


        // The time that the rumble began
        [HideInInspector]
        public float startTime;

        public Rumble(bool distanceBased, Vector3 position, float maxLevel, float duration, AnimationCurve rumbleCurve)
        {
            this.distanceBased = distanceBased;

            this.position = position;

            this.maxLevel = maxLevel;

            this.duration = duration;

            this.rumbleCurve = rumbleCurve;

            this.startTime = Time.time;
        }
    }


    /// <summary>
    /// This class provides a way the create 'rumbles' based on things like being hit, colliding, and boosting, which 
    /// can be used to drive camera shaking, controller vibration etc.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class RumbleManager : MonoBehaviour
    {
        [Tooltip("The transform that represents where the rumble will be felt. Most often will be the player.")]
        [SerializeField]
        protected Transform listener;

        [Tooltip("The maximum distance from where a rumble originates that it can be felt.")]
        [SerializeField]
        protected float maxEffectDistance = 1000;

        [Tooltip("A curve that represents the magnitude of the rumble based on the fraction of the maximum effect distance.")]
        [SerializeField]
        protected AnimationCurve rumbleDistanceFalloff = AnimationCurve.Linear(0, 1, 1, 0);

        // All of the rumbles currently executing.
        protected List<Rumble> rumbles = new List<Rumble>();

        // The current rumble level (max of all rumbles)
        protected float currentLevel = 0;
        public float CurrentLevel { get { return currentLevel; } }

        // The current single-frame rumble strength
        protected float currentSingleFrameRumbleStrength = 0;

        public OnRumbleEventHandler onRumble;

        // Singleton

        public static RumbleManager Instance;


        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Set the transform that represents where the rumble is being felt. Usually the player.
        /// </summary>
        /// <param name="listener">The position where the rumble is being felt.</param>
        public void SetListener(Transform listener)
        {
            this.listener = listener;
        }


        /// <summary>
        /// Add a new rumble.
        /// </summary>
        /// <param name="distanceBased">Whether the rumble is attenuated by distance.</param>
        /// <param name="position">The rumble origin world position.</param>
        /// <param name="maxLevel">The rumble strength.</param>
        /// <param name="duration">The duration of the rumble.</param>
        /// <param name="rumbleCurve">The rumble animation curve.</param>
        public void AddRumble(bool distanceBased, Vector3 position, float maxLevel, float duration, AnimationCurve rumbleCurve)
        {
            // Add a new rumble
            Rumble newRumble = new Rumble(distanceBased, position, maxLevel, duration, rumbleCurve);
            rumbles.Add(newRumble);
        }


        /// <summary>
        /// Add a new rumble for a single frame.
        /// </summary>
        /// <param name="singleRumbleLevel">The single frame rumble level.</param>
        public void AddSingleFrameRumble(float rumbleLevel, Vector3 position, bool distanceBased = true)
        {
            currentSingleFrameRumbleStrength = Mathf.Max(currentSingleFrameRumbleStrength, distanceBased ? ApplyDistanceAttenuation(rumbleLevel, position) : rumbleLevel);
        }


        /// <summary>
        /// Add a rumble that fades linearly over a specified time.
        /// </summary>
        /// <param name="distanceBased">Whether the rumble is attenuated by distance to the player.</param>
        /// <param name="position">The rumble origin world position.</param>
        /// <param name="maxLevel">The rumble strength.</param>
        /// <param name="duration">The duration of the rumble.</param>
        public virtual void AddLinearFadeRumble(bool distanceBased, Vector3 position, float maxLevel, float duration)
        {
            // Add a new rumble
            Rumble newRumble = new Rumble(distanceBased, position, maxLevel, duration, AnimationCurve.Linear(0, 1, 1, 0));
            rumbles.Add(newRumble);
        }


        // Apply distance attenuation to a rumble level.
        protected float ApplyDistanceAttenuation(float rumbleLevel, Vector3 position)
        {
            float distance = listener != null ? Vector3.Distance(listener.position, position) : 0;
            float distanceAmount = maxEffectDistance == 0 ? 0 : distance / maxEffectDistance;
            return (rumbleDistanceFalloff.Evaluate(distanceAmount) * rumbleLevel);
        }


        // Called every frame
        protected virtual void Update()
        {
           
            // Update the current rumble level (max of all rumbles).
            currentLevel = 0;
            for (int i = 0; i < rumbles.Count; ++i)
            {

                float timeSinceStart = Time.time - rumbles[i].startTime;

                // If the rumble is finished, remove it.
                if (timeSinceStart > rumbles[i].duration)
                {
                    rumbles.RemoveAt(i);
                    i--;
                    continue;
                }

                float level = rumbles[i].rumbleCurve.Evaluate(timeSinceStart / rumbles[i].duration);

                // Update the current level if this is the biggest rumble
                currentLevel = Mathf.Max(currentLevel, ApplyDistanceAttenuation(level, rumbles[i].position) * rumbles[i].maxLevel);
            }
            
            // Update the current level with the single shake level
            currentLevel = Mathf.Max(currentLevel, currentSingleFrameRumbleStrength);

            if (currentLevel > 0.00001f)
            {
                onRumble.Invoke(currentLevel);
            }

            // Reset the single shake level
            currentSingleFrameRumbleStrength = 0;

        }
    }
}
