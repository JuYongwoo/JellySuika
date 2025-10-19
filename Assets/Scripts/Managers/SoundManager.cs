using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    //private Dictionary<Skills, AudioClip> skillSoundsMap;

    private Dictionary<AudioClip, AudioSource> audioSources = new Dictionary<AudioClip, AudioSource>();
    private float masterVolume = 1f; //모든 사운드에 곱해지는 값

    public void OnStart()
    {
        ManagerObject.instance.actionManager.PlayAudioClipEvent -= PlayAudioClip;
        ManagerObject.instance.actionManager.PlayAudioClipEvent += PlayAudioClip;
        ManagerObject.instance.actionManager.StopAudioClipEvent -= StopAudioClip;
        ManagerObject.instance.actionManager.StopAudioClipEvent += StopAudioClip;
        ManagerObject.instance.actionManager.StopAllAudioClipEvent -= StopAllAudioClip;
        ManagerObject.instance.actionManager.StopAllAudioClipEvent += StopAllAudioClip;
        ManagerObject.instance.actionManager.SetMasterVolumeEvent -= SetMasterVolume;
        ManagerObject.instance.actionManager.SetMasterVolumeEvent += SetMasterVolume;
    }
    public void OnDestroy()
    {
        ManagerObject.instance.actionManager.PlayAudioClipEvent -= PlayAudioClip;
        ManagerObject.instance.actionManager.StopAudioClipEvent -= StopAudioClip;
        ManagerObject.instance.actionManager.StopAllAudioClipEvent -= StopAllAudioClip;
        ManagerObject.instance.actionManager.SetMasterVolumeEvent -= SetMasterVolume;

    }

    private void PlayAudioClip(AudioClip ac, float volume, bool isLoop)
    {
        if (!audioSources.ContainsKey(ac)) //존재하지 않는 오디오 클립 대응 소스
        {
            KeyValuePair<AudioClip, AudioSource> removeCandi = new KeyValuePair<AudioClip, AudioSource>(null, null);



            //지금 사용한다는 것은 앞으로도 재생할 일 O, 쉬고있는 오디오소스를 이용하도록 딕셔너리의 key만 바꿔주도록 한다.
            foreach (var pair in audioSources)
            {
                if (!pair.Value.isPlaying) //쉬고있는 오디오소스가 있는가
                {
                    removeCandi = new KeyValuePair<AudioClip, AudioSource>(pair.Key, pair.Value); //새로운 오디오클립 & 이미 생성된 오디오소스 사용
                    break; // 쉬고있는 첫 오디오소스만 저장하고 나온다.
                }
            }


            if (removeCandi.Key != null) //쉬고 있는 오디오소스를 찾았다
            {
                audioSources.Remove(removeCandi.Key); //기존 딕셔너리 제거(실제 컴포넌트는 제거하지 않음)
                audioSources.Add(ac, removeCandi.Value); //키만 바꿔서 등록한다
            }
            else//쉬고 있는 오디오소스를 못찾았다.
            {
                var src = ManagerObject.instance.gameObject.AddComponent<AudioSource>(); //새로 제작
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                src.priority = 128;
                audioSources.Add(ac, src); //제거 없이 딕셔너리 추가
            }

        }

        if (isLoop)
        {
            var s = audioSources[ac];
            if (s.isPlaying && s.clip == ac) return; // 이미 같은 루프가 재생 중이면 무시
        }
        audioSources[ac].volume = volume * masterVolume;
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

    public void StopAllAudioClip()
    {
        foreach (var source in audioSources.Values)
        {
            source.Stop();
        }
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = vol;
    }


}
