using UnityEngine;
using UnityEngine.UI;

public class InputManager
{
    void Start()
    {
        
    }

    // Update is called once per frame
    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ManagerObject.instance.actionManager.TriggerClick();
        }
    }


}
