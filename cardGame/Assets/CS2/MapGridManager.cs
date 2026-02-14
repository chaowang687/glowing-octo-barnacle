
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using ScavengingGame;

/// <summary>
/// 地图流程管理器：负责连接所有节点、处理骰子移动、路径选择和格子激活。
/// 必须挂载在地图场景中。
/// </summary>
public class MapGridManager : MonoBehaviour
{
    private Coroutine _currentMovementCoroutine;
    
    [Header("系统引用")]
    public PlayerPiece PlayerPiece; // 玩家棋子对象
    public DiceController Dice;     // 骰子控制器引用
    
    [Header("地图配置")]
    // 所有的地图节点（手动拖入或FindObjectsOfType）
    public List<IsometricMapNode> AllNodes = new List<IsometricMapNode>(); 
    // 游戏的起始节点
    public IsometricMapNode StartNode; 

    private IsometricMapNode _currentNode;
    private bool _isPathSelecting = false; // 路径选择锁，防止重复投掷或移动

    void Start()
    {
        InitializeMap();
        // 监听骰子事件
        if (Dice != null)
        {
            Dice.OnDiceRolled.AddListener(StartPlayerMovement);
            Dice.SetDiceRollEnabled(true);
        }
    }
    public void ProcessRandomRouteEvent()
    {
        // 1. 先尝试触发随机事件
        if (RandomEventManager.TryTriggerRandomEvent())
        {
            // 事件已处理，可以结束该节点
            OnEventFinished();
            return;
        }

        // 2. 如果未触发事件，则按原几率判断遭遇战
        // 注意：此处使用了简化后的ScavengingController实例中的概率，你也可以将其配置到MapGridManager或另一个静态类中。
         ScavengingController scavCtrl = GameStateManager.Instance?.ScavengingController;
        float encounterChance = (scavCtrl != null) ? scavCtrl.globalEncounterChance : 0.3f;

        if (Random.value < encounterChance)
        {
            Debug.Log("[MapGridManager] 在路线上遭遇敌人！");
            GameStateManager.Instance.InitiateBattle();
            // 战斗结束后，由GameStateManager调用OnBattleFinished
        }
        else
        {
            // 3. 安全通过，可能获得微量奖励
            Debug.Log("[MapGridManager] 路线安全，发现一些零散物资。");
            List<ItemData> minorRewards = RewardManager.GenerateRandomRewards();
            RewardManager.GrantRewardsToPlayer(minorRewards, GameStateManager.Instance.PlayerInventory);
            OnEventFinished();
        }
    }
    /// <summary>
    /// 初始化地图：定位玩家棋子到正确的节点。
    /// </summary>
    private void InitializeMap()
    {
        // 确保所有节点都已添加到列表中
        if (AllNodes.Count == 0)
        {
            // 修复：使用新的 FindObjectsByType API 替代已过时的 FindObjectsOfType
            #if UNITY_2021_3_OR_NEWER
            // Unity 2021.3 及以上版本使用新 API
            IsometricMapNode[] nodes = FindObjectsByType<IsometricMapNode>(FindObjectsSortMode.None);
            #else
            // 旧版本使用兼容的 API
            IsometricMapNode[] nodes = FindObjectsOfType<IsometricMapNode>();
            #endif
            
            AllNodes = nodes.ToList();
            if (AllNodes.Count == 0)
            {
                Debug.LogError("MapGridManager: Scene contains no IsometricMapNode components!");
                return;
            }
        }

        // 定位到起始节点
        if (StartNode != null)
        {
            _currentNode = StartNode;
            // 移动到起始节点位置（立即移动，不使用动画）
            PlayerPiece.transform.position = _currentNode.transform.position + Vector3.up * 0.1f;
        }
        else
        {
            Debug.LogError("StartNode is not set in MapGridManager!");
        }
    }

    /// <summary>
    /// 供 DiceController 调用，开始玩家移动。
    /// </summary>
    private void StartPlayerMovement(int steps)
    {
        if (_currentNode == null)
        {
            Debug.LogError("Current node is null. Cannot start movement.");
            return;
        }
        
        // 如果已有移动协程在运行，先停止它
        if (_currentMovementCoroutine != null)
        {
            StopCoroutine(_currentMovementCoroutine);
        }
        
        Debug.Log($"开始移动 {steps} 步，从 Node {_currentNode.NodeId}");
        _currentMovementCoroutine = StartCoroutine(MovePlayerStepsCoroutine(steps));
    }

    /// <summary>
    /// 协程：处理玩家移动的每一步，包括动画和平滑过渡。
    /// </summary>
    private IEnumerator MovePlayerStepsCoroutine(int stepsRemaining)
    {
        while (stepsRemaining > 0)
        {
            // 检查是否有下一节点
            IsometricMapNode nextNode = ChooseNextNode(_currentNode);
            
            if (nextNode == null)
            {
                Debug.Log("到达路线终点，移动停止。");
                break; 
            }
            
            // 检查分岔路口
            if (_currentNode.NextNodes.Count > 1)
            {
                _isPathSelecting = true;
                Debug.Log("到达分岔路口，等待玩家选择路径...");
                
                // TODO: 启用路径选择 UI
                // 这里需要实现路径选择UI逻辑
                // 例如：显示UI让玩家选择下一个节点
                // ShowPathSelectionUI(_currentNode.NextNodes, stepsRemaining);
                
                // 等待玩家选择路径
                yield return new WaitUntil(() => !_isPathSelecting);
                
                // 选择路径后，当前协程结束，新的协程将由SelectPath启动
                yield break;
            }

            // 执行移动动画
            yield return PlayerPiece.AnimateMoveTo(nextNode.transform.position);

            // 更新当前节点并减少步数
            _currentNode = nextNode;
            stepsRemaining--;
            Debug.Log($"移动到 Node {_currentNode.NodeId}，剩余 {stepsRemaining} 步。");
        }

        // 移动结束，触发终点节点事件
        Debug.Log($"移动完成。停留在 Node {_currentNode.NodeId}。触发节点事件。");
        _currentNode.ActivateNode(this);
        
        // 清理协程引用
        _currentMovementCoroutine = null;
    }
    
    /// <summary>
    /// 确定下一步移动的节点 (单分支或默认选择逻辑)。
    /// </summary>
    private IsometricMapNode ChooseNextNode(IsometricMapNode current)
    {
        if (current.NextNodes.Count == 0) return null;
        if (current.NextNodes.Count == 1) return current.NextNodes[0];
        
        // 如果有多个分支 (>1)，意味着需要玩家选择
        // 返回 null，协程会暂停等待玩家选择
        return null;
    }
    
    /// <summary>
    /// 供 UI 在分岔路口选择路径后调用。
    /// </summary>
    /// <param name="destinationNode">玩家选择的下一个节点。</param>
    public void SelectPath(IsometricMapNode destinationNode, int remainingSteps)
    {
        if (!_isPathSelecting) return;

        // 1. 设置新的当前节点为玩家选择的节点
        _currentNode = destinationNode;
        _isPathSelecting = false;
        
        // 2. 移动到选择的节点 (无动画)
        PlayerPiece.transform.position = _currentNode.transform.position + Vector3.up * 0.1f;
        
        Debug.Log($"玩家选择路径到 Node {_currentNode.NodeId}。剩余 {remainingSteps} 步继续移动。");
        
        // 3. 继续移动剩余的步数
        if (remainingSteps > 0)
        {
            // 如果已有移动协程在运行，先停止它
            if (_currentMovementCoroutine != null)
            {
                StopCoroutine(_currentMovementCoroutine);
            }
            // 注意：选择节点后已经移动了一步，所以要减1
            _currentMovementCoroutine = StartCoroutine(MovePlayerStepsCoroutine(remainingSteps - 1));
        }
        else
        {
            // 如果步数耗尽，激活当前节点事件
            _currentNode.ActivateNode(this);
        }
    }
    
    // =======================================================================
    // 事件回调 (由 IsometricMapNode 和 GameStateManager 调用)
    // =======================================================================

    /// <summary>
    /// 由 IsometricMapNode 调用，通知 GridManager 当前格子事件已完成。
    /// （用于非战斗类事件，如搜刮、商店、普通路线等）
    /// </summary>
    public void OnEventFinished()
    {
        Debug.Log("[Grid Manager] 节点事件处理完成。解锁骰子。");
        // 启用骰子，允许玩家进行下一次投掷
        if (Dice != null)
            Dice.SetDiceRollEnabled(true);
    }
    
    /// <summary>
    /// 由 GameStateManager 调用，通知 GridManager 战斗已结束。
    /// </summary>
    /// <param name="isVictory">如果战斗胜利则为 True。</param>
    public void OnBattleFinished(bool isVictory)
    {
        if (isVictory)
        {
            Debug.Log("[Grid Manager] 战斗胜利。解锁骰子，继续探索。");
            // 战斗胜利后，回到探索模式，并允许玩家投掷骰子
            if (Dice != null)
                Dice.SetDiceRollEnabled(true);
        }
        else
        {
            Debug.Log("[Grid Manager] 战斗失败。游戏结束，不解锁骰子。");
            // 游戏状态机将处理 Game Over 逻辑
        }
    }
}
