using UnityEngine;

namespace Bag
{
    [CreateAssetMenu(fileName = "EnergyEffect", menuName = "Bag/Effects/EnergyOnTurnStart")]
    public class EnergyOnTurnStartEffect : ScriptableObject, IItemEffect
    {
        public int energyAmount = 1;

        public void OnTurnStart()
        {
            // 这里替换成你战斗系统中增加能量的方法
            // 例如：BattleManager.Instance.AddEnergy(energyAmount);
            Debug.Log($"<color=cyan>[遗物效果]</color> 回合开始，增加能量: {energyAmount}");
        }
    }
}