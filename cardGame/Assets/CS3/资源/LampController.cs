using UnityEngine;

public class LampController : MonoBehaviour
{
    public GameObject lightHalo; // 拖入子物体 LightHalo
    public float turnOnTime = 1.1f; // 黄昏开始变暗的时间点
    public float turnOffTime = 0.4f; // 黎明变亮的时间点

    void Update()
    {
        if (TimeOfDaySystem.Instance == null) return;

        float currentTime = TimeOfDaySystem.Instance.gameTime;

        // 逻辑：如果在深夜阶段（1.1 到 2.0）或者清晨（0 到 0.4）
        if (currentTime >= turnOnTime || currentTime <= turnOffTime)
        {
            if (!lightHalo.activeSelf) lightHalo.SetActive(true);
        }
        else
        {
            if (lightHalo.activeSelf) lightHalo.SetActive(false);
        }
    }
}