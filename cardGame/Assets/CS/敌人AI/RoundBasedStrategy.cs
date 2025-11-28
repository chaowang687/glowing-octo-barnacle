using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RoundBasedStrategy", menuName = "Battle System/Enemy AI/Round Based Strategy")]
// 现在继承自 ScriptableObject 并实现接口，解决了 CS1721 错误
public class RoundBasedStrategy : ScriptableObject, IEnemyIntentStrategy
{
    [Tooltip("每回合对应的行动，按顺序执行")]
    public List<EnemyAction> roundActions = new List<EnemyAction>();

    public EnemyAction GetNextAction(CharacterBase hero, int currentRound)
    {
        // 确保回合数从 1 开始
        int index = (currentRound - 1) % roundActions.Count;
        
        if (roundActions.Count == 0)
        {
            Debug.LogWarning("RoundBasedStrategy has no actions defined. Defaulting to NONE.");
            // 解决了 IntentType 成员不存在的错误 (例如 'ATTACK')
            return new EnemyAction { intentType = IntentType.NONE, value = 0 }; 
        }

        return roundActions[index];
    }
}