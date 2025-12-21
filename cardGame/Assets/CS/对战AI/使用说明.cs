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






这份代码说明书将帮助你理清目前整套类《杀戮尖塔》战斗框架的逻辑。对于独立开发来说，理解这套-数据驱动（Data-Driven）的架构至关重要，因为它能让你在不改动核心代码的情况下，通过配置资源快速增加新的海盗或卡牌。
📂 第一部分：Unity 架构分类
你的代码在 Unity 中逻辑上被分为以下 5 大核心类别：
1. 核心流程与数据中枢 (Managers)
这些脚本通常以“单例”形式存在，是战斗的“大脑”，负责连接数据和 UI。
GameFlowManager.cs: 战斗总导演。负责在游戏开始时生成玩家、生成敌人、绑定 UI 容器。
BattleManager.cs: 回合制核心驱动器。管理“玩家回合”与“敌人回合”的切换，判断战斗胜负。
CharacterManager.cs: 角色仓库。实时记录场上有多少个英雄、多少个敌人，方便卡牌寻找目标。
2. 战斗实体逻辑 (Entities)
定义了战斗中“人”的行为。
CharacterBase.cs: 最基础的数据类。处理生命值、格挡、死亡事件以及状态效果（Buff/Debuff）。
Hero.cs / Enemy.cs: 继承自基类，分别处理英雄特有的“能量/卡组”逻辑和敌人特有的“AI 决策”逻辑。
3. 卡牌驱动系统 (Card System)
处理卡牌从手牌到打出的全过程。
CardData.cs / CardAction.cs: 核心配置。定义卡牌的费用、名字和它具体干了什么（伤害、格挡等）。
CardSystem.cs: 牌库管理员。负责洗牌、抽牌、弃牌逻辑以及能量管理。
CardDisplay.cs / CardVisualManager.cs: 视觉表现。负责卡牌在屏幕上的扇形排布、拖拽手感和飞向目标的动画。
4. 敌人 AI 与意图 (AI & Intent)
这部分是实现“海盗机制”的关键。
EnemyAI.cs: 动作执行器。负责根据策略，让海盗头顶显示图标（意图）并实际执行动作。
RoundBasedStrategy.cs: 策略脚本。让你可以配置海盗的固定套路（如：第1轮抢劫，第2轮逃跑）。
IntentIconConfig.cs: 图标字典。根据意图类型找到对应的 Sprite 图片。
5. 视图与反馈 (View / UI)
负责把逻辑数值变成玩家看得见的东西。
EnemyDisplay.cs / CharacterUIDisplay.cs: 更新血条、格挡数值。
CharacterAnimatorController.cs: 动画开关。控制角色在攻击、受击、死亡时的 3D/2D 动画切换。
DeathRelay.cs: 桥梁。当死亡动画播完时，告诉系统可以正式移除这个对象了。
🛠️ 第二部分：核心逻辑流向图
当你从手牌中打出一张卡时，代码是这样流动的：
用户交互: CardDisplay 监测到拖拽释放。
合法性检查: BattleManager 调用 CardSystem 检查能量够不够。
效果执行: CardData 遍历 CardAction 列表，对 Enemy 调用 TakeDamage()。
数据更新: CharacterBase 扣除生命值。
视图同步: CharacterBase 触发事件，CharacterUIDisplay 自动刷新血条。
🏴‍☠️ 第三部分：如何在此基础上开发“海盗功能”？
既然你已经理解了结构，开发海盗机制只需修改以下 3 点：
1. 修改 Enums.cs (定义新行为)
在 IntentType 中增加 Loot（掠夺）和 Escape（逃跑）。
2. 编写 InventoryManager.cs (核心物资系统)
这是一个新脚本，挂在 Hero 身上。
方法 StolenItem()：随机把背包里一个物品设为“被抢”。
方法 ReturnItems()：战斗胜利后把被抢物品还回来。
3. 在 EnemyAI.cs 插入执行逻辑
在 PerformAction 方法的 switch 语句中添加：
Case Loot: 调用玩家的 StolenItem()。
Case Escape: 播放烟雾弹效果并 Destroy(gameObject)。
💡 给独立开发的建议：
善用 ScriptableObject: 你的 CardData 和 EnemyData 都是这个类型。这意味着你可以在 Unity 窗口右键创建几百种不同的海盗，而不需要写一行新代码。
关注事件订阅: 你的代码大量使用了 Action 事件（如 OnHpChanged）。在 Unity 里测试时，如果报错“NullReference”，通常是因为角色销毁了但 UI 没有取消订阅。

*/