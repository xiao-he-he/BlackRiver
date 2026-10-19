using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    [SceneName]
    public string StartSceneName=string.Empty;

     
    private CanvasGroup fadeCanvasGroup;

    private void OnEnable()
    {
        EventHandler.TransitionEvent += OnTransitionEvent;
    }

   

    private void OnDisable()
    {
        EventHandler.TransitionEvent -= OnTransitionEvent;
    }
    private void Awake()
    {
        fadeCanvasGroup = FindObjectOfType<CanvasGroup>(true);
    }

    private void Start()
    {   
        StartCoroutine(LoadSceneSetActive(StartSceneName));
        fadeCanvasGroup = FindObjectOfType<CanvasGroup>(true);
    }
    private void OnTransitionEvent(string sceneToGo, Vector3 positionToGo)
    {
        if(!isFade)
           StartCoroutine(Transition(sceneToGo, positionToGo));
    }

    private IEnumerator Transition(string sceneName, Vector3 positionToGo)
    {
        //卸载场景前需要做的事情

        EventHandler.CallBeforeSceneUnLoadEvent();

        //场景过渡黑屏
        yield return  Fade(1f);

        //等待当前场景卸载
        yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        
        //等待叠加的加载场景
        yield return LoadSceneSetActive(sceneName);

        //移动人物到指定位置
        EventHandler.CallMoveToPositionEvent(positionToGo);
        
        //场景过渡结束恢复
        yield return Fade(0f);

        //加载新场景后需要做的事情
        EventHandler.CallAfterSceneLoadedEvent();
        
    }


    private IEnumerator LoadSceneSetActive(string sceneName)
    {
        //等待叠加的加载场景
        yield return SceneManager.LoadSceneAsync(sceneName,LoadSceneMode.Additive);

        Scene newScene=SceneManager.GetSceneAt(SceneManager.sceneCount-1);

        SceneManager.SetActiveScene(newScene);
    }

    private bool isFade;

    private IEnumerator Fade(float finalAlpha)//1 is black, 0 is clear
    {
        isFade = true;
        fadeCanvasGroup = FindObjectOfType<CanvasGroup>(true);
        
        fadeCanvasGroup.blocksRaycasts = true;


            float fadeSpeed = Mathf.Abs(fadeCanvasGroup.alpha - finalAlpha) / Settings.fadeDuration;
            while (!Mathf.Approximately(fadeCanvasGroup.alpha, finalAlpha))
            {
                fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, finalAlpha, fadeSpeed * Time.deltaTime);
                yield return null;
            }


            fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.gameObject.SetActive(false);

        

        isFade = false;
    }

}
