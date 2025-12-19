using UnityEngine;

[ExecuteInEditMode] // 允许在编辑器模式下预览效果
public class FollowCamera : MonoBehaviour 
{
    [Header("Main Settings")]
    public Camera mainCam; // 拖入场景中的主摄像机
    private Camera myCam;

    void Start() 
    {
        myCam = GetComponent<Camera>();
        
        // 确保光影相机的基础设置与主相机一致
        if (mainCam != null && myCam != null)
        {
            myCam.orthographic = mainCam.orthographic;
            myCam.farClipPlane = mainCam.farClipPlane;
            myCam.nearClipPlane = mainCam.nearClipPlane;
        }
    }

    // 使用 LateUpdate 确保在主相机移动后再同步坐标
    void LateUpdate() 
    {
        if (mainCam == null || myCam == null) return;

        // 1. 同步位置和层级属性
        myCam.transform.position = mainCam.transform.position;
        myCam.transform.rotation = mainCam.transform.rotation;
        myCam.orthographicSize = mainCam.orthographicSize;
        myCam.aspect = mainCam.aspect; // 关键：确保纵横比一致，防止光圈拉伸

        // 2. 确保目标 Render Texture 存在
        if (myCam.targetTexture != null)
        {
            // 将当前相机渲染的 RT 传递给所有使用 _GlobalLightMap 变量的 Shader
            Shader.SetGlobalTexture("_GlobalLightMap", myCam.targetTexture);
        }
    }
}