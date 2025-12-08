using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 请确保在 Inspector 中拖入正确的 Text 组件
    [Header("UI 引用")]
    public Text energyDisplay; // 用于显示当前能量/最大能量，已替换原来的 energyText
    public Button endTurnButton; // 结束回合按钮

    // 缓存 BattleManager 实例
    private BattleManager battleManager;

    void Start()
    {
        // 尝试获取 BattleManager 实例
        battleManager = BattleManager.Instance;

        if (battleManager == null)
        {
            Debug.LogError("UIManager: 找不到 BattleManager 实例，请确保场景中存在 BattleManager 单例!");
            return;
        }
        
        // 绑定结束回合按钮的点击事件
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

        // ⭐ 优化: 初始更新显示，并订阅能量变化事件（如果你的 CardSystem 有事件的话）
        // 这里只是初始调用，后续更新应该通过事件触发，而不是在 Update 中。
        UpdateEnergyDisplay();

        // 假设 CardSystem 暴露了一个用于订阅能量变化的事件
        if (battleManager.cardSystem != null)
        {
            battleManager.cardSystem.OnEnergyChanged += UpdateEnergyDisplay;
        }
    }

    // 优化: 删除 Update()，改用事件驱动或外部调用
    /*
    void Update()
    {
        // 这是一个简单但不高效的方法。如果必须使用 Update，请使用此代码：
        if (battleManager != null && battleManager.cardSystem != null && energyDisplay != null)
        {
            energyDisplay.text = $"能量: {battleManager.cardSystem.GetCurrentEnergy()}/{battleManager.cardSystem.GetMaxEnergy()}";
        }
    }
    */

    // 专门用于更新能量 UI 的公共方法
    public void UpdateEnergyDisplay()
    {
        if (battleManager != null && battleManager.cardSystem != null && energyDisplay != null)
        {
            int current = battleManager.cardSystem.GetCurrentEnergy();
            int max = battleManager.cardSystem.GetMaxEnergy();
            energyDisplay.text = $"能量: {current}/{max}";
        }
    }

    // 按钮点击时调用的方法
    public void OnEndTurnClicked()
    {
        if (battleManager != null)
        {
            Debug.Log("结束回合按钮被点击.");
            battleManager.EndPlayerTurn();
        }
    }
}