using System.Collections.Generic;
using UnityEngine;

public class PrayerWheelInteraction : MonoBehaviour
{
    public WindSailController sailController;
    public Collider2D interactionTrigger;
    public KeyCode interactionKey = KeyCode.E;

    private readonly HashSet<Collider2D> playerContacts = new HashSet<Collider2D>();

    public bool CanInteract
    {
        get
        {
            if (!isActiveAndEnabled || interactionTrigger == null ||
                !interactionTrigger.enabled || !interactionTrigger.isTrigger)
            {
                return false;
            }
            foreach (Collider2D contact in playerContacts)
            {
                if (contact != null && contact.enabled && contact.gameObject.activeInHierarchy &&
                    interactionTrigger.IsTouching(contact))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private void Update()
    {
        if (Time.timeScale > 0f && Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }
    }

    public bool TryInteract()
    {
        if (!CanInteract || sailController == null)
        {
            return false;
        }
        sailController.RotateCounterClockwise();
        return true;
    }

    // The laser owns per-cast deduplication; no proximity requirement here.
    public bool TryActivateBySpell()
    {
        if (!isActiveAndEnabled || sailController == null)
            return false;
        sailController.RotateCounterClockwise();
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackPlayer(other);
    }

    private void TrackPlayer(Collider2D other)
    {
        if (other.GetComponentInParent<move>() != null && interactionTrigger != null &&
            interactionTrigger.IsTouching(other))
        {
            playerContacts.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerContacts.Remove(other);
    }

    private void OnDisable()
    {
        playerContacts.Clear();
    }
}
