using System;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Configurable "probe then spring back" visual feedback for a blocked move. This is
    /// purely cosmetic: it never mutates the game state.
    /// </summary>
    [Serializable]
    public class BlockFeedbackConfig
    {
        [Tooltip("How far (world units) the player probes toward the blocked direction before returning.")]
        public float amplitude = 0.18f;

        [Tooltip("Normalized probe envelope: evaluated 0..1, expected 0 at both ends with a peak in between.")]
        public AnimationCurve reboundCurve = CreateDefaultCurve();

        [Tooltip("Duration of the probe/return animation in seconds.")]
        public float duration = 0.08f;

        private static AnimationCurve CreateDefaultCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0f));
        }
    }
}
