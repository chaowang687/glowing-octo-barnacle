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
            // 确保原始缩放值是正常的（非0）
            if (transform.localScale == Vector3.zero) 
            {
                transform.localScale = Vector3.one;
            }
            originalScale = transform.localScale;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 初始化UI节点并关联数据
        /// </summary>
        public void Initialize(MapNodeData data)
        {
            this.linkedNodeData = data;
            this.nodeType = data.nodeType; // 确保这一行存在
            UpdateVisuals(); // 换图
            if (data.encounterData == null)
            {
        // 只有不为空时才设置 UI（例如图标、名字等）
        // nodeIcon.sprite = data.encounterData.icon; 
            }
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
            // 如果数据变动了（不论是不是自己），都可能影响自己的可达性（比如邻居解锁了我）
            // 但为了性能，我们通常只在 data 是自己，或者 data 是自己的上游时刷新
            // 简单起见，这里只判断是否是自己
            
            // 修正：我们需要检查全局状态，因为邻居完成会导致我解锁
            // 更好的做法是：MapManager 应该通知“所有受影响的节点”刷新
            // 或者：直接在这里判断 id 是否匹配
            
            if (linkedNodeData != null && data != null)
            {
                // 如果变更的是自己，或者变更的节点连接到了自己（即自己是它的下游）
                // 简化逻辑：每次有节点变动，所有 UI 节点都检查一下自己的状态是否需要更新
                // 因为数量不多（几十个），开销可以接受
                SyncFromData();
                
                bool isCurrent = MapManager.Instance.currentNode == linkedNodeData;
                SetAsCurrentNode(isCurrent);
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
        // 更新节点外观
        public void UpdateVisuals()
        {
            if (linkedNodeData == null) return;

            // 1. 【优先】从全局数据管理器同步最新状态
            if (GameDataManager.Instance != null)
            {
                isSelectable = GameDataManager.Instance.unlockedNodeIds.Contains(linkedNodeData.nodeId);
                isVisited = GameDataManager.Instance.completedNodeIds.Contains(linkedNodeData.nodeId);
                
                // 同步回本地数据对象，保持一致
                linkedNodeData.isUnlocked = isSelectable;
                linkedNodeData.isCompleted = isVisited;
            }
            else
            {
                // 降级：使用本地数据状态
                isSelectable = linkedNodeData.isUnlocked;
                isVisited = linkedNodeData.isCompleted;
            }

            // 2. 根据状态设置颜色
            if (nodeIcon != null)
            {
                // 如果节点已解锁或已访问，显示原色；否则置灰
                nodeIcon.color = (isSelectable || isVisited) ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                // 根据类型设置图标
                Sprite targetSprite = null;
                switch (nodeType)
                {
                    case NodeType.Combat: targetSprite = combatIcon; break;
                    case NodeType.Elite:  targetSprite = eliteIcon; break;
                    case NodeType.Shop:   targetSprite = shopIcon; break;
                    case NodeType.Rest:   targetSprite = restIcon; break;
                    case NodeType.Event:  targetSprite = eventIcon; break;
                    case NodeType.Boss:   targetSprite = bossIcon; break;
                }

                if (targetSprite != null)
                {
                    nodeIcon.sprite = targetSprite;
                }
                else
                {
                    // 兜底提示：如果未配置图标，使用默认白色方块并给个颜色，确保可见
                    if (nodeIcon.sprite == null)
                    {
                        // 尝试加载一个默认资源（如果有的话），或者直接不操作让它显示 Image 默认的白色
                        // nodeIcon.color = Color.magenta; // 调试色
                    }
                }
            }

            // 3. 更新高亮框（当前可点且未访问的节点显示高亮）
            if (selectableHighlight != null)
            {
                selectableHighlight.SetActive(isSelectable && !isVisited);
            }

            // 4. 更新已访问遮罩
            if (visitedOverlay != null)
            {
                visitedOverlay.SetActive(isVisited);
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
            // 如果节点已经完成，就不应该再作为“当前待处理节点”进行呼吸动画了
            if (isVisited) 
            {
                isCurrent = false;
            }

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