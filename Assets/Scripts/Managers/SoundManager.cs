using System.Collections.Generic;
using UnityEngine;

namespace JYW.JellySuika.Managers
{
    public class SoundManager
{
    //private Dictionary<Skills, AudioClip> skillSoundsMap;

    private Dictionary<AudioClip, AudioSource> audioSources = new Dictionary<AudioClip, AudioSource>();
    private float masterVolume = 1f;

    public void OnAwake()
    {
        ManagerObject.instance.eventManager.PlayAudioClipEvent -= PlayAudioClip;
        ManagerObject.instance.eventManager.PlayAudioClipEvent += PlayAudioClip;
        ManagerObject.instance.eventManager.StopAudioClipEvent -= StopAudioClip;
        ManagerObject.instance.eventManager.StopAudioClipEvent += StopAudioClip;
        ManagerObject.instance.eventManager.StopAllAudioClipEvent -= StopAllAudioClip;
        ManagerObject.instance.eventManager.StopAllAudioClipEvent += StopAllAudioClip;
        ManagerObject.instance.eventManager.SetMasterVolumeEvent -= SetMasterVolume;
        ManagerObject.instance.eventManager.SetMasterVolumeEvent += SetMasterVolume;
    }
    public void OnDestroy()
    {
        ManagerObject.instance.eventManager.PlayAudioClipEvent -= PlayAudioClip;
        ManagerObject.instance.eventManager.StopAudioClipEvent -= StopAudioClip;
        ManagerObject.instance.eventManager.StopAllAudioClipEvent -= StopAllAudioClip;
        ManagerObject.instance.eventManager.SetMasterVolumeEvent -= SetMasterVolume;

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
                var src = ManagerObject.instance.gameObject.AddComponent<AudioSource>();
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