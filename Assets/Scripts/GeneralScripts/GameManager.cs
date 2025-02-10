using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject startMenu;    // Start Menu UI
    public GameObject stageMenu;    // Stage Selection UI

    // Called when the Start button is clicked
    public void OnStartButtonClicked()
    {
        startMenu.SetActive(false);    // Hide the Start Menu
        stageMenu.SetActive(true);     // Show the Stage Selection Menu
    }

    // Called when a stage is selected
    public void OnStageSelected(int stageIndex)
    {
        // Hide the Stage Selection Menu
        stageMenu.SetActive(false);

        // Load the selected stage
        SceneManager.LoadScene("Stage" + stageIndex);  // "Stage1", "Stage2", "Stage3" based on the button pressed
    }
}
