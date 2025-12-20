using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InfiniteCarouselController : MonoBehaviour
{
    [System.Serializable]
    public class LayerConfig
    {
        public string layerName = "Ground";  
        public GameObject overridePrefab;    
        public int themeIndexOffset = 0;     
        public float radiusOffset = 0f;      
        public float speedMultiplier = 1.0f; 
        public int sortingOrderOffset = 0;   
        public bool isMainLogicLayer = false;
        public bool syncWithCurrentLevel = false; 
    }

    [Header("多层视差配置")]
    public List<LayerConfig> layers = new List<LayerConfig>();

    [Header("关卡内容配置")]
    public GameObject[] levelPrefabs;        
    public ThemeSequenceSO themeSO;
    public GameObject segmentPrefab;         
    
    [Header("环形参数")]
    public int totalSegments = 30;    
    public float radius = 8f;
    [Range(1.0f, 1.1f)] public float overlapFactor = 1.04f; 

    [Header("移动速度")]
    public float degreesPerSecond = 45f; 

    private List<WorldSegmentItem> _allSegments = new List<WorldSegmentItem>();
    
    private int _globalIndex = 0;           
    private int _currentLevelIndex = 0;      
    private int _segmentsInCurrentLevel = 0; 
    private float _currentTotalRotation = 0f; 
    private bool _isMoving = false;
    public bool IsMoving => _isMoving;

    void Start()
    {
        if (themeSO == null) Debug.LogError("ThemeSO 未关联！");
        _allSegments.Clear();
        foreach (var config in layers)
        {
            CreateLayer(config);
        }
    }

    void CreateLayer(LayerConfig config)
    {
        float angleStep = 360f / totalSegments;
        for (int i = 0; i < totalSegments; i++)
        {
            GameObject prefab = config.overridePrefab != null ? config.overridePrefab : segmentPrefab;
            var go = Instantiate(prefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();
            
            // 身份证：LayerName_isMain_Offset_Sync
            go.name = $"{config.layerName}_{config.isMainLogicLayer}_{config.themeIndexOffset}_{config.syncWithCurrentLevel}";
            
            item.Initialize(config.layerName, config.isMainLogicLayer, config.themeIndexOffset, config.syncWithCurrentLevel);
            item.parallaxMultiplier = config.speedMultiplier;

            if (item.groundRenderer != null)
                item.groundRenderer.sortingOrder += config.sortingOrderOffset;

            go.transform.localScale = new Vector3(go.transform.localScale.x * overlapFactor, go.transform.localScale.y, 1);
            float currentRadius = radius + config.radiusOffset;
            
            ThemeSequenceSO.ThemeConfig theme = config.syncWithCurrentLevel ? 
                themeSO.GetThemeForLevel(_currentLevelIndex) : 
                GetThemeForLayer(i, config.themeIndexOffset);

            // 分层刷新
            var resource = theme?.GetLayerResource(config.layerName);
            item.RefreshWithLayerResource(i * angleStep, currentRadius, resource, theme?.nodeMarkerPrefab);
            
            _allSegments.Add(item);
        }
    }

    public void RollDiceAndMove(int steps) 
    { 
        if(!_isMoving && steps > 0) StartCoroutine(SmoothMoveRoutine(steps)); 
    }

    private IEnumerator SmoothMoveRoutine(int steps) 
    { 
        _isMoving = true;
        float totalAngle = steps * (360f / totalSegments);
        float duration = totalAngle / degreesPerSecond;
        float elapsed = 0f;
        float startAngle = _currentTotalRotation;
        float targetAngle = _currentTotalRotation + totalAngle;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float t = progress < 0.5f ? 4f * progress * progress * progress : 1f - Mathf.Pow(-2f * progress + 2f, 3f) / 2f;
            float newAngle = Mathf.Lerp(startAngle, targetAngle, t);
            RotateWorld(newAngle - _currentTotalRotation);
            _currentTotalRotation = newAngle;
            yield return null;
        }
        RotateWorld(targetAngle - _currentTotalRotation);
        _currentTotalRotation = targetAngle;
        _isMoving = false;
    }

    private void RotateWorld(float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;
        
        foreach (var item in _allSegments)
        {
            item.Rotate(delta);

            if (item.GetAngle() > 100f) 
            {
                float newAngle = item.GetAngle() - 360f;
                string[] parts = item.name.Split('_');
                if (parts.Length < 4) continue;
                
                string lName = parts[0];
                bool isMain = parts[1] == "True";
                int lOffset = int.Parse(parts[2]);
                bool isSync = parts[3] == "True";

                if (isMain && levelPrefabs != null && levelPrefabs.Length > 0)
                {
                    Transform source = levelPrefabs[_currentLevelIndex].transform.GetChild(_globalIndex % totalSegments);
                    item.SyncFromPreset(newAngle, source);
                }
                else
                {
                    ThemeSequenceSO.ThemeConfig theme = isSync ? 
                        themeSO.GetThemeForLevel(_currentLevelIndex) : 
                        GetThemeForLayer(_globalIndex, lOffset);

                    // 必须精准分发 Resource
                    var res = theme?.GetLayerResource(lName);
                    item.RefreshWithLayerResource(newAngle, item.GetRadius(), res, theme?.nodeMarkerPrefab);
                }

                if (isMain) 
                {
                    _globalIndex++;
                    _segmentsInCurrentLevel++;
                    if (_segmentsInCurrentLevel >= totalSegments) 
                    {
                        _currentLevelIndex = (_currentLevelIndex + 1) % (levelPrefabs.Length > 0 ? levelPrefabs.Length : 1);
                        _segmentsInCurrentLevel = 0;
                    }
                }
            }
        }
    }

    ThemeSequenceSO.ThemeConfig GetThemeForLayer(int index, int offset)
    {
        if (themeSO == null || themeSO.themes.Count == 0) return null;
        int circleIndex = index / totalSegments;
        int themeIdx = (circleIndex + offset) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
}