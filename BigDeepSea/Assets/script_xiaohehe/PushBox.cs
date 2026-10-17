
using UnityEngine;
using System.Collections;

public class PushBox : MonoBehaviour
{
    [Header("箱子类型")]
    public bool isSpecialBox = false;

    [Header("移动设置")]
    public float gridSize = 1f;
    public float moveDuration = 0.15f;

    public bool IsMoving { get; private set; }
    public bool IsWindDriven => WindPushZone.IsBoxAffected(this);
    public Vector2 AnchorPosition => (Vector2)transform.position - cellOffset;

    private PushGridCell currentCell;
    private Vector2 cellOffset;
    private Coroutine moveCoroutine;
    private Coroutine windCoroutine;
    private Rigidbody2D boxBody;

    // 记录当前负责吹动箱子的风区
    private WindPushZone currentWindSource;
    private Vector2 currentWindDirection;

    private void Awake()
    {
        boxBody = GetComponent<Rigidbody2D>();

        if (boxBody == null)
        {
            boxBody = gameObject.AddComponent<Rigidbody2D>();
            boxBody.bodyType = RigidbodyType2D.Kinematic;
            boxBody.gravityScale = 0f;
            boxBody.freezeRotation = true;
        }
    }

    private void Start()
    {
        Collider2D boxCollider = GetComponentInChildren<Collider2D>();

        Vector2 anchor = boxCollider != null
            ? (Vector2)boxCollider.bounds.center
            : (Vector2)transform.position;

        cellOffset = (Vector2)transform.position - anchor;

        PushGridCell initialCell =
            PushGridCell.GetCell(anchor, gridSize);

        if (initialCell == null)
        {
            Debug.LogWarning(
                name + " 初始位置没有对应的 PushGridCell。", this);
            return;
        }

        EnterCell(initialCell);

        if (currentCell == initialCell)
        {
            SetPosition(currentCell.Position + cellOffset);
        }
    }

    public void EnterCell(PushGridCell cell)
    {
        if (cell == null || IsMoving || !cell.canBoxMoveHere ||
            (cell.occupiedBox != null && cell.occupiedBox != this))
        {
            return;
        }

        PushGridCell nearestCell = PushGridCell.GetCell(
            (Vector2)transform.position - cellOffset, gridSize);

        if (nearestCell != cell)
            return;

        if (currentCell != null &&
            currentCell != cell &&
            currentCell.occupiedBox == this)
        {
            currentCell.occupiedBox = null;
        }

        currentCell = cell;
        cell.occupiedBox = this;
    }

    public void ExitCell(PushGridCell cell)
    {
        if (cell != null &&
            cell != currentCell &&
            cell.occupiedBox == this)
        {
            cell.occupiedBox = null;
        }
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (windCoroutine != null)
        {
            StopCoroutine(windCoroutine);
            windCoroutine = null;
        }

        if (currentCell != null && currentCell.occupiedBox == this)
        {
            if (IsMoving)
                SetPosition(currentCell.Position + cellOffset);

            currentCell.occupiedBox = null;
        }

        IsMoving = false;
        currentWindSource = null;
    }

    // 玩家推箱子：只要受到风区影响，任何方向都不能推
    public bool TryPush(Vector2 direction)
    {
        if (IsWindDriven)
        {
            Debug.Log(name + " 正受到风力影响，玩家不能推动。");
            return false;
        }

        return TryMove(direction);
    }

    // 风区调用：开始或维持连续风力推动
    public bool TryWindPush(Vector2 direction, WindPushZone source)
    {
        if (source == null || !source.Affects(this))
            return false;

        if (IsMoving)
            return false;

        currentWindSource = source;
        currentWindDirection = NormalizeDirection(direction);

        if (currentWindDirection == Vector2.zero)
            return false;

        if (windCoroutine == null)
        {
            windCoroutine = StartCoroutine(WindMoveToEnd());
        }

        return true;
    }

    private IEnumerator WindMoveToEnd()
    {
        while (true)
        {
            // 风区失效或箱子离开风区，停止连续吹动
            if (currentWindSource == null ||
                !currentWindSource.Affects(this))
            {
                break;
            }

            if (IsMoving)
            {
                yield return null;
                continue;
            }

            // 每次尝试移动一格；被挡住就停止
            bool moved = TryMove(currentWindDirection);

            if (!moved)
                break;

            // 等待本格移动结束，再尝试下一格
            while (IsMoving)
                yield return null;

            yield return new WaitForFixedUpdate();
        }

        windCoroutine = null;
        currentWindSource = null;
    }

    private bool TryMove(Vector2 direction)
    {
        if (IsMoving || currentCell == null || gridSize <= 0f)
            return false;

        direction = NormalizeDirection(direction);

        if (direction == Vector2.zero)
            return false;

        Vector2 targetAnchor =
            currentCell.Position + direction * gridSize;

        PushGridCell targetCell =
            PushGridCell.GetCell(targetAnchor, gridSize);

        if (targetCell == null ||
            targetCell == currentCell ||
            !targetCell.canBoxMoveHere ||
            (targetCell.occupiedBox != null &&
             targetCell.occupiedBox != this))
        {
            return false;
        }

        if (currentCell.occupiedBox == this)
            currentCell.occupiedBox = null;

        currentCell = targetCell;
        targetCell.occupiedBox = this;

        IsMoving = true;

        moveCoroutine = StartCoroutine(
            MoveToPosition(targetCell.Position + cellOffset));

        return true;
    }

    private Vector2 NormalizeDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return new Vector2(Mathf.Sign(direction.x), 0f);
        }

        if (Mathf.Abs(direction.y) > 0f)
        {
            return new Vector2(0f, Mathf.Sign(direction.y));
        }

        return Vector2.zero;
    }

    private void SetPosition(Vector2 position)
    {
        if (boxBody != null)
            boxBody.position = position;

        transform.position = new Vector3(
            position.x, position.y, transform.position.z);
    }

    private IEnumerator MoveToPosition(Vector2 targetPosition)
    {
        Vector2 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.fixedDeltaTime;

            if (boxBody != null)
            {
                boxBody.MovePosition(
                    Vector2.Lerp(
                        startPosition,
                        targetPosition,
                        Mathf.Clamp01(elapsed / moveDuration)));
            }

            yield return new WaitForFixedUpdate();
        }

        SetPosition(targetPosition);

        IsMoving = false;
        moveCoroutine = null;
    }
}
