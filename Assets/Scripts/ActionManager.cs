using System;

public class ActionManager
{

    public event Action OnClick;


    public void TriggerClick()
    {
        OnClick?.Invoke();
    }
}
