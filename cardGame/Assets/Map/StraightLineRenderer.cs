// StraightLineRenderer.cs - 更简单的直线渲染器
using UnityEngine;
using UnityEngine.UI;

namespace SlayTheSpireMap
{
    [ExecuteInEditMode]
    public class StraightLineRenderer : MonoBehaviour
    {
        [Header("连线配置")]
        public RectTransform pointA;
        public RectTransform pointB;
        public float lineWidth = 5f;
        public Color lineColor = Color.gray;
        
        private RectTransform rectTransform;
        private Image image;
        
        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            
            if (image == null)
                image = gameObject.AddComponent<Image>();
            
            image.color = lineColor;
        }
        
        void Start()
        {
            UpdateLine();
        }
        
        void Update()
        {
            if (pointA != null && pointB != null)
            {
                UpdateLine();
            }
        }
        
        public void SetPoints(RectTransform from, RectTransform to)
        {
            pointA = from;
            pointB = to;
            UpdateLine();
        }
        
        void UpdateLine()
        {
            if (pointA == null || pointB == null)
                return;
                
            // 获取世界空间中的位置
            Vector3 worldPosA = pointA.position;
            Vector3 worldPosB = pointB.position;
            
            // 计算中点（世界空间）
            Vector3 midPoint = (worldPosA + worldPosB) / 2f;
            
            // 设置连线位置（世界空间）
            transform.position = midPoint;
            
            // 计算长度和角度
            Vector3 dir = worldPosB - worldPosA;
            float length = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            // 设置大小和旋转
            rectTransform.sizeDelta = new Vector2(length, lineWidth);
            rectTransform.rotation = Quaternion.Euler(0, 0, angle);
            
            // 确保连线在节点下方
            transform.SetAsFirstSibling();
        }
        
        #if UNITY_EDITOR
        void OnValidate()
        {
            if (image != null)
                image.color = lineColor;
            
            UpdateLine();
        }
        #endif
    }
}