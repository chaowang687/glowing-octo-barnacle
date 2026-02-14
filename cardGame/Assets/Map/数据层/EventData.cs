// EventData.cs
using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    [CreateAssetMenu(fileName = "NewEventData", menuName = "SlayTheSpire/Event Data")]
    public class EventData : ScriptableObject
    {
        [Header("事件基本信息")]
        public string eventId;
        public string eventName;
        public string description;
        
        [Header("事件选项")]
        public List<EventOption> options = new List<EventOption>();
        
        [Header("事件类型")]
        public EventType eventType = EventType.Random;
        
        [Header("触发条件")]
        public int minLayer = 1;
        public int maxLayer = 99;
        public bool isRepeatable = false;
        
        public EventData()
        {
            // 默认构造函数
            eventId = "event_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
    
    [System.Serializable]
    public class EventOption
    {
        public string optionText;
        public string requirement; // 可选：触发条件描述
        
        [Header("结果")]
        public EventResult result;
        
        public EventOption() { }
        
        public EventOption(string text, EventResult result)
        {
            optionText = text;
            this.result = result;
        }
    }
    
    [System.Serializable]
    public class EventResult
    {
        [Header("奖励")]
        public int goldChange = 0;
        public int healthChange = 0; // 可以为负值
        public int maxHealthChange = 0;
        public List<string> cardRewards = new List<string>();
        public string relicReward;
        
        [Header("惩罚")]
        public bool loseRandomCard = false;
        public bool takeDamage = false;
        public int damageAmount = 0;
        
        [Header("特殊效果")]
        public bool addCurse = false;
        public string curseCardId;
        
        [Header("后续事件")]
        public string nextEventId;
        
        public EventResult() { }
    }
    
    public enum EventType
    {
        Random,
        Shrine,
        Treasure,
        Mystery,
        Boss,
        Special
    }
}