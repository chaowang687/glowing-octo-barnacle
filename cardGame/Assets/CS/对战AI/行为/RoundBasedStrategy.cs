using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RoundBasedStrategy", menuName = "Battle System/Enemy AI/Round Based Strategy")]
// 现在继承自 ScriptableObject 并实现接口，解决了 CS1721 错误
public class RoundBasedStrategy : ScriptableObject, IEnemyIntentStrategy
{
[Tooltip("每回合对应的行动，按顺序执行")]
    public List<EnemyAction> roundActions = new List<EnemyAction>();

    // ⭐ 核心修正：将方法名从 CalculateNextAction 改为 GetNextAction ⭐
    public EnemyAction GetNextAction(CharacterBase hero, int currentRound)
    {
        // 确保回合数从 1 开始
        
        if (roundActions.Count == 0)
        {
            Debug.LogWarning("RoundBasedStrategy has no actions defined. Defaulting to NONE.");
            return new EnemyAction { intentType = IntentType.NONE, value = 0 }; 
        }

        return roundActions[(currentRound - 1) % roundActions.Count];
    }
}