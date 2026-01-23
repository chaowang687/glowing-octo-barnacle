using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 物品UI组件，负责处理物品的交互逻辑
    /// </summary>
    public class ItemUI : MonoBehaviour, IPointerClickHandler
    {
        // 添加一个公开属性判断是否在拖拽
        private bool isDragging = false;
        public bool IsDragging { get { return isDragging; } set { isDragging = value; } }
        
        // 添加一个标志位，用于控制是否允许拖拽
        public bool allowDrag = true;
        
        public ItemInstance itemInstance;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;
        private Image glowImage;
        private Canvas canvas;
        private float cellSize = 50f; // 保存初始化时的cellSize，默认50f
        
        // 拖拽相关变量
        private InventoryGrid originalGrid;
        private Vector2Int originalPos;
        private Vector2 dragOffset;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
            
            // 确保CanvasGroup组件存在
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // 初始化CanvasGroup状态
            canvasGroup.blocksRaycasts = false; // 不阻止射线检测，让底层Slot接收点击事件
            canvasGroup.interactable = false; // 不可交互，让底层Slot处理交互
            canvasGroup.alpha = 1f;
            
            // 初始化ItemUI的锚点为左上角，与InventoryGrid.GetPositionFromGrid方法匹配
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            
            // 如果iconImage未通过Inspector赋值，尝试查找
            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null)
                {
                    iconImage = iconTransform.GetComponent<Image>();
                }
            }
            
            // 初始化Icon的锚点设置为中心点，避免拉伸冲突
            if (iconImage != null)
            {
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                }
                
                // 确保物品图片不接收点击事件，让底层的Slot组件处理点击
                iconImage.raycastTarget = false;
            }
            
            // 创建外发光效果
            CreateGlowEffect();
        }

        // 移除Start方法中的UpdateVisual调用，因为Initialize方法已经包含了完整的UI更新逻辑
        // 避免在未完成初始化时调用导致的大小错误
        private void Start()
        {
            // 如果Initialize方法没有被调用，手动初始化
            if (itemInstance == null)
            {
                Debug.LogWarning($"[ItemUI] Start: itemInstance 未初始化，物体: {gameObject.name}");
            }
            // 否则，不执行任何操作，因为Initialize方法已经处理了视觉更新
        }











        /// <summary>
        /// 将物品丢弃到世界中
        /// </summary>
        private void DropIntoWorld()
        {
            if (itemInstance == null || itemInstance.data == null) return;
            
            if (itemInstance.data.worldPrefab != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Instantiate(itemInstance.data.worldPrefab, hit.point + Vector3.up, Quaternion.identity);
                    
                    // 从管理器中移除物品
                    InventoryManager.Instance.RemoveFromTracker(itemInstance);
                    
                    // 销毁当前 UI
                    Destroy(gameObject);
                }
            }
            else
            {
                // 如果没有世界模型，返回原位置
                if (originalGrid != null)
                {
                    originalGrid.PlaceItem(itemInstance, originalPos.x, originalPos.y);
                    SnapToGrid(originalGrid, originalPos);
                }
            }
        }



        /// <summary>
        /// 将物品对齐到网格
        /// </summary>
        public void SnapToGrid(InventoryGrid grid, Vector2Int pos)
        {
            if (grid == null || itemInstance == null) return;
            
            rect.anchoredPosition = grid.GetPositionFromGrid(pos.x, pos.y);
        }

        /// <summary>
        /// 更新物品视觉表现
        /// </summary>
        public void UpdateVisual()
        {
            // 1. 强制初始化基础组件引用 (确保安全)
            if (rect == null) rect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            
            // 2. 确保 iconImage 的引用
            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null) iconImage = iconTransform.GetComponent<Image>();
                else iconImage = GetComponentInChildren<Image>();
            }
            
            // 3. 数据安全性检查
            if (itemInstance == null || itemInstance.data == null)
            {
                Debug.LogWarning($"[ItemUI] UpdateVisual: 无效的物品数据，跳过更新。物体: {gameObject.name}");
                return;
            }
            
            // 4. 使用保存的cellSize
            float cellSize = this.cellSize;
            
            // 5. 使用CurrentWidth和CurrentHeight来计算旋转后的尺寸
            int currentWidth = itemInstance.CurrentWidth;
            int currentHeight = itemInstance.CurrentHeight;
            
            // 6. 执行UI更新 (现在访问rect是安全的)
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(currentWidth * cellSize, currentHeight * cellSize);
            }
            
            // 7. 更新外发光效果的大小
            if (glowImage != null)
            {
                RectTransform glowRect = glowImage.rectTransform;
                if (glowRect != null)
                {
                    // 确保发光效果与物品UI大小一致
                    glowRect.anchorMin = Vector2.zero;
                    glowRect.anchorMax = Vector2.one;
                    glowRect.offsetMin = Vector2.zero;
                    glowRect.offsetMax = Vector2.zero;
                }
            }
            
            // 8. 设置图标
            if (iconImage != null)
            {
                iconImage.sprite = itemInstance.data.icon;
                
                // 1. 设置图片类型和保持宽高比
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true; // 保持原始宽高比，不拉伸图片
                
                // 设置图标旋转和大小
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    // 旋转 Icon 角度，支持360度旋转
                    iconRect.localEulerAngles = new Vector3(0, 0, itemInstance.rotation);
                    
                    // 2. 保持图标锚点为中心点，避免拉伸冲突
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // 3. 确保图标大小不为0，使用物品原始宽高
                    if (iconRect.sizeDelta.x == 0 || iconRect.sizeDelta.y == 0)
                    {
                        iconRect.sizeDelta = new Vector2(itemInstance.data.width * cellSize, itemInstance.data.height * cellSize);
                    }
                }
            }
        }

        /// <summary>
        /// 手动开始拖拽
        /// </summary>
        public void StartManualDrag()
        {
            isDragging = true;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.7f;
            originalGrid = null;
            dragOffset = Vector2.zero;
        }

        /// <summary>
        /// 初始化物品UI
        /// </summary>
        public void Initialize(ItemInstance item, float cellSize)
        {
            // 1. 强制初始化基础组件引用 (手动补齐 Awake 的工作)
            if (rect == null) rect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            // 2. 确保 iconImage 的引用 (处理 Prefab 动态生成)
            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null) iconImage = iconTransform.GetComponent<Image>();
                else iconImage = GetComponentInChildren<Image>();
            }

            // 3. 保存cellSize
            this.cellSize = cellSize;

            // 4. 数据安全性检查
            if (item == null)
            {
                Debug.LogError($"[ItemUI] 错误: 传入的 ItemInstance 为空！物体: {gameObject.name}");
                return;
            }
            this.itemInstance = item;

            if (item.data == null)
            {
                Debug.LogError($"[ItemUI] 错误: 物品 {item} 的 data 属性未赋值！");
                return;
            }

            // 5. 执行 UI 更新 (现在访问 rect 是 100% 安全的)
            rect.sizeDelta = new Vector2(item.CurrentWidth * cellSize, item.CurrentHeight * cellSize);
            
            if (iconImage != null)
            {
                iconImage.sprite = item.data.icon;
                
                // 1. 设置图片类型和保持宽高比
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true; // 保持原始宽高比，不拉伸图片
                
                // 2. 设置图标旋转和锚点
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null)
                {
                    // 支持360度旋转
                    iconRect.localEulerAngles = new Vector3(0, 0, item.rotation);
                    
                    // 3. 保持图标锚点为中心点，避免拉伸冲突
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // 4. 设置图标初始大小，确保宽高不为0
                    // 使用物品原始宽高作为图标大小，避免宽高变成0
                    iconRect.sizeDelta = new Vector2(item.data.width * cellSize, item.data.height * cellSize);
                }
            }
        }


        
        /// <summary>
        /// 处理点击事件
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 右键旋转物品
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                // 请求管理器尝试旋转
                InventoryManager.Instance.TryRotateItem(this);
                return;
            }
            
            // 左键选中物品
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // 设置当前选中物品
                InventoryManager.Instance.SelectedItem = this;
                
                // 添加选中效果反馈
                canvasGroup.alpha = 0.7f;
                Invoke(nameof(ResetAlpha), 0.1f);
            }
        }
        
        /// <summary>
        /// 重置透明度
        /// </summary>
        private void ResetAlpha()
        {
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// 创建外发光效果
        /// </summary>
        private void CreateGlowEffect()
        {
            // 检查是否已经存在发光效果
            Transform glowTransform = transform.Find("Glow");
            if (glowTransform == null)
            {
                // 创建发光效果的Image对象
                GameObject glowObj = new GameObject("Glow");
                glowObj.transform.SetParent(transform);
                
                // 设置锚点，使其与父对象大小一致
                RectTransform glowRect = glowObj.AddComponent<RectTransform>();
                glowRect.anchorMin = Vector2.zero;
                glowRect.anchorMax = Vector2.one;
                glowRect.pivot = new Vector2(0.5f, 0.5f);
                glowRect.offsetMin = Vector2.zero;
                glowRect.offsetMax = Vector2.zero;
                glowRect.localScale = Vector3.one;
                glowRect.localEulerAngles = Vector3.zero;
                
                // 创建Image组件
                glowImage = glowObj.AddComponent<Image>();
                glowImage.color = new Color(1f, 1f, 0f, 0.5f); // 黄色发光效果，半透明
                glowImage.type = Image.Type.Simple;
                glowImage.raycastTarget = false; // 不接收射线检测
                glowImage.preserveAspect = false; // 不保持宽高比，自适应父对象大小
                
                // 设置发光效果的层级，使其在图标后面
                glowObj.transform.SetSiblingIndex(0);
                
                // 初始隐藏发光效果
                HideGlow();
            }
            else
            {
                glowImage = glowTransform.GetComponent<Image>();
                HideGlow();
            }
        }
        
        /// <summary>
        /// 显示外发光效果
        /// </summary>
        public void ShowGlow()
        {
            if (glowImage != null)
            {
                glowImage.enabled = true;
                
                // 添加简单的闪烁动画
                StartCoroutine(GlowAnimation());
            }
        }
        
        /// <summary>
        /// 隐藏外发光效果
        /// </summary>
        public void HideGlow()
        {
            if (glowImage != null)
            {
                glowImage.enabled = false;
            }
        }
        
        /// <summary>
        /// 外发光动画
        /// </summary>
        private System.Collections.IEnumerator GlowAnimation()
        {
            if (glowImage == null) yield break;
            
            // 闪烁3次
            for (int i = 0; i < 3; i++)
            {
                // 渐亮
                for (float alpha = 0.3f; alpha <= 0.7f; alpha += 0.1f)
                {
                    glowImage.color = new Color(1f, 1f, 0f, alpha);
                    yield return new WaitForSeconds(0.1f);
                }
                
                // 渐暗
                for (float alpha = 0.7f; alpha >= 0.3f; alpha -= 0.1f)
                {
                    glowImage.color = new Color(1f, 1f, 0f, alpha);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // 恢复初始状态
            glowImage.color = new Color(1f, 1f, 0f, 0.5f);
            glowImage.enabled = false;
        }
        
        /// <summary>
        /// 执行视觉旋转（只处理数据和UI，不处理逻辑判断）
        /// </summary>
        public void DoVisualRotate() 
        {
            if (itemInstance == null) return;

            // --- 核心数据变更 ---
            // 旋转90度
            itemInstance.Rotate();

            // --- 视觉变更 ---
            float cellSize = this.cellSize;
            
            // 重新计算宽高，旋转后宽高互换
            int w = itemInstance.CurrentWidth;
            int h = itemInstance.CurrentHeight;
            
            // 更新布局区域（绿框）宽高，确保ItemUI_Prefab(Clone)的宽高正确变化
            rect.sizeDelta = new Vector2(w * cellSize, h * cellSize);
            
            // 更新外发光效果的大小
            if (glowImage != null)
            {
                RectTransform glowRect = glowImage.rectTransform;
                if (glowRect != null)
                {
                    // 确保发光效果与物品UI大小一致
                    glowRect.anchorMin = Vector2.zero;
                    glowRect.anchorMax = Vector2.one;
                    glowRect.offsetMin = Vector2.zero;
                    glowRect.offsetMax = Vector2.zero;
                }
            }

            // 确保 iconImage 的引用
            if (iconImage == null)
            {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null) iconImage = iconTransform.GetComponent<Image>();
                else iconImage = GetComponentInChildren<Image>();
            }

            // 图标处理
            if (iconImage != null) 
            {
                RectTransform iconRect = iconImage.rectTransform;
                
                // 1. 设置图片类型和保持宽高比
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true; // 保持原始宽高比，不拉伸图片
                
                // 2. 旋转图片（只旋转，不改变宽高）
                iconRect.localEulerAngles = new Vector3(0, 0, itemInstance.rotation);
                
                // 3. 保持图标锚点为中心点，避免拉伸冲突
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                
                // 4. 确保图标大小不为0
                if (iconRect.sizeDelta.x == 0 || iconRect.sizeDelta.y == 0)
                {
                    // 使用物品原始宽高，确保图标大小不为0
                    iconRect.sizeDelta = new Vector2(itemInstance.data.width * cellSize, itemInstance.data.height * cellSize);
                }
            }
        }
        
        /// <summary>
        /// 补充：为了解决Unity EventSystem中 Drag 会吞掉 Click 的问题
        /// 如果想在拖拽过程中按键盘（如 'R' 键）旋转，需要在 Update 中监听
        /// </summary>
        private void Update() 
        { 
            if (Input.GetKeyDown(KeyCode.R)) 
            { 
                // 只有正在拖拽的物品才允许旋转
                if (IsDragging)
                {
                    Debug.Log("ItemUI: 拖拽中按R键旋转物品");
                    InventoryManager.Instance.TryRotateItem(this);
                }
            } 
        }
    }
}