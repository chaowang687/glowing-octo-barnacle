using UnityEngine;
using TMPro;
using SlayTheSpireMap;
using UnityEngine.Localization; // 必须引用

/// <summary>
/// 存档槽UI脚本，用于显示存档信息
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI timeText;   // 拖入显示时间的文本组件
    public TextMeshProUGUI detailText; // 显示“金币: 100 血量: 30”
    
    [Header("存档配置")]
    public int slotIndex;              // 存档编号
    
    /// <summary>
    /// 设置存档槽显示信息
    /// </summary>
    /// <param name="data">玩家状态数据</param>
    public void Setup(GameDataManager.PlayerStateData data)
    {
        // 检查 UI 组件是否已赋值
        if (timeText == null || detailText == null)
        {
            Debug.LogError("SaveSlotUI: timeText or detailText is not assigned!");
            return;
        }
        
        if (data != null)
        {
            timeText.text = data.saveTime; // 显示如 "2023-10-27 14:30"
            detailText.text = $"金币: {data.gold}  血量: {data.health}";
        }
        else
        {
            timeText.text = "空存档";
            detailText.text = "";
        }
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 清空存档槽显示
    /// </summary>
    public void ClearSlot()
    {
        // 检查 UI 组件是否已赋值
        if (timeText != null && detailText != null)
        {
            timeText.text = "空存档";
            detailText.text = "";
        }
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏存档槽
    /// </summary>
    public void HideSlot()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
        /// 加载该存档槽的游戏数据
        /// </summary>
        public void LoadThisSlot()
        {
            Debug.Log($"SaveSlotUI: LoadThisSlot - 开始加载存档槽 {slotIndex}");
            
            if (SlayTheSpireMap.GameDataManager.Instance == null)
            {
                Debug.LogError("SaveSlotUI: LoadThisSlot - GameDataManager.Instance为null，无法加载存档");
                return;
            }

            // 1. 构建存档路径
            string slotPath = System.IO.Path.Combine(Application.persistentDataPath, $"save_{slotIndex}.json");
            string currentPath = System.IO.Path.Combine(Application.persistentDataPath, "save_current.json");
            
            // 背包数据文件路径
            string slotInventoryPath = System.IO.Path.Combine(Application.persistentDataPath, $"inventory_{slotIndex}.json");
            string currentInventoryPath = System.IO.Path.Combine(Application.persistentDataPath, "inventory.json");
            
            Debug.Log($"SaveSlotUI: LoadThisSlot - 槽位路径: {slotPath}");
            Debug.Log($"SaveSlotUI: LoadThisSlot - 当前存档路径: {currentPath}");
            Debug.Log($"SaveSlotUI: LoadThisSlot - 槽位背包路径: {slotInventoryPath}");
            Debug.Log($"SaveSlotUI: LoadThisSlot - 当前背包路径: {currentInventoryPath}");
            
            // 2. 检查指定槽位存档是否存在
            Debug.Log($"SaveSlotUI: LoadThisSlot - 检查槽位存档是否存在: {slotPath}");
            if (System.IO.File.Exists(slotPath))
            {
                Debug.Log($"SaveSlotUI: LoadThisSlot - 槽位存档存在，开始复制到当前存档");
                
                // 3. 将槽位存档复制到当前存档，这样场景加载时会自动加载当前存档
                System.IO.File.Copy(slotPath, currentPath, true);
                
                // 4. 复制背包数据
                if (System.IO.File.Exists(slotInventoryPath))
                {
                    System.IO.File.Copy(slotInventoryPath, currentInventoryPath, true);
                    Debug.Log($"SaveSlotUI: LoadThisSlot - 已将槽位背包数据复制到当前背包数据");
                }
                else
                {
                    Debug.LogWarning($"SaveSlotUI: LoadThisSlot - 槽位背包数据不存在: {slotInventoryPath}");
                    // 如果槽位背包数据不存在，清空当前背包数据
                    if (System.IO.File.Exists(currentInventoryPath))
                    {
                        System.IO.File.Delete(currentInventoryPath);
                        Debug.Log("SaveSlotUI: LoadThisSlot - 已清空当前背包数据");
                    }
                }
                
                Debug.Log($"SaveSlotUI: LoadThisSlot - 已将槽位存档复制到当前存档，准备跳转到MapScene");
                
                // 5. 执行跳转
                // 注意：不直接调用LoadGameData，而是让MapScene的GameDataManager自动加载当前存档
                UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
            }
            else
            {
                Debug.LogError($"存档文件缺失，拒绝跳转！路径: {slotPath}");
                return; // 拦截跳转
            }
        }
    
    /// <summary>
    /// 删除该存档槽的游戏数据
    /// </summary>
    public void DeleteThisSlot()
    {
        string fileName = $"save_{slotIndex}.json";
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
        
        if (System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
            ClearSlot();
            Debug.Log($"已删除存档槽 {slotIndex} 的数据");
        }
    }
}