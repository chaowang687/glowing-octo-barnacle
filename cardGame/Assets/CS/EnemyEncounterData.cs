using UnityEngine;
using System.Collections.Generic;
using SlayTheSpireMap; // 确保引用了地图插件的命名空间

[CreateAssetMenu(fileName = "NewEncounter", menuName = "Battle/EncounterData")]
public class EnemyEncounterData : EncounterData
{
   // public string encounterName; // 战斗名称（如：海盗小队）
    
    [Tooltip("这场战斗中会出现的敌人配置列表")]
    public List<EnemyData> enemyList; 
    
    [Tooltip("可选：战斗背景图")]
    public Sprite battleBackground;
}