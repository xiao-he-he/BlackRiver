
using UnityEngine;

public class move : MonoBehaviour
{
    [Header("角色移动")]
    public float moveSpeed = 3f;

    [Header("推箱子检测")]
    public float pushCheckDistance = 0.65f;
    public LayerMask boxLayer;

    [Header("动画状态物体")]
    public GameObject upIdle;
    public GameObject downIdle;
    public GameObject leftIdle;
    public GameObject rightIdle;

    public GameObject upMove;
    public GameObject downMove;
    public GameObject leftMove;
    public GameObject rightMove;

    [Header("推箱子动画状态物体")]
    public GameObject upPush;
    public GameObject downPush;
    public GameObject leftPush;
    public GameObject rightPush;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private readonly RaycastHit2D[] pushHits = new RaycastHit2D[16];
    private readonly System.Collections.Generic.HashSet<Collider2D> pushContacts =
        new System.Collections.Generic.HashSet<Collider2D>();

    private Vector2 moveInput;
    private Vector2 lastDirection = Vector2.down;

    private bool isPushing;
    private PushBox nearbyBox;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        ShowState(downIdle);
    }

    private void Update()
    {
        ReadInput();
        CheckPushBox();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (isPushing)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    private void ReadInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // 保留原来上下左右单方向移动的方式
        if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
        {
            moveInput.y = 0f;
        }
        else
        {
            moveInput.x = 0f;
        }

        if (moveInput != Vector2.zero)
        {
            lastDirection = moveInput.normalized;
        }

        isPushing = false;
    }

    private void CheckPushBox()
    {
        if (moveInput == Vector2.zero)
        {
            return;
        }

        Vector2 direction = moveInput.normalized;
        if (nearbyBox != null && nearbyBox.IsMoving &&
            Vector2.Dot((Vector2)nearbyBox.transform.position - rb.position, direction) > 0f)
        {
            isPushing = true;
            return;
        }

        nearbyBox = null;
        if (playerCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(boxLayer);
        filter.useTriggers = true;
        int hitCount = playerCollider.Cast(direction, filter, pushHits,
            Mathf.Clamp(pushCheckDistance, 0f, 0.05f));
        float nearestDistance = float.PositiveInfinity;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            PushBox box = pushHits[hitIndex].collider.GetComponentInParent<PushBox>();
            Vector2 offset = (Vector2)pushHits[hitIndex].collider.bounds.center -
                (Vector2)playerCollider.bounds.center;
            if (Vector2.Dot(offset, direction) <= 0f)
            {
                continue;
            }
            if (box != null && pushHits[hitIndex].distance < nearestDistance)
            {
                nearbyBox = box;
                nearestDistance = pushHits[hitIndex].distance;
            }
        }

        foreach (Collider2D contact in pushContacts)
        {
            if (contact == null || !contact.enabled || !contact.gameObject.activeInHierarchy)
            {
                continue;
            }
            Vector2 offset = (Vector2)contact.bounds.center - (Vector2)playerCollider.bounds.center;
            if (offset.sqrMagnitude > 0f &&
                Vector2.Dot(offset.normalized, direction) > 0.5f &&
                offset.sqrMagnitude < nearestDistance * nearestDistance)
            {
                nearbyBox = contact.GetComponentInParent<PushBox>();
                nearestDistance = offset.magnitude;
            }
        }

        if (nearbyBox != null)
        {
            nearbyBox.TryPush(direction);
            isPushing = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((boxLayer.value & (1 << other.gameObject.layer)) != 0 &&
            other.GetComponentInParent<PushBox>() != null)
        {
            pushContacts.Add(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        pushContacts.Remove(other);
    }

    private void OnDisable()
    {
        pushContacts.Clear();
        nearbyBox = null;
    }

    private void UpdateAnimation()
    {
        if (isPushing)
        {
            if (lastDirection.y > 0f)
                ShowState(upPush);
            else if (lastDirection.y < 0f)
                ShowState(downPush);
            else if (lastDirection.x < 0f)
                ShowState(leftPush);
            else
                ShowState(rightPush);

            return;
        }

        if (moveInput == Vector2.zero)
        {
            if (lastDirection.y > 0f)
                ShowState(upIdle);
            else if (lastDirection.y < 0f)
                ShowState(downIdle);
            else if (lastDirection.x < 0f)
                ShowState(leftIdle);
            else
                ShowState(rightIdle);
        }
        else
        {
            if (moveInput.y > 0f)
                ShowState(upMove);
            else if (moveInput.y < 0f)
                ShowState(downMove);
            else if (moveInput.x < 0f)
                ShowState(leftMove);
            else
                ShowState(rightMove);
        }
    }

    private void ShowState(GameObject target)
    {
        GameObject[] states =
        {
            upIdle, downIdle, leftIdle, rightIdle,
            upMove, downMove, leftMove, rightMove,
            upPush, downPush, leftPush, rightPush
        };

        foreach (GameObject state in states)
        {
            if (state != null)
            {
                state.SetActive(state == target);
            }
        }
    }
}
