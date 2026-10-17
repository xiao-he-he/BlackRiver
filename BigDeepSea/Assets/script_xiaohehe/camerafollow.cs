using UnityEngine;

public class camerafollow : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("跟随参数")]
    [SerializeField] private float smoothTime = 0.08f;

    [Header("摄像机边界")]
    [SerializeField] private float minX = -10f;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    private Camera cam;

    private Vector3 velocity = Vector3.zero;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position;

        // 保持摄像机原来的 Z
        targetPosition.z = transform.position.z;

        // SmoothDamp 平滑跟随
        Vector3 newPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // 计算摄像机实际尺寸
        float cameraHalfHeight = cam.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * cam.aspect;

        // 摄像机能够移动到的最大范围
        float cameraMinX = minX + cameraHalfWidth;
        float cameraMaxX = maxX - cameraHalfWidth;

        float cameraMinY = minY + cameraHalfHeight;
        float cameraMaxY = maxY - cameraHalfHeight;

        // X轴限制
        if (cameraMinX > cameraMaxX)
        {
            newPosition.x = (minX + maxX) * 0.5f;
        }
        else
        {
            newPosition.x = Mathf.Clamp(
                newPosition.x,
                cameraMinX,
                cameraMaxX
            );
        }

        // Y轴限制
        if (cameraMinY > cameraMaxY)
        {
            newPosition.y = (minY + maxY) * 0.5f;
        }
        else
        {
            newPosition.y = Mathf.Clamp(
                newPosition.y,
                cameraMinY,
                cameraMaxY
            );
        }

        transform.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 center = new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            0f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            maxY - minY,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
}