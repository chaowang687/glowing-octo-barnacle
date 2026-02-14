## 修复InventoryManager物品消失问题

### 问题分析
1. InventoryManager应该挂在MainMenu场景中，使用DontDestroyOnLoad保持全局唯一
2. 物品消失是因为场景切换时`OnSceneLoaded`会调用`LoadInventoryData(0)`，如果存档为空或不存在，会清空物品列表
3. 在非背包场景（如战斗场景）中CurrentGrid不存在，导致物品UI无法正确刷新

### 修复方案
1. **修改场景切换逻辑**：
   - 只在从主菜单进入游戏场景时才加载存档数据
   - 在其他场景切换时，保持现有物品数据不变
   - 移除`OnSceneLoaded`中对非主菜单场景自动加载存档的逻辑

2. **改进存档加载逻辑**：
   - 在`LoadInventoryData`中添加存档有效性检查
   - 只有当存档文件存在且包含有效物品数据时，才清空并重新加载
   - 否则保持现有物品数据不变

3. **优化CurrentGrid初始化**：
   - 在CurrentGrid不存在时，保持现有物品数据不变
   - 只在CurrentGrid存在时才刷新物品UI

### 修改文件
- `Assets/Bag/InventoryManager.cs`：`LoadInventoryData`和`DelayInitializeAndRefresh`方法