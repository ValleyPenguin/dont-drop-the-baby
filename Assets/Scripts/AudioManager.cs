using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Source used for looping songs.")]
    [FormerlySerializedAs("TitleScreenAudioSource")]
    [FormerlySerializedAs("bgAudioSource")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Source used for one-shot sound effects.")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Source used for the near-drop crying loop.")]
    [SerializeField] private AudioSource cryingSource;

    [Header("Music Slots")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip loseMusic;
    [SerializeField] private AudioClip winMusic;

    [Header("Sound Effect Slots")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip babyDropScream;
    [SerializeField] private AudioClip babyNearlyDroppedCrying;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.75f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float cryingVolume = 0.8f;

    private AudioClip currentMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSources();
    }

    private void OnValidate()
    {
        ApplySourceSettings();
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    public void PlayLoseMusic()
    {
        PlayMusic(loseMusic);
    }

    public void PlayWinMusic()
    {
        PlayMusic(winMusic);
    }

    public void PlayWinSound()
    {
        PlayOneShot(winSound);
    }

    public void PlayBabyDropScream()
    {
        PlayOneShot(babyDropScream);
    }

    public void SetBabyCrying(bool shouldCry)
    {
        EnsureSources();

        if (babyNearlyDroppedCrying == null || cryingSource == null)
        {
            return;
        }

        if (shouldCry)
        {
            if (cryingSource.isPlaying && cryingSource.clip == babyNearlyDroppedCrying)
            {
                return;
            }

            cryingSource.clip = babyNearlyDroppedCrying;
            cryingSource.loop = true;
            cryingSource.volume = cryingVolume;
            cryingSource.Play();
            return;
        }

        if (cryingSource.isPlaying)
        {
            cryingSource.Stop();
        }
    }

    public void StopBabyCrying()
    {
        SetBabyCrying(false);
    }

    public void PlaySound(AudioClip clip, AudioSource source)
    {
        if (clip == null)
        {
            return;
        }

        if (source == null)
        {
            PlayOneShot(clip);
            return;
        }

        source.PlayOneShot(clip, sfxVolume);
    }

    public void PlayTitleMusic()
    {
        PlayMainMenuMusic();
    }

    public void PlayBGMusic()
    {
        PlayGameplayMusic();
    }

    private void PlayMusic(AudioClip clip)
    {
        EnsureSources();

        if (musicSource == null || clip == null)
        {
            return;
        }

        if (musicSource.isPlaying && currentMusic == clip)
        {
            return;
        }

        currentMusic = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void PlayOneShot(AudioClip clip)
    {
        EnsureSources();

        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void EnsureSources()
    {
        musicSource = EnsureSource(musicSource, "Music Source");
        sfxSource = EnsureSource(sfxSource, "SFX Source");
        cryingSource = EnsureSource(cryingSource, "Crying Source");
        ApplySourceSettings();
    }

    private AudioSource EnsureSource(AudioSource source, string sourceName)
    {
        if (source != null)
        {
            return source;
        }

        AudioSource existingSource = GetComponent<AudioSource>();
        if (existingSource != null && sourceName == "Music Source")
        {
            return existingSource;
        }

        AudioSource createdSource = gameObject.AddComponent<AudioSource>();
        createdSource.playOnAwake = false;
        return createdSource;
    }

    private void ApplySourceSettings()
    {
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.spatialBlend = 0f;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;
            sfxSource.spatialBlend = 0f;
        }

        if (cryingSource != null)
        {
            cryingSource.playOnAwake = false;
            cryingSource.loop = true;
            cryingSource.volume = cryingVolume;
            cryingSource.spatialBlend = 0f;
        }
    }
}
