using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;

    [Header("=== FRUIT NINJA SOUNDS ===")]
    [Header("Katana Sounds")]
    public AudioClip katanaSwipe;
    public AudioClip katanaWhoosh;

    [Header("Fruit Sounds")]
    public AudioClip fruitSlice;           // Son commun pour toutes les découpes
    public AudioClip fruitFall;            // Son quand un fruit tombe sans être coupé

    [Header("Bomb Sounds")]
    public AudioClip bombTouch;            // Quand on touche une bombe
    public AudioClip bombExplosion;        // Quand la bombe explose

    [Header("Game State Sounds")]
    public AudioClip gameStart;
    public AudioClip gameOver;
    public AudioClip scoreBonus;

    [Header("Background Music")]
    public AudioClip backgroundMusic;

    [Header("UI Sounds")]
    public AudioClip buttonClick;
    public AudioClip buttonHover;

    // Pool d'AudioSources pour les SFX multiples
    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int poolSize = 10;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioManager()
    {
        // Créer les AudioSources si elles n'existent pas
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
        }

        if (sfxSource == null)
        {
            GameObject sfxGO = new GameObject("SFXSource");
            sfxGO.transform.SetParent(transform);
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.volume = sfxVolume * masterVolume;
        }

        if (uiSource == null)
        {
            GameObject uiGO = new GameObject("UISource");
            uiGO.transform.SetParent(transform);
            uiSource = uiGO.AddComponent<AudioSource>();
            uiSource.volume = sfxVolume * masterVolume;
        }

        // Créer la pool d'AudioSources pour les SFX multiples
        CreateSFXPool();
    }

    private void CreateSFXPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject sfxObj = new GameObject($"SFXPool_{i}");
            sfxObj.transform.SetParent(transform);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.volume = sfxVolume * masterVolume;
            source.playOnAwake = false;
            sfxPool.Add(source);
        }
    }

    // ==================== MÉTHODES DE LECTURE ====================

    // KATANA
    public void PlayKatanaSwipe()
    {
        PlaySFX(katanaSwipe);
    }

    public void PlayKatanaWhoosh()
    {
        PlaySFX(katanaWhoosh);
    }

    // FRUITS
    public void PlayFruitSlice()
    {
        PlaySFX(fruitSlice);
    }

    public void PlayFruitSliceAtPosition(Vector3 position)
    {
        PlaySFXAtPosition(fruitSlice, position);
    }

    public void PlayFruitFall()
    {
        PlaySFX(fruitFall);
    }

    // BOMBES
    public void PlayBombTouch()
    {
        PlaySFX(bombTouch);
    }

    public void PlayBombExplosion()
    {
        PlaySFX(bombExplosion);
    }

    // ÉTAT DU JEU
    public void PlayGameStart()
    {
        PlayUI(gameStart);
    }

    public void PlayGameOver()
    {
        PlayUI(gameOver);
    }

    public void PlayScoreBonus()
    {
        PlaySFX(scoreBonus);
    }

    // UI
    public void PlayButtonClick()
    {
        PlayUI(buttonClick);
    }

    public void PlayButtonHover()
    {
        PlayUI(buttonHover);
    }

    // MUSIQUE
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    // ==================== MÉTHODES GÉNÉRIQUES ====================

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    private void PlaySFXAtPosition(AudioClip clip, Vector3 position)
    {
        if (clip != null)
        {
            // Trouver un AudioSource libre dans la pool
            AudioSource freeSource = sfxPool.Find(source => !source.isPlaying);
            if (freeSource != null)
            {
                freeSource.transform.position = position;
                freeSource.PlayOneShot(clip, sfxVolume * masterVolume);
            }
            else
            {
                // Fallback sur la source principale
                AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
            }
        }
    }

    private void PlayUI(AudioClip clip)
    {
        if (clip != null && uiSource != null)
        {
            uiSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // ==================== CONTRÔLES VOLUME ====================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    private void UpdateAllVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * masterVolume;
        
        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
        
        if (uiSource != null)
            uiSource.volume = sfxVolume * masterVolume;

        // Mettre à jour la pool SFX
        foreach (AudioSource source in sfxPool)
        {
            if (source != null)
                source.volume = sfxVolume * masterVolume;
        }
    }

    // ==================== MÉTHODES UTILITAIRES ====================

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (musicSource != null) musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null) musicSource.UnPause();
    }

    public void StopAllSFX()
    {
        if (sfxSource != null) sfxSource.Stop();
        if (uiSource != null) uiSource.Stop();
        
        foreach (AudioSource source in sfxPool)
        {
            if (source != null) source.Stop();
        }
    }

    // Fade in/out musique
    public IEnumerator FadeMusic(float targetVolume, float duration)
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }
}