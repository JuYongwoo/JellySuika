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



        //둘다 사용 가능하게 할 예정
        //바깥부분 누르면 거기서 가장 가까운 소환 가능영역에 소환

        //마우스 사용할 때
        if (Input.GetMouseButtonDown(0))
        {
            ManagerObject.instance.actionManager.OnClickEvent();
        }

        //키보드 사용할 때
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //들고 있는 과일을 놓는다.
            ManagerObject.instance.actionManager.OnLockReleaesCurrentFruit(false);

        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            ManagerObject.instance.actionManager.OnMoveLeftRight(true);
            //들고 있는 과일을 왼쪽으로
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            //들고 있는 과일을 오른쪽으로
            ManagerObject.instance.actionManager.OnMoveLeftRight(false);

        }



    }


}
