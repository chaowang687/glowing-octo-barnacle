using UnityEngine;
using UnityEngine.EventSystems;

namespace Bag
{

public class ItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private bool isDragging = false;
    public ItemInstance itemInstance;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private InventoryGrid currentGrid; // 当前所属的网格
   // ItemUI.cs 内部修改
private InventoryGrid originalGrid; // 记录抓起时的网格
private Vector2Int originalPos;    // 记录抓起时的坐标

    void Awake() {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }
    // 在 ItemUI.cs 内部添加
public void StartManualDrag() {
    isDragging = true;
    canvasGroup.blocksRaycasts = false;
    canvasGroup.alpha = 0.7f;
    // 此时 originalGrid 可以设为 null，因为它是从外界生成的，不是从格子里抓起来的
    originalGrid = null; 
}
    // 在 ItemUI.cs 中添加
public void UpdateVisual() {
    if (itemInstance == null || itemInstance.data == null) return;
    if (rect == null) rect = GetComponent<RectTransform>();

    // 获取当前网格的格子大小 (比如 50px)
    float cellSize = InventoryManager.Instance.CurrentGrid.cellSize;

    // 关键修正：根据物品的原始数据宽度/高度乘以格子大小
    // 注意：这里用 data.width 而不是 CurrentWidth，因为旋转是通过旋转 Z 轴实现的
    rect.sizeDelta = new Vector2(itemInstance.data.width * cellSize, itemInstance.data.height * cellSize);

    // 同步图标
    Transform iconTransform = transform.Find("Icon");
    if (iconTransform != null) {
        iconTransform.GetComponent<UnityEngine.UI.Image>().sprite = itemInstance.data.icon;
        
        // 确保 Icon 的锚点是全拉伸(Stretch)，这样它才会填满 rect.sizeDelta 设置的大小
    }
}
    public void Initialize(ItemInstance item, float cellSize) {
    this.itemInstance = item;
    if (rect == null) rect = GetComponent<RectTransform>();
    
    // 这里的关键：根据数据调整 UI 实际尺寸
    rect.sizeDelta = new Vector2(item.CurrentWidth * cellSize, item.CurrentHeight * cellSize);
    
    // 设置旋转角度
    rect.localEulerAngles = item.isRotated ? new Vector3(0, 0, -90) : Vector3.zero;
    
    // 找到子物体 Icon 并设置图片
    var icon = transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
    if (icon != null) icon.sprite = item.data.icon;
}
    public void OnBeginDrag(PointerEventData eventData) {
        isDragging = true; // 开始拖拽
        canvasGroup.blocksRaycasts = false; // 拖拽时让射线穿透自己，才能检测到下方的格子
        canvasGroup.alpha = 0.7f;
        transform.SetAsLastSibling(); // 显示在最前面

        // 【新增】记录原始信息并从网格中移除占位
        originalGrid = GetGridUnderMouse(eventData);
        if (originalGrid != null) {
            originalPos = originalGrid.GetGridFromPosition(rect.anchoredPosition);
            originalGrid.RemoveItem(itemInstance); // 清理原位占位
        }
    }


    public void OnDrag(PointerEventData eventData) {
        rect.anchoredPosition += eventData.delta / GetComponentInParent<Canvas>().scaleFactor;

        // 实时预览逻辑
        InventoryGrid grid = GetGridUnderMouse(eventData);
        if (grid != null) {
            grid.ShowPlacementPreview(itemInstance, rect.anchoredPosition);
        }
    }
       public void OnEndDrag(PointerEventData eventData) {
        isDragging = false; 
        canvasGroup.blocksRaycasts = true; 
        canvasGroup.alpha = 1f;

        // 1. 如果扔到了 UI 以外（空白处），执行丢弃
        if (!EventSystem.current.IsPointerOverGameObject()) { DropIntoWorld(); return; }

        // 2. 找到鼠标下的网格（统一用 targetGrid 变量名，修复报错）
        InventoryGrid targetGrid = GetGridUnderMouse(eventData); 
        
        if (targetGrid != null) {
            Vector2Int targetPos = targetGrid.GetGridFromPosition(rect.anchoredPosition);
            // 3. 尝试放置
            if (InventoryManager.Instance.TryPlace(itemInstance, targetPos.x, targetPos.y, targetGrid)) {
                SnapToGrid(targetGrid, targetPos);
                targetGrid.ClearPreview(); 
                return;
            }
        }

        // 4. 【关键回弹】如果上面没 return，说明失败了，回原位
        if (originalGrid != null) {
            originalGrid.PlaceItem(itemInstance, originalPos.x, originalPos.y);
            SnapToGrid(originalGrid, originalPos);
            originalGrid.ClearPreview();
        }
    }


private void DropIntoWorld() {
    // 1. 从相机射出一道射线到地面
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out RaycastHit hit)) {
        // 2. 在点击位置生成 3D 模型
        Instantiate(itemInstance.data.worldPrefab, hit.point + Vector3.up, Quaternion.identity);
        
        // 3. 销毁当前 UI
        Destroy(gameObject);
        
        // 4. 注意：还要记得从 InventoryGrid 的数据中移除它
    }
}


    public void SnapToGrid(InventoryGrid grid, Vector2Int pos) {
        rect.anchoredPosition = grid.GetPositionFromGrid(pos.x, pos.y, itemInstance.CurrentWidth, itemInstance.CurrentHeight);
    }
        void Update() {
        // 如果这个物品是被交换出来的，或者正处于跟随鼠标状态
        if (isDragging) {
            // 让坐标跟随鼠标
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform, 
                Input.mousePosition, 
                null, 
                out mousePos);
            rect.anchoredPosition = mousePos;

            if (Input.GetKeyDown(KeyCode.R)) RotateItem();
        }
    }

    
    private InventoryGrid GetGridUnderMouse(PointerEventData eventData) 
    {
        // 增加 eventData 及其成员的判空
        if (eventData != null && eventData.pointerEnter != null)
        {
            return eventData.pointerEnter.GetComponentInParent<InventoryGrid>();
        }
        // 如果 eventData 为空，尝试直接通过全局变量获取（备用方案）
        return InventoryManager.Instance.CurrentGrid; 
    }
        void RotateItem() {
        itemInstance.isRotated = !itemInstance.isRotated;
        
        // 1. 执行旋转表现
        rect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, -90) : Vector3.zero;

        // 2. 坐标修正逻辑 (针对 Pivot 0,1)
        // 旋转后，为了保持物品依然对齐在鼠标抓取点，需要根据格点尺寸偏移
        // 3. 关键修正：顺时针旋转90度后，物品会向上“翻转”出格
        // 我们需要手动调整 anchoredPosition 
        // 偏移量公式：x偏移 = 旋转后高度 * 格子大小 (根据你的 Pivot 设置可能需要微调)
        float cellSize = InventoryManager.Instance.CurrentGrid.cellSize;
    
        if (itemInstance.isRotated) {
            // 顺时针转90度后，向右平移（高度 * 格子大小）
            rect.anchoredPosition += new Vector2(itemInstance.data.height * cellSize, 0);
        } else {
            // 转回来时，向左还原（旋转前的高度，即现在的宽度 * 格子大小）
            rect.anchoredPosition -= new Vector2(itemInstance.data.height * cellSize, 0);
        }
        }

    }


}