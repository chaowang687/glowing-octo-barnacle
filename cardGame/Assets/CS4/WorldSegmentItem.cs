using UnityEngine;
using System.Collections.Generic;

public class WorldSegmentItem : MonoBehaviour
{
    // 新增字段，用于存储配置信息
    public string layerName;
    public bool isMainLayer;
    public int layerThemeOffset;
    public bool syncWithCurrentLevel;
    
    // 修改现有字段，确保正确初始化
    public void Initialize(string lName, bool isMain, int themeOffset, bool sync)
    {
        layerName = lName;
        isMainLayer = isMain;
        layerThemeOffset = themeOffset;
        syncWithCurrentLevel = sync;
         }
    // 添加这些字段
   // public bool isMainLayer;
   // public int layerThemeOffset;
    public float parallaxMultiplier = 1.0f;
   // public bool syncWithCurrentLevel = false; // 新增：是否与当前关卡同步
    
    [Header("渲染器引用")]
    public SpriteRenderer groundRenderer;
    public Transform decoContainer;
    public Transform nodeMarkerAnchor;
    
    [Header("分布配置")]
    [Range(0, 1)] public float spawnChance = 0.6f;     
    public Vector2 heightOffsetRange = new Vector2(0f, 0.5f); 
    public Vector2 sizeScaleRange = new Vector2(0.7f, 1.3f);  
    public float zSpread = 1.0f;                       

    private float _currentAngle;
    private float _radius;
    private float[] _slotX = new float[] { -1.3f, 0f, 1.3f }; 

    // 性能优化：静态属性块和 ID 缓存
    private static MaterialPropertyBlock _sharedPropBlock;
    private static readonly int CurrentAngleID = Shader.PropertyToID("_CurrentAngle");

    public void SyncFromPreset(float angle, Transform presetSource)
    {
        _currentAngle = angle;
        ClearDecos();

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

        var sourceRenderer = presetSource.Find("GroundVisual")?.GetComponent<SpriteRenderer>();
        if (sourceRenderer != null && groundRenderer != null)
            groundRenderer.sprite = sourceRenderer.sprite;

        UpdatePosition();
    }

    // 新增：使用 LayerResource 刷新
    public void RefreshWithLayerResource(float angle, float radius, ThemeSequenceSO.LayerResource layerResource, GameObject nodeMarkerPrefab = null)
    {
        _currentAngle = angle;
        _radius = radius;

        if (nodeMarkerAnchor != null)
        {
            foreach (Transform child in nodeMarkerAnchor) 
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        ClearDecos();

        if (layerResource != null) 
        {
            if (groundRenderer != null) 
            {
                if (layerResource.groundSprite != null)
                    groundRenderer.sprite = layerResource.groundSprite;
                
                if (layerResource.material != null)
                    groundRenderer.material = layerResource.material;
                
                groundRenderer.color = layerResource.tintColor;
            }
            
            if (nodeMarkerAnchor != null && nodeMarkerPrefab != null)
                Instantiate(nodeMarkerPrefab, nodeMarkerAnchor);
            
            if (layerResource.decorations != null && layerResource.decorations.Length > 0)
            {
                for (int i = 0; i < _slotX.Length; i++)
                {
                    if (Random.value > spawnChance) continue;
                    var prefab = layerResource.decorations[Random.Range(0, layerResource.decorations.Length)];
                    var deco = Instantiate(prefab, decoContainer);
                    deco.transform.localPosition = new Vector3(_slotX[i] + Random.Range(-0.2f, 0.2f), Random.Range(heightOffsetRange.x, heightOffsetRange.y), Random.Range(-zSpread, zSpread));
                    float scale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
                    deco.transform.localScale = new Vector3(scale, scale, scale);
                    if (deco.TryGetComponent<SpriteRenderer>(out var sr)) sr.flipX = Random.value > 0.5f;
                }
            }
        }
        UpdatePosition(); 
    }

    // 保持向后兼容的 Refresh 方法
    public void Refresh(float angle, float radius, ThemeSequenceSO.ThemeConfig theme)
    {
        _currentAngle = angle;
        _radius = radius;

        if (nodeMarkerAnchor != null)
        {
            foreach (Transform child in nodeMarkerAnchor) 
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        ClearDecos();

        if (theme != null) 
        {
            if (groundRenderer != null) groundRenderer.sprite = theme.groundSprite;
            if (nodeMarkerAnchor != null && theme.nodeMarkerPrefab != null)
                Instantiate(theme.nodeMarkerPrefab, nodeMarkerAnchor);
            
            if (theme.decorations != null && theme.decorations.Length > 0)
            {
                for (int i = 0; i < _slotX.Length; i++)
                {
                    if (Random.value > spawnChance) continue;
                    var prefab = theme.decorations[Random.Range(0, theme.decorations.Length)];
                    var deco = Instantiate(prefab, decoContainer);
                    deco.transform.localPosition = new Vector3(_slotX[i] + Random.Range(-0.2f, 0.2f), Random.Range(heightOffsetRange.x, heightOffsetRange.y), Random.Range(-zSpread, zSpread));
                    float scale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
                    deco.transform.localScale = new Vector3(scale, scale, scale);
                    if (deco.TryGetComponent<SpriteRenderer>(out var sr)) sr.flipX = Random.value > 0.5f;
                }
            }
        }
        UpdatePosition(); 
    }

    private void ClearDecos()
    {
        if (decoContainer == null) return;
        for (int i = decoContainer.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(decoContainer.GetChild(i).gameObject);
            else DestroyImmediate(decoContainer.GetChild(i).gameObject);
        }
    }

    public float GetRadius() => _radius;
    public float GetAngle() => _currentAngle;

    public void Rotate(float deltaAngle)
    {
        _currentAngle += (deltaAngle * parallaxMultiplier);
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        // 坐标更新
        float rad = _currentAngle * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * _radius;
        transform.localRotation = Quaternion.Euler(0, 0, -_currentAngle);

        // 极坐标同步逻辑 (解决独立跳动且优化卡顿)
        if (groundRenderer != null) 
        {
            if (_sharedPropBlock == null) _sharedPropBlock = new MaterialPropertyBlock();
            groundRenderer.GetPropertyBlock(_sharedPropBlock);
            _sharedPropBlock.SetFloat(CurrentAngleID, _currentAngle); 
            groundRenderer.SetPropertyBlock(_sharedPropBlock);
        }
    }
}