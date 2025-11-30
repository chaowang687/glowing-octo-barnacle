using UnityEngine;

public class 使用说明 : MonoBehaviour
{}
/*
正确的配置流程 (再次强调)
为了确保 AI 功能能够正常运行，您必须确保从上到下的引用链是完整的：

策略资产：您创建了 RoundBasedStrategy 资产，并在其中配置了敌人的行动序列（攻击、格挡等）。

数据引用 (关键一步)：您创建了 EnemyData 资产，并在 Inspector 中将 策略资产（RoundBasedStrategy） 拖入 EnemyData 资产的 intentStrategy 字段。

脚本引用：您将配置好的 数据资产（EnemyData） 拖入到场景中敌人预制体（挂载了 EnemyAI.cs）的 enemyData 字段。

 

EnemyData.cs
数据存储 (AI 的“配置表”)存储敌人的基础属性（HP、名称）和决策策略的引用。它定义了敌人的所有可能行动列表。
1. 在 Unity 中创建此类型的 ScriptableObject 资产（如 Create/Enemy System/Enemy Data）
2. 配置 maxHp, startingActions (起始回合动作) 和 roundActions (循环动作)。 
3. 将一个 RoundBasedStrategy 资产拖入 intentStrategy 字段。
IEnemyIntentStrategy.cs决策接口 (AI 的“蓝图”)定义了所有敌人 AI 策略必须遵循的规范：提供一个 GetNextAction() 方法来计算下一回合的行动。
这是一个接口，不需要挂载到任何 GameObject 上。它用于强制执行 AI 策略的结构。RoundBasedStrategy.cs具体策略逻辑 (AI 的“大脑”)实现了 IEnemyIntentStrategy 接口，
提供了最基础的 基于回合数的循环 决策逻辑（类似于《杀戮尖塔》）。
1. 在 Unity 中创建此类型的 ScriptableObject 资产（如 Create/Enemy System/Round Based Strategy）。 
2. 在 Inspector 中，必须将此资产拖入 EnemyData.cs 的 intentStrategy 字段。
EnemyAI.cs 
(已提供的最新版本)行动驱动 (AI 的“执行者”)这是一个 MonoBehaviour 组件，负责在游戏运行时：
1. 调用策略计算意图 (CalculateIntent)。
2. 执行意图 (PerformAction)，将伤害、格挡、状态效果施加给目标。
必须挂载到您的 Enemy_X_Prefab 上。并将对应的 EnemyData 资产拖入其 enemyData 字段。
*/