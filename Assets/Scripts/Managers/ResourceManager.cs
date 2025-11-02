using JYW.JellySuika.Common;
using JYW.JellySuika.SOs;
using JYW.JellySuika.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JYW.JellySuika.Managers
{

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
}