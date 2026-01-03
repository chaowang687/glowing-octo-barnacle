using UnityEngine;

namespace Bag
{
    [CreateAssetMenu(fileName = "EnergyEffect", menuName = "Bag/Effects/EnergyOnTurnStart")]
    public class EnergyOnTurnStartEffect : ScriptableObject, IItemEffect
    {
        public int energyAmount = 1;

        public void OnTurnStart(object cardSystemObj)
        {
            // 使用反射调用AddEnergy方法，避免命名空间冲突
            if (cardSystemObj != null)
            {
                // 获取AddEnergy方法
                System.Reflection.MethodInfo addEnergyMethod = cardSystemObj.GetType().GetMethod("AddEnergy");
                if (addEnergyMethod != null)
                {
                    // 调用AddEnergy方法
                    addEnergyMethod.Invoke(cardSystemObj, new object[] { energyAmount });
                    Debug.Log($"<color=cyan>[遗物效果]</color> 回合开始，增加能量: {energyAmount}");
                }
            }
        }
    }
}