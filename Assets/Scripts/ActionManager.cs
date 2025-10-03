using System;
using Unity.VisualScripting;

public class ActionManager
{

    public event Action ClickEvent;
    public event Action<bool> MoveLeftRight;
    public event Action<bool> LockReleaesCurrentFruit;


    public void OnClickEvent()
    {
        ClickEvent?.Invoke();
    }

    public void OnMoveLeftRight(bool isLeft)
    {
        MoveLeftRight?.Invoke(isLeft);
    }

    public void OnLockReleaesCurrentFruit(bool isLock)
    {
        LockReleaesCurrentFruit?.Invoke(isLock);
    }
}
