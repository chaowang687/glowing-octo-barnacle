using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class DiceController : MonoBehaviour
{
    [Header("UI 引用")]
    public Button RollButton;
    public TextMeshProUGUI ResultText;

    [Header("动画设置")]
    public float RollAnimationDuration = 0.5f;
    public int RollAnimationFrames = 10;
    public string ResultFormat = "🎲 {0}";
    public string StartText = "Click the dice to start exploring";

    [Header("事件")]
    public UnityEvent<int> OnDiceRolled; // 参数为骰子点数

    private bool _canRoll = true;

    void Start()
    {
        if (RollButton != null)
        {
            RollButton.onClick.AddListener(OnRollButtonClicked);
        }
        
        ResultText.text = StartText;
        SetDiceRollEnabled(true);
    }

    public void OnRollButtonClicked()
    {
        if (!_canRoll)
        {
            Debug.Log("骰子投掷被禁用，请等待移动或操作完成。");
            return;
        }

        // 1. 禁用投掷
        SetDiceRollEnabled(false);

        // 2. 开始动画协程
        StartCoroutine(RollAnimationCoroutine());
    }

    /// <summary>
    /// 投掷动画协程
    /// </summary>
    private IEnumerator RollAnimationCoroutine()
    {
        // 先声明变量
        int rollResult = 0;
        
        // 播放随机数字动画
        for (int i = 0; i < RollAnimationFrames; i++)
        {
            int randomNumber = Random.Range(1, 7);
            ResultText.text = string.Format(ResultFormat, randomNumber);
            yield return new WaitForSeconds(RollAnimationDuration / RollAnimationFrames);
        }

        // 生成最终结果
        rollResult = Random.Range(1, 7);
        ResultText.text = string.Format(ResultFormat, rollResult);

        // 3. 触发事件
        OnDiceRolled?.Invoke(rollResult);
    }

    /// <summary>
    /// 直接投掷骰子（不播放动画）
    /// </summary>
    public int Roll()
    {
        return Random.Range(1, 7); 
    }

    /// <summary>
    /// 控制骰子按钮的可用性
    /// </summary>
    public void SetDiceRollEnabled(bool enabled)
    {
        _canRoll = enabled;
        if (RollButton != null)
        {
            RollButton.interactable = enabled;
        }
    }
}