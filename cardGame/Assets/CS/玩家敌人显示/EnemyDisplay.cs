// EnemyDisplay.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class EnemyDisplay : MonoBehaviour
{
    // ⭐ 必须在 Inspector 中拖入这些 UI 组件 ⭐
    public Image intentIcon;
    public TextMeshProUGUI intentValueText; // 建议使用 TextMeshProUGUI
    // ⭐ 引用配置资产 ⭐
    public IntentIconConfig iconConfig;
    
    [Header("格挡 UI 引用")]
    public GameObject blockDisplayRoot; // 整个格挡 UI 的根对象
    public Image blockIcon;            // 格挡图标（通常是盾牌）
    public TextMeshProUGUI blockValueText; // 格挡数值

    // EnemyDisplay.cs
private CharacterBase character; // 用于缓存 CharacterBase 实例

    void Awake()
    {
       
        character = GetComponentInParent<CharacterBase>(); // ✅ 这个应该成功，但需确认
        if (character == null)
        {
            Debug.LogError("EnemyDisplay 找不到 CharacterBase 组件!");
        }
    }
    void Start()
    {
        if (character != null)
        {
            // 订阅格挡变化事件
            character.OnBlockChanged += RefreshBlockDisplay;
        }
        else
        {
            Debug.LogError("EnemyDisplay 订阅失败：CharacterBase 对象为 null。");
        }
        // 初始显示
        RefreshBlockDisplay(); 
    }
    public void RefreshBlockDisplay()
    {
        if (character == null || blockDisplayRoot == null)
        {
        Debug.LogError("致命错误：EnemyDisplay 缺少 CharacterBase 或 UI 引用。无法刷新。");
        return; 
        }
        
        // 确保 CharacterBase 拥有 CurrentBlock 或 Block 属性
        int currentBlock = character.CurrentBlock; // 假设属性名为 CurrentBlock

        Debug.Log($"DEBUG UI: {character.characterName} 刷新中。Block={currentBlock}. Root={blockDisplayRoot.name}.");
     
        
        if (currentBlock > 0)
        {
            // 激活 UI
            blockDisplayRoot.SetActive(true);
            
            // 更新数值
            blockValueText.text = currentBlock.ToString();
            Debug.Log("DEBUG REFRESH: Block UI SET ACTIVE."); // ⭐ 新增日志 2 ⭐
            
            // 可选：如果格挡图标会根据数值变化，在这里设置 Sprite
        }
        else
        {
            // 隐藏 UI
            blockDisplayRoot.SetActive(false);
        }
    }

    public void RefreshIntent(IntentType type, int value)
    {
        // EnemyDisplay.cs (RefreshIntent 内部)
        intentIcon.gameObject.SetActive(true); 
        if (intentValueText.gameObject != null)
        {
            intentValueText.gameObject.SetActive(true);
        }
        // 1. 设置图标（需要一个方法或字典来匹配 IntentType 和 Sprite）
        intentIcon.sprite = GetSpriteForIntent(type); 
        
        // 2. 设置数值
        if (value > 0 || type == IntentType.ATTACK)
        {
            intentValueText.text = value.ToString();
            intentValueText.gameObject.SetActive(true);
        }
        else
        {
            intentValueText.gameObject.SetActive(false);
        }
        // 1. 设置图标：使用配置资产查找
        if (iconConfig != null)
        {
            intentIcon.sprite = iconConfig.GetIcon(type); 
        }
        
        intentIcon.gameObject.SetActive(type != IntentType.NONE);
    }
    
    // 假设这个方法存在
    private Sprite GetSpriteForIntent(IntentType type)
    {
        // ... (根据 type 返回对应的 Sprite) ...
        return null; // 占位符
    }
}