using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace SlayTheSpireMap
{
    public class MapNode : MonoBehaviour, IPointerClickHandler
    {
        public NodeType nodeType;
        public int layer;
        public int indexInLayer;
        public bool isVisited;
        public bool isSelectable;
        
        [Header("UI References")]
        public Image nodeIcon;
        public GameObject selectableHighlight;
        public GameObject visitedOverlay;
        
        [Header("图标配置")]
        public Sprite combatIcon;
        public Sprite eliteIcon;
        public Sprite eventIcon;
        public Sprite shopIcon;
        public Sprite restIcon;
        public Sprite bossIcon;
        
        [Header("动画设置")]
        public float animationScale = 1.2f;
        public float animationSpeed = 2f;
        
        private Vector3 originalScale;
        private Coroutine currentAnimation;
        private bool isCurrentNode = false;
        
        private void Start()
        {
            originalScale = transform.localScale;
            UpdateVisuals();
        }
        
        // 初始化节点
        public void Initialize(NodeType type, int layerNum, int index)
        {
            nodeType = type;
            layer = layerNum;
            indexInLayer = index;
            isVisited = false;
            isSelectable = false;
            UpdateVisuals();
        }
        
        // 更新节点外观
        public void UpdateVisuals()
        {
            // 设置图标
            switch(nodeType)
            {
                case NodeType.Combat:
                    nodeIcon.sprite = combatIcon;
                    break;
                case NodeType.Elite:
                    nodeIcon.sprite = eliteIcon;
                    break;
                case NodeType.Event:
                    nodeIcon.sprite = eventIcon;
                    break;
                case NodeType.Shop:
                    nodeIcon.sprite = shopIcon;
                    break;
                case NodeType.Rest:
                    nodeIcon.sprite = restIcon;
                    break;
                case NodeType.Boss:
                    nodeIcon.sprite = bossIcon;
                    break;
            }
            
            // 设置高亮和访问状态
            selectableHighlight.SetActive(isSelectable && !isVisited);
            visitedOverlay.SetActive(isVisited);
            
            // 调整颜色
            nodeIcon.color = isVisited ? Color.gray : Color.white;
            
            // 如果是当前节点且有动画，确保动画运行
            if (isCurrentNode && currentAnimation == null)
            {
                StartCurrentNodeAnimation();
            }
        }
        
        // 设置节点可选择
        public void SetSelectable(bool selectable)
        {
            isSelectable = selectable;
            UpdateVisuals();
        }
        
        // 访问节点
        public void VisitNode()
        {
            isVisited = true;
            isSelectable = false;
            StopCurrentNodeAnimation();
            UpdateVisuals();
        }
        
        // 设置为当前节点（开始/停止动画）
        public void SetAsCurrentNode(bool isCurrent)
        {
            isCurrentNode = isCurrent;
            
            if (isCurrent)
            {
                StartCurrentNodeAnimation();
            }
            else
            {
                StopCurrentNodeAnimation();
            }
        }
        
        // 开始当前节点动画
        private void StartCurrentNodeAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            currentAnimation = StartCoroutine(CurrentNodeAnimation());
        }
        
        // 停止当前节点动画
        private void StopCurrentNodeAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            
            // 恢复原始大小
            transform.localScale = originalScale;
        }
        
        // 当前节点动画协程
        private IEnumerator CurrentNodeAnimation()
        {
            float time = 0f;
            
            while (true)
            {
                // 使用正弦波实现循环缩放
                float scaleMultiplier = 1f + (animationScale - 1f) * Mathf.Sin(time * animationSpeed);
                transform.localScale = originalScale * scaleMultiplier;
                
                time += Time.deltaTime;
                yield return null;
            }
        }
        
        // 点击事件
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isSelectable && !isVisited && MapManager.Instance != null)
            {
                MapManager.Instance.OnNodeSelected(this);
            }
        }
        
        // 获取节点类型的中文名称
        public string GetNodeTypeName()
        {
            switch(nodeType)
            {
                case NodeType.Combat: return "战斗";
                case NodeType.Elite: return "精英";
                case NodeType.Event: return "事件";
                case NodeType.Shop: return "商店";
                case NodeType.Rest: return "休息";
                case NodeType.Boss: return "Boss";
                default: return "未知";
            }
        }
    }
}