using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene-local endless scoring. Only installed on the player in jump park 4.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(move))]
public sealed class EndlessSurvivalMode : MonoBehaviour
{
    public BarrageEncounter encounter;
    public Text timerText;
    public GameObject resultPanel;
    public Text scoreText;
    public Text leaderboardText;
    public Button retryButton;
    public Button mainMenuButton;

    public bool HasFinished { get; private set; }
    public float FinalTime { get; private set; }

    private void Awake()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
        if (encounter != null)
            encounter.onPlayerDied.AddListener(FinishRun);
        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        UpdateTimer();
    }

    private void LateUpdate() => UpdateTimer();

    private void UpdateTimer()
    {
        if (timerText != null)
            timerText.text = "坚持时间  " + FormatTime(
                HasFinished ? FinalTime : encounter != null ? encounter.Elapsed : 0f);
    }

    public static string FormatTime(float seconds)
    {
        // Floor rather than round so the display never credits uncompleted time.
        var elapsed = TimeSpan.FromMilliseconds(
            Math.Floor(Math.Max(0d, seconds) * 100d + 0.0001d) * 10d);
        return ((long)elapsed.TotalMinutes).ToString("00") + ":" +
            elapsed.Seconds.ToString("00") + "." + (elapsed.Milliseconds / 10).ToString("00");
    }

    public void FinishRun()
    {
        if (HasFinished)
            return;
        HasFinished = true;
        FinalTime = encounter != null ? encounter.Elapsed : 0f;
        if (encounter != null)
            encounter.PlayerDied();
        GetComponent<move>().enabled = false;
        StaffAbility staff = GetComponent<StaffAbility>();
        if (staff != null)
            staff.enabled = false;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
        UpdateTimer();
        if (scoreText != null)
            scoreText.text = "本次坚持时间\n" + FormatTime(FinalTime);
        int rank = EndlessSurvivalLeaderboard.Record(FinalTime);
        if (scoreText != null)
            scoreText.text += rank > 0 ? "\n本次排名：第 " + rank + " 名" : "\n本次未进入前 10 名";
        if (leaderboardText != null)
            leaderboardText.text = EndlessSurvivalLeaderboard.BuildDisplay();
        if (resultPanel != null)
            resultPanel.SetActive(true);
        if (timerText != null)
            timerText.transform.parent.gameObject.SetActive(false);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.path);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Assets/Scenes/主菜单.unity");
    }

    private void OnDestroy()
    {
        if (encounter != null)
            encounter.onPlayerDied.RemoveListener(FinishRun);
        if (retryButton != null)
            retryButton.onClick.RemoveListener(Retry);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
    }
}
