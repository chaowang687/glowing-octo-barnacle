using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace SlayTheSpireMap
{
    public class MapUI : MonoBehaviour
    {
        [Header("滚动控制")]
        public MapScrollController scrollController; 
        
        [Header("UI节点预制体")]
        public GameObject uiNodePrefab;
        
        [Header("连线预制体")]
        public GameObject linePrefab;
        
        [Header("容器")]
        public Transform nodesContainer;
        public Transform linesContainer;
        
        [Header("节点间距")]
        public float horizontalSpacing = 150f;
        public float verticalSpacing = 120f;
        
        public List<UIMapNode> uiNodes = new List<UIMapNode>();
        private List<GameObject> lines = new List<GameObject>();
        private Dictionary<MapNodeData, UIMapNode> dataToUIMap = new Dictionary<MapNodeData, UIMapNode>();
        
        void Start()
        {
            if (MapManager.Instance != null)
            {
                GenerateMapUI();
            }
        }
        
        /// <summary>
        /// 生成地图UI
        /// </summary>
        public void GenerateMapUI()
        {
            // 1. 初始化容器和字典
            if (dataToUIMap == null) dataToUIMap = new Dictionary<MapNodeData, UIMapNode>();
            dataToUIMap.Clear(); 
            ClearMapUI();        

            // 2. 先生成所有节点（必须先有节点，字典里才有数据）
            float maxY = 0;
            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                CreateUINode(nodeData);
                if (nodeData.position.y > maxY) maxY = nodeData.position.y;
            }

            // 3. 调整 Content 高度
            RectTransform contentRect = nodesContainer.parent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, maxY + 200f);
            }

            // 4. 【关键修改】最后生成连线（此时 dataToUIMap 已经填满了）
            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                CreateConnections(nodeData);
            }

            // 5. 滚动和聚焦逻辑
            if (scrollController != null)
            {
                scrollController.UpdateContentSize(MapManager.Instance.allNodes);
                
                if (MapManager.Instance.currentNode != null)
                    scrollController.FocusOnNode(MapManager.Instance.currentNode);
            }
        }

        /// <summary>
        /// 创建UI节点
        /// </summary>
        private void CreateUINode(MapNodeData nodeData)
        {
            if (uiNodePrefab == null || nodesContainer == null) return;
            
            GameObject nodeObj = Instantiate(uiNodePrefab, nodesContainer);
            UIMapNode uiNode = nodeObj.GetComponent<UIMapNode>();
            
            if (uiNode != null)
            {
                // 注意：这里只调用一次 Initialize
                uiNode.Initialize(nodeData);
                uiNodes.Add(uiNode);
                dataToUIMap[nodeData] = uiNode; // 关键：在此处存入字典
            }
        }

        /// <summary>
        /// 创建节点之间的连线
        /// </summary>
        private void CreateConnections(MapNodeData nodeData)
        {
            // 优化后的查找：从字典直接取，复杂度 O(1)
            if (dataToUIMap.TryGetValue(nodeData, out UIMapNode startNode))
            {
                foreach (var connectedNode in nodeData.connectedNodes)
                {
                    if (dataToUIMap.TryGetValue(connectedNode, out UIMapNode endNode))
                    {
                        CreateLine(startNode.gameObject, endNode.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 创建单个连线
        /// </summary>
        private void CreateLine(GameObject start, GameObject end)
        {
            // 确保 linesContainer 不为空，否则生成到根目录
            if (linesContainer == null) {
                Debug.LogError("MapUI: Lines Container 未在 Inspector 中分配！");
                return;
            }

            // 关键：Instantiate 的第二个参数必须是 linesContainer
            GameObject lineObj = Instantiate(linePrefab, linesContainer);
            lineObj.name = $"Line_{start.name}_to_{end.name}";

            StraightLineRenderer lineRenderer = lineObj.GetComponent<StraightLineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.SetPoints(
                    start.GetComponent<RectTransform>(),
                    end.GetComponent<RectTransform>()
                );
            }
            
            lines.Add(lineObj);
        }
        
        /// <summary>
        /// 将视图聚焦到当前节点
        /// </summary>
        public void FocusOnCurrentNode()
        {
            MapNodeData current = MapManager.Instance.currentNode;
            if (current == null) return;

            ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect == null) return;
            
            RectTransform content = nodesContainer.parent.GetComponent<RectTransform>();
            if (content == null) return;
            
            // 计算归一化位置 (0 到 1)
            float targetY = current.position.y;
            float contentHeight = content.sizeDelta.y;
            float normalizedPos = targetY / contentHeight;

            // 设置垂直滚动位置
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
        }

        /// <summary>
        /// 清空地图UI
        /// </summary>
        private void ClearMapUI()
        {
            foreach (var node in uiNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            uiNodes.Clear();
            
            foreach (var line in lines)
            {
                if (line != null) Destroy(line);
            }
            lines.Clear();
        }
        
        /// <summary>
        /// 更新UI状态
        /// </summary>
        public void UpdateUI()
        {
            foreach (var uiNode in uiNodes)
            {
                if (uiNode != null && uiNode.linkedNodeData != null)
                {
                    uiNode.SyncFromData();
                    
                    // 检查是否是当前节点
                    if (MapManager.Instance.currentNode == uiNode.linkedNodeData)
                    {
                        uiNode.SetAsCurrentNode(true);
                    }
                    else
                    {
                        uiNode.SetAsCurrentNode(false);
                    }
                }
            }
        }
        
        /// <summary>
        /// 显示地图UI
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 隐藏地图UI
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}