using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    var obj = new GameObject(typeof(T).Name);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null) _instance = this as T;
        else if (_instance != this) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }
}

// 런타임 시작 시 프로젝트 전체에서 Singleton<>을 상속한 타입들을 찾아 Instance를 강제 호출하여
// 누구도 Instance를 호출하지 않아도 게임 시작 시 자동으로 생기게 합니다.
internal static class SingletonAutoInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitAllSingletons()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract) continue;

                    // 상속 체인에 Singleton<>의 닫힌 타입이 있는지 확인
                    var cur = t.BaseType;
                    while (cur != null && cur != typeof(object))
                    {
                        if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typeof(Singleton<>))
                        {
                            try
                            {
                                // Singleton<ThisDerivedType> 형태의 타입을 만들어 Instance 호출
                                var singletonClosed = typeof(Singleton<>).MakeGenericType(t);
                                var prop = singletonClosed.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                                prop?.GetValue(null);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Singleton 자동 초기화 실패: {t.FullName} -> {ex.Message}");
                            }
                            break;
                        }
                        cur = cur.BaseType;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SingletonAutoInitializer 실패: {ex.Message}");
        }
    }
}