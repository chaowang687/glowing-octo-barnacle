using UnityEngine;
using System.Collections.Generic;

namespace SlayTheSpireMap
{
    [CreateAssetMenu(fileName = "NewCharacterStarter", menuName = "Game Data/Character Starter Data")]
    public class CharacterStarterData : ScriptableObject
    {
        [Header("基础信息")]
        public string characterName = "铁甲卫士";
        public int maxHealth = 80;
        public int startingGold = 99;

        [Header("初始卡组")]
        public List<CardData> startingCards = new List<CardData>();
        
        [Header("初始遗物 (可选)")]
        public List<string> startingRelicIds = new List<string>();
    }
}