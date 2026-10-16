
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class move : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [Header("Animator References - Idle")]
    [SerializeField] private Animator upIdleAnimator;
    [SerializeField] private Animator downIdleAnimator;
    [SerializeField] private Animator leftIdleAnimator;
    [SerializeField] private Animator rightIdleAnimator;

    [Header("Animator References - Move")]
    [SerializeField] private Animator upMoveAnimator;
    [SerializeField] private Animator downMoveAnimator;
    [SerializeField] private Animator leftMoveAnimator;
    [SerializeField] private Animator rightMoveAnimator;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private Direction facing = Direction.Down;
    private Animator currentAnimator;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // 俯视角移动：不受重力影响，避免角色旋转
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // 游戏开始时，强制进入向下待机
        ShowAnimator(downIdleAnimator);
    }

    private void Update()
    {
        // 读取 WASD 输入
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(x, y).normalized;

        // 根据主要移动轴决定朝向
        if (moveInput.sqrMagnitude > 0.001f)
        {
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                facing = x < 0 ? Direction.Left : Direction.Right;
            }
            else
            {
                facing = y > 0 ? Direction.Up : Direction.Down;
            }
        }

        // 根据移动状态和朝向选择显示对象
        bool isMoving = moveInput.sqrMagnitude > 0.001f;
        Animator targetAnimator = GetAnimator(facing, isMoving);

        if (targetAnimator != currentAnimator)
        {
            ShowAnimator(targetAnimator);
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = moveInput * moveSpeed;
    }

    private Animator GetAnimator(Direction direction, bool isMoving)
    {
        if (isMoving)
        {
            switch (direction)
            {
                case Direction.Up: return upMoveAnimator;
                case Direction.Down: return downMoveAnimator;
                case Direction.Left: return leftMoveAnimator;
                case Direction.Right: return rightMoveAnimator;
            }
        }
        else
        {
            switch (direction)
            {
                case Direction.Up: return upIdleAnimator;
                case Direction.Down: return downIdleAnimator;
                case Direction.Left: return leftIdleAnimator;
                case Direction.Right: return rightIdleAnimator;
            }
        }

        return downIdleAnimator;
    }

    private void ShowAnimator(Animator target)
    {
        if (target == null)
        {
            Debug.LogWarning(
                "Animator 引用未设置，请检查 Inspector。",
                this
            );
            return;
        }

        // 关闭其他七个状态物体
        Animator[] allAnimators =
        {
            upIdleAnimator,
            downIdleAnimator,
            leftIdleAnimator,
            rightIdleAnimator,
            upMoveAnimator,
            downMoveAnimator,
            leftMoveAnimator,
            rightMoveAnimator
        };

        foreach (Animator animator in allAnimators)
        {
            if (animator != null && animator != target)
            {
                animator.gameObject.SetActive(false);
            }
        }

        // 显示当前状态物体
        target.gameObject.SetActive(true);
        currentAnimator = target;
    }
}
