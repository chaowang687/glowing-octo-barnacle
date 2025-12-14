using UnityEngine;
using System.Collections.Generic;
using TMPro;
using ScavengingGame; // 【修复 1】：添加 GameStateManager 所在的命名空间


/// <summary>
/// 地图格子类型
/// </summary>
public enum NodeType { 
    Ground,     // 地面格子 (装饰用，不可停留)
    Route,      // 路线格子 (可停留，功能默认)
    Combat,     // 战斗格子
    Treasure,   // 宝箱格子
    Shop,       // 商店格子
    Exit,       // 撤离点
    Boss        // Boss 战
}

/// <summary>
/// 轴测地图节点：定义格子类型、连接和视觉表现。
/// 挂载在每个棱形格子的 GameObject 上。
/// </summary>
public class IsometricMapNode : MonoBehaviour
{
    // 每个节点的唯一标识符，用于 GameStateManager 跟踪位置
    public int NodeId; 
    
    [Header("格子配置")]
    public NodeType Type = NodeType.Route;
    // 下一个可能的节点列表 (用于分岔路和路线定义)
    public List<IsometricMapNode> NextNodes = new List<IsometricMapNode>(); 
    
    [Header("视觉元素")]
    [Tooltip("用于显示格子图标的文本组件 (如 Combat, Treasure)")]
    public TextMeshPro IconDisplay; 
    private NodeType _currentType;
    void Start()
    {
        // 确保启动时更新图标
        _currentType = Type;
        UpdateVisuals();
    }
    void OnValidate()
    {
        // 在编辑器下，当类型改变时更新显示
        if (Type != _currentType)
        {
            UpdateVisuals();
            _currentType = Type;
        }
    }
    /// <summary>
    /// 根据节点类型更新图标和视觉效果。
    /// 假设图标使用 Emoji 或 TextMeshPro 符号代替图片。
    /// </summary>
    public void UpdateVisuals()
    {
        string icon = "";
        bool showIcon = false;

        switch (Type)
        {
            case NodeType.Route:
                // Route 节点通常不显示图标，但可以显示一个占位符或序号
                showIcon = false;
                break;
            case NodeType.Combat:
                icon = "⚔️"; // 战斗图标
                showIcon = true;
                break;
            case NodeType.Treasure:
                icon = "🎁"; // 宝箱图标
                showIcon = true;
                break;
            case NodeType.Shop:
                icon = "💰"; // 商店图标
                showIcon = true;
                break;
            case NodeType.Exit:
                icon = "🚪"; // 撤离图标
                showIcon = true;
                break;
            case NodeType.Boss:
                icon = "💀"; // Boss图标
                showIcon = true;
                break;
        }

        if (IconDisplay != null)
        {
            IconDisplay.text = icon;
            IconDisplay.gameObject.SetActive(showIcon);
        }
    }

    /// <summary>
    /// 触发格子效果。由 MapGridManager 在玩家移动完成后调用。
    /// </summary>
    /// <param name="gridManager">传入调用此方法的 MapGridManager 实例。</param>
    public void ActivateNode(MapGridManager gridManager)
{
    if (Type == NodeType.Ground)
    {
        gridManager.OnEventFinished();
        return;
    }

    switch (Type)
    {
        case NodeType.Route: // 普通路线
            Debug.Log($"Node {NodeId}: 触发路线事件。");
            gridManager.ProcessRandomRouteEvent(); // 调用新方法
            break;

        case NodeType.Treasure: // 宝箱
            Debug.Log($"Node {NodeId}: 打开宝箱！");
            // 1. 生成并发放丰厚奖励
            List<ItemData> treasureRewards = RewardManager.GenerateRandomRewards();
            RewardManager.GrantRewardsToPlayer(treasureRewards, GameStateManager.Instance.PlayerInventory);
            // 2. TODO: 播放宝箱打开动画或UI
            // 3. 奖励发放完毕后，结束事件
            gridManager.OnEventFinished();
            break;

        case NodeType.Combat:
        case NodeType.Boss:
            Debug.Log($"Node {NodeId}: 触发强制战斗！");
            GameStateManager.Instance.InitiateBattle();
            break;

        case NodeType.Shop:
            Debug.Log($"Node {NodeId}: 进入商店。");
            // TODO: 打开商店UI，UI关闭后回调 gridManager.OnEventFinished();
            gridManager.OnEventFinished(); // 临时：立即关闭
            break;

        case NodeType.Exit:
            Debug.Log($"Node {NodeId}: 到达撤离点！");
            // TODO: 触发胜利条件
            gridManager.OnEventFinished();
            break;

        default:
            gridManager.OnEventFinished();
            break;
    }
}

}