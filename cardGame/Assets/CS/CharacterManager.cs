using UnityEngine;
using System.Collections.Generic;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("Hero Setup")]
    public CharacterBase activeHero; // 当前玩家控制的主角
    public List<CharacterBase> allHeroes = new List<CharacterBase>();

    [Header("Enemy Setup")]
    public List<CharacterBase> allEnemies = new List<CharacterBase>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public CharacterBase GetActiveHero() => activeHero;
    public List<CharacterBase> GetAllEnemies() => allEnemies;
    public List<CharacterBase> GetAllHeroes() => allHeroes;

    public void ClearAllBlocks()
    {
        foreach (var hero in allHeroes)
        {
            hero.ClearBlock();
        }
        foreach (var enemy in allEnemies)
        {
            enemy.ClearBlock();
        }
    }
}