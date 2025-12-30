using UnityEngine; // 必须添加这一行
using System.Collections.Generic; // 只有用到 Dictionary 或 List 的脚本才需要这行
public class DigInputHandler : MonoBehaviour {
    public GridManager gridManager;
    public Camera mainCamera;

    void Update() {
    if (Input.GetMouseButtonDown(0)) {
        // 修正：确保射线能射到 Z=0 的平面
        Vector3 clickPoint = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(mainCamera.transform.position.z)));
        
        // 反向计算 Grid 坐标：
        // WorldX = StartX + GridX + 0.5f  =>  GridX = WorldX - StartX - 0.5f
        // WorldY = StartY - GridY - 0.5f  =>  GridY = StartY - WorldY - 0.5f
        
        // 我们需要获取 GridManager 里的 startX 和 startY。
        // 为了简单，直接用 GridManager 的公有属性重新算一遍，或者让 GridManager 提供一个 WorldToGrid 的方法。
        // 这里采用让 GridManager 提供转换方法的方式（更稳健）。
        
        Vector2Int gridPos = gridManager.WorldToGridPosition(clickPoint);
        
        gridManager.Dig(gridPos, 1);
    }
}
}
