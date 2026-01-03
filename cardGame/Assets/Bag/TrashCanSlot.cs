using UnityEngine;
using UnityEngine.EventSystems;
using Bag;

public class TrashCanSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        // 获取当前被拖拽的 UI 物体
        ItemUI draggedItem = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (draggedItem != null)
        {
            InventoryManager.Instance.DropItem(draggedItem);
        }
    }
}