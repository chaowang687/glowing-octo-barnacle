using UnityEngine;
using System.Collections.Generic;

public class InfiniteCarouselController : MonoBehaviour
{
    public ThemeSequenceSO themeSO;
    public GameObject segmentPrefab;
    
    public int totalSegments = 18;  // 一圈 18 张图
    public float radius = 8f;
    public float scrollSpeed = 20f; // 模拟测试速度

    private List<WorldSegmentItem> _items = new List<WorldSegmentItem>();
    private int _globalIndex = 0;   // 记录总共跑过了多少个地块

    void Start()
    {
        float angleStep = 360f / totalSegments;
        for (int i = 0; i < totalSegments; i++)
        {
            var go = Instantiate(segmentPrefab, transform);
            var item = go.GetComponent<WorldSegmentItem>();
            
            // 初始化分布
            item.Refresh(i * angleStep, radius, GetThemeForIndex(_globalIndex));
            _items.Add(item);
            _globalIndex++;
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
                // 瞬移到最后一名（接力）
                float newAngle = item.GetAngle() - 360f;
                item.Refresh(newAngle, radius, GetThemeForIndex(_globalIndex));
                _globalIndex++;
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