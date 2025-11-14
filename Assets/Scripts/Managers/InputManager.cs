using UnityEngine;

namespace JYW.JellySuika.Managers
{
    public class InputManager : Singleton<InputManager>
    {
        private void Update()
        {


            /*
            //마우스 사용할 때
            if (Input.GetMouseButtonDown(0))
            {
                ManagerObject.instance.actionManager.OnClickEvent();
            }
            */

            //키보드 사용할 때
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //들고 있는 과일을 놓는다.
                EventManager.Instance.OnLockReleaesCurrentFruit(false);

            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                EventManager.Instance.OnMoveLeftRightWithKeyBoard(true);
                //들고 있는 과일을 왼쪽으로
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                //들고 있는 과일을 오른쪽으로
                EventManager.Instance.OnMoveLeftRightWithKeyBoard(false);

            }



        }


    }
}