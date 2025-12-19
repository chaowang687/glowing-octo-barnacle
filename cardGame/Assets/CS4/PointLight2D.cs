using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class PointLight2D : MonoBehaviour
{
    [Header("灯光核心属性")]
    public Color lightColor = Color.white;
    [Range(0, 10)] public float intensity = 1.2f;

    private SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void OnValidate() => UpdateLight();
    
    void Update() => UpdateLight();

    private void UpdateLight()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        
        // 只要物体是激活的，就计算颜色
        Color finalColor = lightColor;
        finalColor.a = intensity / 5f; 
        sr.color = finalColor;
    }
}