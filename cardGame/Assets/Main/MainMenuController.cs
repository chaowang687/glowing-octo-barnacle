using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("面板引用")]
    public GameObject mainMenuPanel;    // 拖入主菜单根物体
    public GameObject selectionPanel;   // 拖入角色选择界面根物体
    public GameObject loadPanel;        // 档案面板引用

    [Header("场景设置")]
    public string firstLevelName = "GameScene"; // 初始关卡名

    // 修改：新游戏不再直接跳转，而是打开选人面板
    public void StartNewGame()
    {
        // 如果需要重置旧存档数据，可以调用之前定义的全局重置
        if (SlayTheSpireMap.GameDataManager.Instance != null)
        {
            SlayTheSpireMap.GameDataManager.Instance.ResetToDefault(); //
        }

        if (mainMenuPanel != null && selectionPanel != null)
        {
            mainMenuPanel.SetActive(false);
            selectionPanel.SetActive(true);
        }
    }

    // 档案/继续游戏
    public void OpenLoadMenu()
    {
        if (loadPanel != null) loadPanel.SetActive(true); //
    }

    // 退出游戏
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit(); //
    }
}