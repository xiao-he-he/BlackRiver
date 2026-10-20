using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spotlight.UI3
{
    public sealed class MainMenuUIController : MonoBehaviour
    {
        [SerializeField] private GameObject keyHelpPanel;
        [SerializeField] private GameObject volumePopup;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        private int volumeOpenedFrame = -1;
        private int helpOpenedFrame = -1;
        private bool loadingScene;

        public void StartNewGame()
        {
            if (!loadingScene) loadingScene = GameProgress.StartNewGame();
            RefreshContinueButton();
        }

        public void ContinueGame()
        {
            if (!loadingScene) loadingScene = GameProgress.ContinueGame();
            RefreshContinueButton();
        }

        private void RefreshContinueButton()
        {
            if (continueButton) continueButton.interactable = !loadingScene && GameProgress.CanContinue;
            if (newGameButton) newGameButton.interactable = !loadingScene;
        }

        public void ShowKeyHelp()
        {
            if (keyHelpPanel)
            {
                keyHelpPanel.SetActive(true);
                helpOpenedFrame = Time.frameCount;
            }
        }

        public void HideKeyHelp()
        {
            if (keyHelpPanel)
                keyHelpPanel.SetActive(false);
        }

        public void ToggleVolume()
        {
            if (volumePopup)
            {
                volumePopup.SetActive(!volumePopup.activeSelf);
                if (volumePopup.activeSelf)
                    volumeOpenedFrame = Time.frameCount;
            }
        }

        public void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Awake()
        {
            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(ScenePauseMenu.VolumeKey,
                PlayerPrefs.GetFloat(UI3Controller.MasterVolumePreference, 1f)));
            AudioListener.volume = volume;
            if (volumeSlider)
            {
                volumeSlider.SetValueWithoutNotify(volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
            UpdateVolumeLabel(volume);
            if (newGameButton) newGameButton.onClick.AddListener(StartNewGame);
            if (continueButton) continueButton.onClick.AddListener(ContinueGame);
            RefreshContinueButton();
        }

        private void Update()
        {
            if (keyHelpPanel && keyHelpPanel.activeSelf &&
                Time.frameCount > helpOpenedFrame && Input.anyKeyDown)
                HideKeyHelp();

            if (volumePopup && volumePopup.activeSelf &&
                Time.frameCount > volumeOpenedFrame && Input.anyKeyDown)
            {
                // Keep mouse interaction with the volume slider available.
                var card = volumePopup.transform.Find("Card") as RectTransform;
                bool clickingCard = Input.GetMouseButtonDown(0) && card &&
                    RectTransformUtility.RectangleContainsScreenPoint(card, Input.mousePosition);
                if (!clickingCard)
                    volumePopup.SetActive(false);
            }
        }

        private void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(ScenePauseMenu.VolumeKey, value);
            PlayerPrefs.SetFloat(UI3Controller.MasterVolumePreference, value);
            PlayerPrefs.Save();
            UpdateVolumeLabel(value);
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue)
                volumeValue.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        private void OnDestroy()
        {
            if (newGameButton) newGameButton.onClick.RemoveListener(StartNewGame);
            if (continueButton) continueButton.onClick.RemoveListener(ContinueGame);
            if (volumeSlider) volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }
    }
}
