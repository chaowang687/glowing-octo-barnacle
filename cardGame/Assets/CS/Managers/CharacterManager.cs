using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("Heroes")]
    public CharacterBase activeHero;
    public List<CharacterBase> allHeroes = new List<CharacterBase>();

    [Header("Enemies")]
    public List<CharacterBase> allEnemies = new List<CharacterBase>();
    
    // 修改：ActiveEnemies 改为只读属性，动态计算
    public List<CharacterBase> ActiveEnemies { 
        get { return GetAllEnemies(); } 
    }

    private void Awake()
    {
        // 修复单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保跨场景不销毁
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"销毁重复的CharacterManager实例: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        ClearExtraListeners();
        
        // 确保列表初始化
        if (allHeroes == null) allHeroes = new List<CharacterBase>();
        if (allEnemies == null) allEnemies = new List<CharacterBase>();
    }

    private void OnLevelLog(int level)
    {
        ClearExtraListeners();
    }

    private void ClearExtraListeners()
    {
        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();

        if (allListeners.Length > 1)
        {
            Debug.Log($"<color=yellow>[Audio]</color> 检测到 {allListeners.Length} 个监听器，正在执行清理...");
            
            for (int i = 0; i < allListeners.Length; i++)
            {
                if (i > 0) 
                {
                    allListeners[i].enabled = false;
                    Debug.Log($"已禁用多余监听器：{allListeners[i].gameObject.name}");
                }
            }
        }
    }

    /// <summary>
    /// 获取当前活跃的主角。
    /// </summary>
    public CharacterBase GetActiveHero()
    {
        return activeHero != null && activeHero.currentHp > 0 ? activeHero : null;
    }

    /// <summary>
    /// 获取所有活着的英雄列表。
    /// </summary>
    public List<CharacterBase> GetAllHeroes()
    {
        return allHeroes.Where(h => h != null && h.currentHp > 0).ToList();
    }

    /// <summary>
    /// 获取所有活着的敌人列表。
    /// </summary>
    public List<CharacterBase> GetAllEnemies()
    {
        return allEnemies.Where(e => e != null && e.currentHp > 0).ToList();
    }
    
    /// <summary>
    /// 获取活跃敌人列表（与GetAllEnemies相同，为兼容性保留）
    /// </summary>
    public List<CharacterBase> GetActiveEnemies()
    {
        return GetAllEnemies();
    }
    
    /// <summary>
    /// 添加英雄到管理列表
    /// </summary>
    public void RegisterHero(Hero hero)
    {
       if (hero == null) return;
    this.activeHero = hero; // 确保这里赋值了
    Debug.Log("英雄已注册到 CharacterManager");
    }
    
    /// <summary>
    /// 添加敌人到管理列表
    /// </summary>
    public void RegisterEnemy(CharacterBase enemy)
    {
        if (enemy != null && !allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
            Debug.Log($"注册敌人: {enemy.characterName}");
        }
    }
    
    /// <summary>
    /// 从管理列表移除角色
    /// </summary>
    public void UnregisterCharacter(CharacterBase character)
    {
        if (character == null) return;
        
        if (allHeroes.Contains(character))
        {
            allHeroes.Remove(character);
            if (activeHero == character) activeHero = null;
        }
        
        if (allEnemies.Contains(character))
        {
            allEnemies.Remove(character);
        }
    }

    /// <summary>
    /// 递减指定列表中所有角色的格挡持续时间。
    /// </summary>
    public void DecrementSpecificGroupBlockDurations(List<CharacterBase> characters)
    {
        if (characters == null) return;
        
        foreach (var character in characters.Where(c => c != null))
        {
            character.DecrementBlockDuration(); 
        }
    }

    /// <summary>
    /// 【必须实现】由 BattleManager 在回合结束时调用，以递减并清除过期的格挡。
    /// </summary>
    public void DecrementAllBlockDurations()
    {
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            character.DecrementBlockDuration();
        }
    }

    /// <summary>
    /// 确保所有活着的角色触发回合开始钩子。
    /// </summary>
    public void AtStartOfTurn()
    {
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            character.AtStartOfTurn();
        }
    }

    /// <summary>
    /// 确保所有活着的角色触发回合结束钩子。
    /// </summary>
    public void AtEndOfTurn()
    {
        foreach (var character in allHeroes.Concat(allEnemies).Where(c => c != null && c.currentHp > 0))
        {
            character.AtEndOfTurn();
        }
    }
}