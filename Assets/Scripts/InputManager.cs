using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ManagerObject.instance.actionManager.TriggerClick();
        }
    }


}
