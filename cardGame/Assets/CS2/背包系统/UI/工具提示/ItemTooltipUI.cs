using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ScavengingGame
{
    /// <summary>
    /// 物品提示UI
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        [Header("UI 组件")]
        public GameObject tooltipPanel;
        public Image itemIcon;
        public TextMeshProUGUI itemNameText;
        public TextMeshProUGUI itemDescriptionText;
        public TextMeshProUGUI itemStatsText;
        public RectTransform tooltipRect;
        
        [Header("位置偏移")]
        public Vector2 offset = new Vector2(20, -20);
        
        private Camera uiCamera;
        private Canvas canvas;
        
        void Start()
        {
            canvas = GetComponentInParent<Canvas>();
            uiCamera = canvas.worldCamera ?? Camera.main;
            
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        void Update()
        {
            if (tooltipPanel.activeSelf)
            {
                UpdateTooltipPosition();
            }
        }
        
        /// <summary>
        /// 显示物品提示
        /// </summary>
        public void ShowTooltip(ItemData item, string additionalInfo = "")
        {
            if (item == null || tooltipPanel == null) return;
            
            // 设置物品信息
            if (itemIcon != null)
            {
                itemIcon.sprite = item.Icon;
                itemIcon.color = Color.white;
            }
            
            if (itemNameText != null)
            {
                itemNameText.text = item.ItemName;
            }
            
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = item.Description;
            }
            
            // 设置物品属性
            if (itemStatsText != null)
            {
                string statsText = "";
                
                if (item is EquipmentData equipment)
                {
                    statsText += $"装备部位: {GetSlotName(equipment.Slot)}\n";
                    statsText += $"攻击: +{equipment.AttackBonus}\n";
                    statsText += $"防御: +{equipment.DefenseBonus}\n";
                }
                else
                {
                    statsText += "类型: 消耗品\n";
                }
                
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    statsText += $"\n{additionalInfo}";
                }
                
                itemStatsText.text = statsText;
            }
            
            tooltipPanel.SetActive(true);
            UpdateTooltipPosition();
        }
        
        /// <summary>
        /// 显示文本提示
        /// </summary>
        public void ShowTextTooltip(string text)
        {
            if (tooltipPanel == null) return;
            
            // 清空图标
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.color = Color.clear;
            }
            
            // 设置文本
            if (itemNameText != null)
            {
                itemNameText.text = "";
            }
            
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = text;
            }
            
            if (itemStatsText != null)
            {
                itemStatsText.text = "";
            }
            
            tooltipPanel.SetActive(true);
            UpdateTooltipPosition();
        }
        
        /// <summary>
        /// 隐藏提示
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 更新提示位置
        /// </summary>
        private void UpdateTooltipPosition()
        {
            if (tooltipRect == null || canvas == null) return;
            
            Vector2 mousePos = Input.mousePosition;
            Vector2 tooltipPos;
            
            // 转换为Canvas中的位置
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                mousePos,
                uiCamera,
                out tooltipPos);
            
            // 添加偏移
            tooltipPos += offset;
            
            // 防止提示超出屏幕
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            
            // 检查右边界
            if (tooltipPos.x + tooltipSize.x > canvasSize.x / 2)
            {
                tooltipPos.x = canvasSize.x / 2 - tooltipSize.x;
            }
            
            // 检查左边界
            if (tooltipPos.x - tooltipSize.x < -canvasSize.x / 2)
            {
                tooltipPos.x = -canvasSize.x / 2 + tooltipSize.x;
            }
            
            // 检查下边界
            if (tooltipPos.y - tooltipSize.y < -canvasSize.y / 2)
            {
                tooltipPos.y = -canvasSize.y / 2 + tooltipSize.y;
            }
            
            tooltipRect.anchoredPosition = tooltipPos;
        }
        
        private string GetSlotName(EquipmentData.SlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentData.SlotType.Weapon: return "武器";
                case EquipmentData.SlotType.Armor: return "护甲";
                case EquipmentData.SlotType.Amulet1: return "护符1";
                case EquipmentData.SlotType.Amulet2: return "护符2";
                default: return "未知";
            }
        }
    }
}