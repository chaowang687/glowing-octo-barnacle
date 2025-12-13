using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using ScavengingGame;

namespace ScavengingGame
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject inventoryPanel;        // 整个背包面板
        public Button closeButton;               // 关闭按钮
        public Button bgCloseButton;             // 背景关闭按钮（可选）
        public Button bagIconButton;             // 背包图标按钮
        public Transform itemGrid;               // 物品格子容器
        public GameObject itemSlotPrefab;        // 物品格子预制体
        
        [Header("新物品提示")]
        public GameObject bagIconNewItemHint;    // 新物品提示（可选）
        
        [Header("装备槽位 UI")]
        public Image weaponSlot;
        public Image armorSlot;
        public Image amulet1Slot;
        public Image amulet2Slot;
        
        [Header("状态显示")]
        public Text attackBonusText;     // 攻击加成显示（可选）
        public Text defenseBonusText;    // 防御加成显示（可选）
        
        [Header("控制设置")]
        public KeyCode toggleKey = KeyCode.I;    // 开关快捷键
        public bool closeOnBackgroundClick = true; // 点击背景关闭
        public bool allowESCToClose = true;      // 允许ESC键关闭
        
        private InventoryManager _inventory;
        private bool _isInitialized = false;
        private bool _hasNewItems = false;       // 是否有新物品提示
        
        void Start()
        {
            InitializeUI();
        }
        
        void InitializeUI()
        {
            if (_isInitialized) return;
            
            // 1. 初始隐藏背包面板
            if (inventoryPanel != null)
                inventoryPanel.SetActive(false);
            
            // 2. 设置关闭按钮事件
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseInventory);
            }
            
            // 3. 设置背景关闭（如果启用）
            if (bgCloseButton != null && closeOnBackgroundClick)
            {
                bgCloseButton.onClick.RemoveAllListeners();
                bgCloseButton.onClick.AddListener(CloseInventory);
                Color bgColor = bgCloseButton.image.color;
                bgColor.a = 0.3f;
                bgCloseButton.image.color = bgColor;
            }
            
            // 4. 设置背包图标按钮
            if (bagIconButton != null)
            {
                bagIconButton.onClick.RemoveAllListeners();
                bagIconButton.onClick.AddListener(ToggleInventory);
                Debug.Log("背包图标按钮已绑定");
            }
            else
            {
                Debug.LogWarning("背包图标按钮未设置，请检查Inspector中的引用");
            }
            
            // 5. 初始化新物品提示
            if (bagIconNewItemHint != null)
                bagIconNewItemHint.SetActive(false);
            
            _isInitialized = true;
            Debug.Log("InventoryUI 初始化完成，按 " + toggleKey + " 键或点击背包图标开关背包");
        }
        
        void Update()
        {
            if (!_isInitialized) return;
            
            // 快捷键开关背包
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
            
            // ESC键关闭背包
            if (allowESCToClose && inventoryPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }
        }
        
        /// <summary>
        /// 开关背包
        /// </summary>
        public void ToggleInventory()
        {
            if (inventoryPanel == null) return;
            
            if (inventoryPanel.activeSelf)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
        
        /// <summary>
        /// 打开背包（公开方法）
        /// </summary>
        public void OpenInventory()
        {
            if (inventoryPanel == null) return;
            
            inventoryPanel.SetActive(true);
            RefreshInventoryUI(); // 这里调用刷新方法
            
            // 打开背包时隐藏新物品提示
            HideNewItemHint();
            
            Debug.Log("背包已打开");
        }
        
        /// <summary>
        /// 关闭背包（公开方法）
        /// </summary>
        public void CloseInventory()
        {
            if (inventoryPanel == null) return;
            
            inventoryPanel.SetActive(false);
            Debug.Log("背包已关闭");
        }
        
        /// <summary>
        /// 强制刷新背包UI（外部可调用）
        /// </summary>
        public void ForceRefresh()
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                RefreshInventoryUI(); // 这里调用刷新方法
            }
        }
        
        /// <summary>
        /// 刷新背包UI（核心方法） - 这行是解决问题的关键
        /// </summary>
        private void RefreshInventoryUI()
        {
            if (GameStateManager.Instance == null || 
                GameStateManager.Instance.PlayerInventory == null)
            {
                Debug.LogWarning("无法刷新背包：InventoryManager未找到");
                return;
            }
            
            _inventory = GameStateManager.Instance.PlayerInventory;
            
            // 清空当前显示
            ClearItemGrid();
            
            // 显示所有物品
            if (_inventory.Items != null && _inventory.Items.Count > 0)
            {
                foreach (var item in _inventory.Items)
                {
                    if (item == null) continue;
                    
                    CreateItemSlot(item);
                }
            }
            else
            {
                Debug.Log("背包为空");
            }
            
            // 更新装备槽位显示
            UpdateEquipmentSlots();
            
            // 更新状态显示
            UpdateStatusDisplay();
        }
        
        private void CreateItemSlot(ItemData item)
        {
            if (itemSlotPrefab == null || itemGrid == null)
            {
                Debug.LogError("物品格子预制体或容器未设置");
                return;
            }
            
            GameObject slot = Instantiate(itemSlotPrefab, itemGrid);
            ItemSlotUI slotUI = slot.GetComponent<ItemSlotUI>();
            
            if (slotUI != null)
            {
                // 获取物品数量
                int itemCount = _inventory.GetItemCount(item);
                
                // 设置物品到格子
                slotUI.SetItem(item, itemCount);
                
                // 如果是装备，设置装备回调
                if (item is EquipmentData equipment)
                {
                    slotUI.SetEquipmentCallback(() => OnEquipmentSlotClicked(equipment));
                }
                else
                {
                    // 如果是消耗品，设置使用回调
                    slotUI.SetUseCallback(() => OnUseItemClicked(item));
                }
            }
        }
        
        private void ClearItemGrid()
        {
            if (itemGrid == null) return;
            
            // 销毁所有子物体
            for (int i = itemGrid.childCount - 1; i >= 0; i--)
            {
                Destroy(itemGrid.GetChild(i).gameObject);
            }
        }
        
        private void UpdateEquipmentSlots()
        {
            if (_inventory == null) return;
            
            var equipped = _inventory.GetAllEquippedItems();
            
            // 更新武器槽
            UpdateEquipmentSlot(weaponSlot, 
                equipped.TryGetValue(EquipmentData.SlotType.Weapon, out EquipmentData weapon) ? weapon : null);
            
            // 更新护甲槽
            UpdateEquipmentSlot(armorSlot, 
                equipped.TryGetValue(EquipmentData.SlotType.Armor, out EquipmentData armor) ? armor : null);
            
            // 更新护符槽位
            UpdateEquipmentSlot(amulet1Slot,
                equipped.TryGetValue(EquipmentData.SlotType.Amulet1, out EquipmentData amulet1) ? amulet1 : null);
                
            UpdateEquipmentSlot(amulet2Slot,
                equipped.TryGetValue(EquipmentData.SlotType.Amulet2, out EquipmentData amulet2) ? amulet2 : null);
        }
        
        private void UpdateEquipmentSlot(Image slotImage, EquipmentData equipment)
        {
            if (slotImage == null) return;
            
            if (equipment != null && equipment.Icon != null)
            {
                slotImage.sprite = equipment.Icon;
                slotImage.color = Color.white;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.color = new Color(1, 1, 1, 0.2f);
            }
        }
        
        private void UpdateStatusDisplay()
        {
            if (_inventory == null) return;
            
            var bonuses = _inventory.CalculateEquipmentBonuses();
            
            if (attackBonusText != null)
                attackBonusText.text = $"攻击 +{bonuses.attack}";
            
            if (defenseBonusText != null)
                defenseBonusText.text = $"防御 +{bonuses.defense}";
        }
        
        /// <summary>
        /// 装备槽位点击事件
        /// </summary>
        private void OnEquipmentSlotClicked(EquipmentData equipment)
        {
            if (_inventory == null || equipment == null) return;
            
            // 检查是否已装备
            var currentEquipped = _inventory.GetEquippedItem(equipment.Slot);
            bool isCurrentlyEquipped = currentEquipped != null && currentEquipped.ItemName == equipment.ItemName;
            
            if (isCurrentlyEquipped)
            {
                // 如果已装备，则卸下
                _inventory.UnequipItem(equipment.Slot);
                Debug.Log($"已卸下 {equipment.ItemName}");
            }
            else
            {
                // 如果未装备，则装备
                _inventory.EquipItem(equipment);
                Debug.Log($"已装备 {equipment.ItemName}");
            }
            
            // 刷新UI显示
            RefreshInventoryUI(); // 这里调用刷新方法
        }
        
        /// <summary>
        /// 使用物品点击事件
        /// </summary>
        private void OnUseItemClicked(ItemData item)
        {
            if (_inventory == null || item == null) return;
            
            Debug.Log($"使用物品: {item.ItemName}");
            
            // 使用后移除一个
            bool removed = _inventory.RemoveItem(item, 1);
            
            if (removed)
            {
                Debug.Log($"消耗了 1 个 {item.ItemName}");
                RefreshInventoryUI(); // 这里调用刷新方法
            }
        }
        
        /// <summary>
        /// 显示新物品提示
        /// </summary>
        public void ShowNewItemHint()
        {
            _hasNewItems = true;
            if (bagIconNewItemHint != null)
            {
                bagIconNewItemHint.SetActive(true);
                
                // 添加动画效果（可选）
                StartCoroutine(BlinkHint());
            }
        }
        
        /// <summary>
        /// 闪烁提示效果
        /// </summary>
        private IEnumerator BlinkHint()
        {
            if (bagIconNewItemHint == null) yield break;
            
            Image hintImage = bagIconNewItemHint.GetComponent<Image>();
            if (hintImage == null) yield break;
            
            // 闪烁3次
            for (int i = 0; i < 3; i++)
            {
                hintImage.color = new Color(1, 0, 0, 1); // 红色
                yield return new WaitForSeconds(0.3f);
                hintImage.color = new Color(1, 1, 1, 1); // 白色
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        /// <summary>
        /// 隐藏新物品提示
        /// </summary>
        public void HideNewItemHint()
        {
            _hasNewItems = false;
            if (bagIconNewItemHint != null)
                bagIconNewItemHint.SetActive(false);
        }
        
        /// <summary>
        /// 当玩家获得新物品时调用
        /// </summary>
        public void OnItemAdded(ItemData newItem)
        {
            // 如果背包没打开，显示新物品提示
            if (!inventoryPanel.activeSelf && bagIconButton != null)
            {
                ShowNewItemHint();
            }
            
            // 如果背包已打开，直接刷新
            if (inventoryPanel.activeSelf)
            {
                RefreshInventoryUI(); // 这里调用刷新方法
            }
        }
        
        // 外部调用的静态方法
        public static void Open()
        {
            InventoryUI instance = FindObjectOfType<InventoryUI>();
            if (instance != null)
                instance.OpenInventory();
        }
        
        public static void Close()
        {
            InventoryUI instance = FindObjectOfType<InventoryUI>();
            if (instance != null)
                instance.CloseInventory();
        }
        
        public static void Toggle()
        {
            InventoryUI instance = FindObjectOfType<InventoryUI>();
            if (instance != null)
                instance.ToggleInventory();
        }
    }
}