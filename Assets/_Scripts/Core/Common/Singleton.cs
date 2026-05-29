using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static readonly object syncRoot = new object();

    private static bool isApplicationQuitting;
    private static bool isInitialized;
    private static bool hasCalledOnSingletonInitialized;

    public static T Instance
    {
        get
        {
            if (isApplicationQuitting)
            {
                return null;
            }

            lock (syncRoot)
            {
                if (instance != null)
                {
                    return instance;
                }

                T[] instances = FindObjectsByType<T>(FindObjectsInactive.Include);

                if (instances == null || instances.Length == 0)
                {
                    Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Instance was requested, but no scene object was found.");
                    return null;
                }

                instance = instances[0];

                if (instances.Length > 1)
                {
                    Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Multiple instances found. Keeping '{instance.gameObject.name}' and destroying extras.");

                    for (int i = 1; i < instances.Length; i++)
                    {
                        if (instances[i] == null)
                        {
                            continue;
                        }

                        Destroy(instances[i].gameObject);
                    }
                }

                InitializeSingleton(instance);

                return instance;
            }
        }
    }

    public static bool IsInitialized => isInitialized && instance != null;

    protected virtual void Awake()
    {
        lock (syncRoot)
        {
            if (instance == null)
            {
                instance = this as T;
                InitializeSingleton(instance);
                return;
            }

            if (instance != this)
            {
                Debug.LogWarning($"[Singleton<{typeof(T).Name}>] Duplicate instance destroyed: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            InitializeSingleton(instance);
        }
    }

    private static void InitializeSingleton(T targetInstance)
    {
        if (targetInstance == null)
        {
            return;
        }

        isInitialized = true;

        ApplyDontDestroyOnLoad(targetInstance.gameObject);

        if (hasCalledOnSingletonInitialized)
        {
            return;
        }

        hasCalledOnSingletonInitialized = true;

        if (targetInstance is Singleton<T> singleton)
        {
            singleton.OnSingletonInitialized();
        }
    }

    private static void ApplyDontDestroyOnLoad(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        GameObject root = target.transform.root.gameObject;

        if (IsDontDestroyOnLoad(root))
        {
            return;
        }

        DontDestroyOnLoad(root);
    }

    private static bool IsDontDestroyOnLoad(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        return target.scene.name == "DontDestroyOnLoad";
    }

    protected virtual void OnSingletonInitialized()
    {
    }

    protected virtual void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        instance = null;
        isInitialized = false;
        hasCalledOnSingletonInitialized = false;
    }

    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    public virtual void PostInitialize()
    {
    }

    public virtual void ExecutePostInitialization()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[Singleton<{typeof(T).Name}>] PostInitialize was requested before initialization.");
            return;
        }

        PostInitialize();
    }
}