using UnityEngine;

public class PlayerRunner : MonoBehaviour
{
   
    public InfiniteCarouselController carousel;
    public float bounceHeight = 0.2f; // 跑步时上下颠簸的幅度
    public float bounceSpeed = 12f;  // 颠簸频率
    
    private Vector3 _initialPos;

    void Start() => _initialPos = transform.localPosition;

    void Update()
    {
        if (carousel.IsMoving)
        {
            // 模拟原地跑步的跳动感
            float yOffset = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceHeight;
            transform.localPosition = _initialPos + Vector3.up * yOffset;
            
            // 如果你有 Animator，可以在这里设置：
            // animator.SetBool("isMoving", true);
        }
        else
        {
            // 回归初始位置
            transform.localPosition = Vector3.Lerp(transform.localPosition, _initialPos, Time.deltaTime * 5f);
            // animator.SetBool("isMoving", false);
        }
    }
    
}