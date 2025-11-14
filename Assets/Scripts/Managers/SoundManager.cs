using System.Collections.Generic;
using UnityEngine;

namespace JYW.JellySuika.Managers
{
    public class SoundManager : Singleton<SoundManager>
    {
    //private Dictionary<Skills, AudioClip> skillSoundsMap;

    private Dictionary<AudioClip, AudioSource> audioSources = new Dictionary<AudioClip, AudioSource>();
    private float masterVolume = 1f;

    private void Start()
    {
        EventManager.Instance.PlayAudioClipEvent -= PlayAudioClip;
        EventManager.Instance.PlayAudioClipEvent += PlayAudioClip;
        EventManager.Instance.StopAudioClipEvent -= StopAudioClip;
        EventManager.Instance.StopAudioClipEvent += StopAudioClip;
        EventManager.Instance.StopAllAudioClipEvent -= StopAllAudioClip;
        EventManager.Instance.StopAllAudioClipEvent += StopAllAudioClip;
        EventManager.Instance.SetMasterVolumeEvent -= SetMasterVolume;
        EventManager.Instance.SetMasterVolumeEvent += SetMasterVolume;
    }
    private void OnDestroy()
    {
        EventManager.Instance.PlayAudioClipEvent -= PlayAudioClip;
        EventManager.Instance.StopAudioClipEvent -= StopAudioClip;
        EventManager.Instance.StopAllAudioClipEvent -= StopAllAudioClip;
        EventManager.Instance.SetMasterVolumeEvent -= SetMasterVolume;

    }

    private void PlayAudioClip(AudioClip ac, float volume, bool isLoop)
    {
        if (!audioSources.ContainsKey(ac))
        {
            KeyValuePair<AudioClip, AudioSource> removeCandi = new KeyValuePair<AudioClip, AudioSource>(null, null);

            foreach (var pair in audioSources)
            {
                if (!pair.Value.isPlaying)
                {
                    removeCandi = new KeyValuePair<AudioClip, AudioSource>(pair.Key, pair.Value);
                    break;
                }
            }

            if (removeCandi.Key != null)
            {
                audioSources.Remove(removeCandi.Key);
                audioSources.Add(ac, removeCandi.Value);
            }
            else
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.priority = 128;
                audioSources.Add(ac, src);
            }

        }

        if (isLoop)
        {
            var s = audioSources[ac];
            if (s.isPlaying && s.clip == ac) return;
        }
        audioSources[ac].volume = volume * masterVolume;
        audioSources[ac].loop = isLoop;

        audioSources[ac].Stop();
        audioSources[ac].clip = ac;
        audioSources[ac].Play();

    }

    private void StopAudioClip(AudioClip ac)
    {
        if (audioSources.ContainsKey(ac))
        {
            audioSources[ac].Stop();
        }
    }

    private void StopAllAudioClip()
    {
        foreach (var source in audioSources.Values)
        {
            source.Stop();
        }
    }

    private void SetMasterVolume(float vol)
    {
        masterVolume = vol;
    }


}
}