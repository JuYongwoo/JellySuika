using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum Fruits
{
    Berry,
    Orange,
    Grape,
    Apple,
    Melon,
    Suika,

}

public enum Sounds
{
    BGM1
}

public enum SFX
{
    FruitFusion,
    ScoreGet
}

public class ResourceManager
{
    public Dictionary<Fruits, AsyncOperationHandle<FruitDataSO>> fruitsInfoMap;
    public Dictionary<Sounds, AsyncOperationHandle<AudioClip>> bgmMap;
    public Dictionary<SFX, AsyncOperationHandle<AudioClip>> sfxMap;
    public AsyncOperationHandle<StageDataSO> stageDataSO;

    public void Init() //게임 시작과 동시에 비동기 로드 시작
    {

        fruitsInfoMap = Util.LoadDictWithEnum<Fruits, FruitDataSO>();
        bgmMap = Util.LoadDictWithEnum<Sounds, AudioClip>();
        sfxMap = Util.LoadDictWithEnum<SFX, AudioClip>();
        stageDataSO = Addressables.LoadAssetAsync<StageDataSO>("StageDataSO");
    }

}
