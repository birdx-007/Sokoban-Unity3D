using UnityEngine;

namespace BoxPushing.Gameplay
{
    /// <summary>
    /// Persistent audio service for SFX (and, later, background music). Lives on the
    /// Persistent scene next to GameFlow so it survives every additive scene switch.
    /// Audio clips are optional: when a clip is unassigned the matching Play call is a
    /// no-op, leaving a clean interface for CC0/placeholder assets to be dropped in.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("SFX (leave null to skip)")]
        [SerializeField] private AudioClip moveClip;
        [SerializeField] private AudioClip pushClip;
        [SerializeField] private AudioClip wallHitClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip uiClickClip;

        [Header("Sources")]
        [SerializeField] private AudioSource sfxSource;

        // Background music is intentionally deferred; this source is a reserved hook.
        [SerializeField] private AudioSource musicSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!Application.isPlaying)
                return;

            EnsureSources();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void EnsureSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
        }

        public void PlayMove() => PlayOneShot(moveClip);
        public void PlayPush() => PlayOneShot(pushClip);
        public void PlayWallHit() => PlayOneShot(wallHitClip);
        public void PlayWin() => PlayOneShot(winClip);
        public void PlayUiClick() => PlayOneShot(uiClickClip);

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip);
        }
    }
}
