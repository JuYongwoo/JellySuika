using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum Fruits
{
    Berry,
    Apple,
    Grape,
    Orange,
    Melon,
    Suika,

}

public class ResourceManager
{
    public Dictionary<Fruits, AsyncOperationHandle<GameObject>> fruitsObjMap;


    public void Init() //게임 시작과 동시에 비동기 로드 시작
    {

        fruitsObjMap = Util.LoadDictWithEnum<Fruits, GameObject>();



    }

}
