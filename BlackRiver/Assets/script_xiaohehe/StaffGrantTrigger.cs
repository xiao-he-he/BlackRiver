using UnityEngine;

[DisallowMultipleComponent]
public sealed class StaffGrantTrigger : MonoBehaviour
{
    public Collider2D grantTrigger;
    [Tooltip("配置后先播放神像对白，全部结束后才授予法杖。")]
    public Prologue2OpeningDialogue dialogue;
    public bool ConversationCompleted { get; private set; }
    private bool conversationStarted;

    private void OnTriggerEnter2D(Collider2D other) => Grant(other);
    private void OnTriggerStay2D(Collider2D other) => Grant(other);

    private void Grant(Collider2D other)
    {
        if (grantTrigger == null || !grantTrigger.enabled || !grantTrigger.isTrigger ||
            other == null || other.isTrigger || !grantTrigger.IsTouching(other))
            return;
        StaffAbility ability = other.GetComponentInParent<StaffAbility>();
        if (ability == null) return;
        if (dialogue == null)
        {
            ability.AcquireStaff();
            return;
        }
        if (conversationStarted || ConversationCompleted || !dialogue.IsReady || dialogue.IsBusy)
            return;
        conversationStarted = dialogue.Play(new[]
        {
            new Prologue2OpeningDialogue.Line("神像", "此地非生者久留之所。"),
            new Prologue2OpeningDialogue.Line("女孩", "你是谁？"),
            new Prologue2OpeningDialogue.Line("神像", "守路者。"),
            new Prologue2OpeningDialogue.Line("女孩", "这里……是死去的人会来的地方吗？"),
            new Prologue2OpeningDialogue.Line("神像", "有人从此经过，有人在此徘徊。众生各有去处。"),
            new Prologue2OpeningDialogue.Line("女孩", "那你有没有见过我奶奶？"),
            new Prologue2OpeningDialogue.Line("神像", "此间来往之人，何止千万。"),
            new Prologue2OpeningDialogue.Line("女孩", "……那我要去哪里才能找到她？"),
            new Prologue2OpeningDialogue.Line("神像", "你所寻之人，已走过你脚下的路。"),
            new Prologue2OpeningDialogue.Line("女孩", "那我沿着这条路走，就能见到她吗？"),
            new Prologue2OpeningDialogue.Line("神像", "前路漫漫，聚散无常。能否相见，并非我能应允。"),
            new Prologue2OpeningDialogue.Line("女孩", "可是……至少有可能，对吧？"),
            new Prologue2OpeningDialogue.Line("神像", "……"),
            new Prologue2OpeningDialogue.Line("女孩", "我知道了。谢谢你。")
        }, () =>
        {
            ConversationCompleted = true;
            if (ability != null) ability.AcquireStaff();
        });
    }
}
