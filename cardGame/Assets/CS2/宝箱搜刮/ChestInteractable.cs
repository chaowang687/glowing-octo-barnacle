using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private int chestLevel = 1;
    [SerializeField] private ChestType chestType = ChestType.Wooden;
    [SerializeField] private Animator chestAnimator;
    
    private bool isOpened = false;
    
    public void Interact()
    {
        if (isOpened) return;
        
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
    }
}

public interface IInteractable
{
    void Interact();
}