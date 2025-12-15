using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ScavengingGame;
public class UI_ChestLootWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject windowPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button collectAllButton;
    [SerializeField] private Transform lootSlotsContainer;
    [SerializeField] private Text chestInfoText;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject lootSlotPrefab;
    
    [Header("Components")]
    [SerializeField] private LootGenerator lootGenerator;
    [SerializeField] private LootAnimationManager animationManager;
    
    private ChestData currentChestData;
    private List<LootSlot> lootSlots = new List<LootSlot>();
    private bool isSearching = false;
    
    private void Awake()
    {
        // 初始化UI事件
        closeButton.onClick.AddListener(CloseWindow);
        collectAllButton.onClick.AddListener(CollectAllItems);
        
        // 初始化物品槽
        InitializeLootSlots();
        
        // 默认隐藏窗口
        windowPanel.SetActive(false);
    }
    
    private void InitializeLootSlots()
    {
        // 清空现有槽
        foreach (Transform child in lootSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        lootSlots.Clear();
        
        // 创建5个物品槽
        for (int i = 0; i < 5; i++)
        {
            GameObject slotObj = Instantiate(lootSlotPrefab, lootSlotsContainer);
            LootSlot slot = slotObj.GetComponent<LootSlot>();
            slot.OnItemCollected += OnItemCollected;
            lootSlots.Add(slot);
        }
    }
    
    public void OpenChest(ChestData chestData)
    {
        currentChestData = chestData;
        
        // 更新UI信息
        if (chestInfoText != null)
        {
            chestInfoText.text = $"{chestData.chestType}宝箱 (等级{chestData.chestLevel})";
        }
        
        // 重置所有槽
        foreach (var slot in lootSlots)
        {
            slot.ResetSlot();
        }
        
        // 生成战利品
        List<LootItemData> lootItems = lootGenerator.GenerateLoot(chestData);
        
        // 分配战利品到槽
        for (int i = 0; i < Mathf.Min(lootItems.Count, lootSlots.Count); i++)
        {
            lootSlots[i].Initialize(lootItems[i]);
        }
        
        // 显示窗口
        windowPanel.SetActive(true);
        
        // 开始搜索动画
        StartSearchAnimation();
    }
    
    private void StartSearchAnimation()
    {
        if (isSearching) return;
        
        isSearching = true;
        collectAllButton.interactable = false;
        
        // 播放查找动画序列
        animationManager.PlaySearchAnimationSequence(lootSlots, () =>
        {
            // 动画完成
            isSearching = false;
            collectAllButton.interactable = true;
        });
    }
    
    private void CollectAllItems()
    {
        if (isSearching)
        {
            Debug.Log("还在查找物品中...");
            return;
        }
        
        // 收集所有已显示物品
        foreach (var slot in lootSlots)
        {
            if (!slot.IsEmpty && slot.IsRevealed)
            {
                slot.CollectItem();
            }
        }
    }
    
    private void OnItemCollected(LootSlot slot)
    {
        // 单个物品被收集后的处理
        // 可以在这里更新UI或播放音效
        
        // 检查是否所有物品都已收集
        CheckIfAllCollected();
    }
    
    private void CheckIfAllCollected()
    {
        bool allCollected = true;
        foreach (var slot in lootSlots)
        {
            if (!slot.IsEmpty && slot.IsRevealed)
            {
                allCollected = false;
                break;
            }
        }
        
        if (allCollected)
        {
            // 所有物品已收集，可以自动关闭窗口或显示提示
            Debug.Log("所有物品已收集!");
            
            // 可选：延迟关闭窗口
            // Invoke(nameof(CloseWindow), 1f);
        }
    }
    
    private void CloseWindow()
    {
        windowPanel.SetActive(false);
    }
    
    // 外部调用打开宝箱
    public static void OpenChestWindow(ChestData chestData)
    {
        // 查找或创建UI实例
        // 修复：使用 FindFirstObjectByType 替代 FindObjectOfType
        UI_ChestLootWindow instance = FindFirstObjectByType<UI_ChestLootWindow>();
        if (instance == null)
        {
            Debug.LogError("UI_ChestLootWindow not found in scene!");
            return;
        }
        
        instance.OpenChest(chestData);
    }
}