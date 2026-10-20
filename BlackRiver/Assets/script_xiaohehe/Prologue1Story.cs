using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Prologue1Story : MonoBehaviour
{
    public Prologue2OpeningDialogue dialogue;
    public Transform player;
    public Transform grandmother;
    public next exit;
    [Min(0.01f)] public float parallelTolerance = 0.3f;

    public bool GreetingCompleted { get; private set; }
    private bool introductionFinished;
    private bool greetingStarted;
    private float previousOffset;

    private IEnumerator Start()
    {
        if (exit != null) exit.locked = true;
        if (player == null || grandmother == null || dialogue == null)
        {
            Debug.LogError("Prologue1Story: dialogue, player or grandmother is missing.", this);
            yield break;
        }
        previousOffset = player.position.y - grandmother.position.y;
        yield return new WaitUntil(() => dialogue.IsReady);
        dialogue.Play(new[]
        {
            new Prologue2OpeningDialogue.Line("女孩", "啊啊，终于放学了。看到奶奶了，在前面等着我呢！")
        }, () => introductionFinished = true);
    }

    private void Update()
    {
        if (player == null || grandmother == null || dialogue == null) return;
        float offset = player.position.y - grandmother.position.y;
        bool parallel = Mathf.Abs(offset) <= parallelTolerance || offset * previousOffset < 0f;
        previousOffset = offset;
        if (!introductionFinished || greetingStarted || dialogue.IsBusy ||
            Time.timeScale <= 0f || !parallel) return;

        greetingStarted = dialogue.Play(new[]
        {
            new Prologue2OpeningDialogue.Line("奶奶", "云丹，放学啦。"),
            new Prologue2OpeningDialogue.Line("女孩", "嗯嗯！走吧走吧"),
            new Prologue2OpeningDialogue.Line("奶奶", "好好好。")
        }, () =>
        {
            GreetingCompleted = true;
            if (exit != null) exit.Unlock();
        });
    }
}
