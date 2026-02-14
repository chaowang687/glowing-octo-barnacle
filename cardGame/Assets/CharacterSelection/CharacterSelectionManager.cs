using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Bag;

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
    
    [Header("背包数据")]
    public InventorySO[] inventorySOs; // 可以手动指定要清理的InventorySO，也可以在代码中自动查找

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
            
            // 直接删除所有 PlayerPrefs 数据，确保开始新游戏
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            // 优化后的清理逻辑，防止空指针错误
            // 1. 先停掉所有可能干扰的逻辑
            // 2. 彻底清理 SO (确保在 Inspector 赋值了)
            if (inventorySOs != null && inventorySOs.Length > 0)
            {
                foreach (var inventorySO in inventorySOs)
                {
                    if (inventorySO != null)
                    {
                        inventorySO.Clear();
                        Debug.Log($"已清空指定的InventorySO: {inventorySO.name}");
                    }
                }
            }
            else
            {
                // 自动查找并清理所有InventorySO资源
                var allInventorySOs = Resources.FindObjectsOfTypeAll<InventorySO>();
                foreach (var inventorySO in allInventorySOs)
                {
                    if (inventorySO != null)
                    {
                        inventorySO.Clear();
                        Debug.Log($"已清空InventorySO: {inventorySO.name}");
                    }
                }
            }

            // 3. 强力删除存档文件
            string inventoryPath = System.IO.Path.Combine(Application.persistentDataPath, "inventory.json");
            if (System.IO.File.Exists(inventoryPath))
            {
                System.IO.File.Delete(inventoryPath);
                Debug.Log("背包存档文件已删除");
            }

            // 4. 重置背包数据，而不是销毁实例
            // 这样可以保持InventoryManager的全局存在，同时清空背包数据
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ResetInventoryData();
                Debug.Log("背包数据已重置");
            }

            // 5. 最后跳转
            Debug.Log("已删除所有存档数据，开始新游戏");
            SceneManager.LoadScene("MapScene");
        }
    }
}