using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlayTheSpireMap
{
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance;
        
        [Header("场景名称")]
        public string mapSceneName = "MapScene";
        public string battleSceneName = "BattleScene";
        public string shopSceneName = "ShopScene";
        public string eventSceneName = "EventScene";
        
        [Header("场景切换效果")]
        public float fadeDuration = 0.5f;
        public CanvasGroup fadePanel;
        
        // 跨场景传递的数据
        public class SceneData
        {
            public NodeType nodeType;
            public int currentLayer;
            public int totalLayers;
            public bool isElite;
            public bool isBoss;
        }
        
        private SceneData currentSceneData;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 初始化淡入淡出面板
                if (fadePanel == null)
                {
                    CreateFadePanel();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void CreateFadePanel()
        {
            GameObject fadeObj = new GameObject("FadePanel");
            fadePanel = fadeObj.AddComponent<CanvasGroup>();
            
            // 添加到Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("TransitionCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            fadeObj.transform.SetParent(canvas.transform);
            
            // 设置全屏覆盖
            fadePanel.gameObject.AddComponent<UnityEngine.UI.Image>().color = Color.black;
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            
            RectTransform rt = fadeObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        
        // 从地图场景切换到战斗/商店/事件场景
        public void LoadNodeScene(NodeType nodeType, int layer, int totalLayers)
        {
            // 保存场景数据
            currentSceneData = new SceneData()
            {
                nodeType = nodeType,
                currentLayer = layer,
                totalLayers = totalLayers,
                isElite = nodeType == NodeType.Elite,
                isBoss = nodeType == NodeType.Boss
            };
            
            // 根据节点类型加载不同场景
            switch(nodeType)
            {
                case NodeType.Combat:
                case NodeType.Elite:
                case NodeType.Boss:
                    StartCoroutine(TransitionToScene(battleSceneName));
                    break;
                    
                case NodeType.Shop:
                    StartCoroutine(TransitionToScene(shopSceneName));
                    break;
                    
                case NodeType.Event:
                    StartCoroutine(TransitionToScene(eventSceneName));
                    break;
                    
                case NodeType.Rest:
                    // 休息点可以直接在地图场景处理
                    Debug.Log("在休息点回复生命");
                    break;
            }
        }
        
        // 从其他场景返回地图场景
        public void ReturnToMapScene(bool battleWon = true)
        {
            StartCoroutine(TransitionToScene(mapSceneName, battleWon));
        }
        
        System.Collections.IEnumerator TransitionToScene(string sceneName, bool success = true)
        {
            // 淡出
            yield return StartCoroutine(FadePanel(true));
            
            // 加载场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // 淡入
            yield return StartCoroutine(FadePanel(false));
            
            // 传递数据到新场景
            if (sceneName == battleSceneName)
            {
                InitializeBattleScene();
            }
            else if (sceneName == mapSceneName)
            {
                InitializeMapScene(success);
            }
        }
        
        System.Collections.IEnumerator FadePanel(bool fadeOut)
        {
            float startAlpha = fadeOut ? 0f : 1f;
            float targetAlpha = fadeOut ? 1f : 0f;
            float elapsed = 0f;
            
            fadePanel.blocksRaycasts = true;
            
            while (elapsed < fadeDuration)
            {
                fadePanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            fadePanel.alpha = targetAlpha;
            fadePanel.blocksRaycasts = fadeOut;
        }
        
        void InitializeBattleScene()
        {
            // 找到BattleScene中的BattleManager并传递数据
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null && currentSceneData != null)
            {
                battleManager.InitializeBattle(currentSceneData);
            }
        }
        
        void InitializeMapScene(bool success)
        {
            // 找到MapScene中的MapManager并更新状态
            MapManager mapManager = FindObjectOfType<MapManager>();
            if (mapManager != null)
            {
                mapManager.OnReturnFromBattle(success);
            }
        }
        
        // 获取当前场景数据
        public SceneData GetCurrentSceneData()
        {
            return currentSceneData;
        }
    }
}