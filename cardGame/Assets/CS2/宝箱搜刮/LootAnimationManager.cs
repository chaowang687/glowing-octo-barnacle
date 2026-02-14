using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScavengingGame
{
public class LootAnimationManager : MonoBehaviour
{
    [SerializeField] private GameObject searchingEffectPrefab;
    [SerializeField] private AudioClip searchingSound;
    [SerializeField] private AudioClip foundSound;
    [SerializeField] private float delayBetweenSlots = 0.2f;
    
    private AudioSource audioSource;
    private List<LootSlot> lootSlots;
    private System.Action onAnimationComplete;
    
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public void PlaySearchAnimationSequence(List<LootSlot> slots, System.Action onComplete)
    {
        lootSlots = slots;
        onAnimationComplete = onComplete;
        
        StartCoroutine(SearchSequenceCoroutine());
    }
    
    private IEnumerator SearchSequenceCoroutine()
    {
        // 播放整体搜索开始音效
        if (searchingSound != null)
        {
            audioSource.PlayOneShot(searchingSound);
        }
        
        // 为每个物品槽播放查找动画
        for (int i = 0; i < lootSlots.Count; i++)
        {
            LootSlot slot = lootSlots[i];
            
            if (slot.IsEmpty) continue;
            
            // 延迟开始下一个槽的动画
            if (i > 0) yield return new WaitForSeconds(delayBetweenSlots);
            
            // 开始当前槽的查找动画
            yield return StartCoroutine(PlaySlotSearchAnimation(slot));
        }
        
        onAnimationComplete?.Invoke();
    }
    
    private IEnumerator PlaySlotSearchAnimation(LootSlot slot)
    {
        // 创建查找特效
        GameObject effect = Instantiate(searchingEffectPrefab, slot.transform);
        effect.transform.localPosition = Vector3.zero;
        
        // 获取动画时长
        float duration = slot.CurrentItemData.findAnimationDuration;
        
        // 播放查找动画
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // 可以在这里更新查找动画的视觉效果
            // 例如：旋转、缩放、透明度变化等
            
            yield return null;
        }
        
        // 销毁特效
        Destroy(effect);
        
        // 播放找到物品的音效
        if (foundSound != null)
        {
            audioSource.PlayOneShot(foundSound);
        }
        
        // 显示物品
        slot.RevealItem();
    }
}
}