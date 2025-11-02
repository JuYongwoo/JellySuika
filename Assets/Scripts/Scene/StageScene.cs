using JYW.JellySuika.Fruit;
using JYW.JellySuika.Managers;
using JYW.JellySuika.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JYW.JellySuika.Scenes
{
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

        private Queue<Fruits> fruitQueue = new Queue<Fruits>(new[] { Fruits.Berry });
        private GameObject currentFruit;
        private State gameState = State.Moving_Start;
        private const float height = 3.8f;
        private const float xDeadZone = 2f;


        private Coroutine currentCoroutine = null;

        public int listSize = 6;

        private bool isLocked = false;


        private void Start()
        {
            ManagerObject.instance.eventManager.OnPlayAudioClip(ManagerObject.instance.resourceManager.GetBGM(Sounds.BGM1), 0.2f, true);
        }


        private void Update()
        {
            //Debug.Log($"Current State = {gameState}");



            switch (gameState)
            {
                case State.Moving_Start:
                    SetState(State.Moving);


                    //위에 생성하고

                    Fruits fr = fruitQueue.Dequeue();
                    currentFruit = ManagerObject.instance.poolManager.Spawn(ManagerObject.instance.resourceManager.GetFruitInfo(fr).parentPrefab, new Vector2(0, height), new Quaternion());
                    //currentFruit = Instantiate(ManagerObject.instance.resourceManager.fruitsInfoMap[fr].Result.parentPrefab, new Vector2(0, height), new Quaternion());
                    currentFruit.GetComponent<WaterBalloon>().setGravity(false);
                    isLocked = true;


                    //리스트 추가
                    while (fruitQueue.Count < listSize)
                    {
                        fruitQueue.Enqueue(Enum.Parse<Fruits>(UnityEngine.Random.Range(0, (int)Fruits.Melon).ToString()));
                    }



                    ManagerObject.instance.eventManager.MoveLeftRightWithKeyBoardEvent += MoveWithKeyBorad; //키보드 좌우 움직이기 가능
                    ManagerObject.instance.eventManager.LockReleaesCurrentFruitEvent += LockReleaseCurrentFruit; // 과일 놓기 가능
                    ManagerObject.instance.eventManager.ReleaseCurrentFruitWithMouseEvent += ReleaseCurrentFruitWithMouse; //마우스 클릭으로 놓기 가능
                    break;
                case State.Moving:
                    //놓으면 다음 상태로
                    if (!isLocked)
                    {
                        currentFruit.GetComponent<WaterBalloon>().setGravity(true);
                        SetState(State.Moving_End);
                    }
                    break;
                case State.Moving_End:
                    SetState(State.Droping_Start);
                    break;
                case State.Droping_Start:
                    SetState(State.Droping);
                    ManagerObject.instance.eventManager.MoveLeftRightWithKeyBoardEvent -= MoveWithKeyBorad;
                    ManagerObject.instance.eventManager.LockReleaesCurrentFruitEvent -= LockReleaseCurrentFruit;
                    ManagerObject.instance.eventManager.ReleaseCurrentFruitWithMouseEvent -= ReleaseCurrentFruitWithMouse;
                    break;
                case State.Droping:
                    if (currentCoroutine == null)
                    {
                        currentCoroutine = StartCoroutine(SetStateCo(State.Droping_End, 1.5f));
                    }
                    break;
                case State.Droping_End:
                    SetState(State.Moving_Start);
                    break;


            }
        }

        private void OnDestroy()
        {
            ManagerObject.instance.eventManager.MoveLeftRightWithKeyBoardEvent -= MoveWithKeyBorad;
            ManagerObject.instance.eventManager.LockReleaesCurrentFruitEvent -= LockReleaseCurrentFruit;
            ManagerObject.instance.eventManager.ReleaseCurrentFruitWithMouseEvent -= ReleaseCurrentFruitWithMouse;


        }


        private IEnumerator SetStateCo(State s, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            gameState = s;
            currentCoroutine = null;
        }


        private void SetState(State s)
        {
            int sint = (int)s;
            sint = sint % (Enum.GetValues(typeof(State)).Length); //+1로 넘기더라도 문제되지 않도록
            s = (State)sint;
            gameState = s;
        }



        private void LockReleaseCurrentFruit(bool isLock)
        {
            //Rigidbody2D[] rigids = currentFruit.GetComponentsInChildren<Rigidbody2D>();

            //for (int i = 0; i < rigids.Length; i++)
            //{
            //    if(isLock) rigids[i].constraints |= RigidbodyConstraints2D.FreezePositionY;
            //    else rigids[i].constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            //}

            isLocked = isLock;
        }

        private void MoveWithKeyBorad(bool isLeft)
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



        void ReleaseCurrentFruitWithMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            LayerMask mask = LayerMask.GetMask("Back");

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100, mask);
            if (hit)
            {
                currentFruit.transform.position = GetAvailableClosestPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition)); //현재 마우스로부터 가장 가까운 소환 가능 지점으로 이동
                LockReleaseCurrentFruit(false);
            }
        }

        private Vector3 GetAvailableClosestPosition(Vector3 origin) //마우스로 클릭했을 때 가장 가까운 소환 가능한 영역을 리턴, 
        {
            Vector3 vec = new Vector3(origin.x, height, 0);

            if (origin.x > xDeadZone)
            {
                vec.x = xDeadZone;
            }
            else if (origin.x < -xDeadZone)
            {
                vec.x = -xDeadZone;
            }
            return vec;
        }


    }
}