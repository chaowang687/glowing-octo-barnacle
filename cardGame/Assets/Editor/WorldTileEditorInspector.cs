using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldTileEditor))]
public class WorldTileEditorInspector : Editor
{
    bool isEditMode = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        WorldTileEditor script = (WorldTileEditor)target;
        
        GUILayout.Space(10);
        isEditMode = GUILayout.Toggle(isEditMode, "开启瓦片编辑模式", "Button", GUILayout.Height(30));
        
        if (isEditMode)
        {
            GUILayout.Label("操作说明：\n- [Shift + 左键]：增加物体\n- [Alt/Option + 左键]：删除物体\n- [普通左键]：选中物体进行移动", EditorStyles.helpBox);
        }
    }

    private void OnSceneGUI()
    {
        if (!isEditMode) return;

        WorldTileEditor script = (WorldTileEditor)target;
        Event currentEvent = Event.current; // 将变量名改为 currentEvent，避免冲突

        // 屏蔽 Scene 窗口原有的点击选中功能
        if (currentEvent.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        // 射线检测
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            // 绘制预览框
            Handles.color = currentEvent.shift ? Color.green : (currentEvent.alt ? Color.red : Color.cyan);
            Handles.DrawWireDisc(hit.point, Vector3.forward, 0.3f);

            // 监听鼠标点击
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                // 1. Shift + 左键：增加
                if (currentEvent.shift)
                {
                    script.PaintDeco(hit.point, hit.collider.transform);
                    currentEvent.Use();
                }
                // 2. Alt (Option) + 左键：删除
                else if (currentEvent.alt)
                {
                    script.EraseDeco(hit.collider.gameObject);
                    currentEvent.Use();
                }
                // 3. 普通左键：选中物体（以便手动移动）
                else
                {
                    // 如果点中的是装饰物，则选中它
                    if (hit.collider.gameObject.transform.parent != null && 
                        hit.collider.gameObject.transform.parent.name == "DecoContainer")
                    {
                        Selection.activeGameObject = hit.collider.gameObject;
                        // 选中后不执行 currentEvent.Use()，这样 Unity 自带的移动轴会出现
                    }
                }
            }
        }
        
        // 强制刷新 Scene 视图，让预览圆圈跟随鼠标平滑移动
        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }
}