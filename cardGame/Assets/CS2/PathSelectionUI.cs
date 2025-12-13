// PathSelectionUI.cs (新建)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PathSelectionUI : MonoBehaviour
{
    [Header("UI元素")]
    public GameObject panel;
    public Button buttonPrefab;
    public Transform buttonContainer;
    
    [Header("引用")]
    public MapGridManager mapGridManager;
    
    private List<IsometricMapNode> _availablePaths;
    private int _remainingSteps;
    
    void Start()
    {
        panel.SetActive(false);
    }
    
    /// <summary>
    /// 显示路径选择界面
    /// </summary>
    public void ShowPathSelection(List<IsometricMapNode> paths, int remainingSteps)
    {
        _availablePaths = paths;
        _remainingSteps = remainingSteps;
        
        // 清空现有按钮
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 为每个路径创建按钮
        for (int i = 0; i < paths.Count; i++)
        {
            int index = i; // 闭包需要
            IsometricMapNode node = paths[i];
            
            Button button = Instantiate(buttonPrefab, buttonContainer);
            Text buttonText = button.GetComponentInChildren<Text>();
            buttonText.text = $"Node {node.NodeId} ({node.Type})";
            
            button.onClick.AddListener(() => OnPathSelected(index));
        }
        
        panel.SetActive(true);
    }
    
    private void OnPathSelected(int pathIndex)
    {
        panel.SetActive(false);
        
        if (mapGridManager != null && _availablePaths != null && pathIndex < _availablePaths.Count)
        {
            mapGridManager.SelectPath(_availablePaths[pathIndex], _remainingSteps);
        }
    }
}