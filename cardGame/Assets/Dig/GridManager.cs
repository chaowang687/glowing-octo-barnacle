using System.Collections.Generic;
using UnityEngine;
// 如果你的其他脚本在这个命名空间下，请保留；否则可以删除
 using ScavengingGame; 

public class GridManager : MonoBehaviour {
    [Header("地图设置")]
    public int width = 10;
    public int height = 15;
    public GameObject tilePrefab; // 基础方块预制体
    public TileData defaultTile;  // 默认泥土数据

    [Header("整体震动设置")]
    [Tooltip("整体震动强度")]
    public float gridShakeMagnitude = 0.05f;
    [Tooltip("整体震动持续时间")]
    public float gridShakeDuration = 0.05f;

    [Header("测试配置")]
    public FossilData testFossil; // 在编辑器里把创建好的化石资源拖进来

    private Dictionary<Vector2Int, TileState> gridData = new Dictionary<Vector2Int, TileState>();
    private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>();
    
    // 存储当前地图上所有宝藏的进度实例
    private List<TreasureInstance> activeTreasures = new List<TreasureInstance>();
    [Header("引用设置")]
    public GameObject treasureVisualPrefab; // 拖入上面的预制体

    // 记录当前场景中的视觉物体，方便后续点亮
    private Dictionary<string, TreasureVisual> treasureVisuals = new Dictionary<string, TreasureVisual>();

    void Start() {
        GenerateLevel();

        // 优先使用新的旋转埋宝逻辑
        if (testFossil != null) {
            PlantFossil(testFossil);
        } else {
            // 如果没有配置 FossilData，则回退到旧的矩形埋宝逻辑做测试
            PlantTreasures("Simple_Box_Fossil", 2, 2);
        }
    }

    // 1. 生成关卡基础地块
    void GenerateLevel() {
        // 计算居中偏移量
        // 地图总宽度 = width * 1.0f (假设每个格子大小为1)
        // 地图总高度 = height * 1.0f
        // 左上角起始点应该在 (-width/2, height/2)
        // 格子 (x, y) 的坐标公式：
        // PosX = StartX + x + 0.5f (如果pivot在中心)
        // PosY = StartY - y - 0.5f
        
        float startX = -width / 2f;
        float startY = height / 2f;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector2Int pos = new Vector2Int(x, y);
                TileState state = new TileState {
                    position = pos,
                    data = defaultTile,
                    currentHealth = defaultTile.maxHealth,
                    isRevealed = false,
                    treasureId = null
                };
                gridData[pos] = state;
                CreateTileObject(pos, state, startX, startY);
            }
        }
    }

    // 辅助：获取世界坐标
    public Vector3 GetWorldPosition(Vector2Int gridPos) {
        float startX = -width / 2f;
        float startY = height / 2f;
        // 假设格子中心对齐
        return new Vector3(startX + gridPos.x + 0.5f, startY - gridPos.y - 0.5f, 0);
    }
    
    // ... (PlantFossil 中也要修改父节点生成位置) ...


        // --- 新逻辑：支持旋转和不规则形状的埋宝 ---
      // --- 新逻辑：支持旋转、父子节点对齐与中心动画 ---
    public void PlantFossil(FossilData data) {
        if (data == null) {
            Debug.LogError("PlantFossil: 传入的 FossilData 为空！");
            return;
        }

        // 尝试多次寻找合法位置，避免因随机旋转导致无法放置
        int maxAttempts = 10;
        bool placedSuccessfully = false;

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            // 1. 随机旋转
            int rotationSteps = Random.Range(0, 4);

            // 2. 动态计算安全边界，防止化石越界
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var offset in data.shapeOffsets) {
                Vector2Int rotated = RotateOffset(offset, rotationSteps);
                minX = Mathf.Min(minX, rotated.x);
                maxX = Mathf.Max(maxX, rotated.x);
                minY = Mathf.Min(minY, rotated.y);
                maxY = Mathf.Max(maxY, rotated.y);
            }

            // 计算合法的锚点范围（保留1格边距）
            // 目标：finalPos 在 [1, width-2] 范围内
            // anchor + minOffset >= 1       => anchor >= 1 - minOffset
            // anchor + maxOffset <= width-2 => anchor <= width - 2 - maxOffset
            int startX = 1 - minX;
            int endX = width - 2 - maxX;
            int startY = 1 - minY;
            int endY = height - 2 - maxY;

            // 检查是否有有效空间
            if (startX > endX || startY > endY) {
                // 当前旋转角度放不下，尝试下一次
                continue;
            }

            // 生成锚点 (Random.Range int版是左闭右开，所以max要+1)
            Vector2Int anchor = new Vector2Int(
                Random.Range(startX, endX + 1), 
                Random.Range(startY, endY + 1)
            );

            Debug.Log($"<color=yellow>化石生成中</color>: {data.fossilName} 锚点={anchor} 旋转步数={rotationSteps}");

            // 3. 实例化视觉物体
            GameObject tgo = Instantiate(treasureVisualPrefab, transform);
            TreasureVisual visual = tgo.GetComponent<TreasureVisual>();
            
            // 关键对齐修改：将父节点置于锚点格子的【中心】(现在是世界坐标居中后的位置)
            Vector3 worldPos = GetWorldPosition(anchor);
            visual.SetData(data.fossilSprite, worldPos, rotationSteps);
            
            // 重要：生成唯一 ID，防止同名化石导致进度统计混淆
            string baseId = string.IsNullOrEmpty(data.fossilName) ? data.name : data.fossilName;
            string tid = $"{baseId}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            
            treasureVisuals[tid] = visual;

            // 4. 创建进度实例并绑定格子
            TreasureInstance newTreasure = new TreasureInstance {
                id = tid,
                reward = data.rewardItem
            };

            int partsPlaced = 0;
            foreach (var originalOffset in data.shapeOffsets) {
                Vector2Int rotatedOffset = RotateOffset(originalOffset, rotationSteps);
                Vector2Int finalPos = anchor + rotatedOffset;

                // 双重保险：再次检查格子是否存在（理论上上面的计算已经保证了）
                if (gridData.ContainsKey(finalPos)) {
                    gridData[finalPos].treasureId = tid;
                    newTreasure.totalParts.Add(finalPos);
                    partsPlaced++;
                } else {
                    Debug.LogError($"[Critical] 计算出的位置 {finalPos} 依然越界！请检查边界算法。");
                }
            }

            activeTreasures.Add(newTreasure);
            Debug.Log($"<color=green>化石部署成功</color>: ID={tid} 锚点={anchor} 占据格子数={partsPlaced}");
            
            placedSuccessfully = true;
            break; // 成功放置，跳出循环
        }

        if (!placedSuccessfully) {
            Debug.LogError($"无法为化石 {data.fossilName} 找到合法的放置位置（尝试了 {maxAttempts} 次）。可能是化石太大或地图太小。");
        }
    }


    private Vector2Int RotateOffset(Vector2Int offset, int steps) {
        for (int i = 0; i < steps; i++) {
            // 顺时针旋转 90 度 (Unity Grid Y向下): [x, y] -> [-y, x]
            // (1,0) 右 -> (0,1) 下
            // (0,1) 下 -> (-1,0) 左
            offset = new Vector2Int(-offset.y, offset.x);
        }
        return offset;
    }

    // --- 旧逻辑：简单的矩形埋宝 ---
    public void PlantTreasures(string treasureId, int sizeX, int sizeY) {
        int startX = Random.Range(0, width - sizeX);
        int startY = Random.Range(0, height - sizeY);

        TreasureInstance newTreasure = new TreasureInstance { id = treasureId };

        for (int i = 0; i < sizeX; i++) {
            for (int j = 0; j < sizeY; j++) {
                Vector2Int pos = new Vector2Int(startX + i, startY + j);
                if (gridData.ContainsKey(pos)) {
                    gridData[pos].treasureId = treasureId;
                    newTreasure.totalParts.Add(pos);
                }
            }
        }
        activeTreasures.Add(newTreasure);
    }

    // 2. 挖掘逻辑接口
    public void Dig(Vector2Int pos, int damage) {
        if (!gridData.ContainsKey(pos)) return;

        TileState state = gridData[pos];
        if (state.isRevealed) return;

        state.currentHealth -= damage;
        UpdateTileVisual(pos, state);
        
        // 触发整体震动
        TriggerGridShake();

        if (state.currentHealth <= 0) {
            RevealTile(pos, state);
        }
    }
    
    // --- 整体震动逻辑 ---
    private Vector3 originalGridPos;
    private bool isShaking = false;

    private void TriggerGridShake() {
        if (!isShaking) {
            originalGridPos = transform.position;
            StartCoroutine(GridShakeCoroutine());
        }
    }

    System.Collections.IEnumerator GridShakeCoroutine() {
        isShaking = true;
        float elapsed = 0f;
        
        while (elapsed < gridShakeDuration) {
            transform.position = originalGridPos + (Vector3)Random.insideUnitCircle * gridShakeMagnitude;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalGridPos;
        isShaking = false;
    }

    void RevealTile(Vector2Int pos, TileState state) {
        state.isRevealed = true;
        
        if (state.data.breakEffect) 
            Instantiate(state.data.breakEffect, tileObjects[pos].transform.position, Quaternion.identity);
        
        tileObjects[pos].SetActive(false);

        if (!string.IsNullOrEmpty(state.treasureId)) {
            OnTileBroken(pos, state.treasureId);
        }
    }

        // GridManager.cs 中的 OnTileBroken 方法
        void OnTileBroken(Vector2Int pos, string treasureId) {
            TreasureInstance treasure = activeTreasures.Find(t => t.id == treasureId);
            if (treasure != null) {
                treasure.revealedCount++;
                // 添加更多调试信息，确保引用的是同一个对象
                Debug.Log($"化石进度更新: ID={treasureId} 进度={treasure.revealedCount}/{treasure.totalParts.Count} (ObjHash={treasure.GetHashCode()})");

                if (treasure.IsComplete) {
                    if(treasureVisuals.ContainsKey(treasureId)) {
                        treasureVisuals[treasureId].OnComplete();
                        Debug.Log($"<color=cyan>成功点亮化石: {treasureId}</color>");
                    } else {
                        Debug.LogWarning($"找不到化石 {treasureId} 的视觉引用！");
                    }
                }
            } else {
                Debug.LogWarning($"挖掘到了 ID 为 {treasureId} 的碎片，但找不到对应的化石实例！ActiveTreasures Count: {activeTreasures.Count}");
            }
        }

    void CreateTileObject(Vector2Int pos, TileState state, float startX, float startY) {
        // 使用传入的 startX/Y 计算世界坐标，实现地图居中
        Vector3 worldPos = new Vector3(startX + pos.x + 0.5f, startY - pos.y - 0.5f, 0);
        
        GameObject go = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
        
        // 随机选择贴图逻辑
        Sprite chosenSprite = state.data.defaultSprite;
        if (state.data.randomSprites != null && state.data.randomSprites.Length > 0) {
            chosenSprite = state.data.randomSprites[Random.Range(0, state.data.randomSprites.Length)];
        }
        
        go.GetComponent<SpriteRenderer>().sprite = chosenSprite;
        tileObjects[pos] = go;
    }

    void UpdateTileVisual(Vector2Int pos, TileState state) {
        // 检查这个位置是否有对应的地块物体
        if (tileObjects.TryGetValue(pos, out GameObject go)) {
            
            // 1. 缩放反馈（受伤时缩放一下）
            go.transform.localScale = Vector3.one * 0.8f;
            
            // 2. 调用 TileView 脚本执行震动或颜色变化（如果有的话）
            float hpPercent = (float)state.currentHealth / state.data.maxHealth;
            if (go.TryGetComponent<TileView>(out TileView view)) {
                view.OnHit(hpPercent);
            }

            // 3. 切换裂纹贴图逻辑
            if (state.data.crackSprites != null && state.data.crackSprites.Length > 0) {
                float healthPercent = (float)state.currentHealth / state.data.maxHealth;
                int crackIndex = Mathf.FloorToInt((1f - healthPercent) * (state.data.crackSprites.Length - 1));
                crackIndex = Mathf.Clamp(crackIndex, 0, state.data.crackSprites.Length - 1);
                
                go.GetComponent<SpriteRenderer>().sprite = state.data.crackSprites[crackIndex];
            }
        }
    }

    // 辅助：世界坐标转网格坐标
    public Vector2Int WorldToGridPosition(Vector3 worldPos) {
        float startX = -width / 2f;
        float startY = height / 2f;
        
        int x = Mathf.FloorToInt(worldPos.x - startX);
        int y = Mathf.FloorToInt(startY - worldPos.y);
        
        return new Vector2Int(x, y);
    }
}

[System.Serializable]
public class TreasureInstance 
{
    public string id;
    
    // --- 确保这一行存在！ ---
    // 如果你已经创建了 ItemData.cs，就用下面这行：
    public ItemData reward; 
    
    // 如果你还没准备好 ItemData，可以先用这行代替来消除报错：
    // public string rewardName; 

    public List<Vector2Int> totalParts = new List<Vector2Int>();
    public int revealedCount = 0;
    public bool IsComplete => revealedCount >= totalParts.Count;
}