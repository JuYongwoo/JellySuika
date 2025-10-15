using System;

public class ActionManager
{

    public event Action ReleaseCurrentFruitWithMouseEvent;
    public event Action<bool> MoveLeftRightWithKeyBoardEvent;
    public event Action<bool> LockReleaesCurrentFruitEvent;
    public event Action<int> SetScoreTextEvent;


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
}
