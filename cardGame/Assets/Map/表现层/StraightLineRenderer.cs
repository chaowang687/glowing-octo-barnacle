// StraightLineRenderer.cs - 修复版本
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
        
        private RectTransform _rectTransform;
        private Image _image;
        
        // 属性访问器，确保正确获取组件
        private RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                    if (_rectTransform == null)
                    {
                        _rectTransform = gameObject.AddComponent<RectTransform>();
                    }
                }
                return _rectTransform;
            }
        }
        
        private Image image
        {
            get
            {
                if (_image == null)
                {
                    _image = GetComponent<Image>();
                    if (_image == null)
                    {
                        _image = gameObject.AddComponent<Image>();
                    }
                }
                return _image;
            }
        }
        
        void Awake()
        {
            // 确保组件存在
            EnsureComponents();
        }
        
        void Start()
        {
            EnsureComponents();
            UpdateLine();
        }
        
        void Update()
        {
            if (pointA != null && pointB != null)
            {
                UpdateLine();
            }
        }
        
        // 确保必要的组件存在
        private void EnsureComponents()
        {
            // 强制获取或创建组件
            var rt = rectTransform;
            var img = image;
            
            // 设置默认颜色
            img.color = lineColor;
            img.raycastTarget = false; // 不接收点击事件
            
            // 设置锚点为拉伸
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
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
            {
                // 如果没有两个点，隐藏连线
                if (_image != null)
                    _image.enabled = false;
                return;
            }
            
            // 确保组件已启用
            if (_image != null)
                _image.enabled = true;
            
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
            // 延迟调用以避免编辑器错误
            if (this == null) return;
            
            // 使用少量延迟确保组件已初始化
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    EnsureComponents();
                    
                    if (image != null)
                        image.color = lineColor;
                    
                    UpdateLine();
                }
            };
        }
        #endif
        
        void OnDrawGizmos()
        {
            // 编辑器可视化
            #if UNITY_EDITOR
            if (pointA != null && pointB != null)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(pointA.position, pointB.position);
            }
            #endif
        }
    }
}