using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinLoseSystem : MonoBehaviour
{
    [Header("Result UI")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public GameObject titleButton;

    [Header("Scene Name")]
    public string titleSceneName = "title";

    [Header("Option")]
    public bool stopTimeOnResult = true;

    private bool finished = false;

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (titleButton != null) titleButton.SetActive(false);
        Time.timeScale = 1f;
        finished = false;
    }

    public void Win()
    {
        if (finished) return;
        finished = true;

        AudioManager.I?.PlayYouWin();
        Debug.Log("WIN");
        ShowResult("WIN");
    }

    public void Lose()
    {
        if (finished) return;
        finished = true;

        AudioManager.I?.PlayYouLose();
        Debug.Log("LOSE");
        ShowResult("LOSE");
    }

    private void ShowResult(string message)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
            resultText.text = message;

        if (titleButton != null)
            titleButton.SetActive(true);

        if (stopTimeOnResult)
            Time.timeScale = 0f;
    }

    public void OnClickTitleButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}
