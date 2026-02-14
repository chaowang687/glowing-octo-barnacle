
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ScavengingGame
{
    public class InventoryUIMain : MonoBehaviour
    {
        [Header("UI 引用")]
        public GameObject inventoryPanel;
        public Button closeButton;
        public Button bgCloseButton;
        public Button bagIconButton;
        public Transform itemGrid;
        public GameObject itemSlotPrefab;
        
        [Header("子管理器")]
        public InventorySlotManager slotManager;
        public InventoryEquipmentManager equipmentManager;
        public InventoryInfoManager infoManager;
        public InventoryDragHandler dragHandler;
        
        [Header("状态显示")]
        public TextMeshProUGUI capacityText;
        
        [Header("控制设置")]
        public KeyCode toggleKey = KeyCode.I;
        public int totalSlots = 30;
        
        private IInventoryService _inventoryService;
        private bool _isOpen = false;
        
        void Start()
        {
            Initialize();
        }
        
        void Initialize()
        {
            if (GameStateManager.Instance != null && 
                GameStateManager.Instance.PlayerInventory is IInventoryService inventoryService)
            {
                _inventoryService = inventoryService;
                
                _inventoryService.OnInventoryChanged += OnInventoryChanged;
                _inventoryService.OnItemAdded += OnItemAdded;
            }
            else
            {
                Debug.LogError("无法获取IInventoryService接口");
                return;
            }
            
            closeButton?.onClick.AddListener(CloseInventory);
            bgCloseButton?.onClick.AddListener(CloseInventory);
            bagIconButton?.onClick.AddListener(ToggleInventory);
            
            if (slotManager != null) 
                slotManager.Initialize(this, itemGrid, itemSlotPrefab, totalSlots, _inventoryService);
            
            if (equipmentManager != null)
                equipmentManager.Initialize(this, _inventoryService);
                
            if (infoManager != null)
                infoManager.Initialize(_inventoryService);
                
            if (dragHandler != null)
                dragHandler.Initialize(this, _inventoryService);
            
            inventoryPanel?.SetActive(false);
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                ToggleInventory();
                
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseInventory();
        }
        
        #region 背包控制
        public void ToggleInventory()
        {
            if (_isOpen)
                CloseInventory();
            else
                OpenInventory();
        }
        
        public void OpenInventory()
        {
            _isOpen = true;
            inventoryPanel?.SetActive(true);
            RefreshAllUI();
        }
        
        public void CloseInventory()
        {
            _isOpen = false;
            inventoryPanel?.SetActive(false);
            slotManager?.ClearSelection();
            infoManager?.HideItemInfo();
        }
        
        public void ForceRefresh()
        {
            if (_isOpen)
                RefreshAllUI();
        }
        #endregion
        
        #region 库存操作
        public IInventoryService GetInventoryService()
        {
            return _inventoryService;
        }
        
        // 这里添加一个兼容性方法，供其他代码调用
        public void RefreshInventoryUI()
        {
            RefreshAllUI();
        }
        
        public void RefreshAllUI()
        {
            if (_inventoryService == null) return;
            
            slotManager?.RefreshSlots();
            equipmentManager?.RefreshEquipment();
            UpdateCapacityDisplay();
            
            // 如果信息面板正在显示物品，也需要刷新
            infoManager?.RefreshCurrentInfo();
        }
        
        private void UpdateCapacityDisplay()
        {
            if (capacityText == null || _inventoryService == null) return;
            
            int currentCount = _inventoryService.GetCurrentCapacity();
            int maxCapacity = _inventoryService.GetMaxCapacity();
            
            capacityText.text = $"{currentCount}/{maxCapacity}";
            
            if (currentCount >= maxCapacity)
                capacityText.color = Color.red;
            else if (currentCount >= maxCapacity * 0.8f)
                capacityText.color = Color.yellow;
            else
                capacityText.color = Color.white;
        }
        #endregion
        
        #region 事件处理
        public void OnItemSlotClicked(int slotIndex, ItemData item)
        {
            infoManager?.ShowItemInfo(item);
            slotManager?.SetSelectedSlot(slotIndex);
        }
        
        public void OnItemSlotRightClicked(int slotIndex, ItemData item)
        {
            if (item == null || _inventoryService == null) return;
            
            if (item is EquipmentData equipment)
            {
                bool isEquipped = _inventoryService.IsItemEquipped(equipment);
                if (isEquipped)
                    _inventoryService.UnequipItem(equipment.Slot);
                else
                    _inventoryService.EquipItem(equipment);
            }
            else
            {
                _inventoryService.UseItem(item);
            }
            
            RefreshAllUI();
        }
        
        private void OnInventoryChanged()
        {
            RefreshAllUI();
        }
        
        private void OnItemAdded(ItemData item)
        {
            RefreshAllUI();
        }
        
        public void OnEquipmentSlotClicked(EquipmentData.SlotType slotType)
        {
            var equipment = _inventoryService?.GetEquippedItem(slotType);
            if (equipment != null)
                infoManager?.ShowItemInfo(equipment);
        }
        #endregion
        
        void OnDestroy()
        {
            if (_inventoryService != null)
            {
                _inventoryService.OnInventoryChanged -= OnInventoryChanged;
                _inventoryService.OnItemAdded -= OnItemAdded;
            }
        }
    }
}
