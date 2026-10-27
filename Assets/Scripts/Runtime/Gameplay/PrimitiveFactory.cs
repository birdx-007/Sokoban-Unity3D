using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Creates stylized board geometry. Meshes come from built-in primitives; shading uses
    /// the shared toon material with per-instance color/emission applied through a
    /// MaterialPropertyBlock by <see cref="StylizedController"/>.
    /// </summary>
    public static class PrimitiveFactory
    {
        public static GameObject Create(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            StyledRole role,
            Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();

            Material toon = StylizedController.Instance != null
                ? StylizedController.Instance.ToonMaterial
                : null;

            if (toon != null)
            {
                renderer.sharedMaterial = toon;
                StylizedController.Instance.Register(renderer, role);
            }
            else
            {
                // Toon shader unavailable (stripped build) or no stylization controller:
                // fall back to the plain per-role color material instead of the toon pipeline.
                renderer.sharedMaterial = GetFallbackMaterial(Palette.ForRole(role));
            }

            return go;
        }

        public static void ApplyRole(Renderer renderer, StyledRole role)
        {
            if (renderer == null)
                return;

            if (StylizedController.Instance != null)
            {
                StylizedController.Instance.SetRole(renderer, role);
            }
            else
            {
                renderer.sharedMaterial = GetFallbackMaterial(Palette.ForRole(role));
            }
        }

        public static Mesh GetCube() => Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        private static Material fallbackMaterial;

        private static Material GetFallbackMaterial(Color color)
        {
            if (fallbackMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Standard");
                if (shader != null)
                    fallbackMaterial = new Material(shader);
            }

            if (fallbackMaterial == null)
            {
                Debug.LogError("PrimitiveFactory: no fallback shader ('Unlit/Color' or 'Standard') found; renderer will use Unity's default material.");
                return null;
            }

            // Shared fallback keeps one material instance; color is set per call only when
            // no stylization controller exists (editor tooling / tests).
            fallbackMaterial.color = color;
            return fallbackMaterial;
        }
    }
}
