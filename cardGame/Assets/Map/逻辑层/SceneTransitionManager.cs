using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlayTheSpireMap
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public void GoToBattleScene()
        {
            SceneManager.LoadScene("BattleScene");
        }
        
        public void ReturnToMapScene()
        {
            SceneManager.LoadScene("MapScene");
        }
        
        public void OnSceneLoaded(Scene scene, UIManager ui)
        {
            if (scene.name == "BattleScene")
            {
                if (ui != null) ui.HideMapUI();
            }
            else if (scene.name == "MapScene")
            {
                if (ui != null) ui.ShowMapUI();
            }
        }
    }
}