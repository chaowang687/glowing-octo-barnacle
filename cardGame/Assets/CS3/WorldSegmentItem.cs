using UnityEngine;
using System.Collections.Generic;

public class WorldSegmentItem : MonoBehaviour
{
    public SpriteRenderer groundRenderer;
    public Transform decoContainer;

    [Header("分布配置 (公开可调)")]
    [Range(0, 1)] public float spawnChance = 0.6f;     // 每个坑位长出东西的概率
    public Vector2 heightOffsetRange = new Vector2(0f, 0.5f); // Y轴偏移量（让有的树高，有的树低）
    public Vector2 sizeScaleRange = new Vector2(0.7f, 1.3f);  // 缩放范围
    public float zSpread = 1.0f;                       // Z轴纵深范围（解决遮挡感）

    // 私有核心变量（修复报错的关键）
    private float _currentAngle;
    private float _radius;
    private float[] _slotX = new float[] { -1.3f, 0f, 1.3f }; // 固定坑位

    public void Refresh(float angle, float radius, ThemeSequenceSO.ThemeConfig theme)
    {
        // 修复 CS0103 错误：确保这些变量被正确赋值
        _currentAngle = angle;
        _radius = radius;
        
        if(groundRenderer != null) groundRenderer.sprite = theme.groundSprite;

        // 清理旧装饰
        for (int i = decoContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(decoContainer.GetChild(i).gameObject);
        }

        if (theme.decorations == null || theme.decorations.Length == 0) return;

        // 遍历坑位生成
        for (int i = 0; i < _slotX.Length; i++)
        {
            if (Random.value > spawnChance) continue;

            var prefab = theme.decorations[Random.Range(0, theme.decorations.Length)];
            var deco = Instantiate(prefab, decoContainer);

            // 计算坐标
            float posX = _slotX[i] + Random.Range(-0.2f, 0.2f);
            float posY = Random.Range(heightOffsetRange.x, heightOffsetRange.y);
            float posZ = Random.Range(-zSpread, zSpread);

            deco.transform.localPosition = new Vector3(posX, posY, posZ);

            // 随机缩放与镜像
            float scale = Random.Range(sizeScaleRange.x, sizeScaleRange.y);
            deco.transform.localScale = new Vector3(scale, scale, scale);

            var sr = deco.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = Random.value > 0.5f;
        }
        UpdatePosition();
    }

    public void Rotate(float deltaAngle)
    {
        _currentAngle += deltaAngle;
        UpdatePosition();
    }

    // 修复 CS1061 错误：添加 GetAngle 方法供 Controller 调用
    public float GetAngle() 
    {
        return _currentAngle;
    }

    private void UpdatePosition()
    {
        float rad = _currentAngle * Mathf.Deg2Rad;
        // 映射到圆周坐标
        transform.localPosition = new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * _radius;
        // 保持垂直于圆心
        transform.localRotation = Quaternion.Euler(0, 0, -_currentAngle);
    }
}