using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems; // 添加点击事件所需命名空间

public class TreasureVisual : MonoBehaviour, IPointerClickHandler { // 添加点击事件接口
    public string treasureId;
    public SpriteRenderer sr; 
    public Animator anim;     
    
    [Header("Animation Settings")]
    [Tooltip("挖掘出后向上浮动的高度 (世界坐标单位)")]
    public float floatHeight = 0.5f;
    [Tooltip("点击或挖掘完成后隐藏原始化石的延迟时间 (秒)")]
    public float hideDelay = 0.05f;

    private Vector3 originalLocalPos; // 记录子物体的初始位置

    public void SetData(Sprite fossilSprite, Vector3 position, int rotationSteps) {
        transform.position = position;
        sr.sprite = fossilSprite;
        transform.rotation = Quaternion.Euler(0, 0, -90 * rotationSteps);

        // 计算中心偏移
        float offsetX = fossilSprite.bounds.size.x / 2f - 0.5f;
        float offsetY = fossilSprite.bounds.size.y / 2f - 0.5f; 
        
        originalLocalPos = new Vector3(offsetX, -offsetY, 0);
        sr.transform.localPosition = originalLocalPos;

        sr.color = new Color(0.15f, 0.15f, 0.15f, 1f); 
    }

    public void OnComplete() {
        sr.color = Color.white;
        
        // 停止之前的动画，移除上浮点亮效果
        StopAllCoroutines();
        
        // 旋转归正
        sr.transform.rotation = Quaternion.identity;
        
        // 直接显示最大亮度，不播放上移动画
        if (anim != null) {
            anim.SetTrigger("Show"); 
        }
        
        // 延迟后隐藏原始物品，给飞行动画留出时间
        StartCoroutine(HideAfterDelay(hideDelay));
    }
    
    // 延迟隐藏原始物品，确保飞行动画完成
    IEnumerator HideAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        // 隐藏原始物品
        gameObject.SetActive(false);
    }
    
    // 点击事件处理
    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("点击了宝藏：" + treasureId);
        
        // 1. 化石原地变亮，旋转归正
        sr.color = Color.white;
        
        // 停止之前的动画
        StopAllCoroutines();
        
        // 旋转归正
        sr.transform.rotation = Quaternion.identity;
        
        // 播放显示动画
        if (anim != null) {
            anim.SetTrigger("Show"); 
        }
        
        // 2. 触发飞行动画
        // 获取GridManager实例
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager != null) {
            // 调用GridManager的方法，生成飞行图标并飞向背包
            gridManager.TriggerItemFlyToBag(treasureId);
        }
        
        // 3. 延迟后隐藏原始物品
        StartCoroutine(HideAfterDelay(hideDelay));
    }
}