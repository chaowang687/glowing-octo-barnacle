using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Bag;

[CustomPropertyDrawer(typeof(ShapeData))]
public class ShapeDataDrawer : PropertyDrawer
{
    // 单元格大小
    private const float CellSize = 20f;
    // 单元格间距
    private const float CellSpacing = 2f;
    // 最大尺寸
    private const int MaxSize = 5;
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float headerHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // 计算形状配置的高度
        float shapeConfigHeight = (CellSize + CellSpacing) * MaxSize + spacing * 2;
        
        return headerHeight * 3 + spacing * 4 + shapeConfigHeight;
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        // 获取属性
        SerializedProperty widthProp = property.FindPropertyRelative("width");
        SerializedProperty heightProp = property.FindPropertyRelative("height");
        SerializedProperty shapeArrayProp = property.FindPropertyRelative("shapeArray");
        
        // 配置矩形
        Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect widthRect = new Rect(position.x, headerRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
        Rect heightRect = new Rect(position.x + position.width / 2 + 5, widthRect.y, position.width / 2 - 5, EditorGUIUtility.singleLineHeight);
        Rect shapeLabelRect = new Rect(position.x, heightRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
        Rect shapeRect = new Rect(position.x, shapeLabelRect.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, (CellSize + CellSpacing) * MaxSize);
        
        // 绘制标题
        EditorGUI.LabelField(headerRect, label);
        
        // 绘制尺寸配置
        EditorGUI.LabelField(widthRect, "Width");
        widthProp.intValue = EditorGUI.IntSlider(new Rect(widthRect.x + 50, widthRect.y, widthRect.width - 50, widthRect.height), widthProp.intValue, 1, MaxSize);
        
        EditorGUI.LabelField(heightRect, "Height");
        heightProp.intValue = EditorGUI.IntSlider(new Rect(heightRect.x + 50, heightRect.y, heightRect.width - 50, heightRect.height), heightProp.intValue, 1, MaxSize);
        
        // 绘制形状标签
        EditorGUI.LabelField(shapeLabelRect, "Shape Configuration");
        
        // 绘制形状网格
        DrawShapeGrid(shapeRect, shapeArrayProp, widthProp.intValue, heightProp.intValue);
        
        EditorGUI.EndProperty();
    }
    
    private void DrawShapeGrid(Rect rect, SerializedProperty shapeArrayProp, int width, int height)
    {
        // 绘制背景
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
        
        // 绘制网格线
        for (int i = 0; i <= MaxSize; i++)
        {
            // 水平线
            float y = rect.y + i * (CellSize + CellSpacing);
            EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, CellSpacing), new Color(0.3f, 0.3f, 0.3f, 1f));
            
            // 垂直线
            float x = rect.x + i * (CellSize + CellSpacing);
            EditorGUI.DrawRect(new Rect(x, rect.y, CellSpacing, rect.height), new Color(0.3f, 0.3f, 0.3f, 1f));
        }
        
        // 绘制单元格
        for (int y = 0; y < MaxSize; y++)
        {
            for (int x = 0; x < MaxSize; x++)
            {
                // 计算单元格位置
                float cellX = rect.x + x * (CellSize + CellSpacing) + CellSpacing;
                float cellY = rect.y + y * (CellSize + CellSpacing) + CellSpacing;
                Rect cellRect = new Rect(cellX, cellY, CellSize, CellSize);
                
                // 计算数组索引
                int index = y * MaxSize + x;
                SerializedProperty elementProp = shapeArrayProp.GetArrayElementAtIndex(index);
                
                // 绘制单元格
                Color cellColor;
                if (x < width && y < height)
                {
                    // 有效区域
                    cellColor = elementProp.boolValue ? Color.green : new Color(0.3f, 0.3f, 0.3f, 1f);
                }
                else
                {
                    // 无效区域
                    cellColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                }
                
                EditorGUI.DrawRect(cellRect, cellColor);
                
                // 处理点击事件
                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (x < width && y < height)
                    {
                        elementProp.boolValue = !elementProp.boolValue;
                        elementProp.serializedObject.ApplyModifiedProperties();
                        Event.current.Use();
                    }
                }
            }
        }
        
        // 添加可视化指南
        Rect guideRect = new Rect(rect.x, rect.yMax + 5, rect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(guideRect, "提示：点击格子来切换占用状态 (绿色=占用，灰色=未占用)", EditorStyles.helpBox);
    }
}
