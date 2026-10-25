using System.Collections.Generic;
using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Placeholder solid-color geometry. Uses built-in primitive meshes and an unlit
    /// color material; stylized shading arrives in a later phase.
    /// </summary>
    public static class PrimitiveFactory
    {
        private static readonly Dictionary<Color, Material> materialCache = new Dictionary<Color, Material>();

        public static GameObject Create(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial(color);
            return go;
        }

        public static Mesh GetCube() => Resources.GetBuiltinResource<Mesh>("Cube.fbx");

        public static Material GetMaterial(Color color)
        {
            if (!materialCache.TryGetValue(color, out Material material))
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                material.color = color;
                materialCache[color] = material;
            }

            return material;
        }
    }
}
