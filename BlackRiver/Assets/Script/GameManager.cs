using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI设置")]
    public Canvas winPanel;
    
    public int currentLevel = 1;
   
   

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
       var winPanel1 = FindObjectsOfType<Canvas>();//1"WinCanvas, 2"Canvas

        for (int i = 0; i < winPanel1.Length; i++)
        {
            // 先检查是否为 null，再检查名称
            if (winPanel1 != null && winPanel1[i].name == "WinCanvas")
            {
                winPanel = winPanel1[i];
                break; // 找到第一个后立即退出循环，提高效率
            }
        }



        Box.OnAllBoxesOnTarget += OnGameWin;
      
        
        if (winPanel != null)
        {
            winPanel.transform.GetChild(0).gameObject.SetActive(false);
        }
         
    }

   
    public void DisablePanel()
    {
        StartCoroutine(DelayMethod());
    }

    private IEnumerator DelayMethod()
    {
        yield return new WaitForSeconds(1.5f);
        winPanel.transform.GetChild(0).gameObject.SetActive(false);
    }

    void OnGameWin()
    {
        if (winPanel != null)
        {
            winPanel.transform.GetChild(0).gameObject.SetActive(true);
             
        }
        Debug.Log("恭喜通关！");
    }

     

    

 

    
}
