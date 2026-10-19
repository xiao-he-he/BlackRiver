using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SwitchBounds : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnEnable()
    {
        EventHandler.AfterSceneLoadedEvent += SwitchConfinerShape;//加载新场景之前去找一下相机的边界盒子
    }

    private void OnDisable()
    {
        EventHandler.AfterSceneLoadedEvent -= SwitchConfinerShape;
    }
    private void SwitchConfinerShape()
    {
        PolygonCollider2D confinerShape=GameObject.FindGameObjectWithTag("BoundsConfiner").GetComponent<PolygonCollider2D>(); //"Bounds (UnityEngine.PolygonCollider2D)"

        CinemachineConfiner confiner=GetComponent<CinemachineConfiner>();//"Virtual Camera
        confiner.m_BoundingShape2D=confinerShape;//"Bounds (UnityEngine.PolygonCollider2D)"-->NULL
        //切换场景清楚缓存
        confiner.InvalidatePathCache();
    }
}
