using UnityEngine;

public class TileView : MonoBehaviour {
    // 四个方向的边缘装饰物体
    public GameObject edgeTop, edgeBottom, edgeLeft, edgeRight;
    private SpriteRenderer sr;
    private Vector3 originalPos;
    
    [Header("Shake Settings")]
    [Tooltip("震动强度 (位移幅度)")]
    public float shakeMagnitude = 0.05f;
    [Tooltip("震动持续时间 (秒)")]
    public float shakeDuration = 0.1f;

    void Awake() {
        // 自动查找 Visual 子物体上的渲染器
        Transform visualTransform = transform.Find("Visual");
        if (visualTransform != null) {
            sr = visualTransform.GetComponent<SpriteRenderer>();
        } else {
            sr = GetComponent<SpriteRenderer>(); // 兜底
        }

        // 自动查找边缘子物体 (如果 Inspector 没拖)
        if (edgeTop == null) edgeTop = FindEdge("EdgeTop");
        if (edgeBottom == null) edgeBottom = FindEdge("EdgeBottom");
        if (edgeLeft == null) edgeLeft = FindEdge("EdgeLeft");
        if (edgeRight == null) edgeRight = FindEdge("EdgeRight");
    }

    private GameObject FindEdge(string name) {
        Transform t = transform.Find(name);
        // 也尝试在 Visual 下面找，或者在 Edges 父节点下找
        if (t == null) t = transform.Find("Edges/" + name);
        if (t == null) t = transform.Find("Visual/" + name);
        return t != null ? t.gameObject : null;
    }
    
    void Start() {
         originalPos = transform.localPosition;
    }
    public void HideAllEdges() {
        if(edgeTop) edgeTop.SetActive(false);
        if(edgeBottom) edgeBottom.SetActive(false);
        if(edgeLeft) edgeLeft.SetActive(false);
        if(edgeRight) edgeRight.SetActive(false);
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
    // 当邻居状态改变时，更新自己的边缘显示
    public void UpdateEdgeVisuals(bool upRevealed, bool downRevealed, bool leftRevealed, bool rightRevealed) {
    // 如果上方被挖开了 (upRevealed == true)，我们就需要显示这个“墙面”
    if(edgeTop) edgeTop.SetActive(upRevealed); 
    if(edgeBottom) edgeBottom.SetActive(downRevealed);
    if(edgeLeft) edgeLeft.SetActive(leftRevealed);
    if(edgeRight) edgeRight.SetActive(rightRevealed);
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