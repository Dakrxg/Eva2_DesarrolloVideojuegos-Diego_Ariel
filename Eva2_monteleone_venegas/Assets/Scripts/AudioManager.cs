using UnityEngine;

/// <summary>
/// Música de fondo (BGM) y efectos (SFX) con volúmenes separados (mezcla básica).
/// Va UNA vez por escena en un objeto vacío "AudioManager".
/// Los demás scripts reproducen sonidos con: AudioManager.Play(clip);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Música")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.35f;

    [Header("Efectos")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    /// <summary>Atajo estático seguro: no falla si no hay AudioManager en la escena.</summary>
    public static void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (Instance != null) Instance.PlaySFX(clip, volumeScale);
    }
}
