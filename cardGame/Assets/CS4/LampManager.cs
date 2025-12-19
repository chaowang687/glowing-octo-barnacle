using UnityEngine;
using System.Collections.Generic;

public class LampManager : MonoBehaviour
{
    [Header("灯光管理")]
    public List<PointLight2D> controlledLights = new List<PointLight2D>();

    /// <summary>
    /// 直接切换物体的激活状态来实现开关
    /// </summary>
    public void SwitchAllLights(bool state)
    {
        // 清理可能被删除的引用
        controlledLights.RemoveAll(item => item == null);

        foreach (var light in controlledLights)
        {
            if (light != null)
            {
                // 直接控制整个 GameObject 的显示与隐藏
                // 这样绝对不会出现“关不掉”的情况
                light.gameObject.SetActive(state);
            }
        }
    }

    [ContextMenu("抓取场景中所有灯")]
    private void GetAllLightsInScene()
    {
        controlledLights.Clear();
        // 即使物体被隐藏了(Inactive)，也可以通过这个方法找回来
        controlledLights.AddRange(FindObjectsOfType<PointLight2D>(true));
    }
}