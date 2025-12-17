using UnityEngine;

public class TimeOfDaySystem : MonoBehaviour
{
    // 静态实例，方便其他脚本（如地块、太阳）访问
    public static TimeOfDaySystem Instance;

    [Header("设置")]
    public float cycleDurationMinutes = 2f; // 完成一个昼夜循环的时间
    public Material skyMaterial; // 拖入天空材质

    [Header("实时状态")]
    public float gameTime = 0; // 0=清晨, 0.5=正午, 1.0=傍晚, 1.5=深夜
    
    // 提供一个只读属性给其他脚本判定是否为夜晚
    public bool IsNight => (gameTime % 2.0f > 1.3f || gameTime % 2.0f < 0.2f);

    private void Awake()
    {
        Instance = this;
    }
    void UpdateSkyMaterial()
    {
        if (skyMaterial == null) return;

        float t = gameTime % 2.0f;
        float transition = 0;

        // 映射逻辑：
        // 0.0 - 0.5 (黎明): 1 -> 0 (黑夜退去)
        // 0.5 - 1.0 (白天): 0 (纯白昼)
        // 1.0 - 1.5 (黄昏): 0 -> 1 (黑夜降临)
        // 1.5 - 2.0 (深夜): 1 (纯黑夜)

        if (t < 0.5f) transition = 1.0f - (t / 0.5f);
        else if (t < 1.0f) transition = 0f;
        else if (t < 1.5f) transition = (t - 1.0f) / 0.5f;
        else transition = 1.0f;

        // 传递给材质
        skyMaterial.SetFloat("_Transition", transition);
    }
    void Update()
    {
        // 1. 更新时间流逝 (每秒流逝的 gameTime 数值)
        // 2.0f 代表一个完整的 0->2 循环
        float speed = 2.0f / (cycleDurationMinutes * 60f);
        gameTime += Time.deltaTime * speed;
        
        // 保持在 0-2 范围内
        if (gameTime > 2.0f) gameTime -= 2.0f;

        // 2. 计算 Shader 所需的 Transition 值 (0=完全白天, 1=完全黑夜)
        float transition = CalculateTransition(gameTime);

        // 3. 同步到材质和全局变量
        if (skyMaterial != null)
        {
            skyMaterial.SetFloat("_Transition", transition);
        }
        
        // 设置全局变量，这样所有地块的 Shader 都能自动变暗
        Shader.SetGlobalFloat("_SkyTransition", transition);
    }

    private float CalculateTransition(float t)
    {
        // 复刻 HTML Demo 的曲线逻辑
        if (t < 0.5f) return 1.0f - (t / 0.5f);      // 0.0-0.5: 黑夜 -> 白天
        if (t < 1.0f) return 0f;                    // 0.5-1.0: 纯白天
        if (t < 1.5f) return (t - 1.0f) / 0.5f;     // 1.0-1.5: 白天 -> 黑夜
        return 1.0f;                                // 1.5-2.0: 纯黑夜
    }
}