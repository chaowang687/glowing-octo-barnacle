# 物品图鉴系统使用指南

## 1. 系统概述
物品图鉴系统允许玩家查看和收集游戏中的所有物品，类似于背包乱斗的图鉴功能。系统会自动从背包中同步物品，并记录收集状态。

## 2. 核心组件

### 2.1 数据层
- **ItemCodexSO**：ScriptableObject，存储所有图鉴物品数据和分类
- **CodexItem**：图鉴物品数据结构
- **ItemCategory**：物品分类数据结构

### 2.2 逻辑层
- **ItemCodexManager**：单例管理器，处理收集状态、保存/加载和与背包的同步

### 2.3 UI层
- **ItemCodexUI**：主图鉴界面，包含分类、搜索和物品网格
- **ItemDetailPanel**：物品详情面板
- **ItemCodexEntry**：物品条目组件

## 3. 设置步骤

### 3.1 创建图鉴数据
1. 在Project面板中右键点击 → Create → Bag → Item Codex
2. 将创建的ItemCodexSO重命名为合适的名称（如"GameItemCodex"）
3. 在Inspector中添加物品和分类

### 3.2 设置图鉴管理器
1. 在场景中创建一个空游戏对象，命名为"ItemCodexManager"
2. 添加ItemCodexManager脚本组件
3. 将创建的ItemCodexSO拖入"codexData"字段
4. 确保场景中已有InventoryManager实例

### 3.3 创建图鉴UI
1. 创建图鉴UI面板，包含以下组件：
   - 分类面板（用于显示分类按钮）
   - 物品网格（用于显示物品条目）
   - 搜索输入框
   - 完成度文本
   - 物品详情面板

2. 将ItemCodexUI脚本添加到主面板
3. 拖入各个UI组件引用
4. 创建并设置分类按钮和物品条目预制体

## 4. 使用方法

### 4.1 显示图鉴
在需要显示图鉴的地方调用：
```csharp
// 获取图鉴UI实例（根据你的场景设置）
ItemCodexUI codexUI = FindObjectOfType<ItemCodexUI>();
if (codexUI != null)
{
    codexUI.gameObject.SetActive(true);
}
```

### 4.2 自动同步
- 当背包中添加新物品时，ItemCodexManager会自动将其添加到收集列表
- 每次场景加载时，系统会自动同步背包中的所有物品

### 4.3 测试功能
在Unity编辑器中，点击菜单栏 "Bag/Test Item Codex Integration" 可以测试图鉴系统的集成情况。

## 5. 数据结构说明

### 5.1 ItemCodexSO
- **allItems**：所有图鉴物品列表
- **categories**：物品分类列表

### 5.2 CodexItem
- **itemID**：物品唯一ID（与ItemData.name对应）
- **itemName**：物品显示名称
- **description**：物品描述
- **category**：物品分类
- **icon**：物品图标
- **effects**：物品效果列表

### 5.3 ItemCategory
- **name**：分类内部名称
- **displayName**：分类显示名称

## 6. 扩展建议

### 6.1 添加成就系统
可以根据图鉴完成度添加成就系统，例如：
- 收集10%物品获得成就
- 收集特定分类所有物品获得成就

### 6.2 添加奖励系统
可以为完成图鉴提供奖励，例如：
- 完成100%图鉴获得特殊物品
- 每收集一个新物品获得少量资源

### 6.3 增强UI效果
- 添加物品获得动画
- 为已收集/未收集物品添加不同样式
- 添加图鉴等级系统

## 7. 常见问题

### 7.1 图鉴不显示物品
- 检查ItemCodexSO是否已正确设置
- 确保物品的itemID与ItemData.name匹配
- 检查控制台是否有错误信息

### 7.2 收集状态不更新
- 确保ItemCodexManager已正确注册到InventoryManager的OnItemChanged事件
- 检查SaveCollectedItems方法是否正常执行

### 7.3 分类显示错误
- 确保物品的category字段与ItemCategory.name匹配
- 检查分类按钮是否正确创建

## 8. 版本历史
- v1.0.0：初始版本，实现基本功能
- v1.0.1：修复同步问题，添加详细日志
- v1.0.2：改进分类过滤，处理未分类物品

---

以上就是物品图鉴系统的使用指南。如有任何问题，请查看代码注释或控制台日志。