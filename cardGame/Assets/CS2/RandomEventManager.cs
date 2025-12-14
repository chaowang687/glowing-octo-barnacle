// RandomEventManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

namespace ScavengingGame
{
    [System.Serializable]
    public class RandomEventData
    {
        public string eventName;
        public string description;
        public float weight = 1f;
        public List<EventEffect> effects = new List<EventEffect>();
    }
    
    [System.Serializable]
    public class EventEffect
    {
        public enum EffectType
        {
            Heal,
            Damage,
            AddItem,
            RemoveItem,
            ModifyStat,
            Custom
        }
        
        public EffectType effectType;
        public int value;
        public string itemName;
        public string statName;
        public System.Action customAction;
    }

    public static class RandomEventManager
    {
        // 事件触发概率
        public static float GlobalEventChance { get; set; } = 0.1f;
        
        // 事件委托
        public static event Action<RandomEventData> OnEventTriggered;
        public static event Action<int> OnPlayerHealed;
        public static event Action<int> OnPlayerDamaged;
        public static event Action<string, int> OnItemAdded;
        
        private static List<RandomEventData> _eventPool = new List<RandomEventData>();
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化事件池
        /// </summary>
        public static void Initialize(List<RandomEventData> events = null)
        {
            if (events != null)
            {
                _eventPool = events;
            }
            else
            {
                // 创建默认事件
                CreateDefaultEvents();
            }
            
            _isInitialized = true;
        }
        
        private static void CreateDefaultEvents()
        {
            _eventPool = new List<RandomEventData>
            {
                new RandomEventData
                {
                    eventName = "治疗泉水",
                    description = "你发现了一口治疗泉水，回复了10点生命值。",
                    weight = 1f,
                    effects = new List<EventEffect>
                    {
                        new EventEffect
                        {
                            effectType = EventEffect.EffectType.Heal,
                            value = 10
                        }
                    }
                },
                new RandomEventData
                {
                    eventName = "陷阱",
                    description = "你踩中了陷阱，受到5点伤害！",
                    weight = 0.8f,
                    effects = new List<EventEffect>
                    {
                        new EventEffect
                        {
                            effectType = EventEffect.EffectType.Damage,
                            value = 5
                        }
                    }
                },
                new RandomEventData
                {
                    eventName = "神秘商人",
                    description = "一位神秘的商人短暂出现，但很快又消失了。",
                    weight = 0.5f,
                    effects = new List<EventEffect>()
                },
                new RandomEventData
                {
                    eventName = "发现宝藏",
                    description = "你发现了一个隐藏的宝箱！",
                    weight = 0.3f,
                    effects = new List<EventEffect>
                    {
                        new EventEffect
                        {
                            effectType = EventEffect.EffectType.AddItem,
                            itemName = "金币",
                            value = 50
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 检查并执行随机事件
        /// </summary>
        /// <returns>是否触发了事件</returns>
        public static bool TryTriggerRandomEvent()
        {
            if (!_isInitialized) Initialize();
            
            // 明确使用 UnityEngine.Random.value
            if (UnityEngine.Random.value < GlobalEventChance && _eventPool.Count > 0)
            {
                ExecuteRandomEvent();
                return true;
            }
            return false;
        }

        private static void ExecuteRandomEvent()
        {
            if (_eventPool.Count == 0) return;

            // 根据权重选择事件
            float totalWeight = 0;
            foreach (var evt in _eventPool)
            {
                totalWeight += evt.weight;
            }
            
            // 明确使用 UnityEngine.Random.Range
            float randomPoint = UnityEngine.Random.Range(0, totalWeight);
            RandomEventData selectedEvent = null;
            
            foreach (var evt in _eventPool)
            {
                if (randomPoint < evt.weight)
                {
                    selectedEvent = evt;
                    break;
                }
                randomPoint -= evt.weight;
            }
            
            // 备用方案
            if (selectedEvent == null)
            {
                // 明确使用 UnityEngine.Random.Range
                selectedEvent = _eventPool[UnityEngine.Random.Range(0, _eventPool.Count)];
            }
            
            // 触发事件
            TriggerEvent(selectedEvent);
        }
        
        private static void TriggerEvent(RandomEventData eventData)
        {
            Debug.Log($"[随机事件] {eventData.description}");
            
            // 触发全局事件
            OnEventTriggered?.Invoke(eventData);
            
            // 应用效果
            ApplyEventEffects(eventData);
        }
        
        private static void ApplyEventEffects(RandomEventData eventData)
        {
            foreach (var effect in eventData.effects)
            {
                switch (effect.effectType)
                {
                    case EventEffect.EffectType.Heal:
                        OnPlayerHealed?.Invoke(effect.value);
                        Debug.Log($"玩家回复了{effect.value}点生命值");
                        break;
                        
                    case EventEffect.EffectType.Damage:
                        OnPlayerDamaged?.Invoke(effect.value);
                        Debug.Log($"玩家受到{effect.value}点伤害");
                        break;
                        
                    case EventEffect.EffectType.AddItem:
                        OnItemAdded?.Invoke(effect.itemName, effect.value);
                        Debug.Log($"玩家获得了{effect.value}个{effect.itemName}");
                        break;
                        
                    case EventEffect.EffectType.Custom:
                        effect.customAction?.Invoke();
                        break;
                }
            }
        }
        
        /// <summary>
        /// 添加新事件
        /// </summary>
        public static void AddEvent(RandomEventData newEvent)
        {
            _eventPool.Add(newEvent);
        }
        
        /// <summary>
        /// 移除事件
        /// </summary>
        public static bool RemoveEvent(string eventName)
        {
            return _eventPool.RemoveAll(e => e.eventName == eventName) > 0;
        }
        
        /// <summary>
        /// 获取所有事件
        /// </summary>
        public static List<RandomEventData> GetAllEvents()
        {
            return new List<RandomEventData>(_eventPool);
        }
    }
}