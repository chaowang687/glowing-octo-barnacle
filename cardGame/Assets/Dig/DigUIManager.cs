using UnityEngine;
using UnityEngine.UI;
using SlayTheSpireMap;

public class DigUIManager : MonoBehaviour
{
    [Header("按钮配置")]
    public Button returnToMapButton; // 可在Inspector中配置的按钮接口
    public bool useDynamicCreation = true; // 是否使用动态创建（当Inspector未配置按钮时使用）
    
    private Canvas canvas;
    
    void Start()
    {
        InitializeUI();
    }
    
    void InitializeUI()
    {
        // 如果Inspector中配置了按钮，直接使用
        if (returnToMapButton != null)
        {
            returnToMapButton.onClick.AddListener(ReturnToMap);
            return;
        }
        
        // 如果未配置按钮且启用了动态创建，则动态生成
        if (useDynamicCreation)
        {
            CreateDynamicUI();
        }
    }
    
    void CreateDynamicUI()
    {
        // 创建或获取Canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // 创建返回按钮
        GameObject buttonObj = new GameObject("ReturnToMapButton");
        buttonObj.transform.SetParent(canvas.transform);
        
        // 添加按钮组件
        returnToMapButton = buttonObj.AddComponent<Button>();
        
        // 添加Image组件作为背景
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.6f, 1f); // 蓝色背景
        
        // 设置按钮尺寸和位置
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(200, 60);
        buttonRect.anchorMin = new Vector2(0.5f, 0.1f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.1f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        
        // 创建文本
        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform);
        Text buttonText = textObj.AddComponent<Text>();
        
        // 设置文本属性
        buttonText.text = "返回地图";
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        // 设置文本RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 添加点击事件
        returnToMapButton.onClick.AddListener(ReturnToMap);
    }
    
    void ReturnToMap()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ReturnToMap();
        }
        else
        {
            Debug.LogError("SceneTransitionManager.Instance is null! Cannot return to map.");
            // 备用方案：直接使用SceneManager加载地图场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
        }
    }
}