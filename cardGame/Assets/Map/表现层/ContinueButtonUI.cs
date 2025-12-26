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
            if (continueButton != null && MapManager.Instance != null)
            {
                continueButton.onClick.AddListener(() => MapManager.Instance.OnContinueButtonClicked());
            }
        }
        
       
        void OnEnable()
        {
            // 订阅事件：当玩家移动到新节点时刷新按钮
            MapManager.OnCurrentNodeChanged += HandleCurrentNodeChanged;
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
            if (node == null) 
            {
                // 如果没有当前节点，可能需要隐藏按钮或显示默认文本
                if (nodeInfoText != null) nodeInfoText.text = "请选择起点";
                return;
            }

            // 更新节点信息
            if (nodeInfoText != null)
                nodeInfoText.text = MapManager.Instance.GetCurrentNodeInfo();

            // 更新按钮文本
            if (buttonText != null)
            {
                switch(node.nodeType)
                {
                    case NodeType.Boss: buttonText.text = bossText; break;
                    case NodeType.Combat: buttonText.text = combatText; break;
                    case NodeType.Elite: buttonText.text = eliteText; break;
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