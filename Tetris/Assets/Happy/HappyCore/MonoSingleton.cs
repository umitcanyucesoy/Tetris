using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    public static bool IsInstanceExist => isInstanceExist;
    private static bool isInstanceExist = false;


    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>(true);
                if (instance == null)
                {
                    Debug.LogError($"Cannot find the instance of {typeof(T).FullName}");
                    return null;
                }
            }

            return instance;
        }
    }

    protected void Awake()
    {
        isInstanceExist = false;
        
        if (instance == null)
        {
            instance = this as T;
            isInstanceExist = true;

        }
        else if (instance != this)
        {
            Debug.LogError($"Duplicate instance of {typeof(T).FullName}");
            isInstanceExist = true;
        }
        
    }

    private void OnDestroy()
    {
    }

    private void OnApplicationQuit()
    {
        isInstanceExist = false;

        instance = null;
    }
}