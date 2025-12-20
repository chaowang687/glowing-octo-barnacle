using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InfiniteCarouselController : MonoBehaviour
{
    [System.Serializable]
    public class LayerConfig
    {
        public string layerName = "Layer";
        public GameObject overridePrefab;    
        public int themeIndexOffset = 0;     
        public float radiusOffset = 0f;      
        public float speedMultiplier = 1.0f; 
        public int sortingOrderOffset = 0;   
        public bool isMainLogicLayer = false;
        public bool syncWithCurrentLevel = false; // 新增：是否与当前关卡同步
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
    
    // --- 掷骰子与进度核心变量 ---
    private int _globalIndex = 0;           
    private int _currentLevelIndex = 0;      
    private int _segmentsInCurrentLevel = 0; 
    private float _currentTotalRotation = 0f; 
    private bool _isMoving = false;
    public bool IsMoving => _isMoving;

    void Start()
    {
        Debug.Log($"InfiniteCarouselController 开始初始化");
        Debug.Log($"Total Segments: {totalSegments}");
        Debug.Log($"Level Prefabs Count: {(levelPrefabs != null ? levelPrefabs.Length : 0)}");
        Debug.Log($"ThemeSO: {(themeSO != null ? "已设置" : "未设置")}");
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
            if (prefab == null)
            {
                Debug.LogError($"Prefab is null for layer: {config.layerName}");
                continue;
            }
            
            var go = Instantiate(prefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();
            if (item == null)
            {
                Debug.LogError($"WorldSegmentItem component not found on prefab: {prefab.name}");
                Destroy(go);
                continue;
            }
            
            item.isMainLayer = config.isMainLogicLayer;
            item.layerThemeOffset = config.themeIndexOffset;
            item.parallaxMultiplier = config.speedMultiplier;
            item.syncWithCurrentLevel = config.syncWithCurrentLevel; // 存储同步设置
            
            // 存储更多信息到名字中
            go.name = $"{config.layerName}_{config.isMainLogicLayer}_{config.themeIndexOffset}_{config.syncWithCurrentLevel}";

            if (item.groundRenderer != null)
                item.groundRenderer.sortingOrder += config.sortingOrderOffset;

            Vector3 s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * overlapFactor, s.y, s.z);

            float currentRadius = radius + config.radiusOffset;
            
            // 初始化刷新：根据是否是同步层选择不同的刷新方式
            if (config.syncWithCurrentLevel && themeSO != null)
            {
                // 同步层：使用当前关卡的主题和对应的层资源
                var currentTheme = themeSO.GetThemeForLevel(_currentLevelIndex);
                if (currentTheme != null)
                {
                    var layerResource = currentTheme.GetLayerResource(config.layerName);
                    item.RefreshWithLayerResource(i * angleStep, currentRadius, layerResource, currentTheme.nodeMarkerPrefab);
                }
            }
            else
            {
                // 非同步层：使用原有的Refresh方法
                var theme = GetThemeForLayer(i, config.themeIndexOffset, 0);
                item.Refresh(i * angleStep, currentRadius, theme);
            }
            
            _allSegments.Add(item);
        }
    }

    public void RollDiceAndMove(int steps) 
    { 
        if(_isMoving) 
        {
            Debug.LogWarning("正在移动中，忽略新的移动请求");
            return;
        }
        
        if(steps <= 0)
        {
            Debug.LogError($"无效的步数: {steps}，必须为正数");
            return;
        }
        
        Debug.Log($"开始移动 {steps} 步，当前角度: {_currentTotalRotation}");
        StartCoroutine(SmoothMoveRoutine(steps)); // ✅ 改为调用已存在的方法
    }

   public void SmoothMoveToStep(int steps)
{ 
    if(_isMoving) 
    {
        Debug.LogWarning("正在移动中，忽略新的移动请求");
        return;
    }
    
    if(steps <= 0)
    {
        Debug.LogError($"无效的步数: {steps}，必须为正数");
        return;
    }
    
    Debug.Log($"平滑移动到 {steps} 步，当前角度: {_currentTotalRotation}");
    StartCoroutine(SmoothMoveRoutine(steps)); 
}

private IEnumerator SmoothMoveRoutine(int steps) 
{ 
    _isMoving = true;
    
    // 计算需要旋转的总角度
    float anglePerStep = 360f / totalSegments; 
    float totalAngle = steps * anglePerStep;     // 总旋转角度
    
    // 计算移动时间：角度 / 角速度
    float duration = totalAngle / degreesPerSecond;
    
    Debug.Log($"移动 {steps} 步，角度: {totalAngle}°，时间: {duration}秒，角速度: {degreesPerSecond}°/秒");
    
    float elapsed = 0f;
    float startAngle = _currentTotalRotation;
    float targetAngle = _currentTotalRotation + totalAngle;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        
        // 使用缓动函数（可选）
        float t = EaseInOutCubic(progress);
        
        float newAngle = Mathf.Lerp(startAngle, targetAngle, t);
        float delta = newAngle - _currentTotalRotation;
        
        RotateWorld(delta);
        _currentTotalRotation = newAngle;
        
        yield return null;
    }
    
    // 确保最终位置准确
    RotateWorld(targetAngle - _currentTotalRotation);
    _currentTotalRotation = targetAngle;
    _isMoving = false;
}

// 缓动函数
private float EaseInOutCubic(float t)
{
    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
}
    private void RotateWorld(float delta)
{
    Debug.Log($"RotateWorld 调用，delta: {delta}, 地块数量: {_allSegments.Count}");
    
    if (Mathf.Approximately(delta, 0f))
    {
        Debug.LogWarning("旋转增量为0，不执行旋转");
        return;
    }
    
    int resetCount = 0;
    int errorCount = 0;
    
    foreach (var item in _allSegments)
    {
        try
        {
            item.Rotate(delta);

            if (item.GetAngle() > 100f) 
            {
                resetCount++;
                float newAngle = item.GetAngle() - 360f;
                
                string[] nameParts = item.name.Split('_');
                Debug.Log($"解析地块 {item.name}: 分割结果 = [{string.Join(", ", nameParts)}]");
                
                if (nameParts.Length < 4)
                {
                    Debug.LogError($"地块名称格式错误: {item.name}");
                    errorCount++;
                    continue;
                }
                
                string layerName = nameParts[0];
                bool isMain = nameParts[1] == "True";
                
                // 修正：正确的 TryParse 语法
                int layerOffset;
                if (!int.TryParse(nameParts[2], out layerOffset))
                {
                    Debug.LogError($"无法解析层偏移: '{nameParts[2]}' (类型: {nameParts[2].GetType()})，地块名称: {item.name}");
                    errorCount++;
                    continue;
                }
                
                bool syncWithCurrentLevel = nameParts[3] == "True";
                
                Debug.Log($"地块信息: 层={layerName}, 主层={isMain}, 偏移={layerOffset}, 同步={syncWithCurrentLevel}");

                // --- 逻辑分支：是根据 LevelPrefab 同步还是根据 ThemeSO 生成 ---
                if (levelPrefabs != null && levelPrefabs.Length > 0 && isMain)
                {
                    // 仅主逻辑层同步 LevelPrefab 的预制体内容
                    if (_currentLevelIndex >= 0 && _currentLevelIndex < levelPrefabs.Length)
                    {
                        GameObject currentLevel = levelPrefabs[_currentLevelIndex];
                        int segmentIndexInPrefab = _globalIndex % totalSegments;
                        
                        if (currentLevel.transform.childCount > segmentIndexInPrefab)
                        {
                            Transform source = currentLevel.transform.GetChild(segmentIndexInPrefab);
                            item.SyncFromPreset(newAngle, source);
                        }
                    }
                }
                else
                {
                    // 非主层刷新
                    if (syncWithCurrentLevel && themeSO != null)
                    {
                        // 同步层：使用当前关卡的主题和对应的层资源
                        var currentTheme = themeSO.GetThemeForLevel(_currentLevelIndex);
                        if (currentTheme != null)
                        {
                            var layerResource = currentTheme.GetLayerResource(layerName);
                            item.RefreshWithLayerResource(newAngle, item.GetRadius(), layerResource, currentTheme.nodeMarkerPrefab);
                        }
                    }
                    else
                    {
                        // 独立进度层：根据全局索引和层偏移获取主题
                        var theme = GetThemeForLayer(_globalIndex, layerOffset, _currentLevelIndex);
                        item.Refresh(newAngle, item.GetRadius(), theme);
                    }
                }

                // --- 进度统计：只有主层越界才触发全球索引增加 ---
                if (isMain) 
                {
                    _globalIndex++;
                    _segmentsInCurrentLevel++;

                    if (_segmentsInCurrentLevel >= totalSegments) 
                    {
                        if (levelPrefabs != null && levelPrefabs.Length > 0)
                        {
                            _currentLevelIndex = (_currentLevelIndex + 1) % levelPrefabs.Length;
                        }
                        else
                        {
                            // 如果没有关卡预制体，重置为0
                            _currentLevelIndex = 0;
                        }
                        _segmentsInCurrentLevel = 0;
                        Debug.Log($"切换到下一个关卡: {_currentLevelIndex}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"处理地块 {item.name} 时出错: {e.Message}");
            errorCount++;
        }
    }
    
    Debug.Log($"旋转完成: 重置了 {resetCount} 个地块，遇到 {errorCount} 个错误");
}

    // 获取主题（基于全局索引和层偏移）
    ThemeSequenceSO.ThemeConfig GetThemeForLayer(int index, int offset, int baseLevelIndex)
    {
        if (themeSO == null || themeSO.themes.Count == 0) 
        {
            Debug.LogError("ThemeSO is null or empty!");
            return null;
        }
        
        // 计算当前是第几轮（圈）
        int circleIndex = (index / totalSegments);
        // 加上该层的偏移，实现三层海浪/森林不同的贴图
        int themeIdx = (circleIndex + offset) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
    
    // 新增：获取当前关卡的主题（用于同步层）
    ThemeSequenceSO.ThemeConfig GetThemeForCurrentLevel(int offset)
    {
        if (themeSO == null || themeSO.themes.Count == 0) 
        {
            Debug.LogError("ThemeSO is null or empty!");
            return null;
        }
        
        // 使用当前关卡索引加上层偏移
        int themeIdx = (_currentLevelIndex + offset) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
    
    // 新增：手动切换到指定关卡（用于测试或关卡选择）
    // 新增：手动切换到指定关卡（用于测试或关卡选择）
public void SwitchToLevel(int levelIndex)
{
    if (levelPrefabs == null || levelPrefabs.Length == 0)
    {
        Debug.LogWarning("levelPrefabs 为空，无法切换关卡");
        return;
    }
    
    if (levelIndex < 0 || levelIndex >= levelPrefabs.Length)
    {
        Debug.LogError($"无效的关卡索引: {levelIndex}，有效范围: 0-{levelPrefabs.Length - 1}");
        return;
    }
    
    _currentLevelIndex = levelIndex;
    _segmentsInCurrentLevel = 0;
    
    Debug.Log($"手动切换到关卡: {_currentLevelIndex}");
    
    // 强制刷新所有同步层
    int refreshedCount = 0;
    foreach (var item in _allSegments)
    {
        try
        {
            string[] nameParts = item.name.Split('_');
            if (nameParts.Length < 4) 
            {
                Debug.LogWarning($"地块名称格式错误，跳过: {item.name}");
                continue;
            }
            
            string layerName = nameParts[0];
            bool isMain = nameParts[1] == "True";
            
            // 修正：正确的 TryParse 语法
            int layerOffset;
            if (!int.TryParse(nameParts[2], out layerOffset))
            {
                Debug.LogWarning($"无法解析层偏移，跳过: {item.name}");
                continue;
            }
            
            bool syncWithCurrentLevel = nameParts[3] == "True";
            
            if (!isMain && syncWithCurrentLevel && themeSO != null)
            {
                var currentTheme = themeSO.GetThemeForLevel(_currentLevelIndex);
                if (currentTheme != null)
                {
                    var layerResource = currentTheme.GetLayerResource(layerName);
                    if (layerResource != null)
                    {
                        item.RefreshWithLayerResource(item.GetAngle(), item.GetRadius(), layerResource, currentTheme.nodeMarkerPrefab);
                        refreshedCount++;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"切换关卡时处理地块 {item.name} 出错: {e.Message}");
        }
    }
    
    Debug.Log($"切换关卡完成，刷新了 {refreshedCount} 个同步层地块");
}
    
    // 新增：获取当前关卡索引（供外部使用）
    public int GetCurrentLevelIndex()
    {
        return _currentLevelIndex;
    }
}