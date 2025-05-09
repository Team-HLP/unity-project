﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VSX.Controls.UnityInputManager;

namespace VSX.VehicleCombatKits
{
    /// <summary>
    /// Associates a custom input with a trigger index.
    /// </summary>
    [System.Serializable]
    public class TriggerInput
    {
        public int triggerIndex;
        public CustomInput inputSettings;
    }
}
