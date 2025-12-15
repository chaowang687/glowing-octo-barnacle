// ServiceLocator.cs
using System;
using System.Collections.Generic;
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
                // 修复：使用 FindFirstObjectByType 替代 FindObjectOfType
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
        
        // 注册 InventoryManager
        ScavengingGame.InventoryManager inventoryManager = FindFirstObjectByType<ScavengingGame.InventoryManager>();
        if (inventoryManager != null)
        {
            RegisterService<ScavengingGame.IInventoryService>(inventoryManager);
            Debug.Log("InventoryService 已注册到 ServiceLocator");
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
        
        // 修复：使用 FindFirstObjectByType 替代 FindObjectOfType
        MonoBehaviour monoService = FindFirstObjectByType(typeof(T)) as MonoBehaviour;
        if (monoService != null)
        {
            RegisterService<T>((T)(object)monoService);
            return (T)(object)monoService;
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