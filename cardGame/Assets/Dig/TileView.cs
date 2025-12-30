using UnityEngine;

public class TileView : MonoBehaviour {
    private SpriteRenderer sr;
    private Vector3 originalPos;
    
    [Header("Shake Settings")]
    [Tooltip("震动强度 (位移幅度)")]
    public float shakeMagnitude = 0.05f;
    [Tooltip("震动持续时间 (秒)")]
    public float shakeDuration = 0.1f;

    void Awake() {
        sr = GetComponent<SpriteRenderer>();
        // 注意：Awake 时记录的 localPosition 可能是 (0,0,0) 如果它是动态生成的预制体
        // 但如果它被实例化到了特定位置，Awake 时的 localPosition 就是相对于父级的位置。
        // GridManager 生成时是直接设的世界坐标 (parent=transform)，所以 localPosition = worldPos - parentPos
        // 建议在 Start 或首次调用时记录，或者确保生成后 originalPos 被正确初始化。
        // 为了稳妥，我们在 Awake 记录，但在 GridManager 生成后，位置可能会被修改。
        // 更好的方式是在 OnHit 开始震动前，如果不在震动中，就认为当前位置是“原位”。
    }
    
    void Start() {
         originalPos = transform.localPosition;
    }

    // 当方块被打时调用
    public void OnHit(float healthPercent) {
        // 1. 轻微震动效果
        StopAllCoroutines(); // 停止之前的震动
        // 确保震动前归位，防止多次震动导致位置偏移
        transform.localPosition = originalPos; 
        StartCoroutine(Shake());

        // 2. 颜色变暗，模拟受损
        sr.color = Color.Lerp(Color.white, Color.gray, 1f - healthPercent);
    }

    System.Collections.IEnumerator Shake() {
        float elapsed = 0f;
        while (elapsed < shakeDuration) {
            transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * shakeMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}