using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CameraShake2D : MonoBehaviour
{
    private Vector3 appliedOffset;
    private float elapsed;
    private float duration;
    private float magnitude;

    public static void ShakeMainCamera(float shakeDuration, float shakeMagnitude)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || shakeDuration <= 0f || shakeMagnitude <= 0f)
        {
            return;
        }

        CameraShake2D shaker = mainCamera.GetComponent<CameraShake2D>();
        if (shaker == null)
        {
            shaker = mainCamera.gameObject.AddComponent<CameraShake2D>();
        }

        shaker.duration = Mathf.Max(shaker.duration, shakeDuration);
        shaker.magnitude = Mathf.Max(shaker.magnitude, shakeMagnitude);
        shaker.elapsed = 0f;
    }

    private void LateUpdate()
    {
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;

        if (elapsed >= duration)
        {
            duration = 0f;
            magnitude = 0f;
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float fade = 1f - Mathf.Clamp01(elapsed / duration);
        Vector2 offset = Random.insideUnitCircle * magnitude * fade;
        appliedOffset = new Vector3(offset.x, offset.y, 0f);
        transform.position += appliedOffset;
    }

    private void OnDisable()
    {
        transform.position -= appliedOffset;
        appliedOffset = Vector3.zero;
    }
}
