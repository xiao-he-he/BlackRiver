using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HomeStorySequence : MonoBehaviour
{
    public Prologue2OpeningDialogue dialogue;
    public HomeWindowDarkness window;
    public HomeDogFamilyPhoto dog;
    public Sprite familyPhoto;
    public Sprite thangkaOne;
    public Sprite thangkaTwo;
    public next transition;
    public next oldExit;
    [Min(0f)] public float fadeSeconds = 0.8f;

    private int dogLine;
    private bool windowLinePlayed;
    private bool windowLineFinished;
    private bool introductionFinished;
    private bool finaleStarted;
    private bool dogBubbleClosing;
    private int dogBubbleClosedFrame;
    private CanvasGroup cgGroup;
    private Image cgImage;
    private Image fadeImage;
    private GameObject cgRoot;
    public bool HasDogBubble => dialogue != null && dialogue.IsBubbleVisible;

    private static Prologue2OpeningDialogue.Line L(string speaker, string text)
        => new Prologue2OpeningDialogue.Line(speaker, text);

    private void Start()
    {
        if (transition != null) transition.locked = true;
        if (oldExit != null) oldExit.locked = true;
        StartCoroutine(Begin());
    }

    private IEnumerator Begin()
    {
        yield return new WaitUntil(() => dialogue != null && dialogue.IsReady);
        dialogue.Play(new[]
        {
            L("女孩", "呼……外面真冷啊。不过和奶奶靠在一起就很温暖。"),
            L("奶奶", "等以后你长大了，就要嫌弃奶奶身上有老人味喽～"),
            L("奶奶", "对了，快过年了，你爸爸妈妈过年前就从城里打工回来。"),
            L("女孩", "哦～那他们会给我带礼物吗？"),
            L("奶奶", "那奶奶就不知道咯，你现在打个电话问他们呗。"),
            L("", "【拿到了手机，打电话给爸爸妈妈】"),
            L("女孩", "喂，爸爸妈妈？"),
            L("爸爸", "云丹。你放假回家啦。对呀对呀，最近在学校可累了。你们什么时候回来呀？"),
            L("妈妈", "嗯……大概还有二十多天吧？你在家乖，听奶奶的话好的！"),
            L("爸爸", "是呀，做个乖孩子，我们很快就会回去的。"),
            L("女孩", "那你们回来的时候，会给我带礼物吗？"),
            L("爸爸", "肯定会给你带呀，哪年没有给你带？"),
            L("妈妈", "对呀，安心吧，一定给你准备最棒的礼物。"),
            L("妈妈", "好啦，我们这儿还有点活要干，先挂啦。"),
            L("女孩", "嘿嘿，爸爸妈妈最好了！拜拜！"),
            L("", "【挂断电话】"),
            L("奶奶", "其实奶奶也给你准备了礼物。"),
            L("", "【你拿到了一张唐卡】"),
            L("奶奶", "之后你可能就要去城里上高中，不在奶奶身边了，奶奶希望你能一直平平安安，开开心心的。"),
            L("女孩", "哇，谢谢奶奶！"),
            L("奶奶", "去关下窗户吧，通风通得有点冷了。"),
            L("女孩", "好。")
        }, () => introductionFinished = true);
    }

    private void Update()
    {
        if (HasDogBubble && window != null && window.IsClosed)
            dialogue.HideBubble();
        if (dogBubbleClosing && Time.frameCount > dogBubbleClosedFrame)
            dogBubbleClosing = false;
        if (introductionFinished && !windowLinePlayed && window != null && window.IsClosed && !dialogue.IsBusy)
        {
            windowLinePlayed = true;
            dialogue.Play(new[] { L("女孩", "奶奶？") }, () => windowLineFinished = true);
        }
    }

    public bool OnDogInteract()
    {
        if (!introductionFinished || finaleStarted || dialogue.IsBusy || dog == null || dogBubbleClosing)
            return false;
        if (HasDogBubble)
        {
            dialogue.HideBubble();
            dogBubbleClosing = true;
            dogBubbleClosedFrame = Time.frameCount;
            return true;
        }
        if (window != null && window.IsClosed)
        {
            if (!windowLineFinished) return false;
            finaleStarted = true;
            dialogue.Play(new[] { L("女孩", "……"), L("女孩", "奶奶……") },
                () => StartCoroutine(PlayFinale()));
            return true;
        }

        var lines = new[]
        {
            "（庞大的笨笨狼摇着尾巴跑来了，在你身上蹭来蹭去）\n诶？是笨笨狼！我回来啦，想我没有？",
            "（笨笨狼哼唧哼唧地蹭着你）",
            "（笨笨狼用它的大舌头舔你，你的脸颊感到一股热气）\n哈哈哈哈……好啦好啦",
            "（你摸了摸笨笨狼）",
            "（哼唧哼唧地蹭着你）",
            "（你又摸了摸笨笨狼）",
            "（翻肚皮！）",
            "（你又摸了摸笨笨狼）",
            "（你摸了好久笨笨狼，它终于冷静下来了）"
        };
        if (dogLine >= lines.Length) return false;
        if (!dialogue.ShowBubble(lines[dogLine], dog.transform)) return false;
        dogLine++;
        return true;
    }

    private IEnumerator PlayFinale()
    {
        if (dog.player != null)
        {
            dog.player.enabled = false;
            var body = dog.player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
        }
        yield return ShowCG(familyPhoto, true);
        yield return Say(new[]
        {
            L("", "爸爸妈妈很忙，工作在外地，所以我一直和奶奶一起生活。"),
            L("", "爸爸是个可靠的大人。虽然有时候有些倔强，但我知道他是爱着我的。"),
            L("", "妈妈很温柔，也很坚强。她一直陪着爸爸，陪着我们，一起经营起这个家。"),
            L("", "奶奶一直陪在我身边，我与奶奶相依为命，一起生活在近在咫尺的蓝天下。"),
            L("", "——可是，到底从什么时候开始，奶奶悄悄淡出我的生活了呢？")
        });
        yield return ShowCG(thangkaOne, true);
        yield return Say(new[]
        {
            L("", "奶奶的礼物我一直好好保存着，想奶奶的时候，我会看看它。"),
            L("", "这是奶奶留给我的……遗物。"),
            L("", "奶奶……我真的好想你啊……")
        });
        yield return ShowCG(thangkaTwo, false);
        yield return Say(new[] { L("", "？！"), L("", "这是……！？") });
        fadeImage.color = Color.clear;
        yield return Fade(1f, Color.white);
        if (transition != null)
        {
            transition.Unlock();
            transition.TryTransition();
        }
        else
        {
            Debug.LogError("HomeStorySequence: missing temple corridor transition.");
        }
    }

    private IEnumerator Say(Prologue2OpeningDialogue.Line[] lines)
    {
        yield return null;
        while (dialogue.IsBusy) yield return null;
        dialogue.Play(lines);
        while (dialogue.IsBusy) yield return null;
    }

    private IEnumerator ShowCG(Sprite sprite, bool fade)
    {
        if (cgRoot == null) CreateCG();
        if (sprite == null) Debug.LogError("HomeStorySequence: missing CG image.");
        if (fade)
        {
            yield return Fade(1f);
            cgImage.sprite = sprite;
            cgGroup.alpha = 1f;
            yield return Fade(0f);
        }
        else
        {
            cgImage.sprite = sprite;
            cgGroup.alpha = 1f;
        }
    }

    private void CreateCG()
    {
        cgRoot = new GameObject("Home CG", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(CanvasGroup));
        var canvas = cgRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 2;
        var scaler = cgRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cgGroup = cgRoot.GetComponent<CanvasGroup>();
        cgGroup.alpha = 0f;
        var background = CreateImage("CG Backdrop", Color.black);
        background.transform.SetAsFirstSibling();
        cgImage = CreateImage("CG Image", Color.white);
        cgImage.preserveAspect = true;
        fadeImage = CreateImage("CG Fade", Color.black);
        fadeImage.raycastTarget = false;
        fadeImage.color = Color.clear;
    }

    private Image CreateImage(string title, Color color)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(cgRoot.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator Fade(float alpha, Color? tint = null)
    {
        float start = fadeImage.color.a;
        float time = 0f;
        Color color = tint ?? Color.black;
        while (time < fadeSeconds)
        {
            time += Time.unscaledDeltaTime;
            fadeImage.color = new Color(color.r, color.g, color.b,
                Mathf.Lerp(start, alpha, Mathf.Clamp01(time / Mathf.Max(0.001f, fadeSeconds))));
            yield return null;
        }
        fadeImage.color = new Color(color.r, color.g, color.b, alpha);
    }
}
