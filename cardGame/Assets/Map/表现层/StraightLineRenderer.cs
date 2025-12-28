using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    public class StraightLineRenderer : MonoBehaviour
    {
        [Header("连线配置")]
        public GameObject linePrefab;      // 关键：拖入一个带 Image 的 UI 预制体
        public float lineWidth = 5f;
        public Color lineColor = Color.gray;
        public Transform lineContainer;   // 存放所有线条的父物体

        // --- 新增：批量绘制地图连线的方法 ---
        // 修改：增加 nodeMap 参数，以便查找数据节点对应的 UI 对象
        public void DrawConnections(MapNodeData[] allNodes, Dictionary<MapNodeData, UIMapNode> nodeMap)
        {
            if (allNodes == null || linePrefab == null || nodeMap == null) return;

            // 1. 清理旧线条
            ClearLines();

            // 2. 遍历所有节点
            foreach (var node in allNodes)
            {
                if (node.connectedNodes == null) continue;

                // 获取起始点的 RectTransform (从 UI 字典中获取)
                if (!nodeMap.TryGetValue(node, out UIMapNode startUINode) || startUINode == null) continue;
                RectTransform startRect = startUINode.GetComponent<RectTransform>();

                foreach (var nextNode in node.connectedNodes)
                {
                    if (nextNode == null) continue;

                    // 获取终点的 RectTransform
                    if (!nodeMap.TryGetValue(nextNode, out UIMapNode endUINode) || endUINode == null) continue;
                    RectTransform endRect = endUINode.GetComponent<RectTransform>();

                    // 3. 生成线条并设置点
                    CreateLine(startRect, endRect);
                }
            }
        }

        private void CreateLine(RectTransform from, RectTransform to)
        {
            // 实例化线条预制体
            GameObject lineObj = Instantiate(linePrefab, lineContainer != null ? lineContainer : transform);
            lineObj.name = $"Line_{from.name}_{to.name}";

            // 获取或添加 SingleLine 逻辑组件（见下方辅助类）
            SingleLine lineScript = lineObj.GetComponent<SingleLine>();
            if (lineScript == null) lineScript = lineObj.AddComponent<SingleLine>();

            // 设置线条属性
            lineScript.Initialize(from, to, lineWidth, lineColor);
        }

        public void ClearLines()
        {
            Transform container = lineContainer != null ? lineContainer : transform;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                // 只销毁自动生成的线条，不销毁脚本所在的物体本身
                if (container.GetChild(i).name.StartsWith("Line_"))
                {
                    Destroy(container.GetChild(i).gameObject);
                }
            }
        }
    }

    // --- 辅助类：处理单条线的位移和旋转 ---
    public class SingleLine : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _img;

        public void Initialize(RectTransform a, RectTransform b, float width, Color color)
        {
            _rect = GetComponent<RectTransform>();
            _img = GetComponent<Image>();

            _img.color = color;
            _img.raycastTarget = false;
            
            // 确保层级在节点下方
            transform.SetAsFirstSibling();

            UpdatePosition(a, b, width);
        }

        private void UpdatePosition(RectTransform pointA, RectTransform pointB, float width)
        {
            Vector3 worldPosA = pointA.position;
            Vector3 worldPosB = pointB.position;
            
            Vector3 midPoint = (worldPosA + worldPosB) / 2f;
            transform.position = midPoint;
            
            Vector3 dir = worldPosB - worldPosA;
            float length = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            _rect.sizeDelta = new Vector2(length, width);
            _rect.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}