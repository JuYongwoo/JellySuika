using JYW.JellySuika.Commons;
using JYW.JellySuika.SOs;
using JYW.JellySuika.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace JYW.JellySuika.Managers
{

    public class ResourceManager : Singleton<ResourceManager>
    {
        private Dictionary<Fruits, AsyncOperationHandle<FruitDataSO>> fruitsInfoMap;
        private Dictionary<Sounds, AsyncOperationHandle<AudioClip>> bgmMap;
        private Dictionary<SFX, AsyncOperationHandle<AudioClip>> sfxMap;
        private AsyncOperationHandle<StageDataSO> stageDataSO;

        private void Start() //게임 시작과 동시에 비동기 로드 시작
        {

            fruitsInfoMap = Util.LoadDictWithEnum<Fruits, FruitDataSO>();
            bgmMap = Util.LoadDictWithEnum<Sounds, AudioClip>();
            sfxMap = Util.LoadDictWithEnum<SFX, AudioClip>();
            stageDataSO = Addressables.LoadAssetAsync<StageDataSO>("StageDataSO");
        }

        public FruitDataSO GetFruitInfo(Fruits fruit)
        {
            return fruitsInfoMap[fruit].Result;
        }
        public AudioClip GetBGM(Sounds sound)
        {
            return bgmMap[sound].Result;
        }

        public AudioClip GetSFX(SFX sfx)
        {
            return sfxMap[sfx].Result;
        }

        public StageDataSO GetStageDataSO()
        {
            return stageDataSO.Result;
        }

    }
}