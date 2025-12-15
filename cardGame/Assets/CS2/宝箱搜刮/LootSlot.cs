// LootSlot.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

using ScavengingGame;

public class LootSlot : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Text quantityText;
    [SerializeField] private GameObject searchingOverlay;
    [SerializeField] private Color[] rarityColors;
    [SerializeField] private float returnToPositionDuration = 0.3f;
    
    public LootItemData CurrentItemData { get; private set; }
    public bool IsEmpty => CurrentItemData == null;
    public bool IsRevealed { get; private set; }
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private ScavengingGame.IInventoryService inventoryService;
    // 移除未使用的 isDragging 字段
    // private bool isDragging = false; // 已移除
    private bool isReturningToPosition = false;
    
    public event Action<LootSlot> OnItemCollected;
    public event Action<LootSlot> OnCollectFailed;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        FindInventoryService();
        ResetSlot();
    }
    
    private void FindInventoryService()
    {
        if (ServiceLocator.Instance != null)
        {
            inventoryService = ServiceLocator.Instance.GetService<ScavengingGame.IInventoryService>();
            if (inventoryService != null) return;
        }
        
        // 修复：使用 FindObjectsByType 替代 FindObjectsOfType
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in allBehaviours)
        {
            if (behaviour is ScavengingGame.IInventoryService service)
            {
                inventoryService = service;
                return;
            }
        }
        
        Debug.LogWarning("未找到 IInventoryService 实现");
    }
    
    public void Initialize(LootItemData itemData)
    {
        CurrentItemData = itemData;
        IsRevealed = false;
        
        if (slotBackground != null && rarityColors.Length > (int)itemData.rarity)
        {
            slotBackground.color = rarityColors[(int)itemData.rarity];
        }
        
        itemIcon.gameObject.SetActive(false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
        if (searchingOverlay != null) searchingOverlay.SetActive(true);
    }
    
    public void RevealItem()
    {
        if (IsEmpty) return;
        
        IsRevealed = true;
        itemIcon.sprite = CurrentItemData.icon;
        itemIcon.gameObject.SetActive(true);
        
        if (quantityText != null && CurrentItemData.quantity > 1)
        {
            quantityText.text = CurrentItemData.quantity.ToString();
            quantityText.gameObject.SetActive(true);
        }
        
        if (searchingOverlay != null) searchingOverlay.SetActive(false);
    }
    
    public void ResetSlot()
    {
        CurrentItemData = null;
        IsRevealed = false;
        itemIcon.gameObject.SetActive(false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
        if (searchingOverlay != null) searchingOverlay.SetActive(false);
        if (slotBackground != null) slotBackground.color = Color.white;
        // 移除 isDragging 相关的设置
        // isDragging = false; // 已移除
        isReturningToPosition = false;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsEmpty || !IsRevealed || isReturningToPosition) return;
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        // 移除对 isDragging 的赋值，因为不再使用
        // isDragging = true; // 已移除
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty || !IsRevealed || isReturningToPosition) return;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.root);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (IsEmpty || !IsRevealed || isReturningToPosition) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsEmpty || !IsRevealed || isReturningToPosition) return;
        // 移除 isDragging 的赋值
        // isDragging = false; // 已移除
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        bool collected = TryCollectToInventory(eventData);
        if (!collected) ReturnToOriginalPosition();
        else
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    private void ReturnToOriginalPosition()
    {
        if (isReturningToPosition) return;
        isReturningToPosition = true;
        StartCoroutine(ReturnToPositionCoroutine());
    }
    
    private System.Collections.IEnumerator ReturnToPositionCoroutine()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < returnToPositionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnToPositionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);
            yield return null;
        }
        rectTransform.anchoredPosition = originalPosition;
        transform.SetParent(originalParent);
        isReturningToPosition = false;
    }
    
    private bool TryCollectToInventory(PointerEventData eventData)
    {
        if (inventoryService == null)
        {
            Debug.LogWarning("库存服务不可用");
            OnCollectFailed?.Invoke(this);
            return false;
        }
        
        if (IsEmpty || !IsRevealed) return false;
        
        bool hasSpace = false;
        try
        {
            var method = inventoryService.GetType().GetMethod("HasSpaceForItem");
            if (method != null)
            {
                hasSpace = (bool)method.Invoke(inventoryService, new object[] { CurrentItemData.itemId, CurrentItemData.quantity });
            }
            else hasSpace = CheckSpaceAlternative();
        }
        catch { hasSpace = CheckSpaceAlternative(); }
        
        if (!hasSpace)
        {
            Debug.Log("背包空间不足");
            ShowCollectFailedFeedback("背包空间不足");
            OnCollectFailed?.Invoke(this);
            return false;
        }
        
        bool success = false;
        try
        {
            ScavengingGame.ItemData itemData = CreateItemDataFromLoot();
            success = inventoryService.AddItem(itemData, CurrentItemData.quantity);
        }
        catch (Exception e)
        {
            Debug.LogError($"添加物品到库存时出错: {e.Message}");
            success = false;
        }
        
        if (success)
        {
            Debug.Log($"成功收集物品: {CurrentItemData.itemName} x{CurrentItemData.quantity}");
            PlayCollectSuccessEffect();
            OnItemCollected?.Invoke(this);
            ResetSlot();
            return true;
        }
        else
        {
            Debug.LogWarning("收集物品失败");
            ShowCollectFailedFeedback("收集失败");
            OnCollectFailed?.Invoke(this);
            return false;
        }
    }
    
    private bool CheckSpaceAlternative()
    {
        try
        {
            int maxCapacity = inventoryService.GetMaxCapacity();
            int currentCapacity = inventoryService.GetCurrentCapacity();
            return currentCapacity < maxCapacity;
        }
        catch { return true; }
    }
    
    protected ScavengingGame.ItemData CreateItemDataFromLoot()
    {
        ScavengingGame.ItemData itemData = ScriptableObject.CreateInstance<ScavengingGame.ItemData>();
        itemData.itemId = CurrentItemData.itemId;
        itemData.itemName = CurrentItemData.itemName;
        itemData.icon = CurrentItemData.icon;
        
        
        itemData.maxStackSize = GetMaxStackSizeByRarity(CurrentItemData.rarity);
        itemData.value = CalculateValueByRarity(CurrentItemData.rarity);
        itemData.description = GetDescriptionByRarity(CurrentItemData.rarity);
        itemData.rarity = CurrentItemData.rarity;
        
        return itemData;
    }
    
    private int GetMaxStackSizeByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 99;
            case ItemRarity.Uncommon: return 50;
            case ItemRarity.Rare: return 20;
            case ItemRarity.Epic: return 5;
            case ItemRarity.Legendary: return 1;
            default: return 99;
        }
    }
    
    private int CalculateValueByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 10;
            case ItemRarity.Uncommon: return 50;
            case ItemRarity.Rare: return 200;
            case ItemRarity.Epic: return 1000;
            case ItemRarity.Legendary: return 5000;
            default: return 10;
        }
    }
    
    private string GetDescriptionByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "一件普通的物品";
            case ItemRarity.Uncommon: return "一件不错的物品";
            case ItemRarity.Rare: return "一件稀有的物品";
            case ItemRarity.Epic: return "一件史诗物品";
            case ItemRarity.Legendary: return "一件传奇物品";
            default: return "一件物品";
        }
    }
    
    public void CollectItem()
    {
        if (IsEmpty || !IsRevealed) return;
        TryCollectToInventory(null);
    }
    
    private void ShowCollectFailedFeedback(string message)
    {
        StartCoroutine(PlayShakeAnimation());
        
        // 移除对 UIManager 的依赖，改为简单的反馈
        ShowSimpleFeedback(message);
    }
    
    private void ShowSimpleFeedback(string message)
    {
        // 方法1：使用Debug.Log
        Debug.LogWarning($"收集失败: {message}");
        
        // 方法2：播放错误音效（如果有）
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            // 播放错误音效
            AudioClip errorSound = Resources.Load<AudioClip>("Sounds/Error");
            if (errorSound != null)
            {
                audioSource.PlayOneShot(errorSound);
            }
        }
        
        // 方法3：显示简单的文本效果
        StartCoroutine(ShowFloatingTextCoroutine(message));
    }
    
    private System.Collections.IEnumerator ShowFloatingTextCoroutine(string message)
    {
        // 查找Canvas
        // 修复：使用 FindFirstObjectByType 替代 FindObjectOfType
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning(message);
            yield break;
        }
        
        // 创建临时文本对象
        GameObject textObj = new GameObject("CollectErrorText");
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = message;
        textComponent.color = Color.red;
        
        // 修复：使用更现代的方式加载字体
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (textComponent.font == null)
        {
            // 备用方案：尝试加载 Arial
            textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        
        textComponent.fontSize = 16;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        // 设置到Canvas
        textObj.transform.SetParent(canvas.transform, false);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        
        // 设置位置在槽位附近
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        rect.anchoredPosition = new Vector2(screenPos.x - Screen.width / 2, screenPos.y - Screen.height / 2 + 50);
        rect.sizeDelta = new Vector2(200, 30);
        
        // 淡出效果
        float duration = 1.5f;
        float elapsed = 0f;
        Vector2 startPos = rect.anchoredPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = 1f - t;
            textComponent.color = new Color(1, 0, 0, alpha);
            
            // 向上移动
            rect.anchoredPosition = startPos + new Vector2(0, 50 * t);
            
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    private System.Collections.IEnumerator PlayShakeAnimation()
    {
        float shakeDuration = 0.5f;
        float shakeMagnitude = 0.1f;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = UnityEngine.Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, 0);
            yield return null;
        }
        transform.localPosition = new Vector3(originalPosition.x, originalPosition.y, 0);
    }
    
    private void PlayCollectSuccessEffect()
    {
        GameObject effectPrefab = Resources.Load<GameObject>("Effects/CollectSuccess");
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        StartCoroutine(PlayFadeOutAnimation());
    }
    
    private System.Collections.IEnumerator PlayFadeOutAnimation()
    {
        if (itemIcon == null) yield break;
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        Color originalColor = itemIcon.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            itemIcon.color = newColor;
            if (quantityText != null) quantityText.color = newColor;
            yield return null;
        }
    }
    
    public void SetInventoryService(ScavengingGame.IInventoryService service)
    {
        inventoryService = service;
    }
    
    public ScavengingGame.IInventoryService GetInventoryService()
    {
        return inventoryService;
    }
}