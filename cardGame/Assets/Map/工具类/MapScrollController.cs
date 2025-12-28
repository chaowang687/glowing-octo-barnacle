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
        public float topPadding = 200f;    // 顶部（Boss上方）留白
        public float bottomPadding = 200f; // 底部（起点下方）留白
        public float scrollDuration = 0.5f; // 自动聚焦时的平滑时间

        private ScrollRect scrollRect;

        void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null)
            {
                content = scrollRect.content;
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
            foreach (var node in allNodes)
            {
                if (node.position.y > maxY) maxY = node.position.y;
            }

            // 设置 Content 的高度：最高节点 Y 坐标 + 偏移
            content.sizeDelta = new Vector2(content.sizeDelta.x, maxY + topPadding + bottomPadding);
            
            // 初始状态通常滚动到底部（起始点）
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// 自动平滑滚动到当前节点位置
        /// </summary>
        public void FocusOnNode(MapNodeData targetNode)
        {
            if (targetNode == null) return;
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) content = scrollRect.content;
            if (content == null || scrollRect == null) return;

            float contentHeight = content.sizeDelta.y;
            // 计算目标节点在 0-1 之间的归一化位置
            float targetNormalizedPos = (targetNode.position.y + bottomPadding) / contentHeight;
            
            StopAllCoroutines();
            StartCoroutine(SmoothScroll(targetNormalizedPos));
        }

        private IEnumerator SmoothScroll(float targetPos)
        {
            float elapsedTime = 0;
            float startPos = scrollRect.verticalNormalizedPosition;

            while (elapsedTime < scrollDuration)
            {
                elapsedTime += Time.deltaTime;
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, targetPos, elapsedTime / scrollDuration);
                yield return null;
            }
            scrollRect.verticalNormalizedPosition = targetPos;
        }
    }
}
