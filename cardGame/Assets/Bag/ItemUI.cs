using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Bag
{
    /// <summary>
    /// 物品UI组件，负责处理物品的交互逻辑
    /// </summary>
    public class ItemUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
        [SerializeField] private GameObject starPrefab; // 星星图标预制体
        public GameObject StarPrefab { get { return starPrefab; } set { starPrefab = value; } } // 方便外部访问和调试
        private Image glowImage;
        private Canvas canvas;
        private float cellSize = 50f; // 保存初始化时的cellSize，默认50f
        
        private List<Image> starImages = new List<Image>();
        private Transform starsContainer;
        
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
            canvasGroup.blocksRaycasts = true; // 允许接收射线检测，以便触发鼠标事件
            canvasGroup.interactable = true; // 可交互，以便触发鼠标事件
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
            
            // 创建星星容器
            starsContainer = transform.Find("StarsContainer");
            if (starsContainer == null)
            {
                Debug.Log("[ItemUI] 创建新的StarsContainer");
                GameObject starsObj = new GameObject("StarsContainer");
                starsContainer = starsObj.transform;
                
                // 将StarsContainer作为ItemUI的直接子物体，确保它显示在最上层
                starsContainer.SetParent(transform);
                // 确保StarsContainer显示在最上层
                starsContainer.SetAsLastSibling();
                
                // 设置星星容器的RectTransform
                RectTransform starsRect = starsObj.AddComponent<RectTransform>();
                CanvasGroup starsCanvasGroup = starsObj.AddComponent<CanvasGroup>();
                
                // 使用统一的设置方法，确保锚点和pivot都设置为中心点
                UpdateStarsContainerSettings(starsRect, starsCanvasGroup);
                
                // 设置初始位置为零，因为图标已经在中心
                starsRect.anchoredPosition = Vector2.zero;
            } else {
                // 如果starsContainer已经存在，确保它显示在最上层
                starsContainer.SetAsLastSibling();
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
            
            // 拖拽开始时，将星星设为灰色
            UpdateStarHighlight();
        }

        /// <summary>
        /// 初始化物品UI
        /// </summary>
        public void Initialize(ItemInstance item, float cellSize) {
            // 1. 强制初始化基础组件引用 (手动补齐 Awake 的工作)
            if (rect == null) rect = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            
            // 2. 确保 iconImage 的引用 (处理 Prefab 动态生成)
            if (iconImage == null) {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null) iconImage = iconTransform.GetComponent<Image>();
                else iconImage = GetComponentInChildren<Image>();
            }

            // 3. 保存cellSize
            this.cellSize = cellSize;

            // 4. 数据安全性检查
            if (item == null) {
                Debug.LogError($"[ItemUI] 错误: 传入的 ItemInstance 为空！物体: {gameObject.name}");
                return;
            }
            this.itemInstance = item;

            if (item.data == null) {
                Debug.LogError($"[ItemUI] 错误: 物品 {item} 的 data 属性未赋值！");
                return;
            }
            
            // 确保itemInstance.data.starOffsets不为空，但不再添加默认星星偏移
            if (itemInstance.data.starOffsets == null) {
                itemInstance.data.starOffsets = new List<Vector2Int>();
            }
            // 不再自动添加默认星星偏移，避免在物品自身占用格子生成星星

            // 5. 执行 UI 更新 (现在访问 rect 是 100% 安全的)
            rect.sizeDelta = new Vector2(item.CurrentWidth * cellSize, item.CurrentHeight * cellSize);
            
            if (iconImage != null) {
                iconImage.sprite = item.data.icon;
                
                // 1. 设置图片类型和保持宽高比
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true; // 保持原始宽高比，不拉伸图片
                
                // 2. 设置图标旋转和锚点
                RectTransform iconRect = iconImage.rectTransform;
                if (iconRect != null) {
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
            
            // 6. 生成星星图标
            GenerateStarIcons();
            
            // 7. 更新星星高亮状态
            UpdateStarHighlight();
            
            // 8. 确保CanvasGroup属性正确设置，不影响拖拽和旋转功能
            if (canvasGroup != null) {
                canvasGroup.blocksRaycasts = false; // 不阻止射线检测，让底层Slot接收点击事件
                canvasGroup.interactable = false; // 不可交互，让底层Slot处理交互
            }
        }
        
        /// <summary>
        /// 统一设置StarsContainer的RectTransform和CanvasGroup属性
        /// </summary>
        /// <param name="starsRect">StarsContainer的RectTransform组件</param>
        /// <param name="canvasGroup">StarsContainer的CanvasGroup组件</param>
        private void UpdateStarsContainerSettings(RectTransform starsRect, CanvasGroup canvasGroup) {
            if (starsRect == null || canvasGroup == null) {
                return;
            }
            
            // 将锚点和pivot都设置为中心点，确保坐标计算准确
            starsRect.anchorMin = new Vector2(0.5f, 0.5f);
            starsRect.anchorMax = new Vector2(0.5f, 0.5f);
            starsRect.pivot = new Vector2(0.5f, 0.5f);
            starsRect.localScale = Vector3.one;
            
            // 只有当itemInstance不为空时才设置sizeDelta
            if (itemInstance != null) {
                starsRect.sizeDelta = new Vector2(cellSize * itemInstance.CurrentWidth, cellSize * itemInstance.CurrentHeight);
            } else {
                starsRect.sizeDelta = Vector2.zero;
            }
            
            starsRect.localEulerAngles = Vector3.zero; // 确保不旋转
            starsRect.anchoredPosition = Vector2.zero; // 居中
            
            // 确保星星容器可见且不影响交互
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        
        /// <summary>
        /// 生成星星图标
        /// </summary>
        private void GenerateStarIcons() {
            // 基本安全检查
            if (itemInstance == null || itemInstance.data == null || itemInstance.data.starOffsets == null) {
                return;
            }
            
            // 初始化starsContainer（仅在第一次调用时）
            if (starsContainer == null) {
                // 查找或创建StarsContainer
                starsContainer = transform.Find("StarsContainer");
                if (starsContainer == null) {
                    GameObject starsObj = new GameObject("StarsContainer");
                    starsContainer = starsObj.transform;
                    starsContainer.SetParent(transform);
                    starsContainer.SetAsLastSibling();
                    
                    // 添加必要的组件
                    RectTransform starsRect = starsObj.AddComponent<RectTransform>();
                    CanvasGroup starsCanvasGroup = starsObj.AddComponent<CanvasGroup>();
                    
                    // 统一设置
                    UpdateStarsContainerSettings(starsRect, starsCanvasGroup);
                    starsRect.anchoredPosition = Vector2.zero;
                } else {
                    // 确保StarsContainer的父物体是ItemUI
                    if (starsContainer.parent != transform) {
                        starsContainer.SetParent(transform);
                    }
                    // 确保现有StarsContainer显示在最上层
                    starsContainer.SetAsLastSibling();
                    
                    // 更新设置
                    RectTransform existingStarsRect = starsContainer.GetComponent<RectTransform>();
                    CanvasGroup existingStarsCanvasGroup = starsContainer.GetComponent<CanvasGroup>();
                    if (existingStarsRect == null) existingStarsRect = starsContainer.gameObject.AddComponent<RectTransform>();
                    if (existingStarsCanvasGroup == null) existingStarsCanvasGroup = starsContainer.gameObject.AddComponent<CanvasGroup>();
                    
                    UpdateStarsContainerSettings(existingStarsRect, existingStarsCanvasGroup);
                    existingStarsRect.anchoredPosition = Vector2.zero;
                }
            } else {
                // 确保现有starsContainer显示在最上层
                starsContainer.SetAsLastSibling();
            }
            
            // 确保starPrefab不为空
            if (starPrefab == null) {
                return;
            }
            
            // 获取星星数量
            int starCount = itemInstance.data.starOffsets.Count;
            if (starCount == 0) {
                return;
            }
            
            // 清理多余的星星
            while (starImages.Count > starCount) {
                int lastIndex = starImages.Count - 1;
                Image starImage = starImages[lastIndex];
                if (starImage != null && starImage.gameObject != null) {
                    Destroy(starImage.gameObject);
                }
                starImages.RemoveAt(lastIndex);
            }
            
            // 确保starsContainer可见
            CanvasGroup containerCanvasGroup = null;
            // 添加安全检查，确保starsContainer不是一个已经被销毁的对象
            if (starsContainer != null && starsContainer.gameObject != null) {
                containerCanvasGroup = starsContainer.GetComponent<CanvasGroup>();
                if (containerCanvasGroup != null) {
                    containerCanvasGroup.alpha = 1f;
                    containerCanvasGroup.blocksRaycasts = false;
                    containerCanvasGroup.interactable = false;
                }
                starsContainer.gameObject.SetActive(true);
                starsContainer.gameObject.layer = gameObject.layer;
            }
            
            // 获取旋转角度
            int rotation = itemInstance.rotation;
            
            // 获取物品当前宽高
            int currentWidth = itemInstance.CurrentWidth;
            int currentHeight = itemInstance.CurrentHeight;
            
            // 生成/更新星星
            if (starsContainer != null && starsContainer.gameObject != null) {
                // 初始状态隐藏星星容器，但确保星星已经生成
                starsContainer.gameObject.SetActive(false);
                for (int i = 0; i < starCount; i++) {
                    Vector2Int offset = itemInstance.data.starOffsets[i];
                    Vector2Int rotatedOffset = offset;
                    
                    // 根据旋转角度调整偏移量，使用与物品形状一致的旋转逻辑
                    switch (rotation) {
                        case 90: // 90度顺时针旋转
                            rotatedOffset = new Vector2Int(offset.y, currentHeight - 1 - offset.x);
                            break;
                        case 180: // 180度旋转
                            rotatedOffset = new Vector2Int(currentWidth - 1 - offset.x, currentHeight - 1 - offset.y);
                            break;
                        case 270: // 270度顺时针旋转
                            rotatedOffset = new Vector2Int(currentWidth - 1 - offset.y, offset.x);
                            break;
                    }
                    
                    GameObject starObj;
                    Image starImage;
                    RectTransform starRect;
                    
                    // 复用现有星星或创建新星星
                    if (i < starImages.Count) {
                        // 复用现有星星
                        starImage = starImages[i];
                        if (starImage == null) {
                            // 移除已经被销毁的星星
                            starImages.RemoveAt(i);
                            i--;
                            continue;
                        }
                        starObj = starImage.gameObject;
                        if (starObj == null) {
                            // 移除已经被销毁的星星
                            starImages.RemoveAt(i);
                            i--;
                            continue;
                        }
                        starRect = starImage.GetComponent<RectTransform>();
                        if (starRect == null) {
                            // 移除没有RectTransform的星星
                            starImages.RemoveAt(i);
                            i--;
                            continue;
                        }
                    } else {
                        // 创建新星星
                        starObj = Instantiate(starPrefab, starsContainer);
                        starImage = starObj.GetComponent<Image>();
                        starRect = starObj.GetComponent<RectTransform>();
                        
                        if (starImage != null) {
                            starImages.Add(starImage);
                            starImage.raycastTarget = false;
                            
                            // 初始化RectTransform设置
                            if (starRect != null) {
                                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                                starRect.pivot = new Vector2(0.5f, 0.5f);
                                starRect.localScale = Vector3.one;
                                starRect.localEulerAngles = Vector3.zero;
                            }
                        }
                    }
                    
                    if (starRect != null && starImage != null && starObj != null) {
                        // 计算物品的中心点偏移
                        float centerOffsetX = (currentWidth - 1) * cellSize / 2;
                        float centerOffsetY = (currentHeight - 1) * cellSize / 2;
                        
                        // 转换为UI位置：基于中心点原点的相对位置
                        float xPos = rotatedOffset.x * cellSize - centerOffsetX;
                        float yPos = -rotatedOffset.y * cellSize + centerOffsetY;
                        
                        // 更新星星位置和大小
                        starRect.anchoredPosition = new Vector2(xPos, yPos);
                        float starSize = cellSize * 0.6f;
                        starRect.sizeDelta = new Vector2(starSize, starSize);
                        
                        // 设置星星默认状态
                        starImage.enabled = true;
                        starImage.color = Color.white; // 移除红色，使用默认颜色
                        starObj.SetActive(false); // 初始状态隐藏
                        starObj.layer = starsContainer.gameObject.layer;
                        
                        // 处理没有Sprite的情况
                        if (starImage.sprite == null) {
                            starImage.color = new Color(1, 1, 1, 0.5f); // 使用半透明白色
                            starImage.fillCenter = true;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 旋转偏移量
        /// </summary>
        private Vector2Int RotateOffset(Vector2Int offset, int rotationSteps) {
            Vector2Int rotatedOffset = offset;
            // 使用与物品形状旋转一致的逻辑
            for (int i = 0; i < rotationSteps; i++) {
                // 顺时针旋转90度：(x,y) → (y, -x)
                rotatedOffset = new Vector2Int(rotatedOffset.y, -rotatedOffset.x);
            }
            return rotatedOffset;
        }
        
        /// <summary>
        /// 延迟更新星星高亮，确保旋转完成后再检查
        /// </summary>
        private System.Collections.IEnumerator DelayUpdateStarHighlight() {
            // 等待一帧，确保旋转和位置更新完成
            yield return null;
            UpdateStarHighlight();
        }
        
        /// <summary>
        /// 更新星星高亮状态
        /// </summary>
        public void UpdateStarHighlight() {
            // 保护检查：确保itemInstance和CurrentGrid存在
            if (itemInstance == null || InventoryManager.Instance == null || InventoryManager.Instance.CurrentGrid == null) {
                return;
            }
            
            // 只有当物品不在拖拽状态时，才更新星星高亮状态
            if (isDragging) {
                // 拖拽时，星星显示为灰色
                for (int i = 0; i < starImages.Count; i++) {
                    Image starImage = starImages[i];
                    if (starImage != null && starImage.gameObject != null) {
                        starImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                }
                return;
            }
            
            // 确保星星图标已生成，但避免在旋转时形成无限循环
            // 只有在starImages为空时才调用GenerateStarIcons，否则直接使用现有星星
            if (starImages.Count == 0) {
                GenerateStarIcons();
                // 如果生成后仍然没有星星图标，直接返回
                if (starImages.Count == 0) {
                    return;
                }
            }
            
            // 检查星星相邻情况
            Dictionary<Vector2Int, ItemInstance> adjacentItems = InventoryManager.Instance.CurrentGrid.CheckStarAdjacency(itemInstance);
            List<Vector2Int> starPositions = itemInstance.GetStarPositions();
            
            // 更新每个星星的高亮状态
            for (int i = 0; i < starImages.Count; i++) {
                if (i < starPositions.Count) {
                    Image starImage = starImages[i];
                    if (starImage != null && starImage.gameObject != null) {
                        Vector2Int starPos = starPositions[i];
                        bool isAdjacent = adjacentItems.ContainsKey(starPos);
                        // 移除红色状态，使用更自然的高亮颜色
                        starImage.color = isAdjacent ? new Color(1, 1, 0, 0.8f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        // 保持当前的显示/隐藏状态，不强制显示
                    }
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
        /// 鼠标进入物品时的处理
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 显示星星
            ShowStars();
        }
        
        /// <summary>
        /// 鼠标离开物品时的处理
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // 隐藏星星
            HideStars();
        }
        
        /// <summary>
        /// 显示星星
        /// </summary>
        private void ShowStars()
        {
            if (starsContainer != null && starsContainer.gameObject != null) {
                // 显示星星容器
                starsContainer.gameObject.SetActive(true);
                
                // 确保星星容器显示在物品的最上层
                starsContainer.SetAsLastSibling();
                
                // 显示所有星星
                for (int i = 0; i < starsContainer.childCount; i++) {
                    Transform child = starsContainer.GetChild(i);
                    if (child != null && child.gameObject != null) {
                        child.gameObject.SetActive(true);
                        // 确保每个星星都显示在最上层
                        child.SetAsLastSibling();
                    }
                }
                
                // 显示starImages列表中的星星
                foreach (Image starImage in starImages) {
                    if (starImage != null && starImage.gameObject != null) {
                        starImage.gameObject.SetActive(true);
                        // 确保每个星星都显示在最上层
                        starImage.transform.SetAsLastSibling();
                    }
                }
            }
        }
        
        /// <summary>
        /// 隐藏星星
        /// </summary>
        private void HideStars()
        {
            if (starsContainer != null && starsContainer.gameObject != null) {
                // 隐藏所有星星
                for (int i = 0; i < starsContainer.childCount; i++) {
                    Transform child = starsContainer.GetChild(i);
                    if (child != null && child.gameObject != null) {
                        child.gameObject.SetActive(false);
                    }
                }
                
                // 隐藏starImages列表中的星星
                foreach (Image starImage in starImages) {
                    if (starImage != null && starImage.gameObject != null) {
                        starImage.gameObject.SetActive(false);
                    }
                }
                
                // 隐藏星星容器
                starsContainer.gameObject.SetActive(false);
            }
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
            
            // 更新星星图标
            GenerateStarIcons();
            
            // 重置星星高亮状态，确保旋转后星星不会立即变黄
            for (int i = 0; i < starImages.Count; i++) {
                Image starImage = starImages[i];
                if (starImage != null && starImage.gameObject != null) {
                    starImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            
            // 延迟更新星星高亮，确保旋转完成后再检查
            StartCoroutine(DelayUpdateStarHighlight());
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
            
            // 自定义鼠标检测逻辑，不依赖于EventSystem
            // 这样既可以显示星星，又不影响拖拽和旋转功能
            if (gameObject.activeSelf && rect != null && itemInstance != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect, 
                    Input.mousePosition, 
                    null, 
                    out Vector2 localPoint);
                
                // 检查鼠标是否在物品范围内
                bool isMouseOver = rect.rect.Contains(localPoint);
                bool isOverShape = false;
                
                if (isMouseOver)
                {
                    // 进一步检查鼠标是否在物品的实际形状内（针对异形物品）
                    float cellSize = this.cellSize;
                    int currentWidth = itemInstance.CurrentWidth;
                    int currentHeight = itemInstance.CurrentHeight;
                    
                    // 将本地坐标转换为物品内部的网格坐标
                    // 注意：物品UI的pivot在左上角，Y轴向下为负
                    int gridX = Mathf.FloorToInt(localPoint.x / cellSize);
                    int gridY = Mathf.FloorToInt(-localPoint.y / cellSize);
                    
                    // 检查网格坐标是否在物品形状范围内
                    if (gridX >= 0 && gridX < currentWidth && gridY >= 0 && gridY < currentHeight)
                    {
                        // 获取物品的实际形状
                        bool[,] shape = itemInstance.GetActualShape();
                        if (shape != null && shape.GetLength(0) > gridX && shape.GetLength(1) > gridY)
                        {
                            // 检查该网格位置是否为实心
                            isOverShape = shape[gridX, gridY];
                        }
                    }
                }
                
                // 更新星星显示状态
                if (isOverShape)
                {
                    // 将物品移到最上层
                    transform.SetAsLastSibling();
                    ShowStars();
                } else {
                    HideStars();
                }
            }
        }
    }
}