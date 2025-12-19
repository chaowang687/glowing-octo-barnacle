using UnityEngine;

public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance;

    [Header("时间设置")]
    public float cycleDurationMinutes = 2f; 
    public Material skyMaterial; 

    [Header("实时状态")]
    [Range(0, 2)] public float gameTime = 0; 
    
    [Header("饥荒效果引用")]
    public Transform playerTransform; 
    public CanvasGroup nightVignette;

    private void Awake() => Instance = this;
    [Header("灯光管理")]
    public LampManager lampManager; // [新增] 拖拽你的 LampManager 物体到这里

    // ... 其他变量保持不变 ...

    private bool lightsAreOn = false; // [新增] 记录当前灯光状态，防止每帧重复调用

    void Update()
{
    // 1. 先计算时间流逝
    float speed = 2.0f / (cycleDurationMinutes * 60f);
    gameTime += Time.deltaTime * speed;
    if (gameTime >= 2.0f) gameTime -= 2.0f;

    // 2. 【关键】必须先计算出 transition，后面才能用它
    float transition = CalculateTransition(gameTime);

    // 3. 现在使用 transition 来开关灯
    if (lampManager != null)
    {
        if (transition > 0.5f && !lightsAreOn)
        {
            lightsAreOn = true;
            lampManager.SwitchAllLights(true);
        }
        else if (transition <= 0.5f && lightsAreOn)
        {
            lightsAreOn = false;
            lampManager.SwitchAllLights(false);
        }
    }

    // 4. 同步 Shader 和其他效果
    Shader.SetGlobalFloat("_SkyTransition", transition);

    if (playerTransform != null)
    {
        Shader.SetGlobalVector("_PlayerPos", playerTransform.position);
    }

    if (skyMaterial != null)
    {
        skyMaterial.SetFloat("_Transition", transition);
    }

    if (nightVignette != null)
    {
        nightVignette.alpha = Mathf.SmoothStep(0, 1, (transition - 0.5f) * 2f);
    }

    Debug.Log($"[昼夜系统] Transition: {transition} | 游戏时间: {gameTime}");
}

    private float CalculateTransition(float t)
    {
        if (t < 0.5f) return 1.0f - (t / 0.5f); // 黎明
        if (t < 1.0f) return 0f;                // 白天
        if (t < 1.5f) return (t - 1.0f) / 0.5f; // 黄昏
        return 1.0f;                           // 深夜
    }

    // 供钟表调用的时间获取
    public void GetTimeValues(out int hours, out int minutes)
    {
        float totalHours = (gameTime / 2.0f) * 24f;
        float displayHour = (totalHours + 6f) % 24f; 
        hours = Mathf.FloorToInt(displayHour);
        minutes = Mathf.FloorToInt((displayHour - hours) * 60f);
    }
}