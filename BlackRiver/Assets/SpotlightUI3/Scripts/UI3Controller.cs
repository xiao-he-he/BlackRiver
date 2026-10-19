using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spotlight.UI3
{
    public sealed class UI3Controller : MonoBehaviour
    {
        public GameObject mainMenu, inGameUI, pausePanel, failurePanel, keyHelpPanel, confirmPanel;
        public GameObject globalControls, volumePopup;
        public Button continueButton, pauseIcon, volumeButton;
        public Slider masterVolumeSlider;
        public TMP_Text masterVolumeValue, confirmMessage;
        public GameObject previewBackdrop;
        public bool previewMode = true, enablePreviewShortcuts = true, showMainMenuOnStart = true;
        public GameObject[] dialoguePanels;
        public TMP_Text[] dialogueTexts;
        public UnityEvent onNewGame = new UnityEvent(), onContinueGame = new UnityEvent();
        public UnityEvent onRestartLevel = new UnityEvent(), onReturnToMenu = new UnityEvent(), onAdvanceDialogue = new UnityEvent();
        public UnityEvent<float> onMasterVolumeChanged = new UnityEvent<float>();
        [SerializeField] private bool canContinue;
        private GameObject helpSourceContent;
        private int dialogueKind, dialogueLine;
        private string pendingAction;
        public const string MasterVolumePreference = "SpotlightUI.Master";
        public float MasterVolume => masterVolumeSlider ? masterVolumeSlider.value : AudioListener.volume;
        private readonly string[] lines = {
            "（你和奶奶一起吃着饭，有一搭没一搭地聊着。邻居家谁家结婚了、谁搬走了……）",
            "下雨了啊，去把窗子关上吧。",
            "你看向窗外，雨水顺着玻璃落了下来。"
        };
        private void Start()
        {
            if (previewBackdrop) previewBackdrop.SetActive(previewMode);
            if (masterVolumeSlider) masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MasterVolumePreference, .7f));
            ApplyMasterVolume(MasterVolume); CloseMasterVolume();
            SetCanContinue(canContinue);
            if (showMainMenuOnStart) ShowMainMenu();
            RefreshGlobalControls();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                if (volumePopup.activeSelf) CloseMasterVolume();
                else if (confirmPanel.activeSelf) CancelConfirmation();
                else if (keyHelpPanel.activeSelf) CloseKeyHelp();
                else if (pausePanel.activeSelf) ResumeGame();
                else if (inGameUI.activeSelf && !failurePanel.activeSelf) PauseGame();
            }
            if (!inGameUI.activeSelf || pausePanel.activeSelf || volumePopup.activeSelf || failurePanel.activeSelf || confirmPanel.activeSelf || keyHelpPanel.activeSelf) return;
            if (enablePreviewShortcuts) {
                if (Input.GetKeyDown(KeyCode.F2)) CycleDialogue();
                if (Input.GetKeyDown(KeyCode.F3)) ShowFailure();
            }
            if (Input.GetKeyDown(KeyCode.Space) && System.Array.Exists(dialoguePanels, p => p && p.activeSelf)) AdvanceDialogue();
        }
        private void ClearSelection() { if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null); }
        private void HideOverlays()
        {
            CloseKeyHelp(); CloseMasterVolume();
            pausePanel.SetActive(false); failurePanel.SetActive(false);
            keyHelpPanel.SetActive(false); confirmPanel.SetActive(false); pendingAction = null;
        }
        public void ShowMainMenu()
        {
            HideOverlays(); inGameUI.SetActive(false); mainMenu.SetActive(true);
            Time.timeScale = 1; SetCanContinue(canContinue); RefreshGlobalControls(); ClearSelection();
        }
        public void NewGame()
        {
            SetCanContinue(true); HideOverlays(); mainMenu.SetActive(false); inGameUI.SetActive(true); Time.timeScale = 1;
            dialogueLine = 0; ShowDialogueKind(0); SetDialogueLine(); RefreshGlobalControls(); ClearSelection(); onNewGame.Invoke();
        }
        public void ContinueGame()
        {
            if (!canContinue) return;
            HideOverlays(); mainMenu.SetActive(false); inGameUI.SetActive(true); Time.timeScale = 1; RefreshGlobalControls(); ClearSelection(); onContinueGame.Invoke();
        }
        public void PauseGame() { if (!inGameUI.activeSelf) return; CloseMasterVolume(); pausePanel.SetActive(true); Time.timeScale = 0; ClearSelection(); }
        public void ResumeGame() { CloseKeyHelp(); CloseMasterVolume(); pausePanel.SetActive(false); Time.timeScale = 1; ClearSelection(); }
        public void OnPauseIcon()
        {
            if (keyHelpPanel.activeSelf) { CloseKeyHelp(); return; }
            if (confirmPanel.activeSelf || failurePanel.activeSelf) return;
            if (pausePanel.activeSelf) ResumeGame(); else PauseGame();
        }
        public void OpenKeyHelp()
        {
            CloseMasterVolume();
            if (keyHelpPanel.activeSelf) return;
            helpSourceContent = pausePanel.activeSelf ? pausePanel.transform.Find("Card").gameObject :
                mainMenu.activeSelf ? mainMenu.transform.Find("Content").gameObject : null;
            if (helpSourceContent) helpSourceContent.SetActive(false);
            // Reuse the pause mask when opening help from pause; main menu needs its own mask.
            keyHelpPanel.transform.Find("Background").GetComponent<Image>().color = pausePanel.activeSelf ? Color.clear : new Color(0, 0, 0, .2f);
            keyHelpPanel.SetActive(true);
            ClearSelection();
        }
        public void CloseKeyHelp()
        {
            if (keyHelpPanel) keyHelpPanel.SetActive(false);
            if (helpSourceContent) helpSourceContent.SetActive(true);
            helpSourceContent = null; ClearSelection();
        }
        public void ToggleMasterVolume()
        {
            if (keyHelpPanel.activeSelf) CloseKeyHelp();
            volumePopup.SetActive(!volumePopup.activeSelf); ClearSelection();
        }
        public void CloseMasterVolume() { if (volumePopup) volumePopup.SetActive(false); ClearSelection(); }
        private void RefreshGlobalControls()
        {
            if (globalControls) globalControls.SetActive(true);
            if (pauseIcon) pauseIcon.gameObject.SetActive(inGameUI.activeSelf);
        }
        public void ShowFailure() { if (!inGameUI.activeSelf) return; HideOverlays(); failurePanel.SetActive(true); Time.timeScale = 0; ClearSelection(); }
        public void Retry()
        {
            HideOverlays(); mainMenu.SetActive(false); inGameUI.SetActive(true); Time.timeScale = 1;
            dialogueLine = 0; SetDialogueLine(); RefreshGlobalControls(); ClearSelection(); onRestartLevel.Invoke();
        }
        public void AskRestart() { Ask("是否重新开始本关？", "restart"); }
        public void AskReturnToMenu() { Ask("是否返回主菜单？", "menu"); }
        public void AskQuit() { Ask("是否退出游戏？", "quit"); }
        public void ShowAbout() { Ask("游戏标题", "about"); }
        private void Ask(string message, string action) { CloseMasterVolume(); confirmMessage.text = message; pendingAction = action; confirmPanel.SetActive(true); ClearSelection(); }
        public void CancelConfirmation() { confirmPanel.SetActive(false); pendingAction = null; ClearSelection(); }
        public void AcceptConfirmation()
        {
            string action = pendingAction; CancelConfirmation();
            if (action == "restart") Retry();
            else if (action == "menu") { ShowMainMenu(); onReturnToMenu.Invoke(); }
            else if (action == "quit") {
                Time.timeScale = 1;
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        public void OnMasterVolumeChanged(float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumePreference, value); ApplyMasterVolume(value); onMasterVolumeChanged.Invoke(value);
        }
        private void ApplyMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            if (masterVolumeValue) masterVolumeValue.text = Mathf.RoundToInt(value * 100) + "%";
        }
        public void CycleDialogue() { if (dialoguePanels.Length > 0) ShowDialogueKind((dialogueKind + 1) % dialoguePanels.Length); }
        public void ShowDialogueKind(int kind)
        {
            dialogueKind = Mathf.Clamp(kind, 0, dialoguePanels.Length - 1);
            for (int i = 0; i < dialoguePanels.Length; i++) dialoguePanels[i].SetActive(i == dialogueKind);
        }
        public void SetDialogueText(string content) { foreach (var text in dialogueTexts) if (text) text.text = content; }
        public void AdvanceDialogue()
        {
            if (previewMode) { dialogueLine = (dialogueLine + 1) % lines.Length; SetDialogueLine(); }
            onAdvanceDialogue.Invoke();
        }
        private void SetDialogueLine() { for (int i = 0; i < dialogueTexts.Length; i++) dialogueTexts[i].text = i == 2 ? "下雨了啊，去把\n窗子关上吧。" : lines[dialogueLine]; }
        public void SetCanContinue(bool value) { canContinue = value; if (continueButton) continueButton.interactable = value; }
        private void OnApplicationPause(bool pause) { if (pause) PlayerPrefs.Save(); }
        private void OnDestroy() { Time.timeScale = 1; PlayerPrefs.Save(); }
    }
}
