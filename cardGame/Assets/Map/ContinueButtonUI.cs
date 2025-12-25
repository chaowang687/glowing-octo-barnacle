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
        
        void Update()
        {
            UpdateButtonUI();
        }
        
        void UpdateButtonUI()
        {
            if (MapManager.Instance == null) return;
            
            // 更新节点信息文本
            if (nodeInfoText != null)
            {
                nodeInfoText.text = MapManager.Instance.GetCurrentNodeInfo();
            }
            
            // 根据当前节点类型更新按钮文本
            if (buttonText != null)
            {
                // 这里可以根据需要自定义不同节点类型的按钮文本
                buttonText.text = defaultText;
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