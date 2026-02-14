// MainMenuController.cs - 修复版本
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SlayTheSpireMap
{
    public class MainMenuController : MonoBehaviour
    {
        public Button startButton;
        public Button quitButton;
        
        void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }
        
        void StartGame()
        {
            Debug.Log("开始游戏");
            
            // 重置游戏状态
            ResetGameState();
            
            // 直接加载地图场景
            SceneManager.LoadScene("MapScene");
        }
        
        void QuitGame()
        {
            Debug.Log("退出游戏");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        /// <summary>
        /// 重置游戏状态
        /// </summary>
        private void ResetGameState()
        {
            // 清除玩家进度
            PlayerPrefs.DeleteKey("PlayerGold");
            PlayerPrefs.DeleteKey("PlayerHealth");
            PlayerPrefs.DeleteKey("PlayerMaxHealth");
            PlayerPrefs.DeleteKey("CurrentMapLevel");
            PlayerPrefs.DeleteKey("CurrentNodeId");
            PlayerPrefs.DeleteKey("PlayerDeck");
            
            // 清除战斗相关数据
            PlayerPrefs.DeleteKey("MapBattle_Encounter");
            PlayerPrefs.DeleteKey("MapBattle_PlayerHealth");
            PlayerPrefs.DeleteKey("MapBattle_PlayerMaxHealth");
            PlayerPrefs.DeleteKey("JustReturnedFromBattle");
            PlayerPrefs.DeleteKey("LastBattleResult");
            
            PlayerPrefs.Save();
            Debug.Log("游戏状态已重置");
            
            // 如果MapManager已经存在，重置它
            if (MapManager.Instance != null)
            {
                MapManager.Instance.ResetMapProgress();
            }
        }
    }
}