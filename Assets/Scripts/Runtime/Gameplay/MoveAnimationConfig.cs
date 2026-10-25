using System;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Serializable movement animation configuration: one curve + duration per phase.
    /// </summary>
    [Serializable]
    public class MoveAnimationConfig
    {
        [Header("Walk")]
        public AnimationCurve walkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float walkDuration = 0.12f;

        [Header("Push")]
        public AnimationCurve pushCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float pushDuration = 0.16f;

        [Header("Turn")]
        public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float turnDuration = 0.06f;
    }
}
