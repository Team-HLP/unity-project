﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using VSX.Utilities;


namespace VSX.Engines3D
{
    /// <summary>
    /// Base class for engines vehicle engines.
    /// </summary>
    public class Engines : MonoBehaviour
    {
        // Whether the engines are on or off
        protected bool enginesActivated = false;
        public virtual bool EnginesActivated { get { return enginesActivated; } }

        public UnityEvent onEnginesActivated;

        public UnityEvent onEnginesDeactivated;


        [Tooltip("Disable input for the engines.")]
        [SerializeField]
        protected bool controlsDisabled = false;
        public bool ControlsDisabled
        {
            get { return controlsDisabled; }
            set { controlsDisabled = value; }
        }


        [Tooltip("Whether to activate the engines when the scene starts.")]
        [SerializeField]
        protected bool activateEnginesAtStart = true;
        public bool ActivateEnginesAtStart
        {
            get { return activateEnginesAtStart; }
            set { activateEnginesAtStart = value; }
        }


        [Header("Input")]


        [Tooltip("The current steering rotation input values (-1 to 1) for each axis.")]
        [SerializeField]
        protected Vector3 steeringInputs;
        public Vector3 SteeringInputs { get { return steeringInputs; } }
        public virtual Vector3 ModulatedSteeringInputs
        {
            get
            {
                Vector3 _steeringInputs = steeringInputs;
                for(int i = 0; i < steeringModulators.Count; ++i)
                {
                    _steeringInputs *= steeringModulators[i].Value();
                }

                return _steeringInputs;
            }
        }
        

        [Tooltip("The current movement input values (-1 to 1) for each axis.")]
        [SerializeField]
        protected Vector3 movementInputs;
        public Vector3 MovementInputs { get { return movementInputs; } }
        public virtual Vector3 ModulatedMovementInputs
        {
            get
            {
                Vector3 _movementInputs = movementInputs;
                for (int i = 0; i < movementModulators.Count; ++i)
                {
                    _movementInputs *= movementModulators[i].Value();
                }

                return _movementInputs;
            }
        }


        [Tooltip("The current boost input values (-1 to 1) for each axis.")]
        [SerializeField]
        protected Vector3 boostInputs;
        public Vector3 BoostInputs { get { return boostInputs; } }
        public virtual Vector3 ModulatedBoostInputs
        {
            get
            {
                Vector3 _boostInputs = boostInputs;
                for (int i = 0; i < boostModulators.Count; ++i)
                {
                    _boostInputs *= boostModulators[i].Value();
                }

                return _boostInputs;
            }
        }


        public virtual void ClearInputs()
        {
            steeringInputs = Vector3.zero;
            movementInputs = Vector3.zero;
            boostInputs = Vector3.zero;
        }


        [Header("Input Limits")]


        // The minimum translation throttle settings for each axis (i.e. the maximum input values along the negative axis)
        [Tooltip("The minimum throttle settings (>= -1) for each axis (e.g. to limit reverse speed).")]
        [SerializeField]
        protected Vector3 minMovementInputs = new Vector3(-1, -1, -0.1f);
        public Vector3 MinMovementInputs { get { return minMovementInputs; } }


        // The maximum translation throttle settings for each axis 
        [Tooltip("The maximum throttle settings for each axis (in the positive direction).")]
        [SerializeField]
        protected Vector3 maxMovementInputs = new Vector3(1f, 1f, 1f);
        public Vector3 MaxMovementInputs { get { return maxMovementInputs; } }


        [Tooltip("These values are multiplied to the steering inputs. Use them to modify steering (e.g. when vehicle is damaged).")]
        [SerializeField]
        protected Vector3 steeringInputModifiers = new Vector3(1f, 1f, 1f);
        public Vector3 SteeringInputModifiers 
        { 
            get { return steeringInputModifiers; }
            set { steeringInputModifiers = value; }
        }


        [Header("Modulators")]


        [SerializeField]
        protected List<Modulator> steeringModulators;

        [SerializeField]
        protected List<Modulator> movementModulators;

        [SerializeField]
        protected List<Modulator> boostModulators;

        public UnityAction onMovementInputsChanged;
        public UnityAction onSteeringInputsChanged;
        public UnityAction onBoostInputsChanged;



        // Called when scene starts
        protected virtual void Start()
        {
            // Turn the engine on or off at start
            if (activateEnginesAtStart)
            {
                SetEngineActivation(true);
            }
        }


        /// <summary>
        /// Turn the engine on or off.
        /// </summary>
        /// <param name="setActivated">Whether the engine should be activated or deactivated.</param>
        public virtual void SetEngineActivation(bool setActivated)
        {
            if (setActivated == enginesActivated) { return; }

            enginesActivated = setActivated;
            
            if (!enginesActivated)
            {
                steeringInputs = Vector3.zero;
                movementInputs = Vector3.zero;
            }

            if (enginesActivated)
            {
                onEnginesActivated.Invoke();
            }
            else
            {
                onEnginesDeactivated.Invoke();
            }
        }


        /// <summary>
        /// Set the movement input limits.
        /// </summary>
        /// <param name="minMovementInputs">The minimum movement inputs.</param>
        /// <param name="maxMovementInputs">The maximum movement inputs.</param>
        public virtual void SetMovementInputLimits(Vector3 minMovementInputs, Vector3 maxMovementInputs)
        {
            this.minMovementInputs = minMovementInputs;
            this.maxMovementInputs = maxMovementInputs;
        }


        /// <summary>
        /// Set the movement input values.
        /// </summary>
        /// <param name="newValuesByAxis"> New values by axis. </param>
        public virtual void SetMovementInputs(Vector3 newValuesByAxis)
        {
            if (controlsDisabled) return;

            movementInputs.x = Mathf.Clamp(newValuesByAxis.x, minMovementInputs.x, maxMovementInputs.x);
            movementInputs.y = Mathf.Clamp(newValuesByAxis.y, minMovementInputs.y, maxMovementInputs.y);
            movementInputs.z = Mathf.Clamp(newValuesByAxis.z, minMovementInputs.z, maxMovementInputs.z);
           
            if (onMovementInputsChanged != null) onMovementInputsChanged.Invoke();
        }


        /// <summary>
        /// Increase/decrease movement inputs.
        /// </summary>
        /// <param name="incrementationAmountsByAxis"> Incrementation amounts by axis. </param>
        public virtual void IncrementMovementInputs(Vector3 incrementationAmountsByAxis)
        {

            if (controlsDisabled) return;

            movementInputs += incrementationAmountsByAxis;

            // Clamp the translation throttle values
            movementInputs.x = Mathf.Clamp(movementInputs.x, minMovementInputs.x, maxMovementInputs.x);
            movementInputs.y = Mathf.Clamp(movementInputs.y, minMovementInputs.y, maxMovementInputs.y);
            movementInputs.z = Mathf.Clamp(movementInputs.z, minMovementInputs.z, maxMovementInputs.z);

            if (onMovementInputsChanged != null) onMovementInputsChanged.Invoke();
        }


        /// <summary>
        /// Set the steeringInput values.
        /// </summary>
        /// <param name="inputValuesByAxis"> New values by axis.</param>
        public virtual void SetSteeringInputs(Vector3 inputValuesByAxis)
        {

            if (controlsDisabled) return;

            // Set and clamp the rotation throttle values 
            steeringInputs.x = Mathf.Clamp(inputValuesByAxis.x, -1f, 1f);
            steeringInputs.y = Mathf.Clamp(inputValuesByAxis.y, -1f, 1f);
            steeringInputs.z = Mathf.Clamp(inputValuesByAxis.z, -1f, 1f);

            steeringInputs = Vector3.Scale(steeringInputs, steeringInputModifiers);

            if (onSteeringInputsChanged != null) onSteeringInputsChanged.Invoke();

        }


        /// <summary>
        /// Increase/decrease steering input values.
        /// </summary>
        /// <param name="incrementationAmountsByAxis">Rotation throttle incrementation amounts by axis.</param>
        public virtual void IncrementSteeringInputs(Vector3 incrementationAmountsByAxis)
        {

            if (controlsDisabled) return;

            // Update the rotation throttle values 
            steeringInputs += incrementationAmountsByAxis;

            // Clamp the rotation throttle values
            steeringInputs.x = Mathf.Clamp(steeringInputs.x, -1f, 1f);
            steeringInputs.y = Mathf.Clamp(steeringInputs.y, -1f, 1f);
            steeringInputs.z = Mathf.Clamp(steeringInputs.z, -1f, 1f);

            steeringInputs = Vector3.Scale(steeringInputs, steeringInputModifiers);

            if (onSteeringInputsChanged != null) onSteeringInputsChanged.Invoke();

        }


        /// <summary>
        /// Set the boost input values.
        /// </summary>
        /// <param name="newValues">New values by axis.</param>
        public virtual void SetBoostInputs(Vector3 newValuesByAxis)
        {

            if (controlsDisabled) return;

            // Set and clamp the boost throttle values
            boostInputs.x = Mathf.Clamp(newValuesByAxis.x, -1f, 1f);
            boostInputs.y = Mathf.Clamp(newValuesByAxis.y, -1f, 1f);
            boostInputs.z = Mathf.Clamp(newValuesByAxis.z, -1f, 1f);

            if (onBoostInputsChanged != null) onBoostInputsChanged.Invoke();

        }


        /// <summary>
        /// Clear the boost inputs.
        /// </summary>
        public virtual void ClearBoostInputs()
        {
            SetBoostInputs(Vector3.zero);
        }


        /// <summary>
        /// Clear all the rotation, translation and boost inputs.
        /// </summary>
        public virtual void ClearAllInputs()
        {
            SetMovementInputs(Vector3.zero);
            SetSteeringInputs(Vector3.zero);
            SetBoostInputs(Vector3.zero);
        }


        /// <summary>
        /// Get the default maximum speed on each axis, for example for loadout data.
        /// </summary>
        /// <param name="withBoost">Whether to include boost in the maximum speed.</param>
        /// <returns>The maximum speed on each axis.</returns>
        public virtual Vector3 GetDefaultMaxSpeedByAxis(bool withBoost)
        {
            // Override in derived classes.
            return Vector3.zero;
        }


        /// <summary>
        /// Get the current maximum speed on each axis, for example for normalizing speed indicators.
        /// </summary>
        /// <param name="withBoost">Whether to include boost in the maximum speed.</param>
        /// <returns>The current maximum speed on each axis.</returns>
        public virtual Vector3 GetCurrentMaxSpeedByAxis(bool withBoost)
        {
            // Override in derived classes.
            return Vector3.zero;
        }


        /// <summary>
        /// Get the maximum angular speed (rotation speed) of the vehicle by axis.
        /// </summary>
        /// <returns>The angular speed.</returns>
        public virtual Vector3 GetMaxAngularSpeedByAxis()
        {
            // Override in derived classes.
            return Vector3.zero;
        }
    }
}
