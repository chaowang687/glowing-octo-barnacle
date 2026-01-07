using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlayTheSpireMap
{
    public class SceneTransitionManager : MonoBehaviour
    {
        // 场景名称配置（硬编码简单版）
        public const string BATTLE_SCENE = "BattleScene";
        public const string SHOP_SCENE = "ShopScene";
        public const string EVENT_SCENE = "EventScene";
        public const string REST_SCENE = "RestScene";
        public const string DIG_SCENE = "DigScene";     // 挖掘场景
        public const string MAP_SCENE = "MapScene";
        public const string MAIN_MENU_SCENE = "MainMenu";
        
        // 单例实例
        public static SceneTransitionManager Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 根据节点类型跳转到对应场景
        /// </summary>
        public void GoToSceneByNodeType(NodeType nodeType)
        {
            string sceneName = MAP_SCENE;
            
            switch (nodeType)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    sceneName = BATTLE_SCENE;
                    break;
                    
                case NodeType.Shop:
                    sceneName = SHOP_SCENE;
                    break;
                    
                case NodeType.Event:
                    sceneName = EVENT_SCENE;
                    break;
                    
                case NodeType.Rest:
                    sceneName = REST_SCENE;
                    break;
                    
                case NodeType.Dig:
                    sceneName = DIG_SCENE;
                    break;
                    
                default:
                    sceneName = MAP_SCENE;
                    break;
            }
            
            Debug.Log($"跳转到场景: {sceneName} (节点类型: {nodeType})");
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// 直接跳转到指定场景
        /// </summary>
        public void GoToScene(string sceneName)
        {
            // === 新增：每当发生场景切换（推进关卡）时自动存档 ===
            AutoSaveCurrentProgress();

            Debug.Log($"跳转到场景: {sceneName}，已自动存档");
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// 自动保存当前进度
        /// </summary>
        private void AutoSaveCurrentProgress()
        {
            // 1. 保存地图进度、血量、金币等
            if (SlayTheSpireMap.GameDataManager.Instance != null)
            {
                SlayTheSpireMap.GameDataManager.Instance.SaveGameData();
            }

            // 2. 保存背包物品
            if (Bag.InventoryManager.Instance != null)
            {
                Bag.InventoryManager.Instance.SaveInventory();
            }
            
            // 3. 强制 PlayerPrefs 写入磁盘（防止强行关闭导致丢失）
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 返回地图场景
        /// </summary>
        public void ReturnToMap()
        {
            // 在返回地图前保存游戏数据
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SaveGameData();
            }
            
            GoToScene(MAP_SCENE);
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            // 保存所有数据
            if (Bag.InventoryManager.Instance != null)
            {
                Bag.InventoryManager.Instance.SaveInventory();
                Debug.Log("SceneTransitionManager: 背包数据已保存");
            }
            
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.SaveGameData();
                Debug.Log("SceneTransitionManager: 游戏数据已保存");
            }
            
            // 注意：不要清理任何资源引用，保持资源引用，这样下次加载游戏时资源仍然可用
            GoToScene(MAIN_MENU_SCENE);
        }
        
        /// <summary>
        /// 重启当前场景
        /// </summary>
        public void RestartCurrentScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}