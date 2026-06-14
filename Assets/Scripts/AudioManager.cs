using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    private float _currentBGtrackLength;
    private float  _currentAmbientAudioLength;
    
    public AudioSource TitleScreenAudioSource;
    public AudioSource bgAudioSource;
    public AudioClip[] bgMusic;
    
    public int BgMusicIndex;
    

    //null check and functionality to play a one shot audio clip when authorized.
        
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }

    public void PlaySound(AudioClip clip, AudioSource source)
    {
        if (clip == null) return;
        source.clip = clip;
        source.Play();
    }
    
    public void PlayTitleMusic()
    {
        TitleScreenAudioSource.loop = true;
        TitleScreenAudioSource.Play();
    }

    public void PlayBGMusic()
    {
        bgAudioSource.clip = bgMusic[BgMusicIndex];
        bgAudioSource.Play();
            
        _currentBGtrackLength = bgAudioSource.clip.length;
        incrementBgMusic();
        StartCoroutine(NextBGMusicClip());
    }

    public void incrementBgMusic()
    {
        BgMusicIndex++;
        if (BgMusicIndex >= bgMusic.Length) BgMusicIndex = 0;
    }
    
    private IEnumerator NextBGMusicClip()
    {
        yield return new WaitForSeconds(_currentBGtrackLength);
        PlayBGMusic();
    }    
        
}
