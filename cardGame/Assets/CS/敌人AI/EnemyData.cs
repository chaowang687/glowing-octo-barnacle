using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Battle System/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Slime";
    public int maxHp = 50;
    public Sprite artwork;

    [Header("AI")]
    // 解决 CS0246 错误: IEnemyIntentStrategy 接口
    public ScriptableObject intentStrategy; 
}