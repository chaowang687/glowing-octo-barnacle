using UnityEngine;
using TMPro;

public class DamagePopupAnimation : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public Animation anim; // 拖入 Animation 组件

    public void Setup(int damageAmount)
    {
        // 将数字 123 转换为字符串 "123"
        string amountStr = damageAmount.ToString();
        string spriteText = "";

        foreach (char c in amountStr)
        {
            // 关键步骤：计算当前字符对应的索引
            // 如果字符是 '0'，index 就是 0；如果是 '1'，index 就是 1
            int index = c - '0'; 

            // 使用刚才定义好的 index 变量
            spriteText += $"<sprite index={index}>";
        }

        // 将富文本赋值给 TMP 组件
        textMesh.text = spriteText;
        
        // 播放动画
        if (anim != null) anim.Play("DamagePop_Anim");
        
        // 自动销毁
        Destroy(gameObject, 1.0f);
    }
}


