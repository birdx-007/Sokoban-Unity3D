using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Owns the stylized look for a gameplay run. Holds an ordered list of
    /// <see cref="StylizedScheme"/> presets and applies the current one to a shared toon
    /// material plus per-renderer MaterialPropertyBlocks (palette, emission, pulse). It
    /// also sets up the radial-gradient skybox and the restrained post-processing stack.
    /// </summary>
    public class StylizedController : MonoBehaviour
    {
        public static StylizedController Instance { get; private set; }

        [SerializeField] private StylizedScheme[] schemes = new StylizedScheme[0];
        [SerializeField] private int currentIndex = 0;

        [Header("Post Processing")]
        [Range(0f, 2f)] public float bloomIntensity = 0.25f;
        [Range(0f, 1f)] public float vignetteIntensity = 0.28f;
        [Range(-1f, 1f)] public float colorSaturation = 0.05f;
        [Range(-1f, 1f)] public float colorContrast = 0.04f;
        [Range(-4f, 4f)] public float postExposure = 0.1f;

        private readonly List<StyledRenderer> renderers = new List<StyledRenderer>();
        private Material toonMaterial;
        private Material skyboxMaterial;
        private Material previousSkybox;
        private PostProcessVolume volume;

        public StylizedScheme CurrentScheme
        {
            get
            {
                if (schemes == null || schemes.Length == 0)
                    return null;
                return schemes[Mathf.Clamp(currentIndex, 0, schemes.Length - 1)];
            }
        }

        public Material ToonMaterial
        {
            get
            {
                EnsureMaterials();
                return toonMaterial;
            }
        }

        private class StyledRenderer
        {
            public Renderer renderer;
            public StyledRole role;
            public MaterialPropertyBlock block;
        }

        private void Awake()
        {
            Instance = this;
            EnsureMaterials();
        }

        private void Start()
        {
            ApplyScheme();
            ApplyEnvironment();
            EnsurePostProcessing();
        }

        private void Update()
        {
            StylizedScheme scheme = CurrentScheme;
            if (scheme == null || renderers.Count == 0)
                return;

            PruneDestroyed();

            float pulse01 = Mathf.Sin(Time.time * scheme.pulseSpeed) * 0.5f + 0.5f;

            for (int i = 0; i < renderers.Count; i++)
            {
                StyledRenderer styled = renderers[i];
                if (styled.renderer == null || styled.block == null)
                    continue;

                float pulse;
                if (styled.role == StyledRole.Goal)
                    pulse = scheme.goalPulseAmount * pulse01;
                else if (styled.role == StyledRole.BoxOnGoal)
                    pulse = scheme.boxOnGoalPulseAmount * pulse01;
                else
                    continue;

                styled.block.SetFloat("_Pulse", pulse);
                styled.renderer.SetPropertyBlock(styled.block);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (previousSkybox != null)
                RenderSettings.skybox = previousSkybox;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && Instance == this)
                ApplyScheme();
        }

        // ---- Renderer registry ----

        public void Register(Renderer renderer, StyledRole role)
        {
            if (renderer == null)
                return;

            Unregister(renderer);

            var styled = new StyledRenderer
            {
                renderer = renderer,
                role = role,
                block = new MaterialPropertyBlock()
            };
            renderers.Add(styled);
            ApplyRenderer(styled);
        }

        public void Unregister(Renderer renderer)
        {
            if (renderer == null)
                return;

            for (int i = renderers.Count - 1; i >= 0; i--)
            {
                if (renderers[i].renderer == renderer)
                    renderers.RemoveAt(i);
            }
        }

        public void SetRole(Renderer renderer, StyledRole role)
        {
            if (renderer == null)
                return;

            StyledRenderer styled = Find(renderer);
            if (styled == null)
            {
                Register(renderer, role);
                return;
            }

            styled.role = role;
            ApplyRenderer(styled);
        }

        private StyledRenderer Find(Renderer renderer)
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i].renderer == renderer)
                    return renderers[i];
            }
            return null;
        }

        private void PruneDestroyed()
        {
            for (int i = renderers.Count - 1; i >= 0; i--)
            {
                if (renderers[i] == null || renderers[i].renderer == null)
                    renderers.RemoveAt(i);
            }
        }

        // ---- Application ----

        public void ApplyScheme()
        {
            EnsureMaterials();

            StylizedScheme scheme = CurrentScheme;
            if (scheme == null)
                return;

            if (toonMaterial != null)
            {
                toonMaterial.SetFloat("_RampBands", scheme.rampBands);
                toonMaterial.SetFloat("_RampSmoothness", scheme.rampSmoothness);
                toonMaterial.SetColor("_ShadowTint", scheme.shadowTint);
                toonMaterial.SetFloat("_AmbientStrength", scheme.ambientStrength);
                toonMaterial.SetFloat("_FlatShade", scheme.flatShade ? 1f : 0f);
            }

            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor("_CenterColor", scheme.backgroundCenter);
                skyboxMaterial.SetColor("_EdgeColor", scheme.backgroundEdge);
            }

            PruneDestroyed();
            for (int i = 0; i < renderers.Count; i++)
                ApplyRenderer(renderers[i]);
        }

        private void ApplyRenderer(StyledRenderer styled)
        {
            if (styled.renderer == null)
                return;

            StylizedScheme scheme = CurrentScheme;
            if (scheme == null)
                return;

            styled.block.Clear();
            styled.renderer.GetPropertyBlock(styled.block);

            styled.block.SetColor("_Color", scheme.GetColor(styled.role));

            bool grid = styled.role == StyledRole.Floor && scheme.showGridLines;
            styled.block.SetColor("_GridLineColor", grid ? scheme.gridLine : new Color(0f, 0f, 0f, 0f));
            styled.block.SetFloat("_GridCellSize", 1f);

            if (styled.role == StyledRole.Goal)
            {
                styled.block.SetColor("_EmissionColor", scheme.goalEmission);
                styled.block.SetFloat("_EmissionStrength", scheme.goalEmissionStrength);
            }
            else if (styled.role == StyledRole.BoxOnGoal)
            {
                styled.block.SetColor("_EmissionColor", scheme.boxOnGoalEmission);
                styled.block.SetFloat("_EmissionStrength", scheme.boxOnGoalEmissionStrength);
            }
            else
            {
                styled.block.SetColor("_EmissionColor", Color.black);
                styled.block.SetFloat("_EmissionStrength", 0f);
            }

            styled.block.SetFloat("_Pulse", 0f);
            styled.renderer.SetPropertyBlock(styled.block);
        }

        private void ApplyEnvironment()
        {
            if (skyboxMaterial == null)
                return;

            previousSkybox = RenderSettings.skybox;
            RenderSettings.skybox = skyboxMaterial;
        }

        private void EnsureMaterials()
        {
            if (toonMaterial == null)
            {
                Shader toon = Shader.Find("BoxPushing/Toon");
                toonMaterial = new Material(toon != null ? toon : Shader.Find("Standard"));
            }

            if (skyboxMaterial == null)
            {
                Shader sky = Shader.Find("BoxPushing/RadialGradientSkybox");
                if (sky != null)
                    skyboxMaterial = new Material(sky);
            }
        }

        private void EnsurePostProcessing()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            PostProcessLayer layer = cam.GetComponent<PostProcessLayer>();
            if (layer == null)
            {
                Debug.LogWarning("StylizedController: no PostProcessLayer on the gameplay camera; post-processing is disabled.");
                return;
            }

            layer.volumeLayer = LayerMask.GetMask("Default");
            layer.volumeTrigger = null;

            if (volume != null)
                return;

            var go = new GameObject("GameplayPostVolume");
            go.transform.SetParent(transform, false);
            go.layer = 0;

            volume = go.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();

            Bloom bloom = profile.AddSettings<Bloom>();
            bloom.enabled.value = true;
            bloom.intensity.value = bloomIntensity;
            bloom.threshold.value = 1f;
            bloom.softKnee.value = 0.6f;
            bloom.diffusion.value = 7f;
            bloom.anamorphicRatio.value = 0f;
            bloom.color.value = Color.white;
            bloom.fastMode.value = false;

            Vignette vignette = profile.AddSettings<Vignette>();
            vignette.enabled.value = true;
            vignette.color.value = Color.black;
            vignette.intensity.value = vignetteIntensity;
            vignette.smoothness.value = 0.45f;
            vignette.roundness.value = 1f;
            vignette.rounded.value = false;

            ColorGrading grading = profile.AddSettings<ColorGrading>();
            grading.enabled.value = true;
            grading.tonemapper.value = Tonemapper.ACES;
            grading.saturation.value = colorSaturation * 100f;
            grading.contrast.value = colorContrast * 100f;
            grading.postExposure.value = postExposure;

            volume.sharedProfile = profile;
        }
    }
}
