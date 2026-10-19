
using UnityEngine;
using UnityEngine.Events;

public class PushWinTrigger : MonoBehaviour
{
    [Header("胜利事件")]
    public UnityEvent onWin;

    private bool hasWon = false;

    public bool HasWon => hasWon;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;

        PushBox box = other.GetComponentInParent<PushBox>();

        if (box != null && box.isSpecialBox)
        {
            hasWon = true;

            Debug.Log("特殊箱子进入胜利区域，游戏胜利！");

            onWin.Invoke();
        }
    }
}
