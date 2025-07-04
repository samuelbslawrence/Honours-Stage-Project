using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuActions : MonoBehaviour
{
  public void StartInvestigation()
  {
    // Replace the scene to start the investigation
    SceneManager.LoadScene("Roling");
  }

  public void QuitApplication()
  {
    // Exit the application
    Application.Quit();
  }
}