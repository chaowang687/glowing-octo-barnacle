using UnityEngine;
using UnityEngine.UI;

namespace SlayTheSpireMap
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI引用")]
        public MapUI mapUI;
        public Text healthText;
        public Text goldText;
        public Button continueButton;
        
        public void UpdateAllUI()
        {
            UpdateMapUI();
            UpdatePlayerStatusUI();
        }
        
        public void UpdateMapUI()
        {
            if (mapUI != null)
            {
                mapUI.GenerateMapUI();
            }
        }
        
        public void UpdatePlayerStatusUI()
        {
            if (MapManager.Instance == null || MapManager.Instance.playerState == null) return;
            
            if (healthText != null)
            {
                healthText.text = $"{MapManager.Instance.playerState.Health}/{MapManager.Instance.playerState.MaxHealth}";
            }
            
            if (goldText != null)
            {
                goldText.text = $"{MapManager.Instance.playerState.Gold}G";
            }
        }
        
        public void ShowMapUI()
        {
            if (mapUI != null) mapUI.Show();
        }
        
        public void HideMapUI()
        {
            if (mapUI != null) mapUI.Hide();
        }
        
        public void SetContinueButtonState(bool interactable)
        {
            if (continueButton != null)
            {
                continueButton.interactable = interactable;
            }
        }
    }
}