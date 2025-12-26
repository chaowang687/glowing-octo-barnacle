using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace SlayTheSpireMap
{
    public class UIMapNode : MonoBehaviour, IPointerClickHandler
    {
        // 关联的数据节点
        [System.NonSerialized]
        public MapNodeData linkedNodeData;
        
        [Header("节点显示属性")]
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
        
        /// <summary>
        /// 初始化UI节点并关联数据
        /// </summary>
        public void Initialize(MapNodeData data)
        {
            if (data == null)
            {
                Debug.LogError("UIMapNode: 初始化数据为空");
                return;
            }
            
            linkedNodeData = data;
            nodeType = data.nodeType;
            isVisited = data.isCompleted;
            isSelectable = data.isUnlocked && !data.isCompleted;
            
            // 可以设置其他UI属性，如位置等
            if (TryGetComponent<RectTransform>(out var rectTransform))
            {
                rectTransform.anchoredPosition = data.position;
            }
            
            UpdateVisuals();
        }
        private void OnEnable()
        {
            // 合并后的逻辑：
            // 1. 订阅全局状态变更事件（用于刷新 UI 状态）
            MapManager.OnNodeStatusChanged += HandleNodeStatusChanged;
            
            // 2. 如果有其他初始化逻辑也放在这里
            if (linkedNodeData != null)
            {
                SyncFromData();
            }
        }

        private void OnDisable()
        {
            // 取消订阅，防止内存泄漏
            MapManager.OnNodeStatusChanged -= HandleNodeStatusChanged;
        }
      

    
        private void HandleNodeStatusChanged(MapNodeData data)
        {
             if(this.linkedNodeData == data) {
                UpdateVisuals(); // 仅刷新自己
            }
            // 关键：只有当变化的数据是自己关联的数据时，才执行刷新
            if (data == linkedNodeData)
            {
                SyncFromData(); // 内部调用 UpdateVisuals()
                // 检查自己是不是当前节点，决定是否播呼吸动画
                SetAsCurrentNode(MapManager.Instance.currentNode == linkedNodeData);
                Debug.Log($"UI节点 {data.nodeId} 已局部更新");
            }
        }
        /// <summary>
        /// 从数据节点同步状态到UI
        /// </summary>
        public void SyncFromData()
        {
            if (linkedNodeData == null) return;
            
            isVisited = linkedNodeData.isCompleted;
            isSelectable = linkedNodeData.isUnlocked && !linkedNodeData.isCompleted;
            UpdateVisuals();
        }
        
        // 更新节点外观
        public void UpdateVisuals()
        {
            // 设置图标
            switch(nodeType)
            {
                case NodeType.Combat:
                    if (combatIcon != null) nodeIcon.sprite = combatIcon;
                    break;
                case NodeType.Elite:
                    if (eliteIcon != null) nodeIcon.sprite = eliteIcon;
                    break;
                case NodeType.Event:
                    if (eventIcon != null) nodeIcon.sprite = eventIcon;
                    break;
                case NodeType.Shop:
                    if (shopIcon != null) nodeIcon.sprite = shopIcon;
                    break;
                case NodeType.Rest:
                    if (restIcon != null) nodeIcon.sprite = restIcon;
                    break;
                case NodeType.Boss:
                    if (bossIcon != null) nodeIcon.sprite = bossIcon;
                    break;
            }
            
            // 设置高亮和访问状态
            if (selectableHighlight != null)
                selectableHighlight.SetActive(isSelectable && !isVisited);
            
            if (visitedOverlay != null)
                visitedOverlay.SetActive(isVisited);
            
            // 调整颜色
            if (nodeIcon != null)
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
            
            // 更新数据节点
            if (linkedNodeData != null)
            {
                linkedNodeData.isCompleted = true;
            }
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
                MapManager.Instance.OnUINodeClicked(this);
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