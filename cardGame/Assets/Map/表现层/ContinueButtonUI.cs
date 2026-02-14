using UnityEngine;
using UnityEngine.UI;

namespace SlayTheSpireMap
{
    public class ContinueButtonUI : MonoBehaviour
    {
        [Header("UI引用")]
        public Button continueButton;
        public Text buttonText;
        public Text nodeInfoText;
        
        [Header("文本设置")]
        public string defaultText = "继续";
        public string bossText = "挑战Boss";
        public string combatText = "开始战斗";
        public string eliteText = "挑战精英";
        
        void Start()
        {
            if (continueButton == null)
                continueButton = GetComponent<Button>();
                
            if (buttonText == null)
                buttonText = continueButton.GetComponentInChildren<Text>();
                
            // 绑定按钮事件
            if (continueButton != null)
            {
                // 使用匿名函数包装，确保调用时 MapManager.Instance 肯定是最新的
                continueButton.onClick.AddListener(() => {
                    if (MapManager.Instance != null)
                    {
                        MapManager.Instance.OnContinueButtonClicked();
                    }
                });
            }
            
            // Start 中也执行一次刷新，作为双重保险
            // 适用于组件已经 Enable 但 MapManager 还没准备好的情况
            if (MapManager.Instance != null && MapManager.Instance.currentNode != null)
            {
                RefreshButtonUI(MapManager.Instance.currentNode);
            }
        }
        
       
        void OnEnable()
        {
            // 订阅事件：当玩家移动到新节点时刷新按钮
            MapManager.OnCurrentNodeChanged += HandleCurrentNodeChanged;
            
            // 关键修复：启用时主动获取一次当前状态，防止事件已错过
            if (MapManager.Instance != null && MapManager.Instance.currentNode != null)
            {
                RefreshButtonUI(MapManager.Instance.currentNode);
            }
        }

        void OnDisable()
        {
            MapManager.OnCurrentNodeChanged -= HandleCurrentNodeChanged;
        }

        
        private void HandleCurrentNodeChanged(MapNodeData newNode)
        {
            RefreshButtonUI(newNode);
        }

        private void RefreshButtonUI(MapNodeData node)
        {
            // Debug: 追踪调用来源和传入数据
            Debug.Log($"[ContinueButtonUI] RefreshButtonUI Called. Node: {(node == null ? "NULL" : node.nodeId + " (" + node.nodeName + ")")}");
            if (MapManager.Instance != null)
            {
                Debug.Log($"[ContinueButtonUI] MapManager CurrentNode: {(MapManager.Instance.currentNode == null ? "NULL" : MapManager.Instance.currentNode.nodeId)}");
            }

            if (node == null) 
            {
                // 尝试最后的补救：从 MapManager 强制拉取
                if (MapManager.Instance != null && MapManager.Instance.currentNode != null)
                {
                    node = MapManager.Instance.currentNode;
                    Debug.Log($"[ContinueButtonUI] 修正：从 MapManager 重新获取到 Node: {node.nodeId}");
                }
                
                if (node == null)
                {
                    if (nodeInfoText != null) nodeInfoText.text = "请选择起点";
                    if (buttonText != null) buttonText.text = "请选择节点";
                    SetInteractable(false); // 禁用按钮
                    Debug.LogWarning("[ContinueButtonUI] 最终 Node 仍为空，禁用按钮。");
                    return;
                }
            }

            // 恢复交互
            SetInteractable(true);

            // 更新节点信息
            if (nodeInfoText != null)
                nodeInfoText.text = MapManager.Instance.GetCurrentNodeInfo();

            // 更新按钮文本
            if (buttonText != null)
            {
                Debug.Log($"[ContinueButtonUI] 更新按钮文本为类型: {node.nodeType}");
                switch(node.nodeType)
                {
                    case NodeType.Boss: buttonText.text = bossText; break;
                    case NodeType.Combat: buttonText.text = combatText; break;
                    case NodeType.Elite: buttonText.text = eliteText; break;
                    case NodeType.Shop: buttonText.text = "进入商店"; break;
                    case NodeType.Rest: buttonText.text = "休息一下"; break;
                    case NodeType.Event: buttonText.text = "探索事件"; break;
                    default: buttonText.text = defaultText; break;
                }
            }
            
        }
        
        
        // 设置按钮交互状态
        public void SetInteractable(bool interactable)
        {
            if (continueButton != null)
            {
                continueButton.interactable = interactable;
            }
        }
    }
}