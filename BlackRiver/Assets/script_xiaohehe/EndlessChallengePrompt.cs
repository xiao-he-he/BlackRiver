using UnityEngine;
using UnityEngine.UI;

/// <summary>Final story encounter offers the optional endless scene through next.</summary>
[DisallowMultipleComponent]
public sealed class EndlessChallengePrompt : MonoBehaviour
{
    public BarrageEncounter encounter;
    public GameObject popup;
    public Button enterButton;
    public Button cancelButton;
    public next endlessTransition;
    private bool opened;
    private bool movementWasEnabled;
    private bool staffWasEnabled;

    private void Awake()
    {
        popup.SetActive(false);
        enterButton.onClick.AddListener(EnterEndless);
        cancelButton.onClick.AddListener(Close);
        encounter.Survived += OnSurvived;
    }

    private void OnSurvived(BarrageEncounter _) => Show();

    public void Show()
    {
        if (opened)
            return;
        opened = true;
        move movement = encounter.player.GetComponent<move>();
        movementWasEnabled = movement.enabled;
        movement.enabled = false;
        StaffAbility staff = encounter.player.GetComponent<StaffAbility>();
        if (staff != null)
        {
            staffWasEnabled = staff.enabled;
            staff.enabled = false;
        }
        Rigidbody2D body = encounter.player.GetComponent<Rigidbody2D>();
        if (body != null)
            body.linearVelocity = Vector2.zero;
        popup.SetActive(true);
    }

    public void EnterEndless()
    {
        if (!opened)
            return;
        enterButton.interactable = false;
        cancelButton.interactable = false;
        endlessTransition.TryTransition();
    }

    public void Close()
    {
        if (!opened)
            return;
        popup.SetActive(false);
        opened = false;
        encounter.player.GetComponent<move>().enabled = movementWasEnabled;
        StaffAbility staff = encounter.player.GetComponent<StaffAbility>();
        if (staff != null)
            staff.enabled = staffWasEnabled;
    }

    private void OnDestroy()
    {
        if (encounter != null)
            encounter.Survived -= OnSurvived;
        if (enterButton != null)
            enterButton.onClick.RemoveListener(EnterEndless);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);
    }
}
