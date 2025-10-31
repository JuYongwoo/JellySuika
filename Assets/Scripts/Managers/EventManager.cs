using System;
using UnityEngine;

public class EventManager
{

    public event Action ReleaseCurrentFruitWithMouseEvent;
    public event Action<bool> MoveLeftRightWithKeyBoardEvent;
    public event Action<bool> LockReleaesCurrentFruitEvent;
    public event Action<int> SetScoreTextEvent;

    public event Action<AudioClip, float, bool> PlayAudioClipEvent;
    public event Action<AudioClip> StopAudioClipEvent;
    public event Action StopAllAudioClipEvent;
    public event Action<float> SetMasterVolumeEvent;

    public void OnReleaseCurrentFruitWithMouse()
    {
        ReleaseCurrentFruitWithMouseEvent?.Invoke();
    }

    public void OnMoveLeftRightWithKeyBoard(bool isLeft)
    {
        MoveLeftRightWithKeyBoardEvent?.Invoke(isLeft);
    }

    public void OnLockReleaesCurrentFruit(bool isLock)
    {
        LockReleaesCurrentFruitEvent?.Invoke(isLock);
    }

    public void OnSetScoreText(int score)
    {
        SetScoreTextEvent?.Invoke(score);
    }

    public void OnPlayAudioClip(AudioClip ac, float volume, bool isLoop)
    {
        PlayAudioClipEvent?.Invoke(ac, volume, isLoop);
    }

    public void OnStopAudioClip(AudioClip ac)
    {
        StopAudioClipEvent?.Invoke(ac);
    }

    public void OnStopAllAudioClip()
    {
        StopAllAudioClipEvent?.Invoke();
    }

    public void OnSetMasterVolume(float vol)
    {
        SetMasterVolumeEvent?.Invoke(vol);
    }
}
