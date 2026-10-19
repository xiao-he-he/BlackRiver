
using System.Collections;
using UnityEngine;

public class CameraZoneMove : MonoBehaviour
{
    [Header("摄像机设置")]
    public Transform mainCamera;
    public float moveDistance = 5f;
    public float moveDuration = 1.2f;
    public string playerTag = "Player";

    [Header("双向 Trigger 设置")]
    public GameObject nextTrigger;

    private bool hasTriggered;
    private Coroutine moveCoroutine;

    private void OnEnable()
    {
        // 每次 Trigger 重新启用时，允许再次触发
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;

        if (!other.CompareTag(playerTag) &&
            !other.transform.root.CompareTag(playerTag))
            return;

        if (mainCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                mainCamera = cam.transform;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("没有找到 Main Camera。", this);
            return;
        }

        hasTriggered = true;

        Vector3 startPosition = mainCamera.position;
        Vector3 targetPosition =
            startPosition + Vector3.down * moveDistance;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(
            SmoothMove(startPosition, targetPosition)
        );
    }

    private IEnumerator SmoothMove(
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            mainCamera.position =
                Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        mainCamera.position = targetPosition;
        moveCoroutine = null;

        // 摄像机移动完成后，隐藏当前 Trigger
        gameObject.SetActive(false);

        // 显示下一个 Trigger
        if (nextTrigger != null)
            nextTrigger.SetActive(true);
    }
}
