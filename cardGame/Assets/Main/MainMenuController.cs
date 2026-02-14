using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SlayTheSpireMap;

public class MainMenuController : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject mainMenuPanel;    // 拖入主菜单根物体
    public GameObject selectionPanel;   // 拖入角色选择界面根物体
    public GameObject loadPanel;        // 档案面板引用

    [Header("存档槽设置")]
    public SaveSlotUI[] saveSlots;      // 存档槽UI数组，最多3个

    [Header("场景设置")]
    public string firstLevelName = "GameScene"; // 初始关卡名

    // 修改：新游戏不再直接跳转，而是打开选人面板
    public void StartNewGame()
    {
        // 如果 GameDataManager 实例不存在，创建一个新实例
        if (SlayTheSpireMap.GameDataManager.Instance == null)
        {
            Debug.LogWarning("GameDataManager.Instance is null! Creating a new instance...");
            // 创建一个新的 GameDataManager 游戏对象
            GameObject gdmObject = new GameObject("GameDataManager");
            SlayTheSpireMap.GameDataManager gdm = gdmObject.AddComponent<SlayTheSpireMap.GameDataManager>();
            // 直接调用 ResetToDefault，因为 Awake 方法已经执行
            gdm.ResetToDefault();
        }
        else
        {
            // 如果需要重置旧存档数据，可以调用之前定义的全局重置
            SlayTheSpireMap.GameDataManager.Instance.ResetToDefault();
        }

        if (mainMenuPanel != null && selectionPanel != null)
        {
            mainMenuPanel.SetActive(false);
            selectionPanel.SetActive(true);
        }
    }

    // 档案/继续游戏
        public void OpenLoadMenu(int slotIndex = 0)
        {
            Debug.Log("[MainMenuController] OpenLoadMenu 被调用");
            
            if (loadPanel != null) 
            {
                Debug.Log("[MainMenuController] loadPanel 存在，尝试显示");
                // 先隐藏再显示，确保面板状态更新
                loadPanel.SetActive(false);
                loadPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[MainMenuController] loadPanel 为 null！请在Inspector中赋值");
            }
            
            // 检查 GameDataManager 实例是否存在
            if (SlayTheSpireMap.GameDataManager.Instance == null)
            {
                Debug.LogWarning("GameDataManager.Instance is null! Creating a new instance...");
                // 创建一个新的 GameDataManager 游戏对象
                GameObject gdmObject = new GameObject("GameDataManager");
                gdmObject.AddComponent<SlayTheSpireMap.GameDataManager>();
                // 等待一帧让 Awake 方法执行，确保实例已初始化
                StartCoroutine(WaitForGDMInitAndContinue());
                return;
            }
            
            // 检查 saveSlots 数组是否为 null
            if (saveSlots == null)
            {
                Debug.LogError("saveSlots array is null! Please assign save slots in the inspector.");
                return;
            }
            
            // 手动加载当前存档到GameDataManager
            SlayTheSpireMap.GameDataManager.Instance.LoadGameData(0);
            
            Debug.Log($"[MainMenuController] saveSlots 数组长度: {saveSlots.Length}");
            
            // 假设你有 3 个存档槽位
            for (int i = 1; i <= 3; i++) 
            {
                if (saveSlots.Length >= i && saveSlots[i-1] != null)
                {
                    // 检查是否存在当前存档
                    string currentSavePath = System.IO.Path.Combine(Application.persistentDataPath, "save_current.json");
                    if (System.IO.File.Exists(currentSavePath))
                    {
                        // 直接使用GameDataManager实例中的playerData
                        SlayTheSpireMap.GameDataManager.PlayerStateData data = SlayTheSpireMap.GameDataManager.Instance.playerData;
                        saveSlots[i-1].Setup(data);
                    }
                    else
                    {
                        saveSlots[i-1].ClearSlot(); // 文件不存在，强制显示为空
                    }
                }
            }
        }

    // 退出游戏
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit(); //
    }
    
    // 返回主菜单
    public void ReturnToMainMenu()
    {
        if (loadPanel != null) loadPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
    
    /// <summary>
    /// 退出到主页并存档
    /// </summary>
    public void ExitToMainWithSave()
    {
        // 1. 保存所有数据
        if (Bag.InventoryManager.Instance != null)
        {
            Bag.InventoryManager.Instance.SaveInventory();
            Debug.Log("背包数据已保存");
        }
        
        if (SlayTheSpireMap.GameDataManager.Instance != null)
        {
            SlayTheSpireMap.GameDataManager.Instance.SaveGameData();
            Debug.Log("游戏数据已保存");
        }
        
        // 3. 跳转到主菜单
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// 等待 GameDataManager 初始化完成后继续执行 OpenLoadMenu 逻辑
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator WaitForGDMInitAndContinue()
        {
            // 等待一帧，让 GameDataManager 的 Awake 方法执行
            yield return null;
            
            // 重新执行 OpenLoadMenu 逻辑
            if (SlayTheSpireMap.GameDataManager.Instance != null)
            {
                Debug.Log("GameDataManager 实例已初始化，继续执行存档加载逻辑");
                
                // 确保存档面板显示
                if (loadPanel != null)
                {
                    Debug.Log("[MainMenuController] 显示存档面板");
                    loadPanel.SetActive(false);
                    loadPanel.SetActive(true);
                }
                
                // 假设你有 3 个存档槽位
                for (int i = 1; i <= 3; i++) 
                {
                    if (saveSlots != null && saveSlots.Length >= i && saveSlots[i-1] != null)
                    {
                        // 检查是否存在当前存档
                        string currentSavePath = System.IO.Path.Combine(Application.persistentDataPath, "save_current.json");
                        if (System.IO.File.Exists(currentSavePath))
                        {
                            // 直接使用GameDataManager实例中的playerData
                            SlayTheSpireMap.GameDataManager.PlayerStateData data = SlayTheSpireMap.GameDataManager.Instance.playerData;
                            saveSlots[i-1].Setup(data);
                        }
                        else
                        {
                            saveSlots[i-1].ClearSlot(); // 文件不存在，强制显示为空
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("创建 GameDataManager 实例失败！");
            }
        }
}