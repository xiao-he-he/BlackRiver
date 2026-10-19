using UnityEngine;

[DisallowMultipleComponent]
public sealed class EncounterNextTransition : MonoBehaviour
{
    public BarrageEncounter encounter;
    public next transition;
    public bool endGameAfterSurvival;
    private bool subscribed;

    private void Start()
    {
        if (encounter == null)
            encounter = GetComponent<BarrageEncounter>();
        if (encounter != null)
        {
            encounter.Survived += OnSurvived;
            subscribed = true;
        }
    }

    private void OnSurvived(BarrageEncounter _)
    {
        if (endGameAfterSurvival)
        {
            Debug.Log("Final jump park completed. No next scene configured.");
            return;
        }
        if (transition != null)
            transition.TryTransition();
    }

    private void OnDestroy()
    {
        if (subscribed && encounter != null)
            encounter.Survived -= OnSurvived;
    }
}
