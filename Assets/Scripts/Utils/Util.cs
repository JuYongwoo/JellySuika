using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class Util
{


    public static AsyncOperationHandle<T> AsyncLoad<T>(string key) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> obj = Addressables.LoadAssetAsync<T>(key);
        return obj;
    }


    public static Dictionary<T, AsyncOperationHandle<T2>> LoadDictWithEnum<T, T2>() where T : Enum where T2 : UnityEngine.Object
    {
        Dictionary<T, AsyncOperationHandle<T2>> dict = new Dictionary<T, AsyncOperationHandle<T2>>();
        foreach (T s in Enum.GetValues(typeof(T)))
            dict[s] = Addressables.LoadAssetAsync<T2>(s.ToString());
        return dict;
    }

    public static Dictionary<T1, AsyncOperationHandle<T2>> LoadDictByLabel<T1, T2>(string label) where T2 : UnityEngine.Object
    {
        var dict = new Dictionary<T1, AsyncOperationHandle<T2>>();

        // 라벨에 해당하는 모든 리소스 위치 가져오기
        IList<IResourceLocation> locations = Addressables.LoadResourceLocationsAsync(label, typeof(T2)).WaitForCompletion();

        foreach (var loc in locations)
        {
            // Address(PrimaryKey)가 곧 Dictionary의 key
            string key = loc.PrimaryKey;

            // 동기 로드
            AsyncOperationHandle<T2> asset = Addressables.LoadAssetAsync<T2>(key);


            object parsedKey;

            if (typeof(T1) == typeof(string))
            {
                parsedKey = key;
            }
            else if (typeof(T1).IsEnum)
            {
                parsedKey = Enum.Parse(typeof(T1), key);
            }
            else if (typeof(T1) == typeof(int))
            {
                parsedKey = int.Parse(key);
            }
            else
            {
                throw new NotSupportedException($"Unsupported key type: {typeof(T1)}");
            }

            dict[(T1)parsedKey] = asset;

        }

        return dict;
    }


    public static Dictionary<TEnum, AsyncOperationHandle<TObject>> LoadDictWithEnumAndLabel<TEnum, TObject>(string label) where TEnum : Enum where TObject : UnityEngine.Object
    {
        var dict = new Dictionary<TEnum, AsyncOperationHandle<TObject>>();

        var locHandle = Addressables.LoadResourceLocationsAsync(label, typeof(TObject));
        var addressList = locHandle.Result; // ← FIX: 핸들 자체가 아니라 결과 리스트를 써야 함

        if (addressList == null || addressList.Count == 0)
        {
            Debug.LogWarning($"[Util] No Addressable assets found for label {label}");
            return dict;
        }

        foreach (TEnum e in Enum.GetValues(typeof(TEnum)))
        {
            string enumName = e.ToString();
            foreach (var addr in addressList)
            {
                if (addr.PrimaryKey.Equals(enumName, StringComparison.OrdinalIgnoreCase))
                {
                    var assetHandle = Addressables.LoadAssetAsync<TObject>(addr);
                    dict[e] = assetHandle;
                    break;
                }
            }
            if (!dict.ContainsKey(e))
            {
                Debug.LogWarning($"[Util] No matching asset for Enum {enumName} in label {label}");
                dict[e] = default; // ← FIX: AsyncOperationHandle<T>는 struct라 null 할당 불가
            }
        }
        return dict;
    }


    public static Dictionary<TEnum, AsyncOperationHandle<TObject>> LoadDictWithOnlyLabelsFromEnumAndParameter<TEnum, TObject>(string commonLabel) where TEnum : Enum where TObject : UnityEngine.Object
    {
        var dict = new Dictionary<TEnum, AsyncOperationHandle<TObject>>();
        foreach (TEnum e in Enum.GetValues(typeof(TEnum)))
        {
            IEnumerable keys = new[] { commonLabel, e.ToString() };
            var locHandle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Intersection, typeof(TObject));
            var locations = locHandle.WaitForCompletion();

            if (locations == null || locations.Count == 0)
            {
                Debug.LogWarning($"[MapEnumToAddressablesByLabels] No asset found for Enum {e} with label '{commonLabel}'");
                continue;
            }
            var assetHandle = Addressables.LoadAssetAsync<TObject>(locations[0]);
            var asset = assetHandle;
            dict[e] = asset;

            Addressables.Release(locHandle);
            Addressables.Release(assetHandle);
        }
        return dict;
    }

    public static Dictionary<string, T2> MapAllChildObjects<T2>(GameObject go) where T2 : UnityEngine.Object
    {
        Dictionary<string, T2> dict = new Dictionary<string, T2>();
        Transform[] children = go.GetComponentsInChildren<Transform>();

        foreach (Transform child in children)
        {
            if (typeof(T2) == typeof(GameObject))
                dict[child.name] = (T2)(object)child.gameObject;
            else
                dict[child.name] = child.GetComponent<T2>();
        }
        return dict;
    }

    public static Dictionary<T, T2> MapEnumChildObjects<T, T2>(GameObject go) where T : Enum where T2 : UnityEngine.Object
    {
        Dictionary<T, T2> dict = new Dictionary<T, T2>();
        Transform[] children = go.GetComponentsInChildren<Transform>();

        foreach (T enumName in Enum.GetValues(typeof(T)))
        {
            foreach (Transform child in children)
            {
                if (child.name == enumName.ToString())
                {
                    if (typeof(T2) == typeof(GameObject))
                        dict[enumName] = (T2)(object)child.gameObject;
                    else
                        dict[enumName] = child.GetComponent<T2>();
                    break;
                }
            }
        }
        return dict;
    }

    public static GameObject GetObjectInChildren(GameObject go, string childName)
    {
        Transform[] children = go.GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
            if (child.name == childName)
                return child.gameObject;
        return null;
    }

    public static Dictionary<T, T2> MapEumToObjectDEPRECATED<T, T2>(string filePath) where T : Enum where T2 : UnityEngine.Object
    {
        Dictionary<T, T2> dict = new Dictionary<T, T2>();
        foreach (T s in Enum.GetValues(typeof(T)))
            dict[s] = Resources.Load<T2>(filePath + "/" + s.ToString());
        return dict;
    }



    /*
    public static Dictionary<string, TObject> MapStringKeyToObjectWithLabel<TObject>(string commonLabel)
    where TObject : UnityEngine.Object
    {
        var dict = new Dictionary<string, TObject>();
        IEnumerable keys = new[] { commonLabel }; //라벨로 전부 불러와서
        var locHandle = Addressables.LoadResourceLocationsAsync(keys, Addressables.MergeMode.Intersection, typeof(TObject));
        var locations = locHandle.WaitForCompletion();

        foreach (var i in locations)
        {

            var assetHandle = Addressables.LoadAssetAsync<TObject>(i);
            var asset = assetHandle.WaitForCompletion();
            dict.Add(i.PrimaryKey, asset); //불러온 오브젝트들을 맵에 <키값, 오브젝트> 로 대응
            Addressables.Release(assetHandle);
        }
        Addressables.Release(locHandle);

        return dict;
    }
    */

    public static T LoadOneResourceInFolderDEPRECATED<T>(string filePath) where T : UnityEngine.Object
    {
        T[] resources = Resources.LoadAll<T>(filePath);
        if (resources.Length == 0)
        {
            Debug.LogError($"No resources found at {filePath}");
            return null;
        }
        if (resources.Length > 1)
            Debug.LogWarning($"Multiple resources found at {filePath}, returning the first one.");
        return resources[0];
    }

    public static Dictionary<TEnum, TAsset> MapEnumToChildFileDEPRECATED<TEnum, TAsset>(string basePath, string fileName)
        where TEnum : Enum where TAsset : UnityEngine.Object
    {
        var dict = new Dictionary<TEnum, TAsset>();
        foreach (TEnum e in Enum.GetValues(typeof(TEnum)))
            dict[e] = Resources.Load<TAsset>($"{basePath}/{e}/{fileName}");
        return dict;
    }

    public static T AddOrGetComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null) component = go.AddComponent<T>();
        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive);
        return transform == null ? null : transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null) return null;

        if (!recursive)
        {
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform t = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || t.name == name)
                {
                    T comp = t.GetComponent<T>();
                    if (comp != null) return comp;
                }
            }
        }
        else
        {
            foreach (T comp in go.GetComponentsInChildren<T>())
            {
                if (string.IsNullOrEmpty(name) || comp.name == name)
                    return comp;
            }
        }
        return null;
    }


}