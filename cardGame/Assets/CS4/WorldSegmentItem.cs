using UnityEngine;
using System.Collections.Generic;

public class WorldSegmentItem : MonoBehaviour
{
    [Header("层级配置")]
    public string layerName;
    public bool isMainLayer;
    public int layerThemeOffset;
    public bool syncWithCurrentLevel;
    public float parallaxMultiplier = 1.0f;
    
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

    private static MaterialPropertyBlock _sharedPropBlock;
    private static readonly int CurrentAngleID = Shader.PropertyToID("_CurrentAngle");

    public void Initialize(string lName, bool isMain, int themeOffset, bool sync)
    {
        layerName = lName;
        isMainLayer = isMain;
        layerThemeOffset = themeOffset;
        syncWithCurrentLevel = sync;
    }

    /// <summary>
    /// 核心修复：这个方法现在会根据地块自身的 layerName 自动从主题包中提取正确资源
    /// 解决了 WorldEditorHelper.cs 的编译错误，同时也修复了三层同图的 Bug
    /// </summary>
    public void Refresh(float angle, float radius, ThemeSequenceSO.ThemeConfig theme)
    {
        if (theme == null) return;
        
        // 自动识别我是哪一层，如果是空的 layerName，默认回退到 Ground
        string targetLayer = string.IsNullOrEmpty(layerName) ? "Ground" : layerName;
        var resource = theme.GetLayerResource(targetLayer);
        
        // 调用统一的资源刷新逻辑
        RefreshWithLayerResource(angle, radius, resource, theme.nodeMarkerPrefab);
    }

    /// <summary>
    /// 精准刷新逻辑：由 Controller 或 内部 Refresh 调用
    /// </summary>
    public void RefreshWithLayerResource(float angle, float radius, ThemeSequenceSO.LayerResource layerResource, GameObject nodeMarkerPrefab = null)
    {
        _currentAngle = angle;
        _radius = radius;

        // 1. 清理旧内容
        if (nodeMarkerAnchor != null)
        {
            for (int i = nodeMarkerAnchor.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(nodeMarkerAnchor.GetChild(i).gameObject);
                else DestroyImmediate(nodeMarkerAnchor.GetChild(i).gameObject);
            }
        }
        ClearDecos();

        // 2. 资源分离保护：如果该层配置为空，隐藏渲染器
        if (layerResource == null || layerResource.groundSprite == null)
        {
            if (groundRenderer != null) groundRenderer.enabled = false;
            UpdatePosition();
            return;
        }

        // 3. 应用视觉资源
        if (groundRenderer != null) 
        {
            groundRenderer.enabled = true;
            groundRenderer.sprite = layerResource.groundSprite;
            if (layerResource.material != null)
                groundRenderer.sharedMaterial = layerResource.material;
            groundRenderer.color = layerResource.tintColor;
        }
        
        if (nodeMarkerAnchor != null && nodeMarkerPrefab != null)
            Instantiate(nodeMarkerPrefab, nodeMarkerAnchor);
        
        // 4. 生成装饰物
        if (layerResource.decorations != null && layerResource.decorations.Length > 0)
        {
            for (int i = 0; i < _slotX.Length; i++)
            {
                if (Random.value > spawnChance) continue;
                var prefab = layerResource.decorations[Random.Range(0, layerResource.decorations.Length)];
                var deco = Instantiate(prefab, decoContainer);
                deco.transform.localPosition = new Vector3(_slotX[i] + Random.Range(-0.2f, 0.2f), Random.Range(heightOffsetRange.x, heightOffsetRange.y), Random.Range(-zSpread, zSpread));
                float scale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
                deco.transform.localScale = Vector3.one * scale;
                if (deco.TryGetComponent<SpriteRenderer>(out var sr)) sr.flipX = Random.value > 0.5f;
            }
        }
        UpdatePosition(); 
    }

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
        {
            groundRenderer.enabled = true;
            groundRenderer.sprite = sourceRenderer.sprite;
            groundRenderer.color = sourceRenderer.color;
            if (sourceRenderer.sharedMaterial != null) groundRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            groundRenderer.transform.localScale = sourceRenderer.transform.localScale;
            groundRenderer.transform.localRotation = sourceRenderer.transform.localRotation;
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
        float rad = _currentAngle * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * _radius;
        transform.localRotation = Quaternion.Euler(0, 0, -_currentAngle);

        if (groundRenderer != null && groundRenderer.enabled) 
        {
            if (_sharedPropBlock == null) _sharedPropBlock = new MaterialPropertyBlock();
            groundRenderer.GetPropertyBlock(_sharedPropBlock);
            _sharedPropBlock.SetFloat(CurrentAngleID, _currentAngle); 
            groundRenderer.SetPropertyBlock(_sharedPropBlock);
        }
    }
}