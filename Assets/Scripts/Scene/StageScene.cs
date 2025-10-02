using UnityEngine;
using System.Collections.Generic;
using System;


public class StageScene : MonoBehaviour
{
    private enum State
    {
        Moving, //플레이어가 조종가능한 상태
        Droping, //과일이 떨어지는 상태, 2초 대기
    }

    private Queue<Fruits> fruitQueue = new Queue<Fruits>();
    public int listSize = 6;

    void Start()
    {
        addRandomFruitList();

        ManagerObject.instance.actionManager.OnClick -= clickEvent;
        ManagerObject.instance.actionManager.OnClick += clickEvent;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void clickEvent()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask mask = LayerMask.GetMask("Back");

        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100, mask);
        if (hit)
        {
            //이번 순서의 프리팹 소환

        }
    }


    void addRandomFruitList() //랜덤으로 과일리스트 한칸을 채운다.
    {
        while(fruitQueue.Count < listSize)
        {
            
            fruitQueue.Enqueue(Enum.Parse<Fruits>(UnityEngine.Random.Range(0, listSize).ToString()));
        }
    }
}
