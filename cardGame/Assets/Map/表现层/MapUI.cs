using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

namespace SlayTheSpireMap
{
    public class MapUI : MonoBehaviour
    {
        [Header("滚动控制")]
        public MapScrollController scrollController; 
        
        [Header("UI节点预制体")]
        public GameObject uiNodePrefab;
        
        [Header("容器")]
        public Transform nodesContainer;  // 确保 Inspector 拖入此项
        public Transform linesContainer;
        
        [Header("渲染器")]
        public StraightLineRenderer mapLineRenderer; // 批量连线管理器

        [Header("节点间距配置")]
        public float horizontalSpacing = 150f;
        public float verticalSpacing = 120f;

        // 运行时缓存
        public List<UIMapNode> uiNodes = new List<UIMapNode>();
        private List<GameObject> lines = new List<GameObject>();
        private Dictionary<MapNodeData, UIMapNode> dataToUIMap = new Dictionary<MapNodeData, UIMapNode>();

        void Start()
        {
            // 协程等待数据就绪后生成
            StartCoroutine(WaitAndGenerate());
            
            // 如果 Instance 已经存在则直接尝试生成一次
            if (MapManager.Instance != null && MapManager.Instance.allNodes != null)
            {
                GenerateMapUI();
            }
        }

        private IEnumerator WaitAndGenerate()
        {
            // 等待 MapManager 实例化并加载完存档
            yield return new WaitUntil(() => MapManager.Instance != null && MapManager.Instance.allNodes != null);
            GenerateMapUI();
        }

        /// <summary>
        /// 生成地图UI（核心入口）
        /// </summary>
        public void GenerateMapUI()
        {
            ClearMapUI();

            if (MapManager.Instance == null || MapManager.Instance.allNodes == null) return;

            // 运行期兜底：自动补齐引用
            if (mapLineRenderer == null) mapLineRenderer = FindObjectOfType<StraightLineRenderer>();
            if (nodesContainer == null)
            {
                var sr = GetComponentInParent<ScrollRect>();
                if (sr != null) nodesContainer = sr.content;
            }

            // 1. 计算地图边界
            float maxLayoutY = float.MinValue;
            float minLayoutY = float.MaxValue;
            float maxLayoutX = float.MinValue;
            float minLayoutX = float.MaxValue;

            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                if (nodeData.position.y > maxLayoutY) maxLayoutY = nodeData.position.y;
                if (nodeData.position.y < minLayoutY) minLayoutY = nodeData.position.y;
                if (nodeData.position.x > maxLayoutX) maxLayoutX = nodeData.position.x;
                if (nodeData.position.x < minLayoutX) minLayoutX = nodeData.position.x;
            }

            // 2. 计算布局参数
            float mapWidth = maxLayoutX - minLayoutX;
            float mapHeight = maxLayoutY - minLayoutY;
            
            // 获取容器宽度（用于X轴居中）
            RectTransform contentRect = nodesContainer as RectTransform;
            
            // 修复：优先使用 Viewport 或 屏幕宽度
            float availableWidth = Screen.width;
            ScrollRect parentScroll = GetComponentInParent<ScrollRect>(); // 重命名变量
            if (parentScroll != null && parentScroll.viewport != null)
            {
                availableWidth = parentScroll.viewport.rect.width;
            }
            else if (contentRect != null && contentRect.rect.width > 100) 
            {
                availableWidth = contentRect.rect.width;
            }

            // 计算 X 轴居中偏移量
            // 用户反馈：不需要复杂的居中计算，X坐标为0即可（左对齐）
            float xOffset = 0f;
            
            // 如果确实需要居中，可以使用下面的逻辑，但目前简化为0
            // float xOffset = (mapWidth < availableWidth) ? (availableWidth - mapWidth) / 2f : 100f;
            
            // 5. 渲染器资源自动补救（如果 Inspector 没拖）
            if (uiNodePrefab != null)
            {
                UIMapNode sampleNode = uiNodePrefab.GetComponent<UIMapNode>();
                if (sampleNode != null)
                {
                    // 尝试加载默认图标（需确保 Resources/MapIcons 目录下有对应图片，或者你自己放哪了）
                    // 这里只是一个示例，如果你的项目结构不同，请调整
                    if (sampleNode.combatIcon == null) sampleNode.combatIcon = Resources.Load<Sprite>("MapIcons/Combat");
                    if (sampleNode.eliteIcon == null) sampleNode.eliteIcon = Resources.Load<Sprite>("MapIcons/Elite");
                    if (sampleNode.restIcon == null) sampleNode.restIcon = Resources.Load<Sprite>("MapIcons/Rest");
                    if (sampleNode.shopIcon == null) sampleNode.shopIcon = Resources.Load<Sprite>("MapIcons/Shop");
                    if (sampleNode.eventIcon == null) sampleNode.eventIcon = Resources.Load<Sprite>("MapIcons/Event");
                    if (sampleNode.bossIcon == null) sampleNode.bossIcon = Resources.Load<Sprite>("MapIcons/Boss");
                }
            }

            // 6. 创建节点 UI 并设置位置
            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                CreateUINode(nodeData);
                
                if (dataToUIMap.TryGetValue(nodeData, out var uiNode))
                {
                    // 归一化坐标并应用偏移
                    // 原始坐标减去最小值 -> 变成从(0,0)开始
                    // 加上 xOffset 实现水平居中
                    // 加上 100f 实现底部留白
                    Vector2 normalizedPos = nodeData.position - new Vector2(minLayoutX, minLayoutY);
                    Vector2 finalPos = new Vector2(normalizedPos.x + xOffset, normalizedPos.y + 100f);
                    
                    var rt = uiNode.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = finalPos;
                }
            }

            // 4. 更新 Content 高度
            // 确保高度足够容纳地图高度 + 上下留白
            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, mapHeight + 400f);
            }

            // 3. 批量生成连线（在节点归一化位置设置之后调用）
            if (mapLineRenderer != null)
            {
                // 修改：传入 dataToUIMap 字典，以便 Renderer 找到对应的 UI 对象
                mapLineRenderer.DrawConnections(MapManager.Instance.allNodes, dataToUIMap);
            }

            // 4. 聚焦到当前节点
            if (scrollController != null && MapManager.Instance.currentNode != null)
            {
                scrollController.FocusOnNode(MapManager.Instance.currentNode);
            }
            else
            {
                FocusOnCurrentNode();
            }
            
            // 5. 【修复】强制刷新一次所有 UI 节点的状态（高亮当前节点）
            UpdateUI();
        }

        /// <summary>
        /// 创建单个UI节点
        /// </summary>
        private void CreateUINode(MapNodeData nodeData)
        {
            if (uiNodePrefab == null || nodesContainer == null) return;
            if (nodeData == null)
            {
                Debug.LogError("[MapUI] 尝试为 null 的数据节点创建 UI，已跳过。");
                return;
            }
            
            GameObject nodeObj = Instantiate(uiNodePrefab, nodesContainer);
            
            // 关键修复：确保缩放为 1
            nodeObj.transform.localScale = Vector3.one;
            
            UIMapNode uiNode = nodeObj.GetComponent<UIMapNode>();
            
            if (uiNode != null)
            {
                uiNode.Initialize(nodeData);
                uiNodes.Add(uiNode);
                dataToUIMap[nodeData] = uiNode; // 存入字典供逻辑查询
            }
        }

        /// <summary>
        /// 清空地图UI（防止重复生成）
        /// </summary>
        private void ClearMapUI()
        {
            // 清理节点容器下的所有子物体
            if (nodesContainer != null)
            {
                foreach (Transform child in nodesContainer) Destroy(child.gameObject);
            }

            // 清理线条渲染器的残留
            if (mapLineRenderer != null)
            {
                mapLineRenderer.ClearLines();
            }

            // 清理缓存
            uiNodes.Clear();
            dataToUIMap.Clear();
            
            foreach (var line in lines)
            {
                if (line != null) Destroy(line);
            }
            lines.Clear();
        }

        /// <summary>
        /// 将视图聚焦到当前节点
        /// </summary>
        public void FocusOnCurrentNode()
        {
            if (MapManager.Instance == null || MapManager.Instance.currentNode == null) return;
            MapNodeData current = MapManager.Instance.currentNode;

            ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect == null) return;
            
            // 注意：这里需要根据你的 Content 大小来归一化坐标
            RectTransform content = nodesContainer.GetComponent<RectTransform>();
            if (content == null) return;
            
            // 简单的 y 轴聚焦逻辑
            float contentHeight = content.rect.height;
            if (contentHeight > 0)
            {
                float normalizedPos = (current.position.y + contentHeight / 2f) / contentHeight;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
            }
        }

        /// <summary>
        /// 更新UI状态（切换关卡后调用）
        /// </summary>
        public void UpdateUI()
        {
            foreach (var uiNode in uiNodes)
            {
                if (uiNode != null && uiNode.linkedNodeData != null)
                {
                    uiNode.SyncFromData();
                    
                    // 检查是否是当前节点，设置高亮或图标
                    bool isCurrent = MapManager.Instance.currentNode == uiNode.linkedNodeData;
                    uiNode.SetAsCurrentNode(isCurrent);
                }
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
