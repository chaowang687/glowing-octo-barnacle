using UnityEngine;
using System; 

/// <summary>
/// 辅助组件：将 Unity 的 OnDestroy 生命周期方法封装为公共 C# Action 事件。
/// 这样 CharacterUIDisplay 就能在角色销毁时安全地取消订阅事件，防止内存泄漏。
/// </summary>
public class DestroyListener : MonoBehaviour
{
    // 公共 Action 事件，其他类可以订阅它
    public Action onDestroy;
    
    // Unity 的内置生命周期方法
    private void OnDestroy()
    {
        // 当附加此脚本的对象被销毁时，触发事件
        onDestroy?.Invoke();
    }
}
