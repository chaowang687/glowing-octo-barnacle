using UnityEngine;
using UnityEngine.UI;
using System.Linq; // 确保包含 Linq 以便使用 .ToList() 等

public class CharacterUIDisplay : MonoBehaviour
{
    // UI 组件引用 (例如：血条 Image 或 Slider)
    [Header("UI Components")]
    public Slider hpSlider;
    public Text nameText; // 用于显示名称，目前未实现绑定逻辑

    private CharacterBase _targetCharacter; // 用于保存对角色的引用

    /// <summary>
    /// 绑定 UI 视图到角色数据，并订阅生命值变化事件。
    /// </summary>
    public void Initialize(CharacterBase character)
    {
        // 第一次检查已确保 character 不为 null
        if (character == null) return; 
        
        _targetCharacter = character;
        Debug.Log($"UI Display bound to character: {character.characterName}"); // 使用 CharacterBase 中的 characterName 属性
        
        // 1. 首次初始化 Slider
        if (hpSlider != null)
        {
            hpSlider.maxValue = character.maxHp;
            hpSlider.value = character.currentHp;
        }
        
        // 2. 订阅事件 (View 绑定到 Model)
        character.OnHealthChanged += UpdateHealthBar;

        // 3. ⭐ 关键修正：只添加一次 DestroyListener，用于在角色死亡或销毁时取消订阅 ⭐
        DestroyListener listener = character.gameObject.AddComponent<DestroyListener>();
        listener.onDestroy += OnTargetDestroyed; // 订阅销毁事件

        // (可选) 绑定名称显示
        if (nameText != null)
        {
            nameText.text = character.characterName;
        }
    }

    /// <summary>
    /// 事件触发时调用的函数：更新血条。
    /// </summary>
    private void UpdateHealthBar(int currentHp, int maxHp)
    {
        // 确保血条的最大值仍然正确 (虽然一般MaxHP不变)
        if (hpSlider != null)
        {
            // 更新血条的当前值
            hpSlider.value = currentHp; 
        }
    }
    
    /// <summary>
    /// 目标角色游戏对象销毁时调用，用于取消订阅，防止内存泄漏。
    /// </summary>
    private void OnTargetDestroyed()
    {
        // 检查目标角色是否仍然存在，如果存在则取消订阅。
        if (_targetCharacter != null)
        {
            _targetCharacter.OnHealthChanged -= UpdateHealthBar;
            Debug.Log($"Successfully unsubscribed {_targetCharacter.characterName}'s health event.");
        }
        
        // 由于 DestroyListener 是作为目标角色的组件添加的，当目标角色销毁时，这个方法会被调用。
        // 然而，CharacterUIDisplay 是一个独立的组件（或者在您的设计中是模型的子对象），
        // 即使目标销毁了，这个 Display 对象也可能不会立即销毁。
    }

    // 推荐：如果 CharacterUIDisplay 对象本身被销毁，也要取消订阅
    private void OnDestroy()
    {
        if (_targetCharacter != null)
        {
             _targetCharacter.OnHealthChanged -= UpdateHealthBar;
        }
    }
}