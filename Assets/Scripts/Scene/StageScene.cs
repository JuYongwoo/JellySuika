using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;


public class StageScene : MonoBehaviour
{
    private enum State
    {
        Moving_Start,
        Moving, //플레이어가 조종가능한 상태
        Moving_End,
        Droping_Start,
        Droping, //과일이 떨어지는 상태, 2초 대기
        Droping_End,
    }

    private Queue<Fruits> fruitQueue = new Queue<Fruits>(new[] { Fruits.Berry});
    private GameObject currentFruit;
    private State gameState = State.Moving_Start;
    public const float height = 3.8f;
    public const float xDeadZone = 2f;

    private Coroutine currentCoroutine = null;

    public int listSize = 6;

    private bool isLocked = false;


    private void Update()
    {
        //Debug.Log($"Current State = {gameState}");



        switch (gameState)
        {
            case State.Moving_Start:
                setState(State.Moving);


                //위에 생성하고

                Fruits fr = fruitQueue.Dequeue();
                currentFruit = Instantiate(ManagerObject.instance.resourceManager.fruitsObjMap[fr].Result, new Vector2(0, height), new Quaternion());
                currentFruit.GetComponent<WaterBalloon>().setGravity(false);
                isLocked = true;


                //리스트 추가
                while (fruitQueue.Count < listSize)
                {
                    fruitQueue.Enqueue(Enum.Parse<Fruits>(UnityEngine.Random.Range(0, (int)Fruits.Melon).ToString()));
                }



                ManagerObject.instance.actionManager.MoveLeftRightWithKeyBoard += moveWithKeyBorad; //키보드 좌우 움직이기 가능
                ManagerObject.instance.actionManager.LockReleaesCurrentFruit += lockReleaseCurrentFruit; // 과일 놓기 가능
                ManagerObject.instance.actionManager.ReleaseCurrentFruitWithMouse += releaseCurrentFruitWithMouse; //마우스 클릭으로 놓기 가능
                break;
            case State.Moving:
                //놓으면 다음 상태로
                if (!isLocked)
                {
                    currentFruit.GetComponent<WaterBalloon>().setGravity(true);
                    setState(State.Moving_End);
                }
                break;
            case State.Moving_End:
                setState(State.Droping_Start);
                break;
            case State.Droping_Start:
                setState(State.Droping);
                ManagerObject.instance.actionManager.MoveLeftRightWithKeyBoard -= moveWithKeyBorad;
                ManagerObject.instance.actionManager.LockReleaesCurrentFruit -= lockReleaseCurrentFruit;
                ManagerObject.instance.actionManager.ReleaseCurrentFruitWithMouse -= releaseCurrentFruitWithMouse;
                break;
            case State.Droping:
                if (currentCoroutine == null)
                {
                    currentCoroutine = StartCoroutine(setState(State.Droping_End, 1.5f));
                }
                break;
            case State.Droping_End:
                setState(State.Moving_Start);
                break;


        }
    }

    private void OnDestroy()
    {
        ManagerObject.instance.actionManager.MoveLeftRightWithKeyBoard -= moveWithKeyBorad;
        ManagerObject.instance.actionManager.LockReleaesCurrentFruit -= lockReleaseCurrentFruit;
        ManagerObject.instance.actionManager.ReleaseCurrentFruitWithMouse -= releaseCurrentFruitWithMouse;


    }

    private void nextState()
    {
        setState(gameState + 1);
    }

    private IEnumerator setState(State s, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameState = s;
        currentCoroutine = null;
    }


    private void setState(State s)
    {
        int sint = (int)s;
        sint = sint % (Enum.GetValues(typeof(State)).Length); //+1로 넘기더라도 문제되지 않도록
        s = (State)sint;
        gameState = s;
    }



    private void lockReleaseCurrentFruit(bool isLock)
    {
        //Rigidbody2D[] rigids = currentFruit.GetComponentsInChildren<Rigidbody2D>();

        //for (int i = 0; i < rigids.Length; i++)
        //{
        //    if(isLock) rigids[i].constraints |= RigidbodyConstraints2D.FreezePositionY;
        //    else rigids[i].constraints &= ~RigidbodyConstraints2D.FreezePositionY;
        //}

        isLocked = isLock;
    }

    private void moveWithKeyBorad(bool isLeft)
    {
        if (currentFruit)
        {
            if (isLeft)
            {
                if (currentFruit.transform.position.x > -xDeadZone)
                    currentFruit.transform.Translate(Vector3.left * Time.deltaTime * 1);
            }
            else
            {
                if (currentFruit.transform.position.x < xDeadZone)
                    currentFruit.transform.Translate(Vector3.right * Time.deltaTime * 1);
            }
        }

    }



    void releaseCurrentFruitWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        LayerMask mask = LayerMask.GetMask("Back");

        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100, mask);
        if (hit)
        {
            currentFruit.transform.position = getAvailableClosestPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition)); //현재 마우스로부터 가장 가까운 소환 가능 지점으로 이동
            lockReleaseCurrentFruit(false);
        }
    }

    private Vector3 getAvailableClosestPosition(Vector3 origin) //마우스로 클릭했을 때 가장 가까운 소환 가능한 영역을 리턴, 
    {
        Vector3 vec = new Vector3(origin.x, height, 0);

        if(origin.x > xDeadZone)
        {
            vec.x = xDeadZone;
        }
        else if(origin.x < -xDeadZone)
        {
            vec.x = -xDeadZone;
        }
            return vec;
    }


}
