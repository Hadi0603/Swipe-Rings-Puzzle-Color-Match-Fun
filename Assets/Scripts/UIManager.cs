using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject winCeleb;
    [SerializeField] private CanvasGroup levelWonUI;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private CanvasGroup pauseUI;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseBtn;
    [SerializeField] private Text timerText;
    [SerializeField] private CanvasGroup levelLostUI;
    [SerializeField] private GameObject lostPanel;
    [SerializeField] GameObject discMover;
    [SerializeField] AudioSource winSound;
    [SerializeField] AudioSource gameOverSound;

    [SerializeField] private float gameTime = 60f;
    private bool isBlinking = false;
    private bool isPaused = false;

    public void Awake()
    {
        levelWonUI.alpha = 0f;
        winPanel.transform.localPosition = new Vector2(0, +Screen.height);
        levelLostUI.alpha = 0f;
        lostPanel.transform.localPosition = new Vector2(0, +Screen.height);
        pauseUI.alpha = 0f;
        pausePanel.transform.localPosition = new Vector2(0, +Screen.height);
        StartCoroutine(TimerCountdown());
    }

    public void TriggerGameWon()
    {
        Debug.Log("Game Won! All discs are destroyed.");
        winSound.Play();
        levelWonUI.gameObject.SetActive(true);
        levelWonUI.LeanAlpha(1, 0.5f);
        pauseBtn.SetActive(false);
        StopCoroutine(TimerCountdown());
        Destroy(timerText);
        winPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        winCeleb.SetActive(true);
        if (GameManager.levelToLoad < 10)
        {
            PlayerPrefs.SetInt("levelToLoad", ++GameManager.levelToLoad);
        }
        PlayerPrefs.Save();
    }

    public void OpenPauseMenu()
    {
        pauseUI.gameObject.SetActive(true);
        pauseUI.LeanAlpha(1, 0.5f);
        pauseBtn.SetActive(false);
        pausePanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
        isPaused = true;
        discMover.SetActive(false);
    }

    public void ClosePauseMenu()
    {
        pauseUI.LeanAlpha(0, 0.5f);
        pausePanel.LeanMoveLocalY(+Screen.height, 0.5f).setEaseInExpo();
        pauseBtn.SetActive(true);
        isPaused = false;
        discMover.SetActive(true);
        Invoke(nameof(DisablePauseUI), 0.5f);
    }

    private void DisablePauseUI()
    {
        pauseUI.gameObject.SetActive(false);
    }

    private IEnumerator TimerCountdown()
    {
        while (gameTime > 0)
        {
            if (!isPaused) // Only update the timer if the game is not paused
            {
                gameTime -= Time.deltaTime;
                UpdateTimerDisplay();

                if (gameTime <= 15f && !isBlinking)
                {
                    StartCoroutine(BlinkTimer());
                }
            }
            yield return null;
        }

        GameOver();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60);
        int seconds = Mathf.FloorToInt(gameTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private IEnumerator BlinkTimer()
    {
        isBlinking = true;
        Color originalColor = timerText.color;
    
        while (gameTime <= 15f && gameTime > 0)
        {
            timerText.color = (timerText.color == Color.red) ? Color.white : Color.red;
            yield return new WaitForSeconds(0.5f);
        }

        timerText.color = originalColor;
        isBlinking = false;
    }

    private void GameOver()
    {
        Debug.Log("Time's up! Game Over.");
        gameOverSound.Play();
        pauseBtn.SetActive(false);
        levelLostUI.gameObject.SetActive(true);
        timerText.enabled = false;
        levelLostUI.LeanAlpha(1, 0.5f);
        lostPanel.LeanMoveLocalY(0, 0.5f).setEaseOutExpo().delay = 0.1f;
    }
}
