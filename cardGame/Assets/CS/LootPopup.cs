using UnityEngine;
using TMPro; // 必须使用 TextMeshPro 否则字不好看
using DG.Tweening;

public class LootPopup : MonoBehaviour
{
    // 如果你在 UI (Canvas) 上使用，必须改成 UGUI 版本
    public TextMeshProUGUI textMesh;
    public void SetText(string itemName)
    {
        if (textMesh != null)
        {
            textMesh.text = $"被抢走了: {itemName}!";
            
            // 顺便做一个飘字动画
            transform.DOMoveY(transform.position.y + 1.5f, 1f);
            textMesh.DOFade(0, 1f).OnComplete(() => Destroy(gameObject));
        }
    }
}
