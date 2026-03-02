using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    private const string PP_SFX = "SFX_ENABLED";

    [Header("Config")]
    public SfxLibrary sfx;
    public AudioSource sfxSource;

    [Header("Optional layering")]
    public AudioSource sfxSource2;

    private bool sfxEnabled = true;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource2 == null)
        {
            sfxSource2 = gameObject.AddComponent<AudioSource>();
            sfxSource2.playOnAwake = false;
        }

        sfxSource.playOnAwake = false;
        sfxSource2.playOnAwake = false;

        // Load persisted SFX setting (default ON)
        bool persistedEnabled = PlayerPrefs.GetInt(PP_SFX, 1) == 1;
        SetSfxEnabled(persistedEnabled);
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;

        // Stop currently playing sounds when disabling
        if (!sfxEnabled)
        {
            if (sfxSource != null) sfxSource.Stop();
            if (sfxSource2 != null) sfxSource2.Stop();
        }
    }

    public void Play(SfxId id)
    {
        if (!sfxEnabled) return;
        PlayInternal(id, sfxSource);
    }

    public void PlayLayered(SfxId front, SfxId back)
    {
        if (!sfxEnabled) return;
        PlayInternal(front, sfxSource);
        PlayInternal(back, sfxSource2);
    }

    private void PlayInternal(SfxId id, AudioSource src)
    {
        if (!sfxEnabled) return;

        if (sfx == null) return;
        if (!sfx.TryGet(id, out var entry)) return;
        if (entry.clips == null || entry.clips.Length == 0) return;

        var clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        src.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
        src.PlayOneShot(clip, entry.volume);
    }
}