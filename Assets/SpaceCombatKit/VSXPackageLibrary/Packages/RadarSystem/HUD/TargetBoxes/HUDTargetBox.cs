﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VSX.Utilities;
using VSX.UI;

namespace VSX.RadarSystem
{ 
    /// <summary>
    /// Unity event for running functions when a target box is updated.
    /// </summary>
    [System.Serializable]
    public class OnTargetBoxUpdatedEventHandler : UnityEvent<Trackable> { } 


    /// <summary>
    /// Base class for controlling a single target box on the HUD.
    /// </summary>
    public class HUDTargetBox : MonoBehaviour
    {

        [Header("References")]

        [SerializeField]
        protected RectTransform rectTransform;
        public RectTransform RectTransform { get { return rectTransform; } }

        [SerializeField]
        protected HUDTargetBox_LocksController locksController;

        [SerializeField]
        protected HUDTargetBox_LeadTargetBoxesController leadTargetBoxesController;

        [SerializeField]
        protected UVCText distanceText;

        [SerializeField]
        protected UIColorManager targetBoxColorManager;

        [Header("UI Objects")]

        [SerializeField]
        protected List<GameObject> onScreenUIObjects = new List<GameObject>();

        [SerializeField]
        protected List<GameObject> offScreenUIObjects = new List<GameObject>();

        [SerializeField]
        protected List<GameObject> selectedUIObjects = new List<GameObject>();

        [SerializeField]
        protected List<GameObject> unselectedUIObjects = new List<GameObject>();

        [SerializeField]
        protected List<GameObject> highlightedUIObjects = new List<GameObject>();

        [SerializeField]
        protected List<GameObject> unhighlightedUIObjects = new List<GameObject>();

        [Header("Settings")]

        [SerializeField]
        protected bool resizeToTarget = true;

        [SerializeField]
        protected bool maintainAspect = false;

        [SerializeField]
        protected float aspect = 1;

        [SerializeField]
        protected Vector2 minSize = new Vector2(30, 30);

        [SerializeField]
        protected Vector2 sizeMargin = new Vector2(15, 15);
        
        [Header("Events")]

        // Target box updated event
        public OnTargetBoxUpdatedEventHandler onTargetBoxUpdated;

        public UnityEvent onShownOnScreen;
        public UnityEvent onShownOffScreen;

        public UnityEvent onSelected;
        public UnityEvent onNotSelected;

        public UnityEvent onHighlighted;
        public UnityEvent onNotHighlighted;

        public UnityAction<float> onDistanceSet;



        protected virtual void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
           
            // Update the linked texts when the target box is updated
            LinkedUIText[] linkedTexts = GetComponentsInChildren<LinkedUIText>();
            foreach(LinkedUIText linkedText in linkedTexts)
            {
                onTargetBoxUpdated.AddListener(linkedText.Set);
            }

            // Update the linked bars every time the target box is updated 
            LinkedUIBar[] bars = GetComponentsInChildren<LinkedUIBar>();
            
            foreach (LinkedUIBar bar in bars)
            {
                onTargetBoxUpdated.AddListener(bar.Set);
            }
        }

        public virtual void SetActivation(bool isActivated)
        {

        }

        /// <summary>
        /// Called before this target box is updated (not necessarily before any other target box is updated).
        /// </summary>
        public virtual void PreTargetBoxUpdate() { }


        /// <summary>
        /// Called after this target box is updated (not necessarily after any other target box is updated).
        /// </summary>
        public virtual void PostTargetBoxUpdate() { }


        /// <summary>
        /// Set whether the target is selected.
        /// </summary>
        /// <param name="isSelected">Whether the target is selected.</param>
        public virtual void SetIsSelectedTarget(bool isSelected)
        {
            for (int i = 0; i < selectedUIObjects.Count; ++i)
            {
                selectedUIObjects[i].SetActive(isSelected);
            }

            for (int i = 0; i < unselectedUIObjects.Count; ++i)
            {
                unselectedUIObjects[i].SetActive(!isSelected);
            }

            if (isSelected)
            {
                onSelected.Invoke();
            }
            else
            {
                onNotSelected.Invoke();
            } 
        }


        /// <summary>
        /// Set whether the target box is in view or off screen.
        /// </summary>
        /// <param name="isInView">Whether the target box is in view.</param>
        public virtual void SetIsInView(bool isInView)
        {
            for (int i = 0; i < onScreenUIObjects.Count; ++i)
            {
                onScreenUIObjects[i].SetActive(isInView);
            }

            for (int i = 0; i < offScreenUIObjects.Count; ++i)
            {
                offScreenUIObjects[i].SetActive(!isInView);
            }

            if (isInView)
            {
                onShownOnScreen.Invoke();
            }
            else
            {
                onShownOffScreen.Invoke();
            }
        }


        /// <summary>
        /// Set whether the target box is highlighted or not.
        /// </summary>
        /// <param name="highlighted">Whether the target box is highlighted.</param>
        public virtual void SetHighlighted(bool highlighted)
        {
            for (int i = 0; i < highlightedUIObjects.Count; ++i)
            {
                highlightedUIObjects[i].SetActive(highlighted);
            }

            for (int i = 0; i < unhighlightedUIObjects.Count; ++i)
            {
                unhighlightedUIObjects[i].SetActive(!highlighted);
            }


            if (highlighted)
            {
                onHighlighted.Invoke();
            }
            else
            {
                onNotHighlighted.Invoke();
            }
        }


        /// <summary>
        /// Set the color for the target box.
        /// </summary>
        /// <param name="newColor">The new color.</param>
        public virtual void SetColor(Color newColor)
        {
            if (targetBoxColorManager != null) targetBoxColorManager.SetColor(newColor);
        }



        /// <summary>
        /// Set the size of the target box.
        /// </summary>
        /// <param name="size">The size of the target box.</param>
        public virtual void SetSize(Vector2 size)
        {
            
            if (resizeToTarget)
            {
                size = new Vector2(Mathf.Max(size.x, minSize.x), Mathf.Max(size.y, minSize.y)) + sizeMargin;
                if (maintainAspect)
                {
                    float maxDim = Mathf.Max(size.x, size.y);
                    size = new Vector2(maxDim * aspect, maxDim);
                }

                // Update the size of the target box
                rectTransform.sizeDelta = size;
            }
        }


        /// <summary>
        /// Set the distance to the target on the target box.
        /// </summary>
        /// <param name="distance">The distance to the target.</param>
        public virtual void SetDistance(float distance)
        {
            if (distanceText != null)
            {
                if (DistanceStringLookup.Instance != null)
                {
                    distanceText.text = DistanceStringLookup.Instance.Lookup(distance);
                }
                else
                {
                    distanceText.text = ((int)distance).ToString() + " M";
                }
            }

            onDistanceSet?.Invoke(distance);
        }


        /// <summary>
        /// Get a lead target box.
        /// </summary>
        /// <returns>The lead target box controller.</returns>
        public virtual HUDTargetBox_LeadTargetBoxController GetLeadTargetBox()
        {
            if (leadTargetBoxesController != null)
            {
                return leadTargetBoxesController.GetLeadTargetBox();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Add a lock to the target box.
        /// </summary>
        /// <param name="targetLocker">The lock information source.</param>
        public virtual void AddLock(TargetLocker targetLocker)
        {
            if (locksController != null)
            {
                locksController.AddLock(targetLocker);
            }
        }


        /// <summary>
        /// Called to set the trackable that this target box represents.
        /// </summary>
        /// <param name="trackable">The trackable that this target box represents.</param>
        public virtual void UpdateTargetBox(Trackable trackable)
        {
            // Call the event
            onTargetBoxUpdated.Invoke(trackable);   
        }

        public virtual void SetLayer(int layer)
        {
            if (gameObject.layer != layer)
            {
                SetLayerRecursively(gameObject, layer);
            }
        }

        protected static void SetLayerRecursively(GameObject go, int layerNumber)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layerNumber;
            }
        }
    }
}
