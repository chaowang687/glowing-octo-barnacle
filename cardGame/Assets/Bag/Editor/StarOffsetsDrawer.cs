using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Bag;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    // 星星槽位编辑器的相关参数
    private const float CellSize = 20f;
    private const float CellSpacing = 2f;
    private const int ExtraRange = 2; // 形状周围额外的格子范围
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 绘制基本信息
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        
        // 绘制形状配置（使用默认的ShapeDataDrawer）
        EditorGUILayout.PropertyField(serializedObject.FindProperty("shapeData"));
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("worldPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effects"), true);
        
        // 绘制星星槽位配置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("星星槽位配置", EditorStyles.boldLabel);
        
        // 获取相关属性
        SerializedProperty starOffsetsProp = serializedObject.FindProperty("starOffsets");
        SerializedProperty shapeDataProp = serializedObject.FindProperty("shapeData");
        SerializedProperty widthProp = shapeDataProp.FindPropertyRelative("width");
        SerializedProperty heightProp = shapeDataProp.FindPropertyRelative("height");
        SerializedProperty shapeArrayProp = shapeDataProp.FindPropertyRelative("shapeArray");
        
        // 绘制可视化配置界面
        DrawStarConfiguration(starOffsetsProp, widthProp.intValue, heightProp.intValue, shapeArrayProp);
        
        // 提供一个重置按钮
        if (GUILayout.Button("清空所有星星槽位"))
        {
            starOffsetsProp.arraySize = 0;
        }
        
        // 绘制标签配置
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("标签配置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawStarConfiguration(SerializedProperty starOffsetsProp, int shapeWidth, int shapeHeight, SerializedProperty shapeArrayProp)
    {
        // 计算总网格大小
        int totalWidth = shapeWidth + ExtraRange * 2;
        int totalHeight = shapeHeight + ExtraRange * 2;
        float gridHeight = (CellSize + CellSpacing) * totalHeight;
        
        // 创建一个矩形区域用于绘制网格
        Rect gridRect = EditorGUILayout.GetControlRect(false, gridHeight);
        
        // 绘制背景
        EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        
        // 绘制网格线
        for (int i = 0; i <= totalWidth; i++)
        {
            float x = gridRect.x + i * (CellSize + CellSpacing);
            EditorGUI.DrawRect(new Rect(x, gridRect.y, CellSpacing, gridRect.height), new Color(0.3f, 0.3f, 0.3f, 1f));
        }
        for (int i = 0; i <= totalHeight; i++)
        {
            float y = gridRect.y + i * (CellSize + CellSpacing);
            EditorGUI.DrawRect(new Rect(gridRect.x, y, gridRect.width, CellSpacing), new Color(0.3f, 0.3f, 0.3f, 1f));
        }
        
        // 获取物品的实际形状
        bool[,] shape = GetShapeFromProperty(shapeArrayProp);
        
        // 绘制现有星星槽位
        Dictionary<Vector2Int, int> starPositions = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < starOffsetsProp.arraySize; i++)
        {
            SerializedProperty elementProp = starOffsetsProp.GetArrayElementAtIndex(i);
            Vector2Int pos = new Vector2Int(
                elementProp.FindPropertyRelative("x").intValue,
                elementProp.FindPropertyRelative("y").intValue
            );
            starPositions[pos] = i;
        }
        
        // 绘制形状和星星槽位
        for (int y = -ExtraRange; y < shapeHeight + ExtraRange; y++)
        {
            for (int x = -ExtraRange; x < shapeWidth + ExtraRange; x++)
            {
                // 计算单元格位置
                int gridX = ExtraRange + x;
                int gridY = ExtraRange + y;
                float cellX = gridRect.x + gridX * (CellSize + CellSpacing) + CellSpacing;
                float cellY = gridRect.y + gridY * (CellSize + CellSpacing) + CellSpacing;
                Rect cellRect = new Rect(cellX, cellY, CellSize, CellSize);
                
                // 绘制单元格
                Color cellColor;
                Vector2Int pos = new Vector2Int(x, y);
                
                if (IsInShape(x, y, shapeWidth, shapeHeight))
                {
                    // 形状内部或边缘
                    if (IsShapeOccupied(x, y, shape))
                    {
                        // 形状占用的格子（绿色）
                        cellColor = Color.green;
                    }
                    else
                    {
                        // 形状范围内但未占用的格子（灰色）
                        cellColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
                else
                {
                    // 形状外部的格子
                    cellColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                }
                
                // 如果有星星槽位，覆盖为黄色
                if (starPositions.ContainsKey(pos))
                {
                    cellColor = Color.yellow;
                }
                
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // 处理点击事件（允许在任何位置添加/移除星星槽位）
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (starPositions.ContainsKey(pos))
                    {
                        // 移除现有星星槽位
                        int index = starPositions[pos];
                        starOffsetsProp.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        // 添加新星星槽位
                        starOffsetsProp.InsertArrayElementAtIndex(starOffsetsProp.arraySize);
                        SerializedProperty newElement = starOffsetsProp.GetArrayElementAtIndex(starOffsetsProp.arraySize - 1);
                        newElement.FindPropertyRelative("x").intValue = x;
                        newElement.FindPropertyRelative("y").intValue = y;
                    }
                    
                    Event.current.Use();
                    // 重新绘制GUI
                    Repaint();
                }
            }
        }
        
        // 绘制图例和提示
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        // 绘制图例
        GUILayout.BeginVertical();
        GUILayout.Label("图例：");
        GUILayout.BeginHorizontal();
        DrawColorLegend(Color.green, "物品形状");
        DrawColorLegend(Color.yellow, "星星槽位");
        DrawColorLegend(new Color(0.3f, 0.3f, 0.3f, 1f), "灰色可配置区域");
        DrawColorLegend(new Color(0.15f, 0.15f, 0.15f, 1f), "深灰色可配置区域");
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        
        GUILayout.Label("提示：在任何格子中点击来添加/移除星星槽位，包括灰色区域", EditorStyles.helpBox);
    }
    
    // 绘制颜色图例
    private void DrawColorLegend(Color color, string label)
    {
        GUILayout.BeginHorizontal();
        // 绘制颜色方块
        Rect colorRect = GUILayoutUtility.GetRect(16, 16, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(colorRect, color);
        // 绘制标签
        GUILayout.Label(label);
        GUILayout.EndHorizontal();
    }
    
    // 检查坐标是否在形状范围内
    private bool IsInShape(int x, int y, int shapeWidth, int shapeHeight)
    {
        return x >= 0 && x < shapeWidth && y >= 0 && y < shapeHeight;
    }
    
    // 检查形状中该位置是否被占用
    private bool IsShapeOccupied(int x, int y, bool[,] shape)
    {
        if (x < 0 || x >= 5 || y < 0 || y >= 5)
            return false;
        return shape[x, y];
    }
    
    // 从属性中获取形状数据
    private bool[,] GetShapeFromProperty(SerializedProperty shapeArrayProp)
    {
        bool[,] shape = new bool[5, 5];
        
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int index = y * 5 + x;
                if (index < shapeArrayProp.arraySize)
                {
                    shape[x, y] = shapeArrayProp.GetArrayElementAtIndex(index).boolValue;
                }
            }
        }
        
        return shape;
    }
    

}