// RandomEventManager.cs
using UnityEngine;

namespace ScavengingGame
{
    public static class RandomEventManager
    {
        // 事件触发概率（由ScavengingController初始化时注入）
        public static float GlobalEventChance { get; set; } = 0.1f;

        /// <summary>
        /// 检查并执行随机事件
        /// </summary>
        /// <returns>是否触发了事件</returns>
        public static bool TryTriggerRandomEvent()
        {
            if (Random.value < GlobalEventChance)
            {
                ExecuteRandomEvent();
                return true;
            }
            return false;
        }

        private static void ExecuteRandomEvent()
        {
            // 示例：简单的事件池。你可以扩展为从ScriptableObject列表读取复杂事件。
            int eventIndex = Random.Range(0, 3);
            switch (eventIndex)
            {
                case 0:
                    Debug.Log("[随机事件] 你发现了一口治疗泉水，回复了10点生命值。");
                    // GameStateManager.Instance.PlayerStats.Heal(10);
                    break;
                case 1:
                    Debug.Log("[随机事件] 你踩中了陷阱，受到5点伤害！");
                    // GameStateManager.Instance.PlayerStats.TakeDamage(5);
                    break;
                case 2:
                    Debug.Log("[随机事件] 一位神秘的商人短暂出现，但很快又消失了。");
                    break;
                default:
                    break;
            }
        }
    }
}