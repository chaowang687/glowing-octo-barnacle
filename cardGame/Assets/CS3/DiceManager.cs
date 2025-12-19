using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiceManager : MonoBehaviour
{
    public InfiniteCarouselController carousel; // 关联你的地形控制器
    public Text resultText;                    // 关联 UI 文字显示点数
    public Button rollButton;                  // 关联掷骰子按钮

    public void RollDice()
    {
        // 1. 运动期间禁用按钮，防止连续点击
        rollButton.interactable = false;

        // 2. 产生随机点数 (1-6)
        int diceResult = Random.Range(1, 7);
        
        // 3. (可选) 播放一个简单的数字滚动动画效果
        StartCoroutine(DiceRollAnim(diceResult));
    }

    private IEnumerator DiceRollAnim(int finalResult)
    {
        // 简单的数字闪烁效果，增加代入感
        for (int i = 0; i < 10; i++)
        {
            resultText.text = Random.Range(1, 7).ToString();
            yield return new WaitForSeconds(0.05f);
        }

        resultText.text = finalResult.ToString();

        // 4. 调用地形控制器开始移动
        carousel.RollDiceAndMove(finalResult);

        // 5. 等待地形移动结束（通过控制器状态判断）
        while (carousel.IsMoving)
        {
            yield return null;
        }

        // 6. 移动结束，重新启用按钮
        rollButton.interactable = true;
    }
}