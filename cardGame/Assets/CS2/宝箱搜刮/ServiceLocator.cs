// ServiceLocator.cs
using System;
using System.Collections.Generic;
using System.Linq; // 添加这个命名空间
using UnityEngine;

public class ServiceLocator : MonoBehaviour
{
    private static ServiceLocator _instance;
    private Dictionary<Type, object> services = new Dictionary<Type, object>();
    
    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ServiceLocator>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("ServiceLocator");
                    _instance = go.AddComponent<ServiceLocator>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        AutoRegisterServices();
    }
    
    private void AutoRegisterServices()
    {
        // 注释掉 AudioManager 相关代码，如果你还没有创建 AudioManager
        // AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        // if (audioManager != null)
        // {
        //     RegisterService<AudioManager>(audioManager);
        // }
        
        // UIManager uiManager = FindFirstObjectByType<UIManager>();
        // if (uiManager != null)
        // {
        //     RegisterService<UIManager>(uiManager);
        // }
        
        // 注册 InventoryManager - 修复接口查找
        RegisterServiceByInterface<ScavengingGame.IInventoryService, ScavengingGame.InventoryManager>();
    }
    
    // 新增方法：通过接口类型查找实现类
    private void RegisterServiceByInterface<TInterface, TImplementation>()
        where TImplementation : MonoBehaviour, TInterface
    {
        TImplementation implementation = FindFirstObjectByType<TImplementation>();
        if (implementation != null)
        {
            RegisterService<TInterface>(implementation);
            Debug.Log($"{typeof(TInterface).Name} 已注册（通过 {typeof(TImplementation).Name}）");
        }
        else
        {
            Debug.LogWarning($"未找到 {typeof(TImplementation).Name} 的实例");
        }
    }
    
    public void RegisterService<T>(T service)
    {
        Type type = typeof(T);
        
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"服务 {type.Name} 已注册，将被覆盖");
            services[type] = service;
        }
        else
        {
            services.Add(type, service);
            Debug.Log($"服务注册成功: {type.Name}");
        }
    }
    
    public T GetService<T>()
    {
        Type type = typeof(T);
        
        if (services.TryGetValue(type, out object service))
        {
            return (T)service;
        }
        
        // 修复：改为查找实现该接口的MonoBehaviour
        if (type.IsInterface)
        {
            // 使用Linq查找所有实现该接口的MonoBehaviour
            MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            MonoBehaviour found = allMonoBehaviours.FirstOrDefault(mb => mb is T) as MonoBehaviour;
            
            if (found != null)
            {
                RegisterService<T>((T)(object)found);
                return (T)(object)found;
            }
        }
        else
        {
            // 如果是具体类型，使用原来的查找方式
            MonoBehaviour monoService = FindFirstObjectByType(type) as MonoBehaviour;
            if (monoService != null)
            {
                RegisterService<T>((T)(object)monoService);
                return (T)(object)monoService;
            }
        }
        
        Debug.LogWarning($"未找到服务: {type.Name}");
        return default;
    }
    
    public bool HasService<T>()
    {
        Type type = typeof(T);
        return services.ContainsKey(type);
    }
    
    public void UnregisterService<T>()
    {
        Type type = typeof(T);
        
        if (services.ContainsKey(type))
        {
            services.Remove(type);
            Debug.Log($"服务移除: {type.Name}");
        }
    }
    
    public void LogAllServices()
    {
        Debug.Log("=== 已注册的服务 ===");
        foreach (var kvp in services)
        {
            Debug.Log($"{kvp.Key.Name}: {kvp.Value.GetType().Name}");
        }
        Debug.Log("==================");
    }
}