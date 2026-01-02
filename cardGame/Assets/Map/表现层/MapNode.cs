using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace SlayTheSpireMap
{
    public class UIMapNode : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
        public Sprite digIcon;
        
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
                
                // 关键修复：当状态变更时（例如从上一关出来，进入了下一层），
                // 需要重新判断自己是否是 CurrentNode。
                // 之前的逻辑只在点击时设置 isCurrentNode，但从存档加载或初始化时可能没同步。
                
                bool isCurrent = false;
                if (MapManager.Instance != null && MapManager.Instance.currentNode != null)
                {
                    // 统一使用 nodeId 字符串匹配，避免对象引用不同导致的问题
                    isCurrent = MapManager.Instance.currentNode.nodeId == linkedNodeData.nodeId;
                }
                
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
            if (GameDataManager.Instance != null && MapManager.Instance != null)
            {
                // 使用 MapManager 的严格校验逻辑来决定是否“可选”
                // 这能解决“解锁列表里有上一层的兄弟节点导致误判”的问题
                isSelectable = MapManager.Instance.IsNodeInteractable(linkedNodeData);
                isVisited = GameDataManager.Instance.completedNodeIds.Contains(linkedNodeData.nodeId);
                
                // 同步回本地数据对象
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
                switch (linkedNodeData.nodeType)
                {
                    case NodeType.Combat: targetSprite = combatIcon; break;
                    case NodeType.Elite:  targetSprite = eliteIcon; break;
                    case NodeType.Event:  targetSprite = eventIcon; break;
                    case NodeType.Shop:   targetSprite = shopIcon; break;
                    case NodeType.Rest:   targetSprite = restIcon; break;
                    case NodeType.Boss:   targetSprite = bossIcon; break;
                    case NodeType.Dig:    targetSprite = digIcon; break;
                }

                if (targetSprite != null)
                {
                    nodeIcon.sprite = targetSprite;
                }
            }

            // 3. 更新高亮框（仅当前选中的节点显示高亮）
            // 修复：之前逻辑是 isSelectable && !isVisited 就会高亮，导致所有解锁层都亮了
            // 现在改为只由 SetAsCurrentNode 控制高亮框的显隐，这里只负责初始化状态（默认关）
            // 或者：如果确实想表达“可达”，可以用另一种视觉（比如微光），而“选中”用强高亮
            // 这里我们遵循用户需求：只保留选中的关有高亮
            if (selectableHighlight != null)
            {
                selectableHighlight.SetActive(isCurrentNode); 
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
        
        // ------------------------ 新增悬停放大逻辑 ------------------------
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 只有已解锁且未访问的节点（即可选节点），鼠标悬停时才播放呼吸动画
            if (isSelectable && !isVisited)
            {
                // 如果当前没有在播放动画（或者不是选中状态导致的动画），则开始呼吸
                if (currentAnimation == null)
                {
                    StartCurrentNodeAnimation();
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 移开时恢复原状
            if (isSelectable && !isVisited)
            {
                 // 如果这个节点不是当前选中的节点，那么移开时就停止动画
                 // 如果是选中的节点，它应该保持呼吸（或者根据需求，选中节点也可能不呼吸？）
                 // 根据“只有选中的节点有高亮框，鼠标悬停播放呼吸动画”的理解：
                 // 选中节点：常亮高亮框
                 // 悬停节点：呼吸动画
                 
                 // 如果当前节点虽然被选中，但鼠标移开了，要不要停？
                 // 用户说“改为鼠标悬停的节点播放呼吸动画”，暗示只有悬停时才呼吸。
                 // 那么我们强制停止。
                 
                 StopCurrentNodeAnimation();
                 
                 // 如果是选中状态，恢复选中时的高亮框逻辑（如果之前有关闭的话，但这里只管动画）
            }
        }
        // -----------------------------------------------------------------

        // 设置为当前节点（控制高亮框显隐，并启用呼吸动画）
        public void SetAsCurrentNode(bool isCurrent)
        {
            isCurrentNode = isCurrent;
            
            // 1. 选中状态的视觉反馈 (高亮框)
            // 只有被选中时才显示高亮框，未选中则关闭
            if (selectableHighlight != null)
            {
                selectableHighlight.SetActive(isCurrent);
            }
            
            // 2. 移除之前的呼吸动画逻辑，只处理图标颜色
            // 用户新需求：改为鼠标悬停播放呼吸动画。所以这里不再自动开启呼吸。
            if (isCurrent)
            {
                // 确保选中时是亮的
                if (nodeIcon != null) nodeIcon.color = Color.white;
            }
            else
            {
                // 如果不再是当前节点，确保动画停止（双重保险）
                StopCurrentNodeAnimation();
                
                // 颜色状态处理
                if (isSelectable)
                {
                     if (nodeIcon != null) nodeIcon.color = new Color(0.8f, 0.8f, 0.8f);
                }
                else if (isVisited)
                {
                     if (nodeIcon != null) nodeIcon.color = new Color(0.5f, 0.5f, 0.5f); // 已访问变灰
                }
                else
                {
                     // 不可达且未访问（未来的节点），变暗
                     if (nodeIcon != null) nodeIcon.color = new Color(0.6f, 0.6f, 0.6f);
                }
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
            // 确保没有全屏遮罩挡住，并且管理器存在
            if (MapManager.Instance != null)
            {
                if (isSelectable && !isVisited)
                {
                    MapManager.Instance.OnUINodeClicked(this);
                }
                else
                {
                    Debug.Log($"[MapNode] 点击无效: Selectable={isSelectable}, Visited={isVisited}");
                }
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
                case NodeType.Dig: return "挖掘场";
                default: return "未知";
            }
        }
    }
}