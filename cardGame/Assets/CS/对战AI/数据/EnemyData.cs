using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Battle System/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName = "Slime";
    public int maxHp = 50;
    public Sprite artwork;

    //public object intentStrategy;

   [Header("AI")]
   public ScriptableObject intentStrategy; 
}