using UnityEngine;

public class next : MonoBehaviour
{
    public GameObject object1;
    public int nextSceneIndex;
    public string nextSceneName;
    [Min(0f)] public float fadeOutDuration = 0.5f;
    [Min(0f)] public float fadeInDuration = 0.5f;
    [Min(1f)] public float fadeInEasePower = 1f;
    [Min(0f)] public float openingFadeDuration = 1.5f;
    public bool locked;
    private bool transitioning;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            TryTransition();
    }

    public void Unlock()
    {
        locked = false;
    }

    public void TryTransition()
    {
        if (transitioning || locked || !CanTransition())
            return;
        transitioning = true;
        if (string.IsNullOrEmpty(nextSceneName))
            PrologueSceneFader.LoadScene(nextSceneIndex, fadeOutDuration, fadeInDuration, fadeInEasePower);
        else
            PrologueSceneFader.LoadScene(nextSceneName, fadeOutDuration, fadeInDuration, fadeInEasePower);
    }

    private bool CanTransition()
    {
        string sceneName = gameObject.scene.name;
        if (sceneName != "序章2" && sceneName != "序章3")
            return true;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            PushWinTrigger winTrigger = root.GetComponentInChildren<PushWinTrigger>(true);
            if (winTrigger != null && winTrigger.HasWon)
                return true;
        }
        return false;
    }
}
