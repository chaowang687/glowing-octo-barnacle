using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("角色数据源")]
    public List<CharacterBase> availableCharacters; 
    
    [Header("展示 UI")]
    public Image previewImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
   
    [Header("开始游戏")]
    public GameObject startButton; 

    private CharacterBase selectedCharacter;
    private SelectionButton lastSelectedButton;

    void Start()
    {
        // 初始状态：隐藏开始按钮
        if (startButton != null) startButton.SetActive(false);

        // 默认显示第一个角色（如果列表不为空）
        if (availableCharacters.Count > 0)
        {
            // 注意：初始展示时不需要传 Button，所以我们要处理 null
            RefreshPreview(availableCharacters[0]);
        }
    }

    // 将刷新 UI 的逻辑独立出来，避免重复代码（解决冗余）
    private void RefreshPreview(CharacterBase character)
    {
        selectedCharacter = character;
        if (previewImage != null) previewImage.sprite = character.characterSprite; 
        if (nameText != null) nameText.text = character.characterName;
        if (descText != null) descText.text = character.description;
    }

    // 当按钮被点击时调用
    public void SelectCharacter(CharacterBase character, SelectionButton clickedButton)
    {
        RefreshPreview(character);
        
        // 1. 处理选中高亮效果（增加了空检查）
        if (lastSelectedButton != null) lastSelectedButton.SetHighlight(false);
        
        if (clickedButton != null)
        {
            clickedButton.SetHighlight(true);
            lastSelectedButton = clickedButton;
        }

        // 2. 显示开始按钮
        if (startButton != null) startButton.SetActive(true);
    }

    public void OnStartGameClicked()
    {
        if (selectedCharacter != null)
        {
            // 确保你已经创建了 GameManager.cs
            GameManager.PlayerStartData = selectedCharacter;
            SceneManager.LoadScene("MapScene");
        }
    }
}