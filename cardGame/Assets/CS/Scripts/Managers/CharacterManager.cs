using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 确保导入 Linq 以支持 Where().ToList()

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; } // 添加单例模式

    [Header("Heroes")]
    public CharacterBase activeHero;
    public List<CharacterBase> allHeroes = new List<CharacterBase>();

    [Header("Enemies")]
    public List<CharacterBase> allEnemies = new List<CharacterBase>();

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
    
    /// <summary>
    /// 战斗回合结束时调用，清除所有角色的格挡。
    /// </summary>
    public void ClearAllBlocks()
    {
        foreach (var hero in allHeroes)
        {
            if(hero != null) hero.ClearBlock();
        }
        
        foreach (var enemy in allEnemies)
        {
            if(enemy != null) enemy.ClearBlock();
        }
    }
}