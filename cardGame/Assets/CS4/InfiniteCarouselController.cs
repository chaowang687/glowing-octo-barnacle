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
    public bool IsMoving => _isMoving; // 修复 DiceManager 的报错

    void Start()
    {
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
            item.isMainLayer = config.isMainLogicLayer;      // 记录是否为主层
            item.layerThemeOffset = config.themeIndexOffset; // 记录主题偏移
            
            item.parallaxMultiplier = config.speedMultiplier;
            
            // 【重要】：存储该层的 Offset，以便在 RotateWorld 刷新时使用
            // 我们通过改名或者动态添加一个标记来实现
            go.name = config.isMainLogicLayer ? $"Main_{config.themeIndexOffset}" : $"Deco_{config.themeIndexOffset}";

            if (item.groundRenderer != null)
                item.groundRenderer.sortingOrder += config.sortingOrderOffset;

            Vector3 s = go.transform.localScale;
            go.transform.localScale = new Vector3(s.x * overlapFactor, s.y, s.z);

            float currentRadius = radius + config.radiusOffset;
            var theme = GetThemeForLayer(i, config.themeIndexOffset);
            item.Refresh(i * angleStep, currentRadius, theme);
            
            _allSegments.Add(item);
        }
    }

    // --- 掷骰子入口 ---
    public void RollDiceAndMove(int steps) 
    { 
        if(_isMoving) return;  // 添加 return 语句
        StartCoroutine(MoveRoutine(steps)); 
    }

    private IEnumerator MoveRoutine(int steps) 
    { 
        _isMoving = true;
        Debug.Log($"MoveRoutine 开始，步数: {steps}");
        // 计算每一格的角度：例如 30 格则每格 12 度
        float anglePerStep = 360f / totalSegments; 
        float moveAngle = steps * anglePerStep;    
        Debug.Log($"每步角度: {anglePerStep}, 总移动角度: {moveAngle}");
        // 根据设定速度计算所需时间
        float duration = moveAngle / degreesPerSecond;
        Debug.Log($"移动持续时间: {duration} 秒");
        float elapsed = 0;
        float startRot = _currentTotalRotation;
        float targetRot = _currentTotalRotation + moveAngle;

        while (elapsed < duration) 
        {
            elapsed += Time.deltaTime;
            // 使用 Lerp 实现平滑旋转
            float nextRot = Mathf.Lerp(startRot, targetRot, elapsed / duration);
            
            // 计算本帧增量并旋转世界
            RotateWorld(nextRot - _currentTotalRotation);
            _currentTotalRotation = nextRot;
            
            yield return null;
        }

        // 确保最终位置精准对齐
        RotateWorld(targetRot - _currentTotalRotation);
        _currentTotalRotation = targetRot;
        
        _isMoving = false;
        Debug.Log($"MoveRoutine 结束，新角度: {_currentTotalRotation}");
    }

    private void RotateWorld(float delta)
    {
        foreach (var item in _allSegments)
        {
            item.Rotate(delta);

            // 当地块旋转到上方 100 度（越界阈值）时，循环回圆环底部
            if (item.GetAngle() > 100f) 
            {
                float newAngle = item.GetAngle() - 360f;
                
                // 从名字里解析出这层地块原本的 ThemeOffset 和是否为主层
                string[] nameParts = item.name.Split('_');
                bool isMain = nameParts[0] == "Main";
                int layerOffset = int.Parse(nameParts[1]);

                // --- 逻辑分支：是根据 LevelPrefab 同步还是根据 ThemeSO 生成 ---
                if (levelPrefabs != null && levelPrefabs.Length > 0 && isMain)
                {
                    // 仅主逻辑层同步 LevelPrefab 的预制体内容
                    GameObject currentLevel = levelPrefabs[_currentLevelIndex];
                    int segmentIndexInPrefab = _globalIndex % totalSegments;
                    
                    if (currentLevel.transform.childCount > segmentIndexInPrefab)
                    {
                        Transform source = currentLevel.transform.GetChild(segmentIndexInPrefab);
                        item.SyncFromPreset(newAngle, source);
                    }
                }
                else
                {
                    // 通用/装饰层刷新：根据 ThemeSO 和自己的层偏移获取资源
                    item.Refresh(newAngle, item.GetRadius(), GetThemeForLayer(_globalIndex, layerOffset));
                }

                // --- 进度统计：只有主层越界才触发全球索引增加 ---
                if (isMain) 
                {
                    _globalIndex++;
                    _segmentsInCurrentLevel++;

                    if (_segmentsInCurrentLevel >= totalSegments) 
                    {
                        _currentLevelIndex = (_currentLevelIndex + 1) % levelPrefabs.Length;
                        _segmentsInCurrentLevel = 0;
                    }
                }
            }
        }
    }

    ThemeSequenceSO.ThemeConfig GetThemeForLayer(int index, int offset)
    {
        if (themeSO == null || themeSO.themes.Count == 0) return null;
        // 计算当前是第几轮（圈）
        int circleIndex = (index / totalSegments);
        // 加上该层的偏移，实现三层海浪/森林不同的贴图
        int themeIdx = (circleIndex + offset) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
}