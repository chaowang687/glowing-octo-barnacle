using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SlayTheSpireMap
{
    [RequireComponent(typeof(ScrollRect))]
    public class MapScrollController : MonoBehaviour
    {
        [Header("配置")]
        public RectTransform content;
        public Image mapBackground; // 地图背景图片引用
        public float topPadding = 200f;    // 顶部（Boss上方）留白
        public float bottomPadding = 200f; // 底部（起点下方）留白
        public float scrollDuration = 0.5f; // 自动聚焦时的平滑时间
        public float mouseWheelSpeed = 0.1f; // 鼠标滚轮滚动速度

        private ScrollRect scrollRect;
        private RectTransform viewport;
        private float backgroundHeight; // 背景图片高度
        private float viewportHeight; // 视口高度

        void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null)
            {
                content = scrollRect.content;
                // 确保ScrollRect的content属性也设置正确
                scrollRect.content = content;
            }
            
            // 获取viewport引用，如果没有则使用ScrollRect的父对象
            if (scrollRect != null)
            {
                if (scrollRect.viewport == null)
                {
                    // 如果没有设置viewport，使用ScrollRect的父对象作为默认viewport
                    scrollRect.viewport = scrollRect.transform.parent as RectTransform;
                }
                viewport = scrollRect.viewport;
            }
            
            // 配置ScrollRect属性 - 锁死水平滚动，只允许垂直滚动，无回弹效果
            if (scrollRect != null)
            {
                scrollRect.horizontal = false; // 禁止水平滚动
                scrollRect.vertical = true;     // 允许垂直滚动
                scrollRect.movementType = ScrollRect.MovementType.Clamped; // 无回弹，到边界停止
                scrollRect.elasticity = 0f; // 去掉弹性
                scrollRect.inertia = true;   // 保留惯性
                scrollRect.decelerationRate = 0.135f;
            }
            
            // 计算背景图片高度
            UpdateBackgroundHeight();
        }
        
        void Start()
        {
            // 延迟一帧更新，确保所有UI元素都已初始化
            StartCoroutine(DelayedUpdate());
        }
        
        void OnEnable()
        {
            // 当对象启用时更新背景高度
            UpdateBackgroundHeight();
        }
        
        private IEnumerator DelayedUpdate()
        {
            yield return null;
            // 更新背景高度和视口高度
            UpdateBackgroundHeight();
            
            // 确保ScrollRect配置正确
            EnsureScrollRectConfig();
        }
        
        /// <summary>
        /// 确保ScrollRect配置正确，包括内容大小
        /// </summary>
        private void EnsureScrollRectConfig()
        {
            if (scrollRect == null || content == null) return;
            
            // 确保content足够大以支持滚动
            float viewportHeight = viewport != null ? viewport.rect.height : scrollRect.GetComponent<RectTransform>().rect.height;
            if (content.rect.height <= viewportHeight)
            {
                // 如果content高度不够，添加一些额外高度以允许滚动
                float extraHeight = 500f;
                content.sizeDelta = new Vector2(content.sizeDelta.x, viewportHeight + extraHeight);
                Debug.Log("Increased content height to enable scrolling: " + content.sizeDelta.y);
            }
        }
        
        /// <summary>
        /// 更新背景图片高度和视口高度
        /// </summary>
        private void UpdateBackgroundHeight()
        {
            // 获取视口高度
            if (viewport != null)
            {
                viewportHeight = viewport.rect.height;
            }
            else if (scrollRect != null)
            {
                // 如果没有viewport引用，使用scrollRect的RectTransform.rect
                viewportHeight = scrollRect.GetComponent<RectTransform>().rect.height;
            }
            
            // 获取背景图片高度
            if (mapBackground != null)
            {
                // 获取背景图片的实际高度
                Sprite sprite = mapBackground.sprite;
                if (sprite != null)
                {
                    // 计算背景图片在Unity单位中的高度
                    backgroundHeight = sprite.rect.height / sprite.pixelsPerUnit * mapBackground.transform.localScale.y;
                }
                else
                {
                    // 如果没有sprite，使用Image的rectTransform高度
                    backgroundHeight = mapBackground.rectTransform.rect.height;
                }
            }
            else
            {
                // 如果没有背景图片引用，使用content高度作为默认值
                if (content != null)
                {
                    backgroundHeight = content.rect.height;
                }
                else
                {
                    backgroundHeight = 2000f; // 默认值
                }
            }
        }

        void Update()
        {
            // 处理鼠标滚轮
            HandleMouseWheel();
        }

        /// <summary>
        /// 处理鼠标滚轮事件 - 只处理垂直滚动，无回弹
        /// </summary>
        private void HandleMouseWheel()
        {
            if (scrollRect == null) return;
            
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta != 0)
            {
                // 只处理垂直滚动
                scrollRect.verticalNormalizedPosition += scrollDelta * mouseWheelSpeed;
                // 使用Clamp01确保在0-1范围内，无回弹
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }
        }

        /// <summary>
        /// 根据生成的节点位置，自动调整 Content 的高度
        /// </summary>
        public void UpdateContentSize(MapNodeData[] allNodes)
        {
            if (allNodes == null || allNodes.Length == 0) return;
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) content = scrollRect.content;
            if (content == null) return;

            float maxY = 0;
            float minY = float.MaxValue;
            
            foreach (var node in allNodes)
            {
                if (node.position.y > maxY) maxY = node.position.y;
                if (node.position.y < minY) minY = node.position.y;
            }

            // 设置 Content 的高度：最高节点 Y 坐标 - 最低节点 Y 坐标 + 上下留白
            float contentHeight = maxY - minY + topPadding + bottomPadding;
            
            // 更新背景高度
            UpdateBackgroundHeight();
            
            // 确保content高度至少等于背景图片高度
            contentHeight = Mathf.Max(contentHeight, backgroundHeight);
            
            // 保持原有宽度，只更新高度
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
            
            // 初始状态通常滚动到底部（起始点）
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// 自动平滑滚动到当前节点位置 - 只处理垂直方向，无回弹
        /// </summary>
        public void FocusOnNode(MapNodeData targetNode)
        {
            if (targetNode == null) return;
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) content = scrollRect.content;
            if (content == null || scrollRect == null) return;

            float contentHeight = content.sizeDelta.y;
            
            // 计算目标节点在 0-1 之间的归一化位置
            float targetNormalizedPosY = (targetNode.position.y + bottomPadding) / contentHeight;
            
            // 确保归一化位置在 0-1 之间，无回弹
            targetNormalizedPosY = Mathf.Clamp01(targetNormalizedPosY);
            
            StopAllCoroutines();
            StartCoroutine(SmoothScroll(targetNormalizedPosY));
        }

        private IEnumerator SmoothScroll(float targetPosY)
        {
            float elapsedTime = 0;
            float startPosY = scrollRect.verticalNormalizedPosition;

            // 确保目标位置在边界内，无回弹
            float clampedTargetPosY = Mathf.Clamp01(targetPosY);

            while (elapsedTime < scrollDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scrollDuration;
                
                float currentPosY = Mathf.Lerp(startPosY, clampedTargetPosY, t);
                // 确保当前位置在边界内，无回弹
                currentPosY = Mathf.Clamp01(currentPosY);
                
                scrollRect.verticalNormalizedPosition = currentPosY;
                yield return null;
            }
            
            // 确保最终位置在边界内，无回弹
            scrollRect.verticalNormalizedPosition = clampedTargetPosY;
        }
    }
}
