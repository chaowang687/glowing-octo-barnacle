using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 这个静态变量就是给 CharacterSelectionManager 用的
    public static CharacterBase PlayerStartData;

    private void Awake()
    {
        // 保证这个对象切换场景时不销毁，数据才能带进战斗
        DontDestroyOnLoad(gameObject);
    }
}