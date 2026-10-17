using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrologueSceneFader : MonoBehaviour
{
    private static PrologueSceneFader instance;

    private CanvasGroup curtain;
    private bool transitioning;
    private float destinationFadeDuration = 0.5f;
    private float destinationFadeEasePower = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("序章转场黑幕", typeof(Canvas), typeof(CanvasGroup));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<PrologueSceneFader>();

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        instance.curtain = root.GetComponent<CanvasGroup>();
        instance.curtain.alpha = 1f;
        instance.curtain.blocksRaycasts = true;

        GameObject panel = new GameObject("黑幕", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void LoadScene(int sceneIndex, float fadeOutDuration, float fadeInDuration, float fadeInEasePower = 1f)
    {
        if (instance == null)
        {
            Initialize();
        }

        if (instance.transitioning || sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            return;
        }

        instance.StartCoroutine(instance.Transition(sceneIndex, null, fadeOutDuration, fadeInDuration, fadeInEasePower));
    }

    public static void LoadScene(string sceneName, float fadeOutDuration, float fadeInDuration, float fadeInEasePower = 1f)
    {
        if (instance == null)
        {
            Initialize();
        }

        if (instance.transitioning || string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return;
        }

        instance.StartCoroutine(instance.Transition(0, sceneName, fadeOutDuration, fadeInDuration, fadeInEasePower));
    }

    private IEnumerator Transition(int sceneIndex, string sceneName, float fadeOutDuration, float fadeInDuration, float fadeInEasePower)
    {
        transitioning = true;
        destinationFadeDuration = Mathf.Max(0f, fadeInDuration);
        destinationFadeEasePower = Mathf.Max(1f, fadeInEasePower);
        curtain.blocksRaycasts = true;
        yield return FadeTo(1f, fadeOutDuration);

        AsyncOperation load = sceneName == null
            ? SceneManager.LoadSceneAsync(sceneIndex)
            : SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            transitioning = false;
            yield return FadeTo(0f, fadeInDuration, destinationFadeEasePower);
            curtain.blocksRaycasts = false;
            yield break;
        }

        yield return load;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!transitioning && !IsPrologue(scene))
        {
            curtain.alpha = 0f;
            curtain.blocksRaycasts = false;
            return;
        }

        curtain.alpha = 1f;
        curtain.blocksRaycasts = true;

        float duration = destinationFadeDuration;
        if (!transitioning)
        {
            duration = 1.5f;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                next exit = root.GetComponentInChildren<next>(true);
                if (exit != null)
                {
                    duration = exit.openingFadeDuration;
                    break;
                }
            }
        }

        StartCoroutine(Reveal(duration, transitioning ? destinationFadeEasePower : 1f));
    }

    private static bool IsPrologue(Scene scene)
    {
        return scene.name == "序章1" || scene.name == "序章2" ||
            scene.name == "序章3" || scene.name == "序章4";
    }

    private IEnumerator Reveal(float duration, float easePower)
    {
        yield return FadeTo(0f, duration, easePower);
        curtain.blocksRaycasts = false;
        transitioning = false;
    }

    private IEnumerator FadeTo(float target, float duration, float easePower = 1f)
    {
        float start = curtain.alpha;
        if (duration <= 0f)
        {
            curtain.alpha = target;
            yield break;
        }

        float elapsed = 0f;
        yield return new WaitForEndOfFrame();
        yield return null;
        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float progress = Mathf.Pow(Mathf.Clamp01(elapsed / duration), easePower);
            curtain.alpha = Mathf.Lerp(start, target, progress);
            yield return null;
        }

        curtain.alpha = target;
    }
}
