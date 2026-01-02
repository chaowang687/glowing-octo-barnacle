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
            Debug.Log($"跳转到场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
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