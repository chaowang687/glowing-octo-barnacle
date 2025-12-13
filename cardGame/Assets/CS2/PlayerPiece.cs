using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 玩家在地图上的棋子表现与移动控制。
/// </summary>
public class PlayerPiece : MonoBehaviour
{
    [Header("移动配置")]
    // 移动到下一个节点所需的时间 (秒)
    public float MoveDuration = 0.3f; 
    public Ease MoveEase = Ease.OutQuad;

    public bool IsMoving { get; private set; } = false;

    /// <summary>
    /// 协程：将玩家棋子从当前位置平滑移动到目标位置。
    /// </summary>
    public IEnumerator AnimateMoveTo(Vector3 targetPosition)
    {
        IsMoving = true;
        
        // 目标位置：在轴测图上，可能需要微调 Y 轴高度以确保视觉效果
        // 例如：稍微抬高 Z 轴或 Y 轴，避免棋子穿模
        Vector3 finalPos = targetPosition + Vector3.up * 0.1f; 

        // 使用 DOTween 进行平滑移动
        yield return transform.DOMove(finalPos, MoveDuration)
            .SetEase(MoveEase)
            .WaitForCompletion(); 
        
        IsMoving = false;
    }
}