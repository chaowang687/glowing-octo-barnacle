using UnityEngine;
using UnityEngine.UI;

namespace Bag.UI
{
    /// <summary>
    /// 图鉴按钮控制器，用于在主页打开图鉴
    /// </summary>
    public class CodexButtonController : MonoBehaviour
    {
        [Header("引用设置")]
        public Button codexButton; // 图鉴按钮引用
        public ItemCodexUI codexUI; // 图鉴UI引用
        
        private void Awake()
        {
            // 注册按钮点击事件
            if (codexButton != null)
            {
                codexButton.onClick.AddListener(OpenCodex);
            }
            
            // 确保图鉴UI初始时处于关闭状态
            if (codexUI != null)
            {
                codexUI.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 打开图鉴
        /// </summary>
        public void OpenCodex()
        {
            if (codexUI != null)
            {
                codexUI.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("CodexUI引用未设置，请在Inspector中拖入图鉴UI对象");
                
                // 尝试自动查找图鉴UI
                codexUI = FindObjectOfType<ItemCodexUI>();
                if (codexUI != null)
                {
                    codexUI.gameObject.SetActive(true);
                    Debug.Log("已自动找到并打开图鉴UI");
                }
                else
                {
                    Debug.LogError("未找到ItemCodexUI组件，请确保场景中已添加图鉴UI");
                }
            }
        }
        
        /// <summary>
        /// 关闭图鉴
        /// </summary>
        public void CloseCodex()
        {
            if (codexUI != null)
            {
                codexUI.gameObject.SetActive(false);
            }
        }
    }
}