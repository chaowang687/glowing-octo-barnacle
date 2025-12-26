// MapUI.cs
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    public class MapUI : MonoBehaviour
    {
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
        /// 
        private Dictionary<MapNodeData, UIMapNode> dataToUIMap = new Dictionary<MapNodeData, UIMapNode>();
        public void GenerateMapUI()
        {
            if (dataToUIMap == null) dataToUIMap = new Dictionary<MapNodeData, UIMapNode>();
            dataToUIMap.Clear(); // 清理旧映射
            ClearMapUI();        // 清理旧对象

            // 1. 创建节点
            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                CreateUINode(nodeData);
            }

            // 2. 创建连线
            foreach (var nodeData in MapManager.Instance.allNodes)
            {
                CreateConnections(nodeData);
            }

           
        }
        // 在 MapUI 类中添加


private void CreateUINode(MapNodeData nodeData)
{
    if (uiNodePrefab == null || nodesContainer == null) return;
    
    GameObject nodeObj = Instantiate(uiNodePrefab, nodesContainer);
    UIMapNode uiNode = nodeObj.GetComponent<UIMapNode>();
    
    if (uiNode != null)
    {
        uiNode.Initialize(nodeData);
        uiNodes.Add(uiNode);
        dataToUIMap[nodeData] = uiNode; // 关键：在此处存入字典
    }
}
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
       
        
        
        private void CreateLine(GameObject start, GameObject end)
        {
            GameObject lineObj = Instantiate(linePrefab, linesContainer);
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
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}