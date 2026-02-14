using UnityEngine; // 必须添加这一行
// 地块配置：泥土、石头、硬金屑等
[CreateAssetMenu(fileName = "NewTileData", menuName = "DigGame/TileData")]
public class TileData : ScriptableObject {
    public string tileName;
    public int maxHealth; // 挖掘次数
    
    [Tooltip("单一默认贴图 (旧)")]
    public Sprite defaultSprite;
    
    [Tooltip("随机贴图池 (新)：生成时会从中随机选一张")]
    public Sprite[] randomSprites; 

    [Header("Auto-Tiling")]
    [Tooltip("边缘贴图数组 (需按 Bitmask 顺序排列 0-15)\n0:四周都有土, 15:四周都空\n顺序: 上(1) 右(2) 下(4) 左(8)")]
    public Sprite[] maskSprites;

    public Sprite[] crackSprites; // 随损坏程度改变的贴图
    public GameObject breakEffect; // 破碎粒子
}

// 存储地块运行时状态的类
public class TileState {
    public Vector2Int position;
    public TileData data;
    public int currentHealth;
    public string treasureId = null; // 如果藏有宝物，记录宝物ID
    public bool isRevealed = false;  // 是否已被挖开
}