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
        /// 是否有任何物品正在被拖拽
        /// </summary>
        public bool IsAnyItemDragging => CarriedItem != null;
        
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
        
        private void OnApplicationQuit()
        {
            SaveInventory();
            // 同时也让地图数据保存
            if(SlayTheSpireMap.GameDataManager.Instance != null)
                SlayTheSpireMap.GameDataManager.Instance.SaveGameData();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 在应用暂停时同步背包数据到GameDataManager
                SyncWithGameDataManager();
            }
        }
        
        /// <summary>
        /// 与GameDataManager同步背包数据
        /// </summary>
        public void SyncWithGameDataManager()
        {
            try
            {
                if (SlayTheSpireMap.GameDataManager.Instance != null && inventoryData != null)
                {
                    // 获取GameDataManager实例
                    SlayTheSpireMap.GameDataManager gdm = SlayTheSpireMap.GameDataManager.Instance;
                    
                    // 清空现有的relicIds列表
                    gdm.playerData.relicIds.Clear();
                    
                    // 遍历背包中的所有物品，将遗物数据同步到GameDataManager
                    foreach (var item in inventoryData.items)
                    {
                        if (item != null && item.data != null)
                        {
                            // 将物品ID添加到relicIds列表
                            // 这里假设所有背包物品都是遗物，实际项目中可能需要根据物品类型过滤
                            gdm.playerData.relicIds.Add(item.data.name);
                            Debug.Log($"SyncWithGameDataManager: 同步物品 {item.data.name} 到GameDataManager");
                        }
                    }
                    
                    Debug.Log($"背包数据已同步到GameDataManager，同步了 {gdm.playerData.relicIds.Count} 个遗物");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"同步背包数据到GameDataManager时出错: {e.Message}");
            }
        }
        
        private void Start()
        {
            Debug.Log("InventoryManager: Start - 开始初始化");
            
            // 尝试初始化CurrentGrid和itemContainer
            if (CurrentGrid == null)
            {
                CurrentGrid = FindObjectOfType<InventoryGrid>(true);
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
                    
                    // 更新所有物品的星星高亮状态
                    UpdateAllStarHighlights();
                    
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
            
            // 创建物品实例
            ItemInstance newItem = new ItemInstance(itemData);
            
            // 如果启用自动寻找空位
            if (autoFindSpace)
            {
                int bestRotation;
                spawnPos = FindEmptySpace(grid, itemData, out bestRotation);
                if (spawnPos.x < 0 || spawnPos.y < 0)
                {
                    Debug.LogWarning($"没有足够的空间放置物品: {itemData.itemName}");
                    return null;
                }
                // 设置最佳旋转角度
                newItem.rotation = bestRotation;
            }
            else
            {
                // 检查指定位置是否可以放置（使用异形物品检查方法）
                if (!grid.CanPlace(newItem, spawnPos.x, spawnPos.y))
                {
                    Debug.LogWarning($"位置 ({spawnPos.x}, {spawnPos.y}) 无法放置物品");
                    return null;
                }
            }
            
            // 设置物品位置
            newItem.posX = spawnPos.x;
            newItem.posY = spawnPos.y;
            
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
            
            // 放置到网格（使用异形物品放置方法）
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
        /// <param name="bestRotation">找到的最佳旋转角度</param>
        /// <returns>找到的空位坐标，未找到则返回(-1,-1)</returns>
        public Vector2Int FindEmptySpace(InventoryGrid grid, ItemData itemData, out int bestRotation)
        {
            bestRotation = 0;
            if (grid == null || itemData == null) return new Vector2Int(-1, -1);
            
            // 创建一个临时物品实例，用于检查旋转后的形状
            ItemInstance tempItem = new ItemInstance(itemData);
            
            // 尝试4个旋转角度
            for (int rotation = 0; rotation < 360; rotation += 90)
            {
                tempItem.rotation = rotation;
                
                // 获取旋转后的实际形状和尺寸
                bool[,] shape = tempItem.GetActualShape();
                int shapeWidth = shape.GetLength(0);
                int shapeHeight = shape.GetLength(1);
                
                // 优化搜索算法：从左上角开始，逐行搜索
                for (int y = 0; y <= grid.height - shapeHeight; y++)
                {
                    for (int x = 0; x <= grid.width - shapeWidth; x++)
                    {
                        if (grid.CanPlace(tempItem, x, y))
                        {
                            bestRotation = rotation;
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // 没有找到空位
        }
        
        /// <summary>
        /// 寻找网格中的空位（优化：从左上角开始搜索）
        /// </summary>
        /// <param name="grid">目标网格</param>
        /// <param name="itemData">物品数据</param>
        /// <returns>找到的空位坐标，未找到则返回(-1,-1)</returns>
        public Vector2Int FindEmptySpace(InventoryGrid grid, ItemData itemData)
        {
            int dummyRotation;
            return FindEmptySpace(grid, itemData, out dummyRotation);
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
            if (targetGrid.CanPlace(item, x, y)) 
            {
                targetGrid.PlaceItem(item, x, y);
                
                // 添加到物品列表
                AddItemToBag(item);
                
                // 更新所有物品的星星高亮状态
                UpdateAllStarHighlights();
                
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// 更新所有物品的星星高亮状态
        /// </summary>
        public void UpdateAllStarHighlights() 
        {
            // 查找所有物品UI并更新星星高亮
            ItemUI[] allItems = FindObjectsOfType<ItemUI>();
            foreach (ItemUI itemUI in allItems) 
            {
                itemUI.UpdateStarHighlight();
            }
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
            
            // 同步到GameDataManager
            SyncWithGameDataManager();
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
            
            // 同步到GameDataManager
            SyncWithGameDataManager();
        }
        
        /// <summary>
        /// 保存背包数据到文件
        /// </summary>
        /// <param name="slotIndex">存档槽位索引，默认0表示当前存档</param>
        public void SaveInventory(int slotIndex = 0)
        {
            Debug.Log($"InventoryManager: SaveInventory - 开始保存背包数据，槽位: {slotIndex}");
            
            if (inventoryData == null)
            {
                Debug.LogError("InventoryManager: SaveInventory - inventoryData为null，无法保存背包数据");
                return;
            }
            
            InventorySaveData saveData = new InventorySaveData();
            
            // 收集所有物品数据
            Debug.Log($"InventoryManager: SaveInventory - 开始收集物品数据，当前背包物品数量: {inventoryData.items.Count}");
            foreach (var item in inventoryData.items) 
            {
                if (item == null || item.data == null)
                {
                    Debug.LogWarning("InventoryManager: SaveInventory - 跳过无效物品");
                    continue;
                }
                
                // 使用资源文件名作为itemID，确保与Resources.Load路径匹配
                string itemID = item.data.name; // 使用资源的实际文件名作为ID
                saveData.items.Add(new ItemSaveEntry {
                    itemID = itemID, // 使用资源文件名作为唯一ID
                    posX = item.posX,
                    posY = item.posY,
                    rotation = item.rotation
                });
                Debug.Log($"InventoryManager: SaveInventory - 添加物品到存档: {itemID}，位置: ({item.posX}, {item.posY})，旋转: {item.rotation}");
                
            }

            // 保存到文件
            string fileName = slotIndex == 0 ? "inventory.json" : $"inventory_{slotIndex}.json";
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            string json = JsonUtility.ToJson(saveData, true);
            
            Debug.Log($"InventoryManager: SaveInventory - 生成的JSON数据: {json}");
            
            try
            {
                System.IO.File.WriteAllText(savePath, json);
                Debug.Log($"InventoryManager: SaveInventory - 背包存档已成功保存至: {savePath}");
                Debug.Log($"InventoryManager: SaveInventory - 保存的物品数量: {saveData.items.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventoryManager: SaveInventory - 保存背包存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从文件加载背包数据
        /// </summary>
        /// <param name="grid">目标网格</param>
        /// <param name="slotIndex">存档槽位索引，默认0表示当前存档</param>
        public void LoadInventory(InventoryGrid grid, int slotIndex = 0)
        {
            Debug.Log($"InventoryManager: LoadInventory - 开始加载背包数据，槽位: {slotIndex}");
            
            if (grid == null)
            {
                Debug.LogError("InventoryManager: LoadInventory - grid为null，无法加载背包数据");
                return;
            }
            if (itemPrefab == null)
            {
                Debug.LogError("InventoryManager: LoadInventory - itemPrefab为null，无法加载背包数据");
                return;
            }
            if (inventoryData == null)
            {
                Debug.LogError("InventoryManager: LoadInventory - inventoryData为null，无法加载背包数据");
                return;
            }
            
            // 构建存档文件路径
            string fileName = slotIndex == 0 ? "inventory.json" : $"inventory_{slotIndex}.json";
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            Debug.Log($"InventoryManager: LoadInventory - 查找存档文件: {savePath}");
            
            if (!System.IO.File.Exists(savePath))
            {
                Debug.LogWarning($"InventoryManager: LoadInventory - 存档文件不存在: {savePath}");
                return;
            }

            try
            {
                Debug.Log($"InventoryManager: LoadInventory - 读取存档文件");
                string json = System.IO.File.ReadAllText(savePath);
                Debug.Log($"InventoryManager: LoadInventory - 读取到JSON数据: {json}");
                
                InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
                Debug.Log($"InventoryManager: LoadInventory - 解析成功，包含物品数量: {saveData.items.Count}");

                // 清空现有物品
                Debug.Log($"InventoryManager: LoadInventory - 清空现有物品");
                ClearInventory(grid);
                
                // 清空静态数据
                Debug.Log($"InventoryManager: LoadInventory - 清空静态数据，当前物品数量: {inventoryData.items.Count}");
                inventoryData.Clear();
                
                // 加载物品
                int loadedCount = 0;
                foreach (var entry in saveData.items)
                {
                    Debug.Log($"InventoryManager: LoadInventory - 处理物品条目: ID={entry.itemID}, 位置=({entry.posX}, {entry.posY}), 旋转角度={entry.rotation}");
                    
                    // 1. 根据 ID 加载配置
                    ItemData data = Resources.Load<ItemData>($"Items/{entry.itemID}");
                    if (data == null)
                    {
                        Debug.LogWarning($"InventoryManager: LoadInventory - 找不到物品配置: {entry.itemID}");
                        continue;
                    }
                    
                    // 2. 创建实例
                    ItemInstance newItem = new ItemInstance(data) {
                        posX = entry.posX,
                        posY = entry.posY,
                        rotation = entry.rotation
                    };

                    // 3. 实例化 UI 并对齐
                    Debug.Log($"InventoryManager: LoadInventory - 实例化物品UI: {entry.itemID}");
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
                        loadedCount++;
                        Debug.Log($"InventoryManager: LoadInventory - 成功加载物品: {entry.itemID}，已加载数量: {loadedCount}");
                    }
                    else
                    {
                        Debug.LogError("InventoryManager: LoadInventory - ItemUI组件不存在于预制体中！");
                        Destroy(go);
                    }
                }
                
                Debug.Log($"InventoryManager: LoadInventory - 背包数据加载完成，总共加载: {loadedCount} 个物品，当前背包物品数量: {inventoryData.items.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventoryManager: LoadInventory - 加载存档失败: {e.Message}");
                Debug.LogError($"InventoryManager: LoadInventory - 异常堆栈: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 从文件加载背包数据（用于UI显示，不实例化UI）
        /// </summary>
        /// <param name="slotIndex">存档槽位索引</param>
        public void LoadInventoryData(int slotIndex = 0)
        {
            Debug.Log($"InventoryManager: LoadInventoryData - 开始加载背包数据，槽位: {slotIndex}");
            
            if (inventoryData == null)
            {
                Debug.LogError("InventoryManager: LoadInventoryData - inventoryData为null，无法加载背包数据");
                return;
            }
            
            // 构建存档文件路径
            string fileName = slotIndex == 0 ? "inventory.json" : $"inventory_{slotIndex}.json";
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            Debug.Log($"InventoryManager: LoadInventoryData - 查找存档文件: {savePath}");
            
            if (!System.IO.File.Exists(savePath))
            {
                Debug.LogWarning($"InventoryManager: LoadInventoryData - 存档文件不存在: {savePath}，保持现有物品数据不变");
                // 不再清空物品列表，保持现有数据不变
                return;
            }

            try
            {
                Debug.Log($"InventoryManager: LoadInventoryData - 读取存档文件");
                string json = System.IO.File.ReadAllText(savePath);
                
                if (string.IsNullOrEmpty(json) || json.Trim() == "{}" || json.Trim() == "[]")
                {
                    Debug.LogWarning($"InventoryManager: LoadInventoryData - 存档内容为空，保持现有物品数据不变");
                    return;
                }
                
                InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
                
                if (saveData == null || saveData.items == null)
                {
                    Debug.LogWarning($"InventoryManager: LoadInventoryData - 存档解析失败，保持现有物品数据不变");
                    return;
                }
                
                Debug.Log($"InventoryManager: LoadInventoryData - 解析成功，包含物品数量: {saveData.items.Count}");
                
                // 如果存档中没有物品，保持现有物品数据不变
                if (saveData.items.Count == 0)
                {
                    Debug.LogWarning($"InventoryManager: LoadInventoryData - 存档中没有物品，保持现有物品数据不变");
                    return;
                }
                
                // 只有当存档有效且包含物品时，才清空并重新加载
                Debug.Log($"InventoryManager: LoadInventoryData - 清空现有背包数据，当前物品数量: {inventoryData.items.Count}");
                inventoryData.Clear();
                
                // 加载物品数据
                int loadedCount = 0;
                foreach (var entry in saveData.items)
                {
                    Debug.Log($"InventoryManager: LoadInventoryData - 处理物品条目: ID={entry.itemID}, 位置=({entry.posX}, {entry.posY}), 旋转角度={entry.rotation}");
                    
                    // 解决资源引用丢失问题：尝试多种资源加载方式
                    ItemData data = null;
                    
                    // 1. 尝试直接加载
                    data = Resources.Load<ItemData>("Items/" + entry.itemID);
                    
                    // 2. 如果失败，尝试去掉路径前缀
                    if (data == null)
                    {
                        data = Resources.Load<ItemData>(entry.itemID);
                    }
                    
                    // 3. 如果失败，尝试全小写
                    if (data == null)
                    {
                        data = Resources.Load<ItemData>("Items/" + entry.itemID.ToLower());
                    }
                    
                    // 4. 如果失败，尝试去掉路径前缀并全小写
                    if (data == null)
                    {
                        data = Resources.Load<ItemData>(entry.itemID.ToLower());
                    }
                    
                    if (data == null)
                    {
                        Debug.LogWarning($"InventoryManager: LoadInventoryData - 找不到物品配置: {entry.itemID}");
                        continue;
                    }
                    
                    // 创建实例
                    ItemInstance newItem = new ItemInstance(data) {
                        posX = entry.posX,
                        posY = entry.posY,
                        rotation = entry.rotation
                    };
                    
                    // 添加到物品列表
                    inventoryData.AddItem(newItem);
                    loadedCount++;
                    Debug.Log($"InventoryManager: LoadInventoryData - 成功加载物品: {entry.itemID}，已加载数量: {loadedCount}");
                }
                
                Debug.Log($"InventoryManager: LoadInventoryData - 背包数据加载完成，总共加载: {loadedCount} 个物品，当前背包物品数量: {inventoryData.items.Count}");
                
                // 延迟一帧刷新 UI，确保 Grid 已经就绪
                StartCoroutine(DelayRefreshUI());
                
                // 同步到GameDataManager
                SyncWithGameDataManager();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"InventoryManager: LoadInventoryData - 加载背包数据失败: {e.Message}");
                Debug.LogError($"InventoryManager: LoadInventoryData - 异常堆栈: {e.StackTrace}");
                // 加载失败时确保背包状态正确
                inventoryData.Clear();
                // 同步到GameDataManager
                SyncWithGameDataManager();
            }
        }
        
        /// <summary>
        /// 延迟刷新UI，确保Grid已经就绪
        /// </summary>
        private System.Collections.IEnumerator DelayRefreshUI()
        {
            yield return new WaitForEndOfFrame(); // 等待所有Awake完成
            Debug.Log("InventoryManager: DelayRefreshUI - 开始刷新UI");
            
            // 确保CurrentGrid和itemContainer已被正确初始化
            if (CurrentGrid == null)
            {
                CurrentGrid = FindObjectOfType<InventoryGrid>(true);
                Debug.Log($"InventoryManager: DelayRefreshUI - 查找CurrentGrid: {(CurrentGrid != null ? CurrentGrid.name : "未找到")}");
            }
            
            if (CurrentGrid != null && itemContainer == null)
            {
                itemContainer = CurrentGrid.transform;
                Debug.Log($"InventoryManager: DelayRefreshUI - 设置itemContainer为CurrentGrid.transform: {itemContainer.name}");
            }
            
            if (itemContainer != null && CurrentGrid != null && itemPrefab != null)
            {
                Debug.Log($"InventoryManager: DelayRefreshUI - 调用RefreshItemsInContainer刷新物品UI");
                RefreshItemsInContainer();
            }
            else
            {
                Debug.LogError("InventoryManager: DelayRefreshUI - 引用缺失！请检查Inspector面板。");
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
        /// 重置背包数据
        /// </summary>
        public void ResetInventoryData()
        {
            if (inventoryData != null)
            {
                inventoryData.Clear();
                Debug.Log("背包数据已重置");
            }
        }
        
        /// <summary>
        /// 场景加载时调用
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"InventoryManager: OnSceneLoaded - 场景 {scene.name} 已加载");
            
            // 等待一帧，确保所有组件都已初始化
            StartCoroutine(DelayInitializeAndRefresh(scene.name));
        }
        
        /// <summary>
        /// 延迟初始化和刷新，解决场景加载时的时间差问题
        /// </summary>
        private System.Collections.IEnumerator DelayInitializeAndRefresh(string sceneName)
        {
            // 等待一帧，确保所有组件都已初始化
            yield return new WaitForEndOfFrame();
            
            // 尝试查找CurrentGrid，包括隐藏对象
            CurrentGrid = FindObjectOfType<InventoryGrid>(true);
            Debug.Log($"InventoryManager: DelayInitializeAndRefresh - 查找CurrentGrid结果: {(CurrentGrid != null ? CurrentGrid.name : "未找到")}");
            
            // 寻找新场景中的容器
            if (CurrentGrid != null)
            {
                itemContainer = CurrentGrid.transform;
                Debug.Log($"InventoryManager: DelayInitializeAndRefresh - 设置itemContainer为: {itemContainer.name}");
            }
            else
            {
                Debug.LogWarning("InventoryManager: DelayInitializeAndRefresh - 没有找到CurrentGrid，保持现有物品数据不变");
            }
            
            // 容器找回后，刷新UI但不重新加载存档
            if (itemContainer != null && CurrentGrid != null && itemPrefab != null)
            {
                Debug.Log("InventoryManager: DelayInitializeAndRefresh - 调用RefreshItemsInContainer刷新UI");
                RefreshItemsInContainer();
            }
            else
            {
                // 检查具体哪个引用缺失
                if (itemPrefab == null)
                {
                    Debug.LogError("InventoryManager: DelayInitializeAndRefresh - itemPrefab引用缺失！请在Inspector面板中赋值。");
                }
                else
                {
                    // 在某些场景中（如图鉴场景），可能不需要InventoryGrid，这是正常的
                    string missingRefs = "";
                    if (CurrentGrid == null) missingRefs += " CurrentGrid";
                    if (itemContainer == null) missingRefs += " itemContainer";
                    Debug.LogWarning($"InventoryManager: DelayInitializeAndRefresh - 场景 {sceneName} 中缺少引用:{missingRefs}，这在部分场景中是正常的。");
                }
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
        /// 彻底丢弃物品：从网格逻辑、数据列表和场景中同步移除
        /// </summary>
        /// <param name="ui">要丢弃的物品UI</param>
        public void DropItem(ItemUI ui)
        {
            if (ui == null || ui.itemInstance == null) return;

            // 1. 从逻辑网格数组中清理占位（防止产生透明的阻塞块）
            if (CurrentGrid != null)
            {
                CurrentGrid.RemoveItem(ui.itemInstance);
            }

            // 2. 从 InventorySO 数据资源中移除物品实例
            RemoveFromTracker(ui.itemInstance);

            // 3. 释放管理器的引用，防止悬空指针
            if (CarriedItem == ui) CarriedItem = null;
            if (SelectedItem == ui) SelectedItem = null;

            // 4. 销毁场景中的物体
            Destroy(ui.gameObject);
            
            Debug.Log($"物品 {ui.itemInstance.data?.itemName} 已被丢弃并销毁");
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
        
        /// <summary>
        /// 触发回合开始遗物效果
        /// </summary>
        public void TriggerTurnStartRelics()
        {
            // 获取CardSystem实例（使用更高效的方式）
            object cardSystem = null;
            // 使用FindObjectOfType直接查找CardSystem组件，避免遍历所有GameObject
            var cardSystemComponent = FindObjectOfType(typeof(UnityEngine.Component)) as UnityEngine.Component;
            if (cardSystemComponent != null)
            {
                // 尝试直接获取CardSystem组件
                cardSystem = cardSystemComponent.gameObject.GetComponent("CardSystem");
            }
            
            // 如果直接获取失败，使用更可靠的方式
            if (cardSystem == null)
            {
                // 获取所有GameObject
                UnityEngine.Object[] allObjects = GameObject.FindObjectsOfType(typeof(GameObject));
                foreach (UnityEngine.Object obj in allObjects)
                {
                    GameObject go = obj as GameObject;
                    if (go != null)
                    {
                        var comp = go.GetComponent("CardSystem");
                        if (comp != null)
                        {
                            cardSystem = comp;
                            break;
                        }
                    }
                }
            }
            
            // 遍历背包中所有物品实例
            foreach (var itemInstance in AllItemsInBag)
            {
                if (itemInstance.data == null || itemInstance.data.effects == null)
                    continue;
                
                // 标记是否触发了效果
                bool effectTriggered = false;
                
                // 遍历物品的所有效果
                foreach (var effectSO in itemInstance.data.effects)
                {
                    // 检查该效果是否实现了IItemEffect接口
                    if (effectSO is IItemEffect effect)
                    {
                        effect.OnTurnStart(cardSystem);
                        effectTriggered = true;
                    }
                }
                
                // 如果触发了效果，显示外发光动画
                if (effectTriggered)
                {
                    ItemUI itemUI = FindUIForItem(itemInstance);
                    if (itemUI != null)
                    {
                        itemUI.ShowGlow();
                    }
                }
            }
        }
    }
}
