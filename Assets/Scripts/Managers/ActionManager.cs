using System;
using Unity.VisualScripting;

public class ActionManager
{

    public event Action ReleaseCurrentFruitWithMouse;
    public event Action<bool> MoveLeftRightWithKeyBoard;
    public event Action<bool> LockReleaesCurrentFruit;


    public void OnClickEvent()
    {
        ReleaseCurrentFruitWithMouse?.Invoke();
    }

    public void OnMoveLeftRight(bool isLeft)
    {
        MoveLeftRightWithKeyBoard?.Invoke(isLeft);
    }

    public void OnLockReleaesCurrentFruit(bool isLock)
    {
        LockReleaesCurrentFruit?.Invoke(isLock);
    }
}
