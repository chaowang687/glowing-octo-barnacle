// PlayerStats.cs 或 GameManager.cs
using UnityEngine;

namespace ScavengingGame
{
    public class PlayerStats : MonoBehaviour
    {
        public int health = 100;
        public int maxHealth = 100;
        
        void Start()
        {
            // 订阅随机事件
            RandomEventManager.OnPlayerHealed += OnPlayerHealed;
            RandomEventManager.OnPlayerDamaged += OnPlayerDamaged;
            RandomEventManager.OnItemAdded += OnItemAdded;
        }
        
        void OnDestroy()
        {
            // 取消订阅
            RandomEventManager.OnPlayerHealed -= OnPlayerHealed;
            RandomEventManager.OnPlayerDamaged -= OnPlayerDamaged;
            RandomEventManager.OnItemAdded -= OnItemAdded;
        }
        
        private void OnPlayerHealed(int amount)
        {
            health = Mathf.Min(health + amount, maxHealth);
            Debug.Log($"生命值回复到: {health}/{maxHealth}");
        }
        
        private void OnPlayerDamaged(int amount)
        {
            health = Mathf.Max(health - amount, 0);
            Debug.Log($"生命值减少到: {health}/{maxHealth}");
            
            if (health <= 0)
            {
                Debug.Log("玩家死亡");
            }
        }
        
        private void OnItemAdded(string itemName, int amount)
        {
            Debug.Log($"获得了 {amount} 个 {itemName}");
            // 这里可以调用库存管理器添加物品
        }
    }
}