using System.Collections.Generic;
using UnityEngine;

namespace SlayTheSpireMap
{
    public class PlayerStateManager : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerState
        {
            public int health = 30;
            public int maxHealth = 30;
            public int gold = 100;
            public List<string> cardIds = new List<string>();
        }
        
        [Header("玩家状态")]
        [SerializeField] private PlayerState currentState = new PlayerState();
        
        [Header("卡组配置")]
        public PlayerDeck playerDeck;
        
        // 属性访问器
        public int Health => currentState.health;
        public int MaxHealth => currentState.maxHealth;
        public int Gold => currentState.gold;
        public List<string> Cards => currentState.cardIds;
        
        public PlayerState GetPlayerState() => currentState;
        
        public void SetHealth(int health)
        {
            currentState.health = Mathf.Clamp(health, 0, currentState.maxHealth);
        }
        
        public void SetMaxHealth(int maxHealth)
        {
            currentState.maxHealth = Mathf.Max(1, maxHealth);
            if (currentState.health > currentState.maxHealth)
            {
                currentState.health = currentState.maxHealth;
            }
        }
        
        public void AddGold(int amount)
        {
            currentState.gold += amount;
            if (currentState.gold < 0) currentState.gold = 0;
        }
        
        public void Heal(int amount)
        {
            currentState.health += amount;
            if (currentState.health > currentState.maxHealth)
            {
                currentState.health = currentState.maxHealth;
            }
        }
        
        public void HealPercentage(int percentage)
        {
            int healAmount = Mathf.RoundToInt(currentState.maxHealth * percentage / 100f);
            Heal(healAmount);
        }
        
        public void AddCard(string cardId)
        {
            if (playerDeck == null) playerDeck = new PlayerDeck();
            playerDeck.AddCard(cardId);
            currentState.cardIds = new List<string>(playerDeck.cardIds);
        }
        
        public void RemoveCard(string cardId)
        {
            if (playerDeck != null)
            {
                playerDeck.RemoveCard(cardId);
                currentState.cardIds = new List<string>(playerDeck.cardIds);
            }
        }
        
        public void ResetState()
        {
            currentState = new PlayerState
            {
                health = 30,
                maxHealth = 30,
                gold = 100,
                cardIds = new List<string> { "Strike", "Defend", "Strike", "Defend", "Strike" }
            };
            
            if (playerDeck == null)
            {
                playerDeck = new PlayerDeck();
            }
            else
            {
                playerDeck.cardIds = new List<string>(currentState.cardIds);
                playerDeck.currentSize = currentState.cardIds.Count;
            }
        }
    }
}
