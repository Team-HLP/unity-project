﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VSX.Vehicles;
using VSX.Weapons;
using VSX.Engines3D;

namespace VSX.SpaceCombatKit
{
    
	/// <summary>
    /// This class provides an example of AI combat behavior for a spaceship.
    /// </summary>
	public class CapitalShipCombatBehaviour : AISpaceshipBehaviour 
	{

        /// <summary>
        /// This class holds data that is used by the AI to make decisions during combat.
        /// </summary>
        public class CombatDecisionInfo
        {

            public Vector3 toTargetVector;

            public Vector3 targetPosition;

            /// <summary>
            /// How much the AI is facing the target (-1 to 1, using the dot product of the forward vectors)
            /// </summary>
            public float facingTargetAmount;

            public float angleToTarget;

            /// <summary>
            /// How much the target is facing the AI (-1 to 1, using the dot product of the forward vectors)
            /// </summary>
            public float targetFacingAmount;

            public float distanceToTarget;

            /// <summary>
            /// A 0-1 value for evaluating the primary firing solution quality.
            /// </summary>
            public float primaryFiringSolutionQuality;

            /// <summary>
            /// A 0-1 value for evaluating the secondary firing solution quality.
            /// </summary>
            public float secondaryFiringSolutionQuality;

        }

        [Header("Combat Parameters")]
	
        [Tooltip("The minimum (x-value) and maximum (y-value) distance in which the AI will engage a target rather than move away.")]
		[SerializeField]
		private Vector2 minMaxEngageDistance = new Vector2(100, 500);

        [Tooltip("The minimum (x-value) and maximum (y-value) duration of firing the primary weapon before starting a pause.")]
        [SerializeField]
		private Vector2 minMaxPrimaryFiringPeriod = new Vector2(1,3);

        [Tooltip("The minimum (x-value) and maximum (y-value) duration of the pause in between firing the primary weapon.")]
        [SerializeField]
		private Vector2 minMaxPrimaryFiringInterval = new Vector2(0.5f, 2);

        [Tooltip("The maximum angle to target within which gimballed weapons will fire at the target.")]
        [SerializeField]
		private float maxFiringAngle = 15f;
	
		private float primaryWeaponActionStartTime = 0;
		private float primaryWeaponActionPeriod = 0f;
		private bool isFiringPrimary = false;

        

        [SerializeField]
        protected bool orientBroadside = true;

        [Tooltip("The distance to target below which the ship will become fully broadside-on to the target.")]
        [SerializeField]
        float broadsideDistanceToTarget = 500;

        [SerializeField]
        [Range(0, 1)]
        protected float broadsideOrbitSpeed = 0.25f;

        [SerializeField]
        [Range(0,1)]
        protected float steering = 1;

        [SerializeField]
        [Range(0, 1)]
        protected float movement = 1;

        protected CombatDecisionInfo decisionInfo;
        protected WeaponsController weapons;
        protected TriggerablesManager triggerablesManager;
        

        protected override void Awake()
		{
			decisionInfo = new CombatDecisionInfo();
		}


        protected override bool Initialize(Vehicle vehicle)
		{
            if (!base.Initialize(vehicle)) return false;

            weapons = vehicle.GetComponent<WeaponsController>();
            if (weapons == null) return false;

            triggerablesManager = vehicle.GetComponent<TriggerablesManager>();
            if (triggerablesManager == null) return false;

            engines = vehicle.GetComponent<VehicleEngines3D>();
            if (engines == null) return false;

            return (engines != null);

        }

        
        // Called when the gameobject is disabled.
		private void OnDisable()
		{
			StopAllCoroutines();
		}

        public override void StopBehaviour()
        {
            base.StopBehaviour();
            if (triggerablesManager != null)
            {
                triggerablesManager.StopTriggeringAll();
            }
        }

        // Update the data that is used to calculate decisions
        private void UpdateDecisionInfo()
		{
			
			decisionInfo.targetPosition = weapons.WeaponsTargetSelector.SelectedTarget.transform.position;
				
		
			decisionInfo.toTargetVector = decisionInfo.targetPosition - vehicle.transform.position;
	
			decisionInfo.distanceToTarget = Vector3.Distance(vehicle.transform.position, decisionInfo.targetPosition);
            decisionInfo.angleToTarget = Vector3.Angle(vehicle.transform.forward, decisionInfo.toTargetVector);
			decisionInfo.facingTargetAmount = Vector3.Dot(vehicle.transform.forward, decisionInfo.toTargetVector.normalized);

            decisionInfo.targetFacingAmount = Vector3.Dot(weapons.WeaponsTargetSelector.SelectedTarget.transform.forward, -(decisionInfo.toTargetVector).normalized);

		}
	
	
		// Get a 0-1 value that describes how good of a firing position the primary weapons are in
		float GetPrimaryFiringSolutionQuality()
		{
            float primaryFiringSolutionQuality = 1;
		
            // Zero if target out of engage range
			primaryFiringSolutionQuality *= decisionInfo.distanceToTarget < minMaxEngageDistance.y ? 1f : 0f;
            
            float angleToTarget;
            float val = 0;
            int numCalculations = 0;
            foreach (GunWeapon gunWeapon in weapons.GunWeapons)
            {
                if (gunWeapon.Gimbal == null || gunWeapon.WeaponController != null) continue;

                gunWeapon.Gimbal.TrackPosition(decisionInfo.targetPosition, out angleToTarget, false);

                if (angleToTarget < maxFiringAngle)
                {
                    val += 1;
                }

                numCalculations++;
            }
                
            if (numCalculations > 0)
            {
                val /= numCalculations;
            }
            
            primaryFiringSolutionQuality *= val;
            
            return primaryFiringSolutionQuality;
		}


		// Update whether or not the AI can fire this frame
		private void UpdateFiring()
		{
            
            float primaryFiringSolutionQuality = GetPrimaryFiringSolutionQuality();
            
            bool canFirePrimary = primaryFiringSolutionQuality > 0.5f;
            
            if (canFirePrimary)
			{ 
				
				// If weapon can fire but has not been firing, check if the cooling off period has finished before firing it again
				if (!isFiringPrimary)
				{
                    // If hasn't finished cooling off period, don't fire
                    if (Time.time - primaryWeaponActionStartTime < primaryWeaponActionPeriod)
					{
						canFirePrimary = false;
					}
					else
					{
						primaryWeaponActionStartTime = Time.time;
						primaryWeaponActionPeriod = Random.Range(minMaxPrimaryFiringPeriod.x, minMaxPrimaryFiringPeriod.y);
						isFiringPrimary = true;
					}
				}
				// If weapon can fire and has been firing, check if it has finished the firing period
				else
				{
					// If weapon has been firing long enough, stop firing
					if (Time.time - primaryWeaponActionStartTime > primaryWeaponActionPeriod)
					{
                        canFirePrimary = false;
						primaryWeaponActionStartTime = Time.time;
						primaryWeaponActionPeriod = Random.Range(minMaxPrimaryFiringInterval.x, minMaxPrimaryFiringInterval.y);
                        isFiringPrimary = false;
					}
				}
			}
            
	        if (canFirePrimary && primaryFiringSolutionQuality > 0.5f)
            {
                triggerablesManager.StartTriggeringAtIndex(0);
            }
            else
            {
                triggerablesManager.StopTriggeringAtIndex(0);
            }
		}


        /// <summary>
        /// Called every frame to update this behavor when it is running.
        /// </summary>
        public override bool BehaviourUpdate()
        {

            if (weapons.WeaponsTargetSelector == null || weapons.WeaponsTargetSelector.SelectedTarget == null) return false;

            UpdateDecisionInfo();

            UpdateFiring();

            float movementAmount;
            Vector3 steeringTarget = weapons.WeaponsTargetSelector.SelectedTarget.transform.position;

            if (orientBroadside)
            {
                Vector3 perpendicularTargetDirection = Quaternion.Euler(0f, 90f, 0f) * decisionInfo.toTargetVector;

                float broadsideAmount = Mathf.Clamp(2 - (decisionInfo.distanceToTarget / broadsideDistanceToTarget), 0, 1);

                // Update the steering target
                steeringTarget = vehicle.transform.position + (broadsideAmount * perpendicularTargetDirection + (1 - broadsideAmount) * decisionInfo.toTargetVector);

                // Used to make the ship slow down as it approaches the target
                float amountOfEngageDistance = (decisionInfo.distanceToTarget - minMaxEngageDistance.x) / (minMaxEngageDistance.y - minMaxEngageDistance.x);

                movementAmount = Mathf.Clamp(amountOfEngageDistance, broadsideOrbitSpeed, 1);
            }
            else
            {
                if (decisionInfo.distanceToTarget < minMaxEngageDistance.x)
                {
                    steeringTarget = weapons.WeaponsTargetSelector.SelectedTarget.transform.position + vehicle.transform.forward * minMaxEngageDistance.x;
                    movementAmount = 1;
                } 
                else if (decisionInfo.distanceToTarget > minMaxEngageDistance.y)
                {
                    steeringTarget = weapons.WeaponsTargetSelector.SelectedTarget.transform.position;
                    movementAmount = 1;
                }
                else
                {
                    steeringTarget = vehicle.transform.position + new Vector3(vehicle.transform.forward.x, 0, vehicle.transform.forward.z).normalized;
                    movementAmount = 0;
                }
                
            }
            
            Maneuvring.TurnToward(vehicle.transform, steeringTarget, maxRotationAngles, shipPIDController.steeringPIDController);
            engines.SetSteeringInputs(steering * shipPIDController.steeringPIDController.GetControlValues());
            engines.SetMovementInputs(movement * movementAmount * new Vector3(0f, 0f, 1));

            return true;

		}
	}
}
