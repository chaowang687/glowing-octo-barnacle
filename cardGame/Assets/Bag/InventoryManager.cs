using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Bag
{
    /// <summary>
    /// 背包管理器，负责协调各个背包组件之间的交互
    /// </summary>
    public class InventoryManager : MonoBehaviour 
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        public static InventoryManager Instance { get; private set; }
        
        /// <summary>
        /// 静态数据资源引用
        /// </summary>
        public InventorySO inventoryData; // 拖入你创建的静态资源文件
        
        /// <summary>
        /// 只读属性，供外部访问背包内所有物品
        /// </summary>
        public List<ItemInstance> AllItemsInBag => inventoryData?.items ?? new List<ItemInstance>();
        
        [Header("引用设置")]
        public GameObject itemPrefab; // 物品预制体
        public InventoryGrid CurrentGrid; // 当前激活的网格
        public Transform itemContainer; // 物品容器
        
        /// <summary>
        /// 当前正在拖拽的物品
        /// </summary>
        public ItemUI CarriedItem { get; set; }
        
        /// <summary>
        /// 当前选中的物品
        /// </summary>
        public ItemUI SelectedItem { get; set; }
        
        /// <summary>
        /// 背包物品变化事件
        /// </summary>
        public System.Action<ItemInstance, bool> OnItemChanged; // 参数：物品实例，是否添加
        
        private void Awake() 
        {
            // 安全的单例实现
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("InventoryManager: Awake - 初始化单例");
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            Debug.Log("InventoryManager: Start - 开始初始化");
            
            // 尝试初始化CurrentGrid和itemContainer
            if (CurrentGrid == null)
            {
                CurrentGrid = FindObjectOfType<InventoryGrid>();
                Debug.Log($"InventoryManager: Start - 尝试查找CurrentGrid: {(CurrentGrid != null ? CurrentGrid.name : "未找到")}");
            }
            
            if (itemContainer == null && CurrentGrid != null)
            {
                itemContainer = CurrentGrid.transform;
                Debug.Log($"InventoryManager: Start - 设置itemContainer为CurrentGrid.transform: {itemContainer.name}");
            }
            
            // 检查必要引用
            CheckRequiredReferences();
        }
        
        private void CheckRequiredReferences()
        {
            Debug.Log("InventoryManager: CheckRequiredReferences - 检查必要引用");
            
            if (inventoryData == null)
            {
                Debug.LogWarning("InventoryManager: inventoryData未赋值！请在Inspector中设置InventorySO资源");
                
                // 尝试创建默认的InventorySO（仅用于调试，实际项目中应在Inspector中设置）
                inventoryData = ScriptableObject.CreateInstance<InventorySO>();
                Debug.Log("InventoryManager: 已创建默认的InventorySO实例");
            }
            
            if (itemPrefab == null)
            {
                Debug.LogError("InventoryManager: itemPrefab未赋值！请在Inspector中设置物品预制体");
            }
            
            if (CurrentGrid == null)
            {
                Debug.LogWarning("InventoryManager: CurrentGrid未赋值！请确保场景中有InventoryGrid组件");
            }
            
            if (itemContainer == null)
            {
                Debug.LogWarning("InventoryManager: itemContainer未赋值！请确保场景中有正确的容器对象");
            }
        }
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            // 取消订阅场景加载事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        /// <summary>
        /// 尝试旋转物品（安全模式）
        /// </summary>
        /// <param name="ui">物品UI引用</param>
        public void TryRotateItem(ItemUI ui) 
        {
            if (ui == null || ui.itemInstance == null) return;
        
            // 情况A：正在拖拽中
            // 允许自由旋转，只需更新视觉，合法性由 OnEndDrag 或 Preview 处理
            if (ui.IsDragging) 
            {
                ui.DoVisualRotate(); // 仅执行UI和数据变更
                // 强制刷新一下预览（如果鼠标不动，OnDrag可能不触发，手动刷一下）
                if (CurrentGrid != null) 
                {
                    CurrentGrid.ShowPlacementPreview(ui.itemInstance, ui.GetComponent<RectTransform>().anchoredPosition);
                }
                return;
            }
        
            // 情况B：物品已放置在网格中
            // 必须检查空间是否足够
            if (CurrentGrid != null) 
            {
                // 1. 检查旋转是否合法
                bool canRotate = CurrentGrid.CheckRotateValidity(ui.itemInstance);
        
                if (canRotate) 
                {
                    // 2. 如果合法：先从网格移除旧数据
                    CurrentGrid.RemoveItem(ui.itemInstance);
        
                    // 3. 执行旋转（修改数据 + UI）
                    ui.DoVisualRotate();
        
                    // 4. 以新形态重新放入网格
                    CurrentGrid.PlaceItem(ui.itemInstance, ui.itemInstance.posX, ui.itemInstance.posY);
                    
                    Debug.Log("物品旋转成功");
                } 
                else 
                {
                    // 5. 如果不合法：播放一个失败动画或音效，拒绝旋转
                    Debug.LogWarning("空间不足，无法旋转！");
                    // 可选：ui.ShakeAnimation();
                }
            }
        }
        
        /// <summary>
        /// 在指定位置生成物品（公共方法，供外部调用）
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <param name="gridPos">网格位置</param>
        /// <param name="autoFindSpace">是否自动寻找空位</param>
        /// <returns>生成的物品UI</returns>
        public ItemUI SpawnItem(ItemData itemData, Vector2Int gridPos, bool autoFindSpace = false)
        {
            if (itemData == null || itemPrefab == null || itemContainer == null || CurrentGrid == null)
            {
                Debug.LogError("生成物品失败：缺少必要的引用！");
                return null;
            }
            
            InventoryGrid grid = CurrentGrid;
            Vector2Int spawnPos = gridPos;
            
            // 如果启用自动寻找空位
            if (autoFindSpace)
            {
                spawnPos = FindEmptySpace(grid, itemData);
                if (spawnPos.x < 0 || spawnPos.y < 0)
                {
                    Debug.LogWarning($"没有足够的空间放置物品: {itemData.itemName}");
                    return null;
                }
            }
            else
            {
                // 检查指定位置是否可以放置
                if (!grid.CanPlace(spawnPos.x, spawnPos.y, itemData.width, itemData.height))
                {
                    Debug.LogWarning($"位置 ({spawnPos.x}, {spawnPos.y}) 无法放置物品");
                    return null;
                }
            }
            
            // 创建物品实例
            ItemInstance newItem = new ItemInstance(itemData)
            {
                posX = spawnPos.x,
                posY = spawnPos.y
            };
            
            // 实例化UI
            GameObject go = Instantiate(itemPrefab, itemContainer);
            
            ItemUI ui = go.GetComponent<ItemUI>();
            if (ui == null)
            {
                Debug.LogError("ItemUI组件不存在于预制体中！");
                Destroy(go);
                return null;
            }
            
            // 设置物品实例
            ui.itemInstance = newItem;
            
            // 初始化UI
            ui.Initialize(newItem, grid.cellSize);
            
            // 放置到网格
            grid.PlaceItem(newItem, spawnPos.x, spawnPos.y);
            ui.SnapToGrid(grid, spawnPos);
            
            // 添加到管理器
            AddItemToBag(newItem);
            
            Debug.Log($"生成物品: {itemData.itemName} 在位置 ({spawnPos.x}, {spawnPos.y})");
            return ui;
        }
        
        /// <summary>
        /// 在鼠标位置生成物品（立即进入拖拽状态）
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <returns>生成的物品UI</returns>
        public ItemUI SpawnItemAtMouse(ItemData itemData)
        {
            if (itemData == null || itemPrefab == null || itemContainer == null || CurrentGrid == null)
            {
                Debug.LogError("生成物品失败：缺少必要的引用！");
                return null;
            }
            
            InventoryGrid grid = CurrentGrid;
            
            // 创建物品实例
            ItemInstance newItem = new ItemInstance(itemData);
            
            // 实例化UI
            GameObject go = Instantiate(itemPrefab, itemContainer);
            
            ItemUI ui = go.GetComponent<ItemUI>();
            if (ui == null)
            {
                Debug.LogError("ItemUI组件不存在于预制体中！");
                Destroy(go);
                return null;
            }
            
            // 设置物品实例
            ui.itemInstance = newItem;
            
            // 初始化UI
            ui.Initialize(newItem, grid.cellSize);
            
            // 设置到鼠标位置
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                itemContainer as RectTransform,
                Input.mousePosition,
                null,
                out mousePos);
            
            ui.GetComponent<RectTransform>().anchoredPosition = mousePos;
            
            // 添加到管理器
            AddItemToBag(newItem);
            
            // 立即开始拖拽
            ui.StartManualDrag();
            CarriedItem = ui;
            
            Debug.Log($"在鼠标位置生成物品: {itemData.itemName}");
            return ui;
        }
        
        /// <summary>
        /// 寻找网格中的空位（优化：从左上角开始搜索）
        /// </summary>
        /// <param name="grid">目标网格</param>
        /// <param name="itemData">物品数据</param>
        /// <returns>找到的空位坐标，未找到则返回(-1,-1)</returns>
        private Vector2Int FindEmptySpace(InventoryGrid grid, ItemData itemData)
        {
            if (grid == null || itemData == null) return new Vector2Int(-1, -1);
            
            int width = itemData.width;
            int height = itemData.height;
            
            // 优化搜索算法：从左上角开始，逐行搜索
            for (int y = 0; y <= grid.height - height; y++)
            {
                for (int x = 0; x <= grid.width - width; x++)
                {
                    if (grid.CanPlace(x, y, width, height))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            
            // 尝试旋转物品后搜索
            if (width != height) // 如果物品不是正方形
            {
                for (int y = 0; y <= grid.height - width; y++)
                {
                    for (int x = 0; x <= grid.width - height; x++)
                    {
                        if (grid.CanPlace(x, y, height, width))
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // 没有找到空位
        }

        /// <summary>
        /// 尝试将物品放入网格（只允许放在空格子上）
        /// </summary>
        /// <param name="item">物品实例</param>
        /// <param name="x">目标X坐标</param>
        /// <param name="y">目标Y坐标</param>
        /// <param name="targetGrid">目标网格</param>
        /// <returns>是否放置成功</returns>
        public bool TryPlace(ItemInstance item, int x, int y, InventoryGrid targetGrid) 
        {
            if (item == null || targetGrid == null) return false;
            
            // 只检查空间是否足够，不允许交换物品
            if (targetGrid.CanPlace(x, y, item.CurrentWidth, item.CurrentHeight)) 
            {
                targetGrid.PlaceItem(item, x, y);
                
                // 添加到物品列表
                AddItemToBag(item);
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将物品添加到背包列表
        /// </summary>
        /// <param name="item">物品实例</param>
        public void AddItemToBag(ItemInstance item)
        {
            Debug.Log($"AddItemToBag: 尝试添加物品: {item.data?.itemName}");
            
            if (item == null)
            {
                Debug.LogError("AddItemToBag: 物品实例为null");
                return;
            }
            
            if (inventoryData == null)
            {
                Debug.LogError("AddItemToBag: inventoryData为null");
                return;
            }
            
            inventoryData.AddItem(item); // 获得即更新
            Debug.Log($"AddItemToBag: 物品 {item.data?.itemName} 已添加到inventoryData，当前物品数量: {inventoryData.items.Count}");
            
            OnItemChanged?.Invoke(item, true);
        }
        
        /// <summary>
        /// 从背包列表中移除物品
        /// </summary>
        /// <param name="item">物品实例</param>
        public void RemoveFromTracker(ItemInstance item)
        {
            if (item == null || inventoryData == null) return;
            
            inventoryData.RemoveItem(item); // 丢弃即更新
            OnItemChanged?.Invoke(item, false);
        }
        
        /// <summary>
        /// 保存背包数据到文件
        /// </summary>
        public void SaveInventory()
        {
            if (inventoryData == null) return;
            
            InventorySaveData saveData = new InventorySaveData();
            
            // 收集所有物品数据
            foreach (var item in inventoryData.items) 
            {
                saveData.items.Add(new ItemSaveEntry {
                    itemID = item.data.itemName, // 或者使用唯一的 GUID
                    posX = item.posX,
                    posY = item.posY,
                    isRotated = item.isRotated
                });
            }

            // 保存到文件
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "inventory.json");
            string json = JsonUtility.ToJson(saveData, true);
            
            try
            {
                System.IO.File.WriteAllText(savePath, json);
                Debug.Log($"存档已保存至: {savePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"保存存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从文件加载背包数据
        /// </summary>
        /// <param name="grid">目标网格</param>
        public void LoadInventory(InventoryGrid grid)
        {
            if (grid == null || itemPrefab == null || inventoryData == null) return;
            
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "inventory.json");
            if (!System.IO.File.Exists(savePath)) return;

            try
            {
                string json = System.IO.File.ReadAllText(savePath);
                InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

                // 清空现有物品
                ClearInventory(grid);
                
                // 清空静态数据
                inventoryData.Clear();
                
                // 加载物品
                foreach (var entry in saveData.items)
                {
                    // 1. 根据 ID 加载配置
                    ItemData data = Resources.Load<ItemData>($"Items/{entry.itemID}");
                    if (data == null)
                    {
                        Debug.LogWarning($"找不到物品配置: {entry.itemID}");
                        continue;
                    }
                    
                    // 2. 创建实例
                    ItemInstance newItem = new ItemInstance(data) {
                        posX = entry.posX,
                        posY = entry.posY,
                        isRotated = entry.isRotated
                    };

                    // 3. 实例化 UI 并对齐
                    GameObject go = Instantiate(itemPrefab, itemContainer);
                    ItemUI ui = go.GetComponent<ItemUI>();
                    if (ui != null)
                    {
                        ui.itemInstance = newItem;
                        ui.Initialize(newItem, grid.cellSize); // 初始化UI
                        grid.PlaceItem(newItem, entry.posX, entry.posY); // 注册到网格数组
                        ui.SnapToGrid(grid, new Vector2Int(entry.posX, entry.posY)); // 视觉对齐
                        
                        // 添加到物品列表
                        AddItemToBag(newItem);
                    }
                    else
                    {
                        Debug.LogError("ItemUI组件不存在于预制体中！");
                        Destroy(go);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从物品实例列表加载背包数据
        /// </summary>
        /// <param name="savedData">物品实例列表</param>
        /// <param name="mainGrid">目标网格</param>
        /// <param name="gridTransform">网格变换（已废弃，所有物品将放在ItemContainer中）</param>
        public void LoadInventory(List<ItemInstance> savedData, InventoryGrid mainGrid, Transform gridTransform) 
        {
            if (savedData == null || mainGrid == null || itemPrefab == null || inventoryData == null) return;
            
            // 确保itemContainer有效
            if (itemContainer == null)
            {
                itemContainer = mainGrid.transform;
            }
            
            // 清空现有物品
            ClearInventory(mainGrid);
            
            // 清空静态数据
            inventoryData.Clear();
            
            // 加载物品
            foreach(var data in savedData) 
            {
                GameObject go = Instantiate(itemPrefab, itemContainer);
                ItemUI ui = go.GetComponent<ItemUI>();
                if (ui != null)
                {
                    ui.itemInstance = data;
                    ui.Initialize(data, mainGrid.cellSize); // 初始化UI
                    mainGrid.PlaceItem(data, data.posX, data.posY); // 注册到网格数组
                    ui.SnapToGrid(mainGrid, new Vector2Int(data.posX, data.posY)); // 视觉对齐
                    
                    // 添加到物品列表
                    AddItemToBag(data);
                }
                else
                {
                    Debug.LogError("ItemUI组件不存在于预制体中！");
                    Destroy(go);
                }
            }
        }
        
        /// <summary>
        /// 清空背包
        /// </summary>
        /// <param name="grid">目标网格</param>
        public void ClearInventory(InventoryGrid grid)
        {
            if (grid == null) return;
            
            // 清空物品列表
            if (inventoryData != null)
            {
                inventoryData.Clear();
            }
            
            // 清空网格数据
            grid.ClearGrid();
            
            // 销毁所有物品UI
            ItemUI[] allItems = grid.GetComponentsInChildren<ItemUI>();
            foreach (ItemUI itemUI in allItems)
            {
                Destroy(itemUI.gameObject);
            }
        }
        
        /// <summary>
        /// 场景加载时调用
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 尝试查找CurrentGrid
            CurrentGrid = FindObjectOfType<InventoryGrid>();
            
            // 寻找新场景中的容器
            if (CurrentGrid != null)
            {
                itemContainer = CurrentGrid.transform;
                Debug.Log($"找到CurrentGrid，设置itemContainer为: {itemContainer.name}");
            }
            else
            {
                Debug.LogWarning("OnSceneLoaded: 没有找到CurrentGrid");
                
                // 尝试直接查找itemContainer
                InventoryManager[] managers = FindObjectsOfType<InventoryManager>();
                if (managers.Length > 0 && managers[0] != this)
                {
                    // 如果有其他InventoryManager实例，使用它的itemContainer
                    itemContainer = managers[0].itemContainer;
                    Debug.Log($"使用其他InventoryManager的itemContainer: {itemContainer?.name}");
                }
            }
            
            // 容器找回后，再执行物品加载
            if (itemContainer != null)
            {
                Debug.Log("OnSceneLoaded: 调用RefreshItemsInContainer");
                RefreshItemsInContainer();
            }
            else
            {
                Debug.LogWarning("OnSceneLoaded: itemContainer为null，无法刷新物品");
            }
        }
        
        /// <summary>
        /// 刷新容器中的物品UI
        /// </summary>
        private void RefreshItemsInContainer()
        {
            Debug.Log($"RefreshItemsInContainer: 开始执行");
            Debug.Log($"RefreshItemsInContainer: itemContainer={itemContainer?.name}, CurrentGrid={CurrentGrid?.name}, itemPrefab={itemPrefab?.name}");
            
            if (itemContainer == null || CurrentGrid == null || itemPrefab == null)
            {
                Debug.LogError("RefreshItemsInContainer: 缺少必要的引用，无法刷新物品");
                return;
            }
            
            // 1. 先清理容器（防止叠加）
            Debug.Log($"RefreshItemsInContainer: 清理容器 {itemContainer.name} 中的物品UI");
            int clearedCount = 0;
            foreach (Transform child in itemContainer)
            {
                // 确保只删除物品 UI，不删背景图等
                if (child.GetComponent<ItemUI>() != null)
                {
                    Destroy(child.gameObject);
                    clearedCount++;
                }
            }
            Debug.Log($"RefreshItemsInContainer: 已清理 {clearedCount} 个物品UI");

            // 2. 从 NewInventorySO 加载
            if (inventoryData != null)
            {
                Debug.Log($"RefreshItemsInContainer: 使用inventoryData加载物品，物品数量: {inventoryData.items.Count}");
                int loadedCount = 0;
                foreach (var item in inventoryData.items)
                {
                    Debug.Log($"RefreshItemsInContainer: 加载物品: {item.data?.itemName}, 位置: ({item.posX}, {item.posY})");
                    
                    // 关键：Instantiate 的第二个参数必须是 itemContainer
                    GameObject go = Instantiate(itemPrefab, itemContainer);
                    loadedCount++;
                    
                    ItemUI ui = go.GetComponent<ItemUI>();
                    
                    if (ui != null)
                    {
                        ui.itemInstance = item;
                        ui.Initialize(item, CurrentGrid.cellSize);
                        
                        // 将坐标对齐到网格，并确保逻辑层也占位
                        CurrentGrid.PlaceItem(item, item.posX, item.posY);
                        ui.SnapToGrid(CurrentGrid, new Vector2Int(item.posX, item.posY));
                        
                        // 确保缩放正确 (UI 常见问题)
                        go.GetComponent<RectTransform>().localScale = Vector3.one;
                        
                        Debug.Log($"RefreshItemsInContainer: 物品 {item.data?.itemName} 加载成功");
                    }
                    else
                    {
                        Debug.LogError($"RefreshItemsInContainer: 无法获取ItemUI组件");
                        Destroy(go);
                    }
                }
                Debug.Log($"RefreshItemsInContainer: 已加载 {loadedCount} 个物品");
            }
            else
            {
                Debug.LogWarning("RefreshItemsInContainer: inventoryData为null，无法加载物品");
            }
        }
        
        /// <summary>
        /// 拿起物品并开始拖拽
        /// </summary>
        /// <param name="item">物品实例</param>
        private void PickUpItem(ItemInstance item) 
        {
            if (item == null) return;
            
            // 查找该物品对应的 UI 对象
            ItemUI targetUI = FindUIForItem(item); 
            
            if (targetUI != null) 
            {
                // 模拟拖拽开始的操作
                PointerEventData eventData = new PointerEventData(null);
                targetUI.OnBeginDrag(eventData);
                
                // 将该 UI 设为当前跟随鼠标的对象
                CarriedItem = targetUI;
            }
        }

        /// <summary>
        /// 查找物品对应的UI组件
        /// </summary>
        /// <param name="item">物品实例</param>
        /// <returns>物品UI组件</returns>
        private ItemUI FindUIForItem(ItemInstance item) 
        {
            if (item == null) return null;
            
            // 优化：只在当前容器中查找
            if (itemContainer != null)
            {
                ItemUI[] allUIs = itemContainer.GetComponentsInChildren<ItemUI>();
                foreach(var ui in allUIs)
                {
                    if(ui.itemInstance == item) return ui;
                }
            }
            
            // 回退：查找整个场景
            ItemUI[] allSceneUIs = FindObjectsOfType<ItemUI>();
            foreach(var ui in allSceneUIs)
            {
                if(ui.itemInstance == item) return ui;
            }
            
            return null;
        }
    }
}
