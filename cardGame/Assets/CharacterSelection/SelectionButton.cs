using UnityEngine;
using UnityEngine.UI;

public class SelectionButton : MonoBehaviour
{
    // 1. 变量声明
    public CharacterBase thisCharacterData;
    public Image highlightImage; // 用于高亮显示的图片组件

    // 2. Unity 生命周期方法
    void Start()
    {
        // 初始化逻辑
    }

    // 3. 自定义公开方法（用于按钮点击）
    public void OnClick() 
    {
        // 确保你的场景里有 CharacterSelectionManager
        var manager = Object.FindAnyObjectByType<CharacterSelectionManager>();
        if (manager != null)
        {
            manager.SelectCharacter(thisCharacterData, this);
        }
    }
    
    // 4. 设置高亮显示
    public void SetHighlight(bool isHighlighted)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(isHighlighted);
        }
    }
}