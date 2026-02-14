using UnityEngine;
using UnityEditor;

public class WorldTileEditor : MonoBehaviour
{
    public GameObject[] decoPrefabs;
    public int selectedBrushIndex = 0;

    public void PaintDeco(Vector3 worldPos, Transform segmentTransform)
    {
        if (decoPrefabs == null || decoPrefabs.Length <= selectedBrushIndex) return;
        Transform container = segmentTransform.Find("DecoContainer");
        if (container == null) return;

        GameObject prefab = decoPrefabs[selectedBrushIndex];
        GameObject newDeco = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
        
        newDeco.transform.position = worldPos;
        Vector3 localPos = newDeco.transform.localPosition;
        localPos.y = 0; 
        newDeco.transform.localPosition = localPos;
        newDeco.transform.localRotation = Quaternion.identity;

        Undo.RegisterCreatedObjectUndo(newDeco, "Paint Deco");
    }

    public void EraseDeco(GameObject target)
    {
        // 判定点击的是否是装饰物（检查其父物体或组件）
        if (target != null && (target.transform.parent != null && target.transform.parent.name == "DecoContainer"))
        {
            Undo.DestroyObjectImmediate(target);
        }
    }
    
    // 【新增】：选中物体以便手动移动
    public void SelectDeco(GameObject target)
    {
        if (target != null && target.transform.parent != null && target.transform.parent.name == "DecoContainer")
        {
            Selection.activeGameObject = target; // 直接在场景中选中它，利用 Unity 原生坐标轴移动
        }
    }
}