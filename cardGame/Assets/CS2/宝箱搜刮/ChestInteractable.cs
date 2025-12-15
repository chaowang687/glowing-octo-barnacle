using UnityEngine;
using UnityEngine.EventSystems; // 添加这个命名空间

// 方案A：继承 IPointerClickHandler 实现点击事件
public class ChestInteractable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int chestLevel = 1;
    [SerializeField] private ChestType chestType = ChestType.Wooden;
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private GameObject highlightEffect; // 高亮效果（可选）
    
    private bool isOpened = false;
    private bool isMouseOver = false;
    
    // 方法1：使用 IPointerClickHandler 接口
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isOpened) return;
        
        OpenChest();
    }
    
    // 方法2：使用旧的 OnMouseDown（需要Collider）
    private void OnMouseDown()
    {
        if (isOpened) return;
        
        OpenChest();
    }
    
    // 方法3：显示鼠标悬停效果
    private void OnMouseEnter()
    {
        isMouseOver = true;
        if (highlightEffect != null && !isOpened)
            highlightEffect.SetActive(true);
    }
    
    private void OnMouseExit()
    {
        isMouseOver = false;
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
    }
    
    private void OpenChest()
    {
        // 播放开箱动画
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
        }
        
        // 创建宝箱数据
        ChestData chestData = new ChestData
        {
            chestLevel = chestLevel,
            chestType = chestType
        };
        
        // 打开搜刮UI
        UI_ChestLootWindow.OpenChestWindow(chestData);
        
        isOpened = true;
        
        // 禁用高亮
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
        
        // 播放音效
        PlayOpenSound();
    }
    
    private void PlayOpenSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioClip openSound = Resources.Load<AudioClip>("Audio/SFX/chest_open");
            if (openSound != null)
            {
                AudioManager.Instance.PlaySFX(openSound, 0.8f);
            }
        }
    }
    
    // 可选的交互提示（如显示"按E打开"）
    private void OnGUI()
    {
        if (isMouseOver && !isOpened)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 60, 100, 30), 
                     "点击打开", 
                     new GUIStyle { alignment = TextAnchor.MiddleCenter, 
                                   normal = { textColor = Color.yellow },
                                   fontSize = 12 });
        }
    }
}