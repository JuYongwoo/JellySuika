using System;
using UnityEngine;

public class ActionManager : MonoBehaviour
{

    public event Action OnClick;


    public void TriggerClick()
    {
        OnClick?.Invoke();
    }
}
