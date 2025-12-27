// BattleDataManager.cs - 专门处理战斗数据
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlayTheSpireMap
{
    public class BattleDataManager : MonoBehaviour
    {
        public static BattleDataManager Instance { get; private set; }
        
        [Header("当前战斗数据")]
        public string battleNodeId;
        public EncounterData battleEncounter;
        
        [Header("战斗结果")]
        public bool isVictory;
        public int goldEarned;
        public string cardEarned;
        public string relicEarned;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadBattleData();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void LoadBattleData()
        {
            // 从GameDataManager加载数据
            if (GameDataManager.Instance != null)
            {
                battleNodeId = GameDataManager.Instance.battleNodeId;
                battleEncounter = GameDataManager.Instance.battleEncounterData;
            }
        }
        
    public void SaveBattleResult(bool victory, int gold = 0, string card = "", string relic = "")
{
    isVictory = victory;
    goldEarned = gold;
    cardEarned = card;
    relicEarned = relic;

    if (GameDataManager.Instance == null) return;

    // 1. 同步英雄当前的生命值（带回地图）
    if (BattleManager.Instance != null && BattleManager.Instance.characterManager != null)
    {
        var hero = BattleManager.Instance.characterManager.GetActiveHero();
        if (hero != null)
        {
            // 只有活着或者胜利时同步真实血量，战败通常有特殊处理
            GameDataManager.Instance.Health = hero.currentHp;
        }
    }

    // 2. 处理战斗结果
    if (victory)
    {
        // 胜利奖励
        GameDataManager.Instance.AddGold(gold);
        if (!string.IsNullOrEmpty(card))
            GameDataManager.Instance.AddCard(card); // 确保这是 CardID
        if (!string.IsNullOrEmpty(relic))
            GameDataManager.Instance.AddRelic(relic);
        
        // 标记地图节点完成并解锁后续
        if (!string.IsNullOrEmpty(battleNodeId))
            GameDataManager.Instance.CompleteNode(battleNodeId);
    }
    else
    {
        // 战败：可以根据需求选择是扣百分比，还是强制回 1 点血防止暴死
        int penaltyDamage = Mathf.RoundToInt(GameDataManager.Instance.MaxHealth * 0.2f);
        GameDataManager.Instance.TakeDamage(penaltyDamage);
        
        // 如果扣完血小于等于0，通常在这里触发游戏结束/回到主菜单逻辑
    }
    
    // 3. 统一存档并清理临时战斗数据
    GameDataManager.Instance.SaveGameData();
    GameDataManager.Instance.ClearBattleData();
}
        
        public void ReturnToMap()
        {
            // 确保数据已保存
            if (GameDataManager.Instance != null)
                GameDataManager.Instance.SaveGameData();
            
            // 跳转到地图场景
            SceneManager.LoadScene("MapScene");
        }
    }
}