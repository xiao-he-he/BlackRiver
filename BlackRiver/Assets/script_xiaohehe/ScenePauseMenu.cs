using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScenePauseMenu : MonoBehaviour
{
    public const string VolumeKey = "MasterVolume";
    public const int DialogueSortingOrder = 32750;
    public const int SettingsSortingOrder = 32760;

    public GameObject volumePanel;
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Text volumeLabel;
    public Button volumeButton;
    public Button settingsButton;
    public Button continueButton;
    public Button restartButton;
    public Button quitButton;
    public Button mainMenuButton;
    public Button keyHelpButton;
    public GameObject keyHelpPrefab;
    public GameObject KeyHelpPanel => keyHelpCanvas;

    private static ScenePauseMenu activeMenu;
    private static int dialogueBlockedFrame = -1;
    public static bool IsSettingsOpen => activeMenu != null && activeMenu.isPaused;
    public static bool BlocksDialogueInput => IsSettingsOpen || Time.frameCount <= dialogueBlockedFrame;
    private bool isPaused;
    private bool leavingScene;
    private GameObject keyHelpCanvas;
    private GameObject modalBlocker;
    private int helpOpenedFrame = -1;
    private int volumeOpenedFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        activeMenu = null;
        dialogueBlockedFrame = -1;
    }

    private void Awake()
    {
        ApplyVolume(PlayerPrefs.GetFloat(VolumeKey, PlayerPrefs.GetFloat("SpotlightUI.Master", 1f)));
        // Keep the entire existing settings layout above portraits, dialogue and home CGs.
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvas.rootCanvas.sortingOrder = SettingsSortingOrder;
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
        AddListener(mainMenuButton, ReturnToMainMenu);
        AddListener(keyHelpButton, ShowKeyHelp);
        if (mainMenuButton != null) mainMenuButton.interactable = true;
        if (keyHelpButton != null) keyHelpButton.interactable = keyHelpPrefab != null;
        CreateModalBlocker();

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        UpdateVolumeLabel(AudioListener.volume);
    }

    private void Update()
    {
        if (isPaused) GamePauseState.EnsurePaused();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (KeyHelpPanel != null && KeyHelpPanel.activeSelf) HideKeyHelp();
            else if (volumePanel != null && volumePanel.activeSelf) volumePanel.SetActive(false);
            else ToggleSettings();
        }
        else if (KeyHelpPanel != null && KeyHelpPanel.activeSelf &&
                 Time.frameCount > helpOpenedFrame && Input.anyKeyDown)
        {
            HideKeyHelp();
        }
        if (volumePanel != null && volumePanel.activeSelf && Time.frameCount > volumeOpenedFrame &&
            Input.anyKeyDown)
        {
            bool clickingPanel = Input.GetMouseButtonDown(0) &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    volumePanel.transform as RectTransform, Input.mousePosition);
            if (!clickingPanel) volumePanel.SetActive(false);
        }
    }

    public void ToggleVolume()
    {
        if (volumePanel == null)
        {
            return;
        }

        HideKeyHelp();
        ContinueGame();
        volumePanel.SetActive(!volumePanel.activeSelf);
        if (volumePanel.activeSelf) volumeOpenedFrame = Time.frameCount;
        ClearSelection();
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

        HideKeyHelp();
        bool showSettings = !settingsPanel.activeSelf;
        settingsPanel.SetActive(showSettings);
        if (modalBlocker != null) modalBlocker.SetActive(showSettings);
        SetPaused(showSettings);
        ClearSelection();
    }

    public void ContinueGame()
    {
        HideKeyHelp();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        if (modalBlocker != null) modalBlocker.SetActive(false);
        SetPaused(false);
        ClearSelection();
    }

    public void RestartLevel()
    {
        if (leavingScene) return;
        leavingScene = GameProgress.RestartLevel();
        if (leavingScene) ContinueGame();
    }

    public void ReturnToMainMenu()
    {
        if (leavingScene) return;
        leavingScene = GameProgress.ReturnToMainMenu();
        if (leavingScene) ContinueGame();
    }

    public void ReturnToDesktop()
    {
        GameProgress.SaveCurrentCheckpoint();
        PlayerPrefs.Save();
        ContinueGame();
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
        PlayerPrefs.SetFloat("SpotlightUI.Master", value);
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

    public void ShowKeyHelp()
    {
        if (keyHelpCanvas == null)
        {
            if (keyHelpPrefab == null) return;
            keyHelpCanvas = new GameObject("Settings Key Help", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = keyHelpCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SettingsSortingOrder + 1;
            var scaler = keyHelpCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            GameObject content = Instantiate(keyHelpPrefab, keyHelpCanvas.transform, false);
            content.SetActive(true);
            var background = content.transform.Find("Background");
            if (background != null && background.TryGetComponent<Image>(out var image))
                image.color = new Color(0f, 0f, 0f, 0.65f);
        }
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (modalBlocker != null) modalBlocker.SetActive(true);
        SetPaused(true);
        keyHelpCanvas.SetActive(true);
        helpOpenedFrame = Time.frameCount;
        ClearSelection();
    }

    public void HideKeyHelp()
    {
        if (keyHelpCanvas != null && keyHelpCanvas.activeSelf)
        {
            keyHelpCanvas.SetActive(false);
            dialogueBlockedFrame = Time.frameCount;
        }
        ClearSelection();
    }

    private void CreateModalBlocker()
    {
        if (settingsPanel == null || settingsPanel.transform.parent == null) return;
        modalBlocker = new GameObject("Settings Modal Blocker", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        modalBlocker.transform.SetParent(settingsPanel.transform.parent, false);
        modalBlocker.transform.SetSiblingIndex(settingsPanel.transform.GetSiblingIndex());
        var rect = (RectTransform)modalBlocker.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        modalBlocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        modalBlocker.SetActive(false);
    }

    private static void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void SetPaused(bool pause)
    {
        if (pause == isPaused) return;
        isPaused = pause;
        if (pause)
        {
            activeMenu = this;
            GamePauseState.Acquire(this);
        }
        else
        {
            if (activeMenu == this) activeMenu = null;
            dialogueBlockedFrame = Time.frameCount;
            GamePauseState.Release(this);
        }
    }

    private void OnDisable()
    {
        SetPaused(false);
        HideKeyHelp();
        if (modalBlocker != null) modalBlocker.SetActive(false);
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
        RemoveListener(mainMenuButton, ReturnToMainMenu);
        RemoveListener(keyHelpButton, ShowKeyHelp);
        SetPaused(false);
        if (keyHelpCanvas != null) Destroy(keyHelpCanvas);
    }

    private void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }
}
