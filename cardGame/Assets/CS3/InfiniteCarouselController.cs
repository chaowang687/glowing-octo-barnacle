using UnityEngine;
using System.Collections.Generic;

public class InfiniteCarouselController : MonoBehaviour
{
        // 在 InfiniteCarouselController.cs 中增加
    public GameObject[] levelPrefabs; // 在 Inspector 中拖入你编辑好的森林、沙漠等 Prefab
    private int _currentLevelIndex = 0;
    private int _segmentsInCurrentLevel = 0; // 记录当前关卡已经跑了多少个地块
    public ThemeSequenceSO themeSO;
    public GameObject segmentPrefab;
    
    public int totalSegments = 18;  // 一圈 18 张图
    public float radius = 8f;
    public float scrollSpeed = 20f; // 模拟测试速度

    private List<WorldSegmentItem> _items = new List<WorldSegmentItem>();
    private int _globalIndex = 0;   // 记录总共跑过了多少个地块

    void Start()
    {
        _items.Clear();
   // 检查场景中是否已经存在地块（即手动编辑好的）
    var existingItems = GetComponentsInChildren<WorldSegmentItem>();
    
    if (existingItems.Length > 0)
    {
        // 如果有，就直接用现成的，不要再 Instantiate 新的了
        _items.AddRange(existingItems);
        _globalIndex = _items.Count; 
        Debug.Log("运行模式：检测到手动编辑的地块，已跳过自动生成。");
    }
    else
    {
        // 如果场景为空，则执行动态生成逻辑
        float angleStep = 360f / totalSegments;
        for (int i = 0; i < totalSegments; i++)
        {
            var go = Instantiate(segmentPrefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();
            item.Refresh(i * angleStep, radius, GetThemeForIndex(_globalIndex));
            _items.Add(item);
            _globalIndex++;
        }
    }
    }

    void Update()
    {
        // 模拟手动滚动或自动滚动
        float delta = scrollSpeed * Time.deltaTime;
        float angleStep = 360f / totalSegments;

        foreach (var item in _items)
        {
            item.Rotate(delta);

            // 如果转出了屏幕左侧（比如角度 > 100度）
           if (item.GetAngle() > 100f) 
        {
            float newAngle = item.GetAngle() - 360f;
            
            // 找到下一个关卡预设中对应的地块内容
            GameObject nextLevelPrefab = levelPrefabs[_currentLevelIndex];
            
            // 获取那个预设里相同索引的地块（比如预设里的第 5 个地块）
            int segmentIndexInPrefab = _globalIndex % totalSegments;
            Transform sourceSegment = nextLevelPrefab.transform.GetChild(segmentIndexInPrefab);

            // 执行接力：把预设里的 DecoContainer 内容复制到当前旋转的地块上
            item.SyncFromPreset(newAngle, sourceSegment);

            _globalIndex++;
            _segmentsInCurrentLevel++;

            // 如果当前关卡 18 个地块都跑完了，准备切换到下一个预设
            if (_segmentsInCurrentLevel >= totalSegments)
            {
                _currentLevelIndex = (_currentLevelIndex + 1) % levelPrefabs.Length;
                _segmentsInCurrentLevel = 0;
            }
        }
    
        }
    }

    // 核心逻辑：转一圈换一个场景
    ThemeSequenceSO.ThemeConfig GetThemeForIndex(int index)
    {
        // 每过 totalSegments(18) 个地块，主题索引 +1
        int themeIdx = (index / totalSegments) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
}