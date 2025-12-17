using UnityEngine;
using System.Collections.Generic;

public class WorldSegmentItem : MonoBehaviour
{
    public SpriteRenderer groundRenderer;
    public Transform decoContainer;

    [Header("分布配置 (公开可调)")]
    [Range(0, 1)] public float spawnChance = 0.6f;     
    public Vector2 heightOffsetRange = new Vector2(0f, 0.5f); 
    public Vector2 sizeScaleRange = new Vector2(0.7f, 1.3f);  
    public float zSpread = 1.0f;                       

    private float _currentAngle;
    private float _radius;
    private float[] _slotX = new float[] { -1.3f, 0f, 1.3f }; 
    public void SyncFromPreset(float angle, Transform presetSource)
    {
        _currentAngle = angle;

        // 1. 清理当前地块旧的装饰
        for (int i = decoContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(decoContainer.GetChild(i).gameObject);
        }

        // 2. 将预设地块中的装饰物克隆过来
        Transform presetDecoContainer = presetSource.Find("DecoContainer");
        if (presetDecoContainer != null)
        {
            foreach (Transform deco in presetDecoContainer)
            {
                GameObject newDeco = Instantiate(deco.gameObject, decoContainer);
                newDeco.transform.localPosition = deco.localPosition;
                newDeco.transform.localRotation = deco.localRotation;
                newDeco.transform.localScale = deco.localScale;
            }
        }

        // 3. 同步地面贴图
        var sourceRenderer = presetSource.Find("GroundVisual")?.GetComponent<SpriteRenderer>();
        if (sourceRenderer != null && groundRenderer != null)
        {
            groundRenderer.sprite = sourceRenderer.sprite;
        }

        UpdatePosition();
    }
    public void Refresh(float angle, float radius, ThemeSequenceSO.ThemeConfig theme)
{
    _currentAngle = angle;
    _radius = radius;
    
    // 1. 彻底清理旧装饰 (适配编辑器模式)
    if (decoContainer != null)
    {
        for (int i = decoContainer.childCount - 1; i >= 0; i--)
        {
            // 编辑器下必须使用 DestroyImmediate，否则循环会因报错中断
            if (Application.isPlaying) 
                Destroy(decoContainer.GetChild(i).gameObject);
            else 
                DestroyImmediate(decoContainer.GetChild(i).gameObject);
        }
    }

    // 2. 判空保护：只有 theme 不为 null 时才执行资源分配和随机生成
    if (theme != null) 
    {
        if (groundRenderer != null) groundRenderer.sprite = theme.groundSprite;
        
        // 自动随机生成逻辑 (仅在有配置时运行)
        if (theme.decorations != null && theme.decorations.Length > 0)
        {
            for (int i = 0; i < _slotX.Length; i++)
            {
                if (Random.value > spawnChance) continue;
                var prefab = theme.decorations[Random.Range(0, theme.decorations.Length)];
                var deco = Instantiate(prefab, decoContainer);
                float posX = _slotX[i] + Random.Range(-0.2f, 0.2f);
                float posY = Random.Range(heightOffsetRange.x, heightOffsetRange.y);
                float posZ = Random.Range(-zSpread, zSpread);
                deco.transform.localPosition = new Vector3(posX, posY, posZ);
                float scale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
                deco.transform.localScale = new Vector3(scale, scale, scale);
                var sr = deco.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = Random.value > 0.5f;
            }
        }
    }
    
    // 3. 无论 theme 是否为空，都必须更新位置，确保排成圆圈
    UpdatePosition(); 
}

    public void Rotate(float deltaAngle)
    {
        _currentAngle += deltaAngle;
        UpdatePosition();
    }

    public float GetAngle() 
    {
        return _currentAngle;
    }

    private void UpdatePosition()
    {
        float rad = _currentAngle * Mathf.Deg2Rad;
        // 计算地块在圆周上的坐标
        transform.localPosition = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * _radius;
        // 让地块始终垂直于圆心
        transform.localRotation = Quaternion.Euler(0, 0, -_currentAngle);
    }
}