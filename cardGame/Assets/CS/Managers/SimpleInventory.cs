using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimpleInventory : MonoBehaviour 
{
    [Header("物资设置")]
    [Tooltip("代表背包格子，True表示有物资，False表示被抢夺")]
    public List<bool> inventorySlots = new List<bool> { true, true, true, true, true };

    // 缓存对英雄组件的引用（可选，用于扩展逻辑）
    private Hero _hero;

    private void Awake()
    {
        _hero = GetComponent<Hero>();
    }

    /// <summary>
    /// 被海盗掠夺一格物资
    /// </summary>
    /// <returns>返回被抢夺的索引，如果没有物资可抢返回-1</returns>
    public int TakeRandomItem()
    {
        // 找到所有还亮着的（true）格子索引
        var availableIndices = inventorySlots
            .Select((val, index) => new { val, index })
            .Where(x => x.val)
            .Select(x => x.index)
            .ToList();

        if (availableIndices.Count == 0) return -1;

        // 随机选一个抢走
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        inventorySlots[randomIndex] = false;

        Debug.Log($"<color=red>[物资损失]</color> 格子 {randomIndex} 的物资被抢走了！");
        
        // TODO: 在这里触发 UI 刷新的事件
        return randomIndex;
    }

    /// <summary>
    /// 拿回物资
    /// </summary>
    public void RecoverItems(int amount)
    {
        int recovered = 0;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (recovered >= amount) break;
            if (!inventorySlots[i])
            {
                inventorySlots[i] = true;
                recovered++;
            }
        }
        Debug.Log($"<color=green>[物资夺回]</color> 成功找回了 {recovered} 件物资！");
    }
}