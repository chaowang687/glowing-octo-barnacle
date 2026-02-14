using UnityEngine;
using UnityEditor;

public class BatchMaterialReplacer : EditorWindow
{
    public Material targetMaterial;

    [MenuItem("Tools/批量替换材质")]
    public static void ShowWindow() => GetWindow<BatchMaterialReplacer>("材质替换器");

    void OnGUI()
    {
        targetMaterial = (Material)EditorGUILayout.ObjectField("目标材质", targetMaterial, typeof(Material), false);

        if (GUILayout.Button("替换选中物体及其子物体的材质"))
        {
            if (targetMaterial == null) return;
            
            foreach (GameObject go in Selection.gameObjects)
            {
                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = targetMaterial;
                }
            }
            Debug.Log("替换完成！");
        }
    }
}