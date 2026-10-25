using System;
using BoxPushing.Core;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    [Serializable]
    public class GameplayCameraSettings
    {
        [Tooltip("Camera X rotation. 75 degrees is a steep, near-orthogonal top-down view.")]
        [Range(0f, 90f)]
        public float pitch = 75f;

        [Tooltip("World-units padding kept around the board when framing.")]
        public float margin = 1f;

        [Min(0.01f)]
        public float minDistance = 1f;

        [Min(0.01f)]
        public float maxDistance = 200f;
    }

    /// <summary>
    /// Frames a perspective camera so the whole board fits on screen, accounting for the
    /// steep pitch, the board's bounding rectangle, margin, and the camera aspect ratio.
    /// </summary>
    public static class GameplayCamera
    {
        public static void Frame(Camera camera, LevelData level, GameplayCameraSettings settings)
        {
            if (camera == null || level == null || settings == null)
                return;

            float theta = settings.pitch * Mathf.Deg2Rad;
            float fovRad = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = Mathf.Max(0.01f, camera.aspect);

            // Board half-extents in world units, centered on the origin, plus margin.
            float hx = level.Width * 0.5f + settings.margin;
            float hz = level.Height * 0.5f + settings.margin;

            float tanHalfFov = Mathf.Tan(fovRad * 0.5f);
            float tanHalfFovH = Mathf.Max(0.0001f, tanHalfFov) * aspect;

            float cos = Mathf.Cos(theta);
            float sin = Mathf.Sin(theta);

            // Camera is placed on the forward axis that passes through the board center.
            // Vertical fit is governed by the near edge; horizontal fit by the near corners.
            float verticalDistance = hz * cos + hz * sin / Mathf.Max(0.0001f, tanHalfFov);
            float horizontalDistance = hz * cos + hx / tanHalfFovH;

            float distance = Mathf.Max(verticalDistance, horizontalDistance);
            distance = Mathf.Clamp(distance, settings.minDistance, settings.maxDistance);

            Vector3 forward = new Vector3(0f, -sin, cos);
            Vector3 boardCenter = Vector3.zero;

            camera.transform.rotation = Quaternion.Euler(settings.pitch, 0f, 0f);
            camera.transform.position = boardCenter - forward * distance;
        }
    }
}
