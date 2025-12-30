using UnityEngine;
using System.Collections;

public class TreasureVisual : MonoBehaviour {
    public string treasureId;
    public SpriteRenderer sr; 
    public Animator anim;     
    
    [Header("Animation Settings")]
    [Tooltip("挖掘出后向上浮动的高度 (世界坐标单位)")]
    public float floatHeight = 0.5f;

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
        
        // 停止之前的动画，开始“上浮点亮”效果
        StopAllCoroutines();
        StartCoroutine(AnimateDiscovery());

        if (anim != null) {
            anim.SetTrigger("Show"); 
        }
    }

    // 发现化石的动画：世界坐标上浮 + 子物体旋转归正 + 缩放反馈
    IEnumerator AnimateDiscovery() {
        float duration = 0.5f; // 动画时长
        float elapsed = 0f;
        
        // 1. 记录初始状态
        Vector3 startWorldPos = transform.position;
        Vector3 startScale = sr.transform.localScale; 
        
        // 记录子物体（Sprite）的世界旋转
        Quaternion startSpriteRotation = sr.transform.rotation;

        // 2. 设定目标状态
        Vector3 targetWorldPos = startWorldPos + Vector3.up * floatHeight; 
        Vector3 targetScale = Vector3.one * 1.2f; 
        
        // 目标：子物体旋转归正（世界坐标下 Rotation 为 0）
        Quaternion targetSpriteRotation = Quaternion.identity;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            
            // 使用平滑插值
            float curve = Mathf.SmoothStep(0, 1, percent);
            
            // 应用变换
            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, curve);
            sr.transform.localScale = Vector3.Lerp(startScale, targetScale, curve);
            
            // 关键：直接操作子物体的世界旋转
            sr.transform.rotation = Quaternion.Lerp(startSpriteRotation, targetSpriteRotation, curve);
            
            yield return null;
        }
        
        // 确保最终状态准确
        transform.position = targetWorldPos;
        sr.transform.localScale = targetScale;
        sr.transform.rotation = targetSpriteRotation;
    }
}