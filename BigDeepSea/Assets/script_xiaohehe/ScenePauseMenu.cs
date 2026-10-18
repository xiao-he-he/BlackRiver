using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScenePauseMenu : MonoBehaviour
{
    private const string VolumeKey = "MasterVolume";

    public GameObject volumePanel;
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Text volumeLabel;
    public Button volumeButton;
    public Button settingsButton;
    public Button continueButton;
    public Button restartButton;
    public Button quitButton;

    private bool isPaused;
    private float previousTimeScale = 1f;
    private bool previousGameClockPause;

    private void Awake()
    {
        ApplyVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (volumePanel != null && volumePanel.activeSelf)
        {
            volumePanel.SetActive(false);
        }
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
            SetPaused(false);
        }
        AddListener(volumeButton, ToggleVolume);
        AddListener(settingsButton, ToggleSettings);
        AddListener(continueButton, ContinueGame);
        AddListener(restartButton, RestartLevel);
        AddListener(quitButton, ReturnToDesktop);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        UpdateVolumeLabel(AudioListener.volume);
    }

    public void ToggleVolume()
    {
        if (volumePanel == null)
        {
            return;
        }

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
            SetPaused(false);
        }

        volumePanel.SetActive(!volumePanel.activeSelf);
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (volumePanel != null && volumePanel.activeSelf)
        {
            volumePanel.SetActive(false);
        }

        bool showSettings = !settingsPanel.activeSelf;
        settingsPanel.SetActive(showSettings);
        SetPaused(showSettings);
    }

    public void ContinueGame()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        SetPaused(false);
    }

    public void RestartLevel()
    {
        SetPaused(false);
        var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(scene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.path);
    }

    public void ReturnToDesktop()
    {
        SetPaused(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
        ApplyVolume(value);
        UpdateVolumeLabel(value);
    }

    private void ApplyVolume(float value)
    {
        value = Mathf.Clamp01(value);
        AudioListener.volume = value;
    }

    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null)
        {
            volumeLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void SetPaused(bool pause)
    {
        if (pause == isPaused)
        {
            return;
        }

        if (pause)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (TimeManager.Instance != null)
            {
                previousGameClockPause = TimeManager.Instance.gameClockPause;
                TimeManager.Instance.gameClockPause = true;
            }
            isPaused = true;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.gameClockPause = previousGameClockPause;
            }
            isPaused = false;
        }
    }

    private void OnDisable()
    {
        SetPaused(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }
        RemoveListener(volumeButton, ToggleVolume);
        RemoveListener(settingsButton, ToggleSettings);
        RemoveListener(continueButton, ContinueGame);
        RemoveListener(restartButton, RestartLevel);
        RemoveListener(quitButton, ReturnToDesktop);
        SetPaused(false);
    }

    private void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
}
