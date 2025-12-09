using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 确保导入 Linq 以支持 Where().ToList() 和 Concat()

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; } // 添加单例模式

    [Header("Heroes")]
    public CharacterBase activeHero;
    public List<CharacterBase> allHeroes = new List<CharacterBase>();

    [Header("Enemies")]
    public List<CharacterBase> allEnemies = new List<CharacterBase>();
    // ⭐ 修复 CS1061：添加 ActiveEnemies 属性 (用于兼容旧代码，但推荐使用 GetAllEnemies()) ⭐
    public List<CharacterBase> ActiveEnemies { get; private set; } = new List<CharacterBase>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 获取当前活跃的主角。
    /// </summary>
    /// <returns>活着的 CharacterBase 实例，否则返回 null。</returns>
    public CharacterBase GetActiveHero()
    {
        // 确保返回活着的英雄
        return activeHero != null && activeHero.currentHp > 0 ? activeHero : null;
    }

    /// <summary>
    /// 获取所有活着的英雄列表。
    /// </summary>
    public List<CharacterBase> GetAllHeroes()
    {
        // 确保只返回活着的英雄
        return allHeroes.Where(h => h != null && h.currentHp > 0).ToList();
    }

    /// <summary>
    /// 获取所有活着的敌人列表。
    /// </summary>
    public List<CharacterBase> GetAllEnemies()
    {
        // 确保只返回活着的敌人
        return allEnemies.Where(e => e != null && e.currentHp > 0).ToList();
    }
    
    // ----------------------------------------------------------------------------------
    // ⭐ 核心修正区域：持久化格挡与回合钩子 ⭐
    // ----------------------------------------------------------------------------------

    // ❌ 旧的 ClearAllBlocks() 方法已被删除，以避免调用不存在的 ClearBlock() 方法，并强制使用新的持久化系统。
    /* public void ClearAllBlocks()
    {
        // 逻辑已迁移到 DecrementAllBlockDurations() 中
    }
    */

    /// <summary>
    /// 【必须实现】由 BattleManager 在回合结束时调用，以递减并清除过期的格挡。
    /// </summary>
    public void DecrementAllBlockDurations()
    {
        // 遍历所有英雄和敌人，过滤掉 null 或已死亡的角色，调用 CharacterBase.DecrementBlockDuration()
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            // ⭐ 调用 CharacterBase 中新的格挡清除逻辑 ⭐
            character.DecrementBlockDuration();
        }
    }

    /// <summary>
    /// 确保所有活着的角色触发回合开始钩子。
    /// </summary>
    public void AtStartOfTurn()
    {
        // 遍历所有活着的角色
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            // ⭐ 调用 CharacterBase 上的 AtStartOfTurn 逻辑 ⭐
            character.AtStartOfTurn();
        }
    }

    /// <summary>
    /// 确保所有活着的角色触发回合结束钩子。
    /// </summary>
    public void AtEndOfTurn()
    {
        // 遍历所有活着的角色
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            // ⭐ 调用 CharacterBase 上的 AtEndOfTurn 逻辑 ⭐
            character.AtEndOfTurn();
        }
    }
}