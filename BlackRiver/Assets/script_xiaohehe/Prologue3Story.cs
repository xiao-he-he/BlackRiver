using System.Collections;
using UnityEngine;

public sealed class Prologue3Story : MonoBehaviour
{
    public Prologue2OpeningDialogue dialogue;
    public PushWinTrigger winTrigger;
    public next exit;
    private bool winHandled;
    private bool introductionFinished;

    private void Start()
    {
        if (exit != null) exit.locked = true;
        StartCoroutine(Begin());
    }

    private IEnumerator Begin()
    {
        yield return new WaitUntil(() => dialogue != null && dialogue.IsReady);
        dialogue.Play(new[]
        {
            new Prologue2OpeningDialogue.Line("奶奶", "啊，这个供奉箱也是呢。"),
            new Prologue2OpeningDialogue.Line("奶奶", "顺风的时候，箱子可以被吹走。"),
            new Prologue2OpeningDialogue.Line("奶奶", "逆风的时候，箱子无法被移动。"),
            new Prologue2OpeningDialogue.Line("奶奶", "去试试看按E键转动转经筒吧。"),
            new Prologue2OpeningDialogue.Line("女孩", "我试试看。")
        }, () => introductionFinished = true);
    }

    private void Update()
    {
        if (winHandled || !introductionFinished || winTrigger == null || !winTrigger.HasWon || dialogue.IsBusy)
            return;
        winHandled = true;
        dialogue.Play(new[]
        {
            new Prologue2OpeningDialogue.Line("奶奶", "嗯嗯，云丹就是聪明，我的好孙女儿。"),
            new Prologue2OpeningDialogue.Line("奶奶", "咱们回家。"),
            new Prologue2OpeningDialogue.Line("女孩", "走吧！")
        }, () => { if (exit != null) exit.Unlock(); });
    }
}
