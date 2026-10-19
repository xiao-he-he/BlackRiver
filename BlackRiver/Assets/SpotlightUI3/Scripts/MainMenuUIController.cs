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
        private int volumeOpenedFrame = -1;

        public void ShowKeyHelp()
        {
            if (keyHelpPanel)
                keyHelpPanel.SetActive(true);
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
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Awake()
        {
            if (volumeSlider)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
                SetVolume(volumeSlider.value);
            }
        }

        private void Update()
        {
            if (keyHelpPanel && keyHelpPanel.activeSelf && Input.GetMouseButtonDown(0))
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
            AudioListener.volume = value;
            if (volumeValue)
                volumeValue.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
