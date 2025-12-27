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
            
            // 同步到GameDataManager
            if (GameDataManager.Instance != null)
            {
                if (victory)
                {
                    // 添加奖励
                    GameDataManager.Instance.AddGold(gold);
                    if (!string.IsNullOrEmpty(card))
                        GameDataManager.Instance.AddCard(card);
                    if (!string.IsNullOrEmpty(relic))
                        GameDataManager.Instance.AddRelic(relic);
                    
                    // 标记节点完成
                    if (!string.IsNullOrEmpty(battleNodeId))
                        GameDataManager.Instance.CompleteNode(battleNodeId);
                }
                else
                {
                    // 失败惩罚：减少生命值
                    int damage = Mathf.RoundToInt(GameDataManager.Instance.MaxHealth * 0.2f);
                    GameDataManager.Instance.TakeDamage(damage);
                }
                
                // 保存数据
                GameDataManager.Instance.SaveGameData();
                GameDataManager.Instance.ClearBattleData();
            }
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