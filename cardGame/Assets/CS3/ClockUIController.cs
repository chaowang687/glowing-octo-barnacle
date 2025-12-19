using UnityEngine;
using UnityEngine.UI;

public class ClockUIController : MonoBehaviour
{
    [Header("UI 引用")]
    public RectTransform hourHand;  // 拖入你的时针图片组件
    public Text digitalTimeText;    // 拖入数字显示文本

    void Update()
    {
        if (TimeOfDaySystem.Instance == null) return;

        // 1. 计算时针旋转
        // gameTime 0-2 对应 24小时，即转一圈
        // Unity UI 旋转中，顺时针是负值
        float rotationAngle = (TimeOfDaySystem.Instance.gameTime / 2.0f) * 360f;
        hourHand.localRotation = Quaternion.Euler(0, 0, -rotationAngle);

        // 2. 更新数字时间
        TimeOfDaySystem.Instance.GetTimeValues(out int h, out int m);
        if (digitalTimeText != null)
        {
            digitalTimeText.text = string.Format("{0:00}:{1:00}", h, m);
        }
    }
}