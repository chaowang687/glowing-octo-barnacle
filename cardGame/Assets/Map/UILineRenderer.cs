using UnityEngine;
using UnityEngine.UI;
using SlayTheSpireMap;

[RequireComponent(typeof(Image))]
public class UILineRenderer : MonoBehaviour
{
    public RectTransform startPoint;
    public RectTransform endPoint;
    
    private RectTransform rectTransform;
    private Image image;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        UpdateLine();
    }
    
    // 设置连线点
    public void SetPoints(RectTransform from, RectTransform to)
    {
        startPoint = from;
        endPoint = to;
        UpdateLine();
    }
    
    // 更新连线位置和旋转
    void UpdateLine()
    {
        if (startPoint == null || endPoint == null)
            return;
            
        // 计算中点
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 midPos = (startPos + endPos) / 2f;
        
        rectTransform.position = midPos;
        
        // 计算角度和长度
        Vector3 direction = endPos - startPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float length = direction.magnitude;
        
        // 设置旋转和大小
        rectTransform.sizeDelta = new Vector2(length, 5f); // 5是线宽
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    void Update()
    {
        // 实时更新连线（如果需要动态调整）
        UpdateLine();
    }
}