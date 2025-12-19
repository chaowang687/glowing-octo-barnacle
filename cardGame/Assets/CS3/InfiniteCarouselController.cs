using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InfiniteCarouselController : MonoBehaviour
{
    [Header("关卡与地块配置")]
    public GameObject[] levelPrefabs; 
    public ThemeSequenceSO themeSO;
    public GameObject segmentPrefab;
    
    [Header("环形参数")]
    public int totalSegments = 30;    
    public float radius = 8f;

    [Header("移动平衡 (匀速配置)")]
    [Tooltip("每秒旋转的角度。例如: 360 表示 1 秒转一圈")]
    public float degreesPerSecond = 45f; 

    [Header("运行时状态")]
    [SerializeField] private bool _isMoving = false;
    public bool IsMoving => _isMoving; 

    private List<WorldSegmentItem> _items = new List<WorldSegmentItem>();
    private int _globalIndex = 0;   
    private int _currentLevelIndex = 0;
    private int _segmentsInCurrentLevel = 0;
    private float _currentTotalRotation = 0f; 

    void Start()
    {
        _items.Clear();
        var existingItems = GetComponentsInChildren<WorldSegmentItem>();
        float angleStep = 360f / totalSegments;

        if (existingItems.Length > 0)
        {
            _items.AddRange(existingItems);
            _globalIndex = _items.Count; 
        }
        else
        {
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

    public void RollDiceAndMove(int steps)
    {
        if (_isMoving) return;
        Debug.Log($"收到指令：走 {steps} 步");
        StartCoroutine(MoveRoutine(steps));
    }

    private IEnumerator MoveRoutine(int steps)
    {
        _isMoving = true;
        
        float anglePerStep = 360f / totalSegments; 
        float moveAngle = steps * anglePerStep;    
        
        // 【核心修改】：根据距离和速度计算时间
        // 时间 = 距离 / 速度
        float dynamicDuration = moveAngle / degreesPerSecond;
        
        float elapsed = 0;
        float startRotation = _currentTotalRotation;
        float targetRotation = _currentTotalRotation + moveAngle;

        while (elapsed < dynamicDuration)
        {
            elapsed += Time.deltaTime;
            // 使用线性插值，确保匀速
            float t = elapsed / dynamicDuration;
            float nextRot = Mathf.Lerp(startRotation, targetRotation, t);
            
            float delta = nextRot - _currentTotalRotation;
            RotateWorld(delta);
            
            _currentTotalRotation = nextRot;
            yield return null;
        }

        // 最终精准对齐
        RotateWorld(targetRotation - _currentTotalRotation);
        _currentTotalRotation = targetRotation;
        _isMoving = false;
    }

    private void RotateWorld(float delta)
    {
        foreach (var item in _items)
        {
            item.Rotate(delta);

            if (item.GetAngle() > 100f) 
            {
                float newAngle = item.GetAngle() - 360f;
                if (levelPrefabs.Length > 0)
                {
                    GameObject nextLevelPrefab = levelPrefabs[_currentLevelIndex];
                    int segmentIndexInPrefab = _globalIndex % totalSegments;
                    
                    if (nextLevelPrefab.transform.childCount > segmentIndexInPrefab)
                    {
                        Transform sourceSegment = nextLevelPrefab.transform.GetChild(segmentIndexInPrefab);
                        item.SyncFromPreset(newAngle, sourceSegment);
                    }
                }

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

    ThemeSequenceSO.ThemeConfig GetThemeForIndex(int index)
    {
        if (themeSO == null || themeSO.themes.Count == 0) return null;
        int themeIdx = (index / totalSegments) % themeSO.themes.Count;
        return themeSO.themes[themeIdx];
    }
}