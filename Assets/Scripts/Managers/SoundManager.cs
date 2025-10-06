using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    //private Dictionary<Skills, AudioClip> skillSoundsMap;

    private Dictionary<AudioClip, AudioSource> audioSources = new Dictionary<AudioClip, AudioSource>();


    public void PlayAudioClip(AudioClip ac, float volume, bool isLoop)
    {
        if (!audioSources.ContainsKey(ac))
        {
            audioSources.Add(ac, ManagerObject.instance.gameObject.AddComponent<AudioSource>());
        }

        audioSources[ac].volume = volume;
        audioSources[ac].loop = isLoop;

        audioSources[ac].Stop();
        audioSources[ac].clip = ac;
        audioSources[ac].Play();

    }

    public void StopAudioClip(AudioClip ac)
    {
        if (audioSources.ContainsKey(ac))
        {
            audioSources[ac].Stop();
        }
    }



}