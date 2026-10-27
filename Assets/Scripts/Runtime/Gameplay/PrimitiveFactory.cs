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

            if (StylizedController.Instance != null)
            {
                renderer.sharedMaterial = StylizedController.Instance.ToonMaterial;
                StylizedController.Instance.Register(renderer, role);
            }
            else
            {
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
                fallbackMaterial = new Material(shader);
            }

            // Shared fallback keeps one material instance; color is set per call only when
            // no stylization controller exists (editor tooling / tests).
            fallbackMaterial.color = color;
            return fallbackMaterial;
        }
    }
}
