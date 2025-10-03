using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;


public class StageScene : MonoBehaviour
{
    private enum State
    {
        Moving, //플레이어가 조종가능한 상태
        Droping, //과일이 떨어지는 상태, 2초 대기
    }

    private Queue<Fruits> fruitQueue = new Queue<Fruits>(new[] { Fruits.Berry});
    private GameObject currentFruit;

    public int listSize = 6;

    void Start()
    {
        //위에 과일 생성
        addFruit();





        ManagerObject.instance.actionManager.ClickEvent -= clickEvent;
        ManagerObject.instance.actionManager.ClickEvent += clickEvent;

        ManagerObject.instance.actionManager.MoveLeftRight -= moveCurrentFruit;
        ManagerObject.instance.actionManager.MoveLeftRight += moveCurrentFruit;

        ManagerObject.instance.actionManager.LockReleaesCurrentFruit -= lockReleaseCurrentFruit;
        ManagerObject.instance.actionManager.LockReleaesCurrentFruit += lockReleaseCurrentFruit;


    }

    private void OnDestroy()
    {
        ManagerObject.instance.actionManager.ClickEvent -= clickEvent;
        ManagerObject.instance.actionManager.MoveLeftRight -= moveCurrentFruit;
        ManagerObject.instance.actionManager.LockReleaesCurrentFruit -= lockReleaseCurrentFruit;


    }

    private void addFruit()
    {


        //위에 생성하고

        Fruits fr = fruitQueue.Dequeue();
        currentFruit = Instantiate(ManagerObject.instance.resourceManager.fruitsObjMap[fr].Result, new Vector2(0, 3.8f), new Quaternion());


        //y축 고정
        Rigidbody2D[] rigids = currentFruit.GetComponentsInChildren<Rigidbody2D>();
        for (int i = 0; i < rigids.Length; i++)
        {
            rigids[i].constraints |= RigidbodyConstraints2D.FreezePositionY;
        }

        //리스트 추가
        while (fruitQueue.Count < listSize)
        {
            fruitQueue.Enqueue(Enum.Parse<Fruits>(UnityEngine.Random.Range(0, (int)Fruits.Melon).ToString()));
        }
    }


    private void lockReleaseCurrentFruit(bool isLock)
    {
        Rigidbody2D[] rigids = currentFruit.GetComponentsInChildren<Rigidbody2D>();

        for (int i = 0; i < rigids.Length; i++)
        {
            if(isLock) rigids[i].constraints |= RigidbodyConstraints2D.FreezePositionY;
            else rigids[i].constraints &= ~RigidbodyConstraints2D.FreezePositionY;
        }

        if(!isLock) addFruit(); //과일을 놓았기 때문에 새로운 과일 생성 & 리스트 채우기
    }

    private void moveCurrentFruit(bool isLeft)
    {
        if (currentFruit)
        {
            if(isLeft) currentFruit.transform.Translate(Vector3.left * Time.deltaTime * 1);
            else currentFruit.transform.Translate(Vector3.right * Time.deltaTime * 1);
        }

    }



    void clickEvent()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask mask = LayerMask.GetMask("Back");

        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100, mask);
        if (hit)
        {

            lockReleaseCurrentFruit(false);
        }
    }


}
