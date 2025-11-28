using UnityEngine;
using System.Collections.Generic;

// 解决 RoundBasedStrategy.cs 报错 CS01721: 不能有多个基类
public interface IEnemyIntentStrategy
{
    // 获取敌人下一回合的行动，返回一个预先计算好的 EnemyAction
    // 接收当前主角和回合数，以便进行复杂的意图计算
    EnemyAction GetNextAction(CharacterBase hero, int currentRound);
}