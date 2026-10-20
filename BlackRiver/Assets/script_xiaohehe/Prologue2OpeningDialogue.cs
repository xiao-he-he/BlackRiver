using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Prologue2OpeningDialogue : MonoBehaviour
{
    [Serializable]
    public struct Line
    {
        public string speaker;
        [TextArea(2, 5)] public string text;
        public Line(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }
    [SerializeField] private Sprite dialoguePanelSprite;
    [SerializeField] private Sprite grandmaPortrait;
    [SerializeField] private Sprite girlPortrait;
    [SerializeField] private Sprite bubbleSprite;
    [SerializeField] private TMP_FontAsset dialogueFont;
    [SerializeField, Min(1f)] private float charactersPerSecond = 32f;

    public bool IsBusy { get; private set; }
    public bool IsReady => dialogueRoot != null;
    public bool IsBubbleVisible { get; private set; }

    private Canvas dialogueCanvas;
    private GameObject dialogueRoot;
    private Image grandmaImage;
    private Image girlImage;
    private RectTransform panelRect;
    private TMP_Text speakerText;
    private TMP_Text dialogueText;
    private Image bubbleImage;
    private TMP_Text bubbleText;
    private float previousTimeScale;
    private bool previousClockPause;
    private bool ownsPause;
    private Transform bubbleAnchor;
    private PushWinTrigger prologue2Win;
    private readonly List<next> prologue2Exits = new List<next>();
    private bool completionStarted;

    private void Start()
    {
        BuildDialogueUI();
        if (gameObject.scene.name == "序章2")
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                if (prologue2Win == null)
                    prologue2Win = root.GetComponentInChildren<PushWinTrigger>(true);
                prologue2Exits.AddRange(root.GetComponentsInChildren<next>(true));
            }
            foreach (next exit in prologue2Exits) exit.locked = true;
            Play(new[]
            {
                new Line("奶奶", "啊，供奉箱没有在正确的位置呢。"),
                new Line("奶奶", "云丹，去推推试试看吧。"),
                new Line("女孩", "嗯嗯，我试试看。")
            });
        }
    }

    private void Update()
    {
        if (!completionStarted && prologue2Win != null && prologue2Win.HasWon && !IsBusy)
        {
            completionStarted = Play(new[]
            {
                new Line("奶奶", "真棒，我们继续走吧。"),
                new Line("女孩", "好！")
            }, () =>
            {
                foreach (next exit in prologue2Exits)
                    if (exit != null) exit.Unlock();
            });
        }
        if ((!IsBusy && !IsBubbleVisible) || dialogueRoot == null)
            return;

        // Settings can restore time temporarily; keep it stopped until this conversation closes.
        if (Time.timeScale != 0f)
            Time.timeScale = 0f;
        if (TimeManager.Instance != null)
            TimeManager.Instance.gameClockPause = true;

        if (!IsBusy) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            (Input.GetMouseButtonDown(0) &&
             RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition)))
            advanceRequested = true;
    }

    private bool advanceRequested;

    public void AdvanceDialogue()
    {
        if (IsBusy) advanceRequested = true;
    }

    public bool Play(Line[] sequence, Action onFinished = null)
    {
        if (IsBusy || IsBubbleVisible || sequence == null || sequence.Length == 0 || dialogueRoot == null)
            return false;
        StartCoroutine(PlaySequence(sequence, onFinished));
        return true;
    }

    public bool ShowBubble(string text, Transform worldAnchor = null)
    {
        if (IsBusy || IsBubbleVisible || bubbleImage == null) return false;
        PauseTime();
        bubbleAnchor = worldAnchor;
        bubbleText.text = text;
        float height = Mathf.Clamp(bubbleText.GetPreferredValues(text, 570f, Mathf.Infinity).y + 90f, 240f, 420f);
        bubbleImage.rectTransform.sizeDelta = new Vector2(680f, height);
        bubbleImage.gameObject.SetActive(true);
        IsBubbleVisible = true;
        PositionBubble();
        return true;
    }

    private void LateUpdate()
    {
        if (IsBubbleVisible) PositionBubble();
    }

    private void PositionBubble()
    {
        if (bubbleAnchor == null || bubbleImage == null || Camera.main == null) return;
        Vector3 world = bubbleAnchor.position;
        var sprite = bubbleAnchor.GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) world.y = sprite.bounds.max.y;
        Vector3 screen = Camera.main.WorldToScreenPoint(world);
        RectTransform canvasRect = (RectTransform)dialogueCanvas.transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 point))
            return;
        RectTransform bubbleRect = bubbleImage.rectTransform;
        bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0f);
        // The artwork's tail is near its right edge; point it at the dog's head.
        point += new Vector2(-bubbleRect.rect.width * 0.39f, 24f);
        Rect bounds = canvasRect.rect;
        point.x = Mathf.Clamp(point.x, bounds.xMin + bubbleRect.rect.width * 0.5f + 16f,
            bounds.xMax - bubbleRect.rect.width * 0.5f - 16f);
        point.y = Mathf.Clamp(point.y, bounds.yMin + 16f, bounds.yMax - bubbleRect.rect.height - 16f);
        bubbleRect.anchoredPosition = point;
    }

    public void HideBubble()
    {
        if (bubbleImage != null) bubbleImage.gameObject.SetActive(false);
        IsBubbleVisible = false;
        RestoreTime();
    }

    private IEnumerator PlaySequence(Line[] sequence, Action onFinished)
    {
        IsBusy = true;
        PauseTime();
        dialogueRoot.SetActive(true);
        for (int i = 0; i < sequence.Length; i++)
        {
            var line = sequence[i];
            string content = line.text ?? string.Empty;
            bool grandmaSpeaking = line.speaker == "奶奶";
            bool girlSpeaking = line.speaker == "女孩" || line.speaker == "云丹";
            grandmaImage.gameObject.SetActive(grandmaSpeaking);
            girlImage.gameObject.SetActive(girlSpeaking);
            speakerText.text = line.speaker ?? string.Empty;
            dialogueText.text = string.Empty;
            advanceRequested = false;

            int visibleCharacters = 0;
            float characterProgress = 0f;
            while (visibleCharacters < content.Length)
            {
                if (advanceRequested)
                {
                    visibleCharacters = content.Length;
                    break;
                }

                characterProgress += charactersPerSecond * Time.unscaledDeltaTime;
                int newlyVisible = Mathf.FloorToInt(characterProgress);
                characterProgress -= newlyVisible;
                visibleCharacters = Mathf.Min(content.Length, visibleCharacters + newlyVisible);
                dialogueText.text = content.Substring(0, visibleCharacters);
                yield return null;
            }

            dialogueText.text = content;
            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
        }

        dialogueRoot.SetActive(false);
        IsBusy = false;
        RestoreTime();
        onFinished?.Invoke();
    }

    private void PauseTime()
    {
        previousTimeScale = Time.timeScale;
        previousClockPause = TimeManager.Instance != null && TimeManager.Instance.gameClockPause;
        ownsPause = true;
        Time.timeScale = 0f;
        if (TimeManager.Instance != null)
            TimeManager.Instance.gameClockPause = true;
    }

    private void BuildDialogueUI()
    {
        var canvasObject = new GameObject("Prologue2 Dialogue Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        dialogueCanvas = canvasObject.GetComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = short.MaxValue - 1;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        dialogueRoot = new GameObject("Dialogue", typeof(RectTransform));
        dialogueRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform rootRect = (RectTransform)dialogueRoot.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image panel = CreateImage("Dialogue Panel", dialogueRoot.transform, dialoguePanelSprite);
        panelRect = panel.rectTransform;
        SetBottomCenter(panel.rectTransform, new Vector2(1500f, 260f), 55f);
        panel.type = Image.Type.Sliced;
        panel.raycastTarget = false;

        grandmaImage = CreateImage("Grandma Portrait", dialogueRoot.transform, grandmaPortrait);
        SetBottomEdge(grandmaImage.rectTransform, new Vector2(500f, 550f), 315f, false);
        grandmaImage.preserveAspect = false;
        grandmaImage.raycastTarget = false;

        girlImage = CreateImage("Girl Portrait", dialogueRoot.transform, girlPortrait);
        SetBottomEdge(girlImage.rectTransform, new Vector2(500f, 550f), 315f, true);
        girlImage.preserveAspect = false;
        girlImage.raycastTarget = false;

        speakerText = CreateText("Speaker", dialogueRoot.transform, 36f, TextAlignmentOptions.MidlineLeft);
        SetBottomCenter(speakerText.rectTransform, new Vector2(1370f, 55f), 235f);
        speakerText.alignment = TextAlignmentOptions.MidlineLeft;

        dialogueText = CreateText("Dialogue Text", dialogueRoot.transform, 38f, TextAlignmentOptions.MidlineLeft);
        SetBottomCenter(dialogueText.rectTransform, new Vector2(1370f, 145f), 82f);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        bubbleImage = CreateImage("Dog Bubble", canvasObject.transform, bubbleSprite);
        SetBottomCenter(bubbleImage.rectTransform, new Vector2(680f, 240f), 365f);
        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.raycastTarget = false;
        bubbleText = CreateText("Dog Bubble Text", bubbleImage.transform, 37f, TextAlignmentOptions.Center);
        RectTransform bubbleRect = bubbleText.rectTransform;
        bubbleRect.anchorMin = Vector2.zero;
        bubbleRect.anchorMax = Vector2.one;
        bubbleRect.offsetMin = new Vector2(55f, 46f);
        bubbleRect.offsetMax = new Vector2(-55f, -28f);
        bubbleText.color = new Color(0.21f, 0.13f, 0.1f, 1f);
        bubbleText.textWrappingMode = TextWrappingModes.Normal;
        bubbleImage.gameObject.SetActive(false);
        dialogueRoot.SetActive(false);
    }

    private Image CreateImage(string objectName, Transform parent, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private TMP_Text CreateText(string objectName, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = dialogueFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.91f, 0.76f, 1f);
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void SetBottomCenter(RectTransform rect, Vector2 size, float bottomOffset)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, bottomOffset);
    }

    private static void SetBottomEdge(RectTransform rect, Vector2 size, float bottomOffset, bool right)
    {
        rect.anchorMin = new Vector2(right ? 1f : 0f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(right ? 1f : 0f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, bottomOffset);
    }

    private void RestoreTime()
    {
        if (!ownsPause)
            return;

        Time.timeScale = previousTimeScale;
        if (TimeManager.Instance != null)
            TimeManager.Instance.gameClockPause = previousClockPause;
        ownsPause = false;
    }

    private void OnDestroy()
    {
        RestoreTime();
        if (dialogueCanvas != null)
            Destroy(dialogueCanvas.gameObject);
    }
}
